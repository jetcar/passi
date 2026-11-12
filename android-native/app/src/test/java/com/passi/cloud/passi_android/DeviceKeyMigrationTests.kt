package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.google.gson.Gson
import com.passi.cloud.passi_android.data.auth.Pkcs12AccountSigner
import com.passi.cloud.passi_android.data.certificate.BackendCertificateRotationService
import com.passi.cloud.passi_android.data.crypto.BouncyCastleCertificateGenerator
import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.data.repository.InMemoryProvidersRepository
import com.passi.cloud.passi_android.domain.auth.AccountSigner
import com.passi.cloud.passi_android.domain.enrollment.CertificateGenerator
import com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate
import com.passi.cloud.passi_android.domain.enrollment.checkpoint
import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.ApiPaths
import com.passi.cloud.passi_android.domain.model.NotificationSession
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flowOf
import kotlinx.coroutines.runBlocking
import okhttp3.mockwebserver.Dispatcher
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import okhttp3.mockwebserver.RecordedRequest
import okhttp3.mockwebserver.SocketPolicy
import org.junit.After
import org.junit.Before
import org.junit.Test
import java.time.Instant
import java.util.UUID
import java.util.concurrent.TimeUnit
import java.util.concurrent.atomic.AtomicBoolean

/** Exercises the real migration coordinator and HTTP boundary; the fake store models durable commits. */
class DeviceKeyMigrationTests {
    private lateinit var server: MockWebServer
    private lateinit var provider: Provider
    private lateinit var old: Account
    private lateinit var repository: CommitStore
    private lateinit var generator: PublicOnlyGenerator
    private val signer = object : AccountSigner {
        override suspend fun sign(account: Account, pin: String?, message: String): Result<String> =
            if (pin == "1234") Result.success("signature-of:$message")
            else Result.failure(IllegalArgumentException("Invalid PIN"))
    }

    @Before fun setUp() {
        server = MockWebServer().apply { start() }
        provider = Provider(UUID.randomUUID(), "test", server.url("/").toString().trimEnd('/'), ApiPaths.defaultPaths(), true)
        old = Account(UUID.randomUUID(), provider.id, "user@example.com", "device-1", true,
            thumbprint = "old-thumbprint", validFrom = Instant.parse("2026-01-01T00:00:00Z"),
            validTo = Instant.parse("2027-01-01T00:00:00Z"), salt = "old-salt",
            privateCertBinary = "old-pkcs12", publicCertBinary = "old-public", pinLength = 4, hasFingerprint = true)
        repository = CommitStore(old)
        generator = PublicOnlyGenerator()
    }

    @After fun tearDown() { server.shutdown() }

    private suspend fun service(accountSigner: AccountSigner = signer): BackendCertificateRotationService {
        val providers = InMemoryProvidersRepository().apply { saveProvider(provider) }
        return BackendCertificateRotationService(PassiApiClient(logger = {}), providers, generator, accountSigner, repository)
    }

    @Test fun checkpointIsDurableBeforePostAndSuccessMarksLegacyCleanup() = runBlocking {
        val observedCheckpoint = AtomicBoolean(false)
        server.dispatcher = object : Dispatcher() {
            override fun dispatch(request: RecordedRequest): MockResponse {
                val pending = repository.current.pendingKey
                val body = Gson().fromJson(request.body.readUtf8(), Map::class.java)
                observedCheckpoint.set(pending?.publicCertBinary == body["PublicCert"] &&
                    body["ParentCertHashSignature"] == "signature-of:Passi:RetireParent:v2\n${old.thumbprint}\n${old.deviceId}\n${pending?.publicCertBinary}" &&
                    repository.current.privateCertBinary == old.privateCertBinary &&
                    repository.commits.size == 2 && pending?.submitted == true &&
                    repository.commits.first().pendingKey?.submitted == false)
                return acceptedResponse()
            }
        }
        val updated = service().rotateCertificate(old, "1234", null, false).getOrThrow()
        assertThat(observedCheckpoint.get()).isTrue()
        assertThat(updated.keyAlias).isEqualTo("passi.account.test-1")
        assertThat(updated.keyProtection).isEqualTo(DeviceKeyProtection.DEVICE_CREDENTIAL)
        assertThat(updated.privateCertBinary).isNull()
        assertThat(updated.salt).isNull()
        assertThat(updated.pinLength).isEqualTo(0)
        assertThat(updated.hasFingerprint).isFalse()
        assertThat(updated.pendingKey).isNull()
        assertThat(updated.legacyCleanupPending).isTrue()
        assertThat(repository.commits.first().privateCertBinary).isEqualTo(old.privateCertBinary)
        assertThat(repository.commits.last()).isEqualTo(updated)
    }

    @Test fun biometricChoiceIsBoundToTheReplacementKey() = runBlocking {
        server.enqueue(acceptedResponse())
        val updated = service().rotateCertificate(old, "1234", null, true).getOrThrow()
        assertThat(updated.keyProtection).isEqualTo(DeviceKeyProtection.BIOMETRIC)
        assertThat(updated.pendingKey).isNull()
    }

    @Test fun httpFailureKeepsOldMaterialAndRetryUsesExactCheckpoint() = runBlocking {
        server.enqueue(MockResponse().setResponseCode(503))
        server.enqueue(acceptedResponse())
        assertThat(service().rotateCertificate(old, "1234", null, false).isFailure).isTrue()
        val interrupted = repository.current
        assertThat(interrupted.privateCertBinary).isEqualTo(old.privateCertBinary)
        assertThat(interrupted.salt).isEqualTo(old.salt)
        assertThat(interrupted.hasFingerprint).isTrue()
        assertThat(interrupted.legacyCleanupPending).isFalse()
        assertThat(interrupted.pendingKey).isNotNull()
        // Recreate the coordinator, as after an application restart.
        val updated = service().rotateCertificate(interrupted, "1234", null, false).getOrThrow()
        assertThat(generator.calls).isEqualTo(1)
        assertThat(updated.publicCertBinary).isEqualTo(interrupted.pendingKey!!.publicCertBinary)
        assertThat(takeBody()).isEqualTo(takeBody())
    }

    @Test fun lostServerResponseRetainsOldMaterialAndRetriesIdenticalRequest() = runBlocking {
        val acknowledge = AtomicBoolean(false)
        server.dispatcher = object : Dispatcher() {
            override fun dispatch(request: RecordedRequest): MockResponse =
                if (acknowledge.get()) acceptedResponse()
                else MockResponse().setSocketPolicy(SocketPolicy.DISCONNECT_AFTER_REQUEST)
        }
        assertThat(service().rotateCertificate(old, "1234", null, false).isFailure).isTrue()
        val attemptsWithoutResponse = server.requestCount
        assertThat(attemptsWithoutResponse).isAtLeast(1)
        val firstRequest = takeBody()
        repeat(attemptsWithoutResponse - 1) { assertThat(takeBody()).isEqualTo(firstRequest) }
        assertThat(repository.current.privateCertBinary).isEqualTo(old.privateCertBinary)
        assertThat(repository.current.pendingKey).isNotNull()
        acknowledge.set(true)
        assertThat(service().rotateCertificate(repository.current, "1234", null, false).isSuccess).isTrue()
        assertThat(takeBody()).isEqualTo(firstRequest)
        assertThat(generator.calls).isEqualTo(1)
    }

    @Test fun failedLocalCommitAfterServerAcceptanceRetainsCheckpointForExactRetry() = runBlocking {
        server.enqueue(acceptedResponse())
        server.enqueue(acceptedResponse())
        repository.failAtCommit = 3
        assertThat(service().rotateCertificate(old, "1234", null, false).isFailure).isTrue()
        val interrupted = repository.current
        assertThat(interrupted.thumbprint).isEqualTo(old.thumbprint)
        assertThat(interrupted.privateCertBinary).isEqualTo(old.privateCertBinary)
        assertThat(interrupted.pendingKey).isNotNull()
        assertThat(interrupted.legacyCleanupPending).isFalse()
        val updated = service().rotateCertificate(interrupted, "1234", null, false).getOrThrow()
        assertThat(updated.thumbprint).isEqualTo(interrupted.pendingKey!!.thumbprint)
        assertThat(updated.legacyCleanupPending).isTrue()
        assertThat(generator.calls).isEqualTo(1)
        assertThat(takeBody()).isEqualTo(takeBody())
    }

    @Test fun failedCheckpointCommitNeverContactsServerOrReplacesOldAccount() = runBlocking {
        repository.failAtCommit = 1
        assertThat(service().rotateCertificate(old, "1234", null, false).isFailure).isTrue()
        assertThat(repository.current).isEqualTo(old)
        assertThat(server.requestCount).isEqualTo(0)
    }

    @Test fun cancellationAfterCheckpointPropagatesWithoutReplacingOldMaterial() = runBlocking {
        val cancellingSigner = object : AccountSigner {
            override suspend fun sign(account: Account, pin: String?, message: String): Result<String> {
                if (account.pendingKey != null) throw CancellationException("User cancelled")
                return signer.sign(account, pin, message)
            }
        }
        var cancelled = false
        try { service(cancellingSigner).rotateCertificate(old, "1234", null, false) }
        catch (_: CancellationException) { cancelled = true }
        assertThat(cancelled).isTrue()
        assertThat(repository.current.privateCertBinary).isEqualTo(old.privateCertBinary)
        assertThat(repository.current.pendingKey).isNotNull()
        assertThat(repository.current.pendingKey!!.submitted).isFalse()
        assertThat(repository.current.legacyCleanupPending).isFalse()
        assertThat(server.requestCount).isEqualTo(0)
        server.enqueue(acceptedResponse())
        assertThat(service().rotateCertificate(repository.current, "1234", null, false).isSuccess).isTrue()
        assertThat(generator.calls).isEqualTo(1)
    }

    @Test fun staleScreenAfterSuccessDoesNotGenerateOrRotateAgain() = runBlocking {
        server.enqueue(acceptedResponse())
        val migrated = service().rotateCertificate(old, "1234", null, false).getOrThrow()
        assertThat(service().rotateCertificate(old, "1234", null, false).getOrThrow()).isEqualTo(migrated)
        assertThat(generator.calls).isEqualTo(1)
        assertThat(server.requestCount).isEqualTo(1)
    }

    @Test fun wrongOrMissingLegacyPinCannotCreatePendingKey() = runBlocking {
        val actualLegacy = BouncyCastleCertificateGenerator().generate(old.email, "1234")
        old = old.copy(privateCertBinary = actualLegacy.privateCertBinary, salt = actualLegacy.salt,
            publicCertBinary = actualLegacy.publicCertBinary, thumbprint = actualLegacy.thumbprint)
        repository = CommitStore(old)
        for (pin in listOf(null, "9999")) {
            assertThat(service(Pkcs12AccountSigner()).rotateCertificate(old, pin, null, false).isFailure).isTrue()
            assertThat(repository.current).isEqualTo(old)
        }
        assertThat(generator.calls).isEqualTo(0)
        assertThat(repository.commits).isEmpty()
        assertThat(server.requestCount).isEqualTo(0)
    }

    @Test fun applicationPinCannotBeSelectedForReplacementKey() = runBlocking {
        assertThat(service().rotateCertificate(old, "1234", "5678", false).isFailure).isTrue()
        assertThat(generator.calls).isEqualTo(0)
        assertThat(repository.current).isEqualTo(old)
        assertThat(server.requestCount).isEqualTo(0)
    }

    @Test fun successStatusWithoutExactCertificateAcknowledgmentKeepsLegacyMaterial() = runBlocking {
        for (response in listOf("", "not json", "{}", "null", "{\"PublicCert\":\"different-certificate\"}")) {
            server.enqueue(MockResponse().setResponseCode(200).setBody(response))
            assertThat(service().rotateCertificate(repository.current, "1234", null, false).isFailure).isTrue()
            assertThat(repository.current.privateCertBinary).isEqualTo(old.privateCertBinary)
            assertThat(repository.current.thumbprint).isEqualTo(old.thumbprint)
            assertThat(repository.current.pendingKey).isNotNull()
            assertThat(repository.current.legacyCleanupPending).isFalse()
        }
        server.enqueue(acceptedResponse())
        assertThat(service().rotateCertificate(repository.current, "1234", null, false).isSuccess).isTrue()
        assertThat(generator.calls).isEqualTo(1)
    }

    @Test fun camelCaseCertificateAcknowledgmentIsAccepted() = runBlocking {
        server.enqueue(MockResponse().setResponseCode(200).setBody("{\"publicCert\":\"public-certificate-1\"}"))
        assertThat(service().rotateCertificate(old, "1234", null, false).isSuccess).isTrue()
        assertThat(repository.current.privateCertBinary).isNull()
    }

    @Test fun redirectCannotSendRotationSignatureToAnotherEndpoint() = runBlocking {
        server.enqueue(MockResponse().setResponseCode(302).setHeader("Location", server.url("/redirect-target")))
        server.enqueue(acceptedResponse())
        assertThat(service().rotateCertificate(old, "1234", null, false).isFailure).isTrue()
        assertThat(server.requestCount).isEqualTo(1)
        assertThat(repository.current.privateCertBinary).isEqualTo(old.privateCertBinary)
        assertThat(repository.current.pendingKey).isNotNull()
        assertThat(repository.current.legacyCleanupPending).isFalse()
    }

    @Test fun authenticationPayloadsAndQueriesAreExcludedFromLogs() = runBlocking {
        val log = mutableListOf<String>()
        val secrets = listOf("private-key-secret", "pkcs12-secret", "pin-secret", "signature-secret", "challenge-secret", "query-secret")
        server.enqueue(MockResponse().setResponseCode(200).setBody("{\"RandomString\":\"challenge-secret\"}"))
        val client = PassiApiClient(logger = log::add)
        assertThat(client.postJson(provider.baseUrl, "/auth?token=query-secret", mapOf(
            "PrivateKey" to secrets[0], "PrivateCertBinary" to secrets[1], "Pin" to secrets[2],
            "SignedHash" to secrets[3], "RandomString" to secrets[4])).isSuccessful).isTrue()
        assertThat(log).isNotEmpty()
        secrets.forEach { assertThat(log.joinToString("\n")).doesNotContain(it) }
    }

    @Test fun pendingKeyInvalidationAfterNetworkFailureStopsRetryBeforeServerRetirement() = runBlocking {
        server.enqueue(MockResponse().setResponseCode(503))
        assertThat(service().rotateCertificate(old, "1234", null, false).isFailure).isTrue()
        val checkpoint = repository.current
        generator.valid = false
        val retry = service().rotateCertificate(checkpoint, "1234", null, false)
        assertThat(retry.isFailure).isTrue()
        assertThat(retry.exceptionOrNull()).isInstanceOf(com.passi.cloud.passi_android.domain.enrollment.DeviceKeyRecoveryRequiredException::class.java)
        assertThat(checkpoint.pendingKey!!.submitted).isTrue()
        assertThat(repository.current).isEqualTo(checkpoint)
        assertThat(repository.current.privateCertBinary).isEqualTo(old.privateCertBinary)
        assertThat(repository.current.legacyCleanupPending).isFalse()
        assertThat(generator.calls).isEqualTo(1)
        assertThat(generator.validations).isEqualTo(2)
        assertThat(server.requestCount).isEqualTo(1)
    }

    @Test fun deadUnsubmittedKeyIsReplacedBeforeSendingAnyCertificate() = runBlocking {
        val stale = generator.generate(
            old.email,
            com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection.DEVICE_CREDENTIAL,
        )
        old = old.copy(pendingKey = stale.checkpoint())
        repository = CommitStore(old)
        generator.invalidAliases.add(stale.keyAlias!!)
        server.enqueue(MockResponse().setResponseCode(200).setBody("{\"PublicCert\":\"public-certificate-2\"}"))
        val result = service().rotateCertificate(old, "1234", null, false).getOrThrow()
        assertThat(result.keyAlias).isEqualTo("passi.account.test-2")
        assertThat(generator.calls).isEqualTo(2)
        assertThat(server.requestCount).isEqualTo(1)
        assertThat(takeBody()).contains("public-certificate-2")
    }

    @Test fun deadHistoricalUnsubmittedKeyKeepsCombinedProtectionWhenReplaced() = runBlocking {
        val stale = generator.generate(
            old.email,
            DeviceKeyProtection.BIOMETRIC_OR_DEVICE_CREDENTIAL,
        ).copy(keyProtection = null)
        old = old.copy(pendingKey = stale.checkpoint())
        repository = CommitStore(old)
        generator.invalidAliases.add(stale.keyAlias!!)
        server.enqueue(MockResponse().setResponseCode(200).setBody("{\"PublicCert\":\"public-certificate-2\"}"))

        val result = service().rotateCertificate(old, "1234", null, false).getOrThrow()

        assertThat(result.keyProtection).isEqualTo(DeviceKeyProtection.BIOMETRIC_OR_DEVICE_CREDENTIAL)
        assertThat(generator.calls).isEqualTo(2)
        assertThat(server.requestCount).isEqualTo(1)
    }

    @Test fun submissionCheckpointFailureStopsBeforeHttpAndRetainsUnsubmittedKey() = runBlocking {
        repository.failAtCommit = 2
        assertThat(service().rotateCertificate(old, "1234", null, false).isFailure).isTrue()
        assertThat(repository.current.pendingKey!!.submitted).isFalse()
        assertThat(repository.current.privateCertBinary).isEqualTo(old.privateCertBinary)
        assertThat(server.requestCount).isEqualTo(0)
    }

    @Test fun historicalUnknownSubmissionCannotBeSilentlyReplaced() = runBlocking {
        val generated = generator.generate(
            old.email,
            com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection.DEVICE_CREDENTIAL,
        )
        old = old.copy(pendingKey = generated.checkpoint().copy(submitted = null))
        repository = CommitStore(old)
        generator.valid = false
        val result = service().rotateCertificate(old, "1234", null, false)
        assertThat(result.exceptionOrNull()).isInstanceOf(com.passi.cloud.passi_android.domain.enrollment.DeviceKeyRecoveryRequiredException::class.java)
        assertThat(repository.current).isEqualTo(old)
        assertThat(generator.calls).isEqualTo(1)
        assertThat(server.requestCount).isEqualTo(0)
    }

    @Test fun subsequentRotationsPreserveAllUnfinishedCleanupAliases() = runBlocking {
        old = old.copy(keyAlias = "passi.account.current", keySecurityLevel = "TEE",
            privateCertBinary = null, salt = null, legacyCleanupPending = true,
            cleanupKeyAlias = "passi.account.previous", retiredKeyAliases = listOf("passi.account.older"))
        repository = CommitStore(old)
        server.enqueue(acceptedResponse())
        val first = service().rotateCertificate(old, "1234", null, false).getOrThrow()
        assertThat(first.cleanupKeyAlias).isEqualTo("passi.account.current")
        assertThat(first.retiredKeyAliases).containsExactly("passi.account.previous", "passi.account.older")
        server.enqueue(MockResponse().setResponseCode(200).setBody("{\"PublicCert\":\"public-certificate-2\"}"))
        val second = service().rotateCertificate(first, "1234", null, false).getOrThrow()
        assertThat(second.cleanupKeyAlias).isEqualTo(first.keyAlias)
        assertThat(second.retiredKeyAliases).containsExactly("passi.account.current", "passi.account.previous", "passi.account.older")
        assertThat(second.legacyCleanupPending).isTrue()
        assertThat(second.keyAlias).isEqualTo("passi.account.test-2")
        assertThat(second.retiredKeyAliases).doesNotContain(second.keyAlias)
    }

    private fun acceptedResponse() = MockResponse().setResponseCode(200)
        .setBody("{\"PublicCert\":\"public-certificate-1\"}")

    private fun takeBody(): String = requireNotNull(server.takeRequest(2, TimeUnit.SECONDS)).body.readUtf8()

    private class PublicOnlyGenerator : CertificateGenerator {
        var calls = 0
        var validations = 0
        var valid = true
        val invalidAliases = mutableSetOf<String>()
        override fun generate(
            email: String,
            protection: com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection,
        ): GeneratedCertificate {
            calls++
            return GeneratedCertificate(null, null, "public-certificate-$calls", "new-thumbprint-$calls",
                Instant.parse("2026-01-01T00:00:00Z"), Instant.parse("2027-01-01T00:00:00Z"),
                "passi.account.test-$calls", "TEE", protection)
        }
        override fun validate(certificate: GeneratedCertificate) {
            validations++
            check(valid && certificate.keyAlias !in invalidAliases) { "Pending signing key is invalidated" }
            check(certificate.privateCertBinary == null && certificate.salt == null && certificate.keyAlias != null)
        }
    }

    private class CommitStore(initial: Account) : AccountsRepository {
        @Volatile var current = initial
        val commits = mutableListOf<Account>()
        var failAtCommit = -1
        private var attempts = 0
        override suspend fun updateAccount(account: Account) {
            attempts++
            if (attempts == failAtCommit) throw IllegalStateException("Disk commit failed")
            commits.add(account)
            current = account
        }
        override suspend fun getAccount(accountId: UUID): Account? = current.takeIf { it.id == accountId }
        override suspend fun getAccounts(): List<Account> = listOf(current)
        override fun observeAccounts(): Flow<List<Account>> = flowOf(listOf(current))
        override suspend fun savePendingAccount(account: Account) = updateAccount(account)
        override suspend fun deleteAccount(accountId: UUID) { error("Unexpected account deletion") }
        override suspend fun syncAccounts(): Result<Unit> = Result.success(Unit)
        override suspend fun pollPendingSession(): Result<NotificationSession?> = Result.success(null)
    }
}
