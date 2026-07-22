package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.google.gson.FieldNamingPolicy
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.passi.cloud.passi_android.data.account.BackendAccountManagementService
import com.passi.cloud.passi_android.data.auth.BackendAuthSessionService
import com.passi.cloud.passi_android.data.auth.Pkcs12AccountSigner
import com.passi.cloud.passi_android.data.certificate.BackendCertificateRotationService
import com.passi.cloud.passi_android.data.crypto.BouncyCastleCertificateGenerator
import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.data.repository.BackendEnrollmentService
import com.passi.cloud.passi_android.data.repository.InMemoryAccountsRepository
import com.passi.cloud.passi_android.data.repository.InMemoryProvidersRepository
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.ApiPaths
import com.passi.cloud.passi_android.domain.model.ConfirmationColor
import com.passi.cloud.passi_android.domain.model.NotificationSession
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.service.PendingEnrollment
import kotlinx.coroutines.ExperimentalCoroutinesApi
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.After
import org.junit.Before
import org.junit.Test
import java.time.Instant
import java.time.temporal.ChronoUnit
import java.util.UUID

@OptIn(ExperimentalCoroutinesApi::class)
class BackendServicesIntegrationTests : CoroutineViewModelTest() {

    private lateinit var server: MockWebServer
    private lateinit var apiClient: PassiApiClient
    private lateinit var provider: Provider

    @Before
    fun setUpServer() {
        server = MockWebServer()
        server.start()
        apiClient = PassiApiClient(
            GsonBuilder()
                .setFieldNamingPolicy(FieldNamingPolicy.UPPER_CAMEL_CASE)
                .disableHtmlEscaping()
                .create()
        )
        provider = Provider(
            id = UUID.fromString("7a22cc55-4d18-4e02-bfa5-f6ce4913fbfb"),
            name = "test",
            baseUrl = server.url("/").toString().trimEnd('/'),
            isDefault = true,
            apiPaths = ApiPaths.defaultPaths(),
        )
    }

    @After
    fun tearDownServer() {
        server.shutdown()
    }

    // ── Enrollment ─────────────────────────────────────────────────────────────

    @Test
    fun enrollmentBeginSignupPostsToSignupEndpoint() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(200))

        val result = BackendEnrollmentService(apiClient)
            .beginSignup("user@example.com", provider, "device-1")

        val request = server.takeRequest()
        val body = request.body.readUtf8()

        assertThat(result.isSuccess).isTrue()
        assertThat(request.path).isEqualTo("/api/SignUp/signup")
        assertThat(body).contains("user@example.com")
        assertThat(body).contains("device-1")
    }

    @Test
    fun enrollmentBeginSignupReturns400ErrorMessage() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(400).setBody("""{"errors":"Email already registered"}"""))

        val result = BackendEnrollmentService(apiClient)
            .beginSignup("user@example.com", provider, "device-1")

        assertThat(result.isFailure).isTrue()
        assertThat(result.exceptionOrNull()?.message).isEqualTo("Email already registered")
    }

    @Test
    fun enrollmentConfirmCodePostsToCheckEndpoint() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(200))

        val result = BackendEnrollmentService(apiClient)
            .confirmSignupCode("account-id", "user@example.com", "123456", provider)

        val request = server.takeRequest()
        val body = request.body.readUtf8()

        assertThat(result.isSuccess).isTrue()
        assertThat(request.path).isEqualTo("/api/SignUp/check")
        assertThat(body).contains("user@example.com")
        assertThat(body).contains("123456")
    }

    @Test
    fun enrollmentConfirmCodeReturnsFailureWhenCodeRejected() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(400).setBody("""{"errors":"Invalid code"}"""))

        val result = BackendEnrollmentService(apiClient)
            .confirmSignupCode("account-id", "user@example.com", "000000", provider)

        assertThat(result.isFailure).isTrue()
        assertThat(result.exceptionOrNull()?.message).isEqualTo("Invalid code")
    }

    @Test
    fun enrollmentFinalizePostsConfirmationWithCertificate() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(200))

        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        val pending = PendingEnrollment(
            accountId = UUID.randomUUID().toString(),
            email = "user@example.com",
            providerId = provider.id.toString(),
            confirmationCode = "654321",
        )

        val result = BackendEnrollmentService(apiClient)
            .finalizeSignup(pending, provider, "device-1", cert)

        val request = server.takeRequest()
        val body = request.body.readUtf8()

        assertThat(result.isSuccess).isTrue()
        assertThat(request.path).isEqualTo("/api/SignUp/confirm")
        assertThat(body).contains("user@example.com")
        assertThat(body).contains("654321")
        assertThat(body).contains("device-1")
        assertThat(body).contains(cert.publicCertBinary)
    }

    @Test
    fun enrollmentFinalizeAdoptsCanonicalAccountGuidFromServer() = runViewModelTest {
        val canonicalGuid = UUID.randomUUID()
        server.enqueue(MockResponse().setResponseCode(200).setBody("""{"AccountGuid":"$canonicalGuid"}"""))

        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        val pending = PendingEnrollment(
            accountId = UUID.randomUUID().toString(),
            email = "user@example.com",
            providerId = provider.id.toString(),
            confirmationCode = "654321",
        )

        val result = BackendEnrollmentService(apiClient)
            .finalizeSignup(pending, provider, "device-1", cert)

        assertThat(result.isSuccess).isTrue()
        assertThat(result.getOrNull()).isEqualTo(canonicalGuid)
    }

    @Test
    fun enrollmentFinalizeFallsBackToLocalGuidWhenResponseHasNoBody() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(200))

        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        val localId = UUID.randomUUID()
        val pending = PendingEnrollment(
            accountId = localId.toString(),
            email = "user@example.com",
            providerId = provider.id.toString(),
            confirmationCode = "654321",
        )

        val result = BackendEnrollmentService(apiClient)
            .finalizeSignup(pending, provider, "device-1", cert)

        assertThat(result.isSuccess).isTrue()
        assertThat(result.getOrNull()).isEqualTo(localId)
    }

    @Test
    fun enrollmentFinalizeFailsWhenConfirmationCodeMissing() = runViewModelTest {
        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        val pending = PendingEnrollment(
            accountId = UUID.randomUUID().toString(),
            email = "user@example.com",
            providerId = provider.id.toString(),
            confirmationCode = null,
        )

        val result = BackendEnrollmentService(apiClient)
            .finalizeSignup(pending, provider, "device-1", cert)

        assertThat(result.isFailure).isTrue()
        assertThat(result.exceptionOrNull()?.message).contains("confirmation code")
    }

    // ── Auth session ────────────────────────────────────────────────────────────

    @Test
    fun authSessionSyncReturnsNullWhenNoSessionPending() = runViewModelTest {
        val accountsRepository = confirmedAccountsRepository()
        val providersRepository = providersRepositoryWithTestServer()
        server.enqueue(MockResponse().setResponseCode(200).setBody("[]"))
        server.enqueue(MockResponse().setResponseCode(200).setBody("null"))

        val result = BackendAuthSessionService(
            apiClient = apiClient,
            accountsRepository = accountsRepository,
            providersRepository = providersRepository,
            signer = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
            deviceIdProvider = { "device-1" },
        ).syncAndPollPendingSession()

        assertThat(result.isSuccess).isTrue()
        assertThat(result.getOrNull()).isNull()
    }

    @Test
    fun authSessionSyncReturnsSessionWhenServerHasPendingSession() = runViewModelTest {
        val accountId = UUID.fromString("4eeb9825-3028-4389-ac41-b6690b0edb9e")
        val accountsRepository = confirmedAccountsRepository()
        val providersRepository = providersRepositoryWithTestServer()
        val sessionJson = buildSessionJson(accountId)
        server.enqueue(MockResponse().setResponseCode(200).setBody("[]"))
        server.enqueue(MockResponse().setResponseCode(200).setBody(sessionJson))

        val result = BackendAuthSessionService(
            apiClient = apiClient,
            accountsRepository = accountsRepository,
            providersRepository = providersRepository,
            signer = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
            deviceIdProvider = { "device-1" },
        ).syncAndPollPendingSession()

        assertThat(result.isSuccess).isTrue()
        val session = result.getOrNull()
        assertThat(session).isNotNull()
        assertThat(session?.sessionId.toString()).isEqualTo("2d9e6360-28ba-4aa8-a9c5-ec7cb5632489")
        assertThat(session?.confirmationColor).isEqualTo(ConfirmationColor.BLUE)
        assertThat(session?.returnHost).isEqualTo("mail.example.com")
    }

    @Test
    fun authSessionSyncParsesNumericConfirmationColor() = runViewModelTest {
        val accountId = UUID.fromString("4eeb9825-3028-4389-ac41-b6690b0edb9e")
        val accountsRepository = confirmedAccountsRepository()
        val providersRepository = providersRepositoryWithTestServer()
        val sessionJson = buildSessionJsonWithNumericColor(accountId, 2)
        server.enqueue(MockResponse().setResponseCode(200).setBody("[]"))
        server.enqueue(MockResponse().setResponseCode(200).setBody(sessionJson))

        val result = BackendAuthSessionService(
            apiClient = apiClient,
            accountsRepository = accountsRepository,
            providersRepository = providersRepository,
            signer = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
            deviceIdProvider = { "device-1" },
        ).syncAndPollPendingSession()

        assertThat(result.isSuccess).isTrue()
        val session = result.getOrNull()
        assertThat(session).isNotNull()
        assertThat(session?.confirmationColor).isEqualTo(ConfirmationColor.RED)
    }

    @Test
    fun authSessionSyncSendsDeviceIdAndAccountGuidsToSyncEndpoint() = runViewModelTest {
        val accountsRepository = confirmedAccountsRepository()
        val providersRepository = providersRepositoryWithTestServer()
        server.enqueue(MockResponse().setResponseCode(200).setBody("[]"))
        server.enqueue(MockResponse().setResponseCode(200).setBody("null"))

        BackendAuthSessionService(
            apiClient = apiClient,
            accountsRepository = accountsRepository,
            providersRepository = providersRepository,
            signer = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
            deviceIdProvider = { "device-1" },
        ).syncAndPollPendingSession()

        val syncRequest = server.takeRequest()
        val body = syncRequest.body.readUtf8()

        assertThat(syncRequest.path).isEqualTo("/api/Auth/SyncAccounts")
        assertThat(body).contains("device-1")
        assertThat(body).contains("4eeb9825-3028-4389-ac41-b6690b0edb9e")
    }

    @Test
    fun authSessionSyncDoesNotMarkMissingConfirmedAccountInactive() = runViewModelTest {
        val accountId = UUID.fromString("4eeb9825-3028-4389-ac41-b6690b0edb9e")
        val accountsRepository = confirmedAccountsRepository()
        val providersRepository = providersRepositoryWithTestServer()
        server.enqueue(MockResponse().setResponseCode(200).setBody("[]"))
        server.enqueue(MockResponse().setResponseCode(200).setBody("null"))

        val before = accountsRepository.getAccount(accountId)
        assertThat(before?.inactive).isFalse()

        val result = BackendAuthSessionService(
            apiClient = apiClient,
            accountsRepository = accountsRepository,
            providersRepository = providersRepository,
            signer = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
            deviceIdProvider = { "device-1" },
        ).syncAndPollPendingSession()

        assertThat(result.isSuccess).isTrue()
        val after = accountsRepository.getAccount(accountId)
        assertThat(after?.inactive).isFalse()
    }

    @Test
    fun authSessionAuthorizePostsSignedHashToAuthorizeEndpoint() = runViewModelTest {
        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        val accountId = UUID.fromString("4eeb9825-3028-4389-ac41-b6690b0edb9e")
        val accountsRepository = confirmedAccountsRepository(cert = cert)
        val providersRepository = providersRepositoryWithTestServer()
        server.enqueue(MockResponse().setResponseCode(200))

        val session = pendingSession(accountId)
        val result = BackendAuthSessionService(
            apiClient = apiClient,
            accountsRepository = accountsRepository,
            providersRepository = providersRepository,
            signer = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
            deviceIdProvider = { "device-1" },
        ).authorize(session, pin = "1234")

        val request = server.takeRequest()
        val body = request.body.readUtf8()

        assertThat(result.isSuccess).isTrue()
        assertThat(request.path).isEqualTo("/api/Auth/Authorize")
        assertThat(body).contains(cert.thumbprint)
        assertThat(body).contains(session.sessionId.toString())
    }

    @Test
    fun authSessionAuthorizeReturnsFailureWhenPinIsWrong() = runViewModelTest {
        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        val accountId = UUID.fromString("4eeb9825-3028-4389-ac41-b6690b0edb9e")
        val accountsRepository = confirmedAccountsRepository(cert = cert)
        val providersRepository = providersRepositoryWithTestServer()

        val session = pendingSession(accountId)
        val result = BackendAuthSessionService(
            apiClient = apiClient,
            accountsRepository = accountsRepository,
            providersRepository = providersRepository,
            signer = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
            deviceIdProvider = { "device-1" },
        ).authorize(session, pin = "wrongpin")

        assertThat(result.isFailure).isTrue()
        assertThat(result.exceptionOrNull()?.message).isEqualTo("Invalid Pin")
    }

    @Test
    fun authSessionCancelSendsDeleteForSession() = runViewModelTest {
        val accountId = UUID.fromString("4eeb9825-3028-4389-ac41-b6690b0edb9e")
        val providersRepository = providersRepositoryWithTestServer()
        server.enqueue(MockResponse().setResponseCode(200))

        val session = pendingSession(accountId)
        val result = BackendAuthSessionService(
            apiClient = apiClient,
            accountsRepository = confirmedAccountsRepository(),
            providersRepository = providersRepository,
            signer = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
            deviceIdProvider = { "device-1" },
        ).cancel(session)

        val request = server.takeRequest()

        assertThat(result.isSuccess).isTrue()
        assertThat(request.method).isEqualTo("DELETE")
        assertThat(request.path).contains("/api/Auth/Cancel")
        assertThat(request.path).contains(session.sessionId.toString())
    }

    // ── Account management ──────────────────────────────────────────────────────

    @Test
    fun accountManagementListsDevicesFromServer() = runViewModelTest {
        val devicesJson = """[
            {"DeviceId":"device-1","Platform":"Android","CreationTime":"2026-01-01T00:00:00Z","IsCurrent":true},
            {"DeviceId":"device-2","Platform":"iOS","CreationTime":"2026-02-01T00:00:00Z","IsCurrent":false}
        ]"""
        server.enqueue(MockResponse().setResponseCode(200).setBody(devicesJson))

        val providersRepository = providersRepositoryWithTestServer()
        val account = confirmedAccountWithCert()

        val result = BackendAccountManagementService(apiClient, providersRepository)
            .getDevices(account, "device-1")

        val request = server.takeRequest()
        val body = request.body.readUtf8()

        assertThat(result.isSuccess).isTrue()
        assertThat(request.path).isEqualTo("/api/Auth/Devices")
        assertThat(body).contains(account.id.toString())
        assertThat(body).contains(account.thumbprint!!)
        assertThat(result.getOrNull()).hasSize(2)
        assertThat(result.getOrNull()?.first { it.deviceId == "device-1" }?.isCurrent).isTrue()
    }

    @Test
    fun accountManagementDeleteDevicePostsCorrectDeviceIds() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(200))

        val providersRepository = providersRepositoryWithTestServer()
        val account = confirmedAccountWithCert()

        val result = BackendAccountManagementService(apiClient, providersRepository)
            .deleteDevice(account, deviceId = "device-2", currentDeviceId = "device-1")

        val request = server.takeRequest()
        val body = request.body.readUtf8()

        assertThat(result.isSuccess).isTrue()
        assertThat(request.path).isEqualTo("/api/Auth/DeleteDevice")
        assertThat(body).contains("device-2")
        assertThat(body).contains("device-1")
        assertThat(body).contains(account.thumbprint!!)
    }

    @Test
    fun accountManagementDeleteDeviceReturnsFailureOn400() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(400).setBody("""{"errors":"Device not found"}"""))

        val providersRepository = providersRepositoryWithTestServer()
        val account = confirmedAccountWithCert()

        val result = BackendAccountManagementService(apiClient, providersRepository)
            .deleteDevice(account, deviceId = "nonexistent", currentDeviceId = "device-1")

        assertThat(result.isFailure).isTrue()
        assertThat(result.exceptionOrNull()?.message).isEqualTo("Device not found")
    }

    @Test
    fun accountManagementDeleteAccountSendsDeleteRequestWithThumbprint() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(200))

        val providersRepository = providersRepositoryWithTestServer()
        val account = confirmedAccountWithCert()

        val result = BackendAccountManagementService(apiClient, providersRepository)
            .deleteAccount(account)

        val request = server.takeRequest()

        assertThat(result.isSuccess).isTrue()
        assertThat(request.method).isEqualTo("DELETE")
        assertThat(request.path).contains("/api/Auth/Delete")
        assertThat(request.path).contains(account.id.toString())
        assertThat(request.path).contains(account.thumbprint!!)
    }

    @Test
    fun deleteAccount_whenThumbprintNullButPublicCertBinaryPresent_computesThumbprintFromCert() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(200))

        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        val providersRepository = providersRepositoryWithTestServer()
        val account = confirmedAccountWithCert().copy(
            thumbprint = null,
            publicCertBinary = cert.publicCertBinary,
        )

        val result = BackendAccountManagementService(apiClient, providersRepository)
            .deleteAccount(account)

        val request = server.takeRequest()

        assertThat(result.isSuccess).isTrue()
        assertThat(request.method).isEqualTo("DELETE")
        assertThat(request.path).contains(cert.thumbprint)
    }

    @Test
    fun deleteAccount_whenUnconfirmedWithNoCert_succeedsWithoutApiCall() = runViewModelTest {
        val providersRepository = providersRepositoryWithTestServer()
        val account = Account(
            id = UUID.randomUUID(),
            email = "unconfirmed@example.com",
            providerId = provider.id,
            deviceId = "device-1",
            isConfirmed = false,
            thumbprint = null,
            publicCertBinary = null,
            privateCertBinary = null,
            salt = null,
            validFrom = java.time.Instant.now(),
            validTo = java.time.Instant.now().plus(365, java.time.temporal.ChronoUnit.DAYS),
        )

        val result = BackendAccountManagementService(apiClient, providersRepository)
            .deleteAccount(account)

        assertThat(result.isSuccess).isTrue()
        assertThat(server.requestCount).isEqualTo(0)
    }

    // ── Certificate rotation ────────────────────────────────────────────────────

    @Test
    fun certificateRotationPostsNewPublicCertToUpdateEndpoint() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(200))

        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        val providersRepository = providersRepositoryWithTestServer()
        val account = accountWithCert(email = "user@example.com", cert = cert, pin = "1234", providerId = provider.id)

        val result = BackendCertificateRotationService(
            apiClient = apiClient,
            providersRepository = providersRepository,
            certificateGenerator = BouncyCastleCertificateGenerator(),
            accountSigner = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
        ).rotateCertificate(account, oldPin = "1234", newPin = "5678", useBiometric = false)

        val request = server.takeRequest()
        val body = request.body.readUtf8()

        assertThat(result.isSuccess).isTrue()
        assertThat(request.path).isEqualTo("/api/Certificate/UpdatePublicCert")
        assertThat(body).contains(cert.thumbprint)
        assertThat(body).contains("device-1")
    }

    @Test
    fun certificateRotationUpdatesAccountWithNewCertificateData() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(200))

        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        val providersRepository = providersRepositoryWithTestServer()
        val account = accountWithCert(email = "user@example.com", cert = cert, pin = "1234", providerId = provider.id)

        val result = BackendCertificateRotationService(
            apiClient = apiClient,
            providersRepository = providersRepository,
            certificateGenerator = BouncyCastleCertificateGenerator(),
            accountSigner = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
        ).rotateCertificate(account, oldPin = "1234", newPin = "5678", useBiometric = false)

        val updatedAccount = result.getOrThrow()
        assertThat(updatedAccount.thumbprint).isNotEqualTo(cert.thumbprint)
        assertThat(updatedAccount.pinLength).isEqualTo(4)
    }

    @Test
    fun certificateRotationReturnsFailureWhenOldPinIsWrong() = runViewModelTest {
        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        val providersRepository = providersRepositoryWithTestServer()
        val account = accountWithCert(email = "user@example.com", cert = cert, pin = "1234", providerId = provider.id)

        val result = BackendCertificateRotationService(
            apiClient = apiClient,
            providersRepository = providersRepository,
            certificateGenerator = BouncyCastleCertificateGenerator(),
            accountSigner = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
        ).rotateCertificate(account, oldPin = "wrong", newPin = "5678", useBiometric = false)

        assertThat(result.isFailure).isTrue()
        assertThat(result.exceptionOrNull()?.message).isEqualTo("Invalid old pin")
    }

    @Test
    fun certificateRotationReturnsNetworkErrorOnServerFailure() = runViewModelTest {
        server.enqueue(MockResponse().setResponseCode(500))

        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        val providersRepository = providersRepositoryWithTestServer()
        val account = accountWithCert(email = "user@example.com", cert = cert, pin = "1234", providerId = provider.id)

        val result = BackendCertificateRotationService(
            apiClient = apiClient,
            providersRepository = providersRepository,
            certificateGenerator = BouncyCastleCertificateGenerator(),
            accountSigner = Pkcs12AccountSigner(),
            biometricCertificateService = FakeBiometricCertificateService(),
        ).rotateCertificate(account, oldPin = "1234", newPin = "5678", useBiometric = false)

        assertThat(result.isFailure).isTrue()
        assertThat(result.exceptionOrNull()?.message).contains("Network error")
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private suspend fun providersRepositoryWithTestServer(): InMemoryProvidersRepository {
        val repo = inMemoryProvidersRepository()
        repo.saveProvider(provider)
        return repo
    }

    private suspend fun confirmedAccountsRepository(
        cert: com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate? = null,
    ): InMemoryAccountsRepository = inMemoryAccountsRepository().also { repo ->
        repo.updateAccount(
            repo.getAccounts().first().copy(
                providerId = provider.id,
                isConfirmed = true,
                salt = cert?.salt,
                privateCertBinary = cert?.privateCertBinary,
                publicCertBinary = cert?.publicCertBinary,
                thumbprint = cert?.thumbprint ?: "sample-thumbprint",
                pinLength = if (cert != null) 4 else 0,
            )
        )
    }

    private suspend fun confirmedAccountWithCert(): Account {
        val cert = BouncyCastleCertificateGenerator().generate("user@example.com", "1234")
        return Account(
            id = UUID.fromString("4eeb9825-3028-4389-ac41-b6690b0edb9e"),
            email = "user@example.com",
            providerId = provider.id,
            deviceId = "device-1",
            isConfirmed = true,
            salt = cert.salt,
            privateCertBinary = cert.privateCertBinary,
            publicCertBinary = cert.publicCertBinary,
            thumbprint = cert.thumbprint,
            validFrom = cert.validFrom,
            validTo = cert.validTo,
            pinLength = 4,
        )
    }

    private fun accountWithCert(
        email: String,
        cert: com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate,
        pin: String,
        providerId: UUID,
    ): Account = Account(
        id = UUID.randomUUID(),
        email = email,
        providerId = providerId,
        deviceId = "device-1",
        isConfirmed = true,
        salt = cert.salt,
        privateCertBinary = cert.privateCertBinary,
        publicCertBinary = cert.publicCertBinary,
        thumbprint = cert.thumbprint,
        validFrom = cert.validFrom,
        validTo = cert.validTo,
        pinLength = pin.length,
    )

    private fun pendingSession(accountId: UUID): NotificationSession = NotificationSession(
        sender = "mailler",
        confirmationColor = ConfirmationColor.BLUE,
        sessionId = UUID.randomUUID(),
        expirationTime = Instant.now().plus(5, ChronoUnit.MINUTES),
        randomString = "nonce-123",
        returnHost = "mail.example.com",
        accountId = accountId,
    )

    private fun buildSessionJson(accountId: UUID): String = """
        {
            "Sender": "mailler",
            "ConfirmationColor": "blue",
            "SessionId": "2d9e6360-28ba-4aa8-a9c5-ec7cb5632489",
            "ExpirationTime": "2030-01-01T00:00:00Z",
            "RandomString": "nonce-123",
            "ReturnHost": "mail.example.com",
            "AccountGuid": "$accountId"
        }
    """.trimIndent()

    private fun buildSessionJsonWithNumericColor(accountId: UUID, color: Int): String = """
        {
            "Sender": "mailler",
            "ConfirmationColor": $color,
            "SessionId": "2d9e6360-28ba-4aa8-a9c5-ec7cb5632489",
            "ExpirationTime": "2030-01-01T00:00:00Z",
            "RandomString": "nonce-123",
            "ReturnHost": "mail.example.com",
            "AccountGuid": "$accountId"
        }
    """.trimIndent()
}
