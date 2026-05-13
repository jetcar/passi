package com.passi.cloud.passi_android

import androidx.arch.core.executor.testing.InstantTaskExecutorRule
import com.passi.cloud.passi_android.data.auth.PendingSessionStore
import com.passi.cloud.passi_android.data.local.PendingEnrollmentStore
import com.passi.cloud.passi_android.data.repository.InMemoryAccountsRepository
import com.passi.cloud.passi_android.data.repository.InMemoryProvidersRepository
import com.passi.cloud.passi_android.domain.account.AccountManagementService
import com.passi.cloud.passi_android.domain.auth.AuthSessionService
import com.passi.cloud.passi_android.domain.biometric.BiometricCertificateService
import com.passi.cloud.passi_android.domain.certificate.CertificateRotationService
import com.passi.cloud.passi_android.domain.enrollment.CertificateGenerator
import com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.ConfirmationColor
import com.passi.cloud.passi_android.domain.model.ManagedDevice
import com.passi.cloud.passi_android.domain.model.NotificationSession
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.service.EnrollmentService
import com.passi.cloud.passi_android.domain.service.PendingEnrollment
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.TestDispatcher
import kotlinx.coroutines.test.TestScope
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.Rule
import java.time.Instant
import java.util.UUID

@OptIn(ExperimentalCoroutinesApi::class)
open class CoroutineViewModelTest {
    @get:Rule
    val instantTaskExecutorRule = InstantTaskExecutorRule()

    protected val dispatcher: TestDispatcher = StandardTestDispatcher()

    protected fun runViewModelTest(block: suspend TestScope.() -> Unit) {
        Dispatchers.setMain(dispatcher)
        try {
            runTest(dispatcher) {
                block()
            }
        } finally {
            Dispatchers.resetMain()
        }
    }
}

fun samplePendingEnrollment(
    accountId: UUID = UUID.fromString("4eeb9825-3028-4389-ac41-b6690b0edb9e"),
    providerId: UUID = UUID.fromString("7a22cc55-4d18-4e02-bfa5-f6ce4913fbfb"),
    email: String = "admin@passi.cloud",
    confirmationCode: String? = null,
): PendingEnrollment = PendingEnrollment(
    accountId = accountId.toString(),
    email = email,
    providerId = providerId.toString(),
    confirmationCode = confirmationCode,
)

fun sampleSession(
    accountId: UUID = UUID.fromString("4eeb9825-3028-4389-ac41-b6690b0edb9e"),
    confirmationColor: ConfirmationColor = ConfirmationColor.BLUE,
): NotificationSession = NotificationSession(
    sender = "mailler",
    confirmationColor = confirmationColor,
    sessionId = UUID.fromString("2d9e6360-28ba-4aa8-a9c5-ec7cb5632489"),
    expirationTime = Instant.parse("2030-01-01T00:00:00Z"),
    randomString = "nonce-123",
    returnHost = "mail.example.com",
    accountId = accountId,
)

fun sampleGeneratedCertificate(): GeneratedCertificate = GeneratedCertificate(
    salt = "salt-123",
    privateCertBinary = "private-cert",
    publicCertBinary = "public-cert",
    thumbprint = "thumbprint-123",
    validFrom = Instant.parse("2026-01-01T00:00:00Z"),
    validTo = Instant.parse("2027-01-01T00:00:00Z"),
)

class FakeEnrollmentService : EnrollmentService {
    var beginSignupResult: Result<PendingEnrollment> = Result.success(samplePendingEnrollment())
    var confirmSignupCodeResult: Result<Unit> = Result.success(Unit)
    var finalizeSignupResult: Result<Unit> = Result.success(Unit)

    var lastBeginSignupEmail: String? = null
    var lastBeginSignupProvider: Provider? = null
    var lastBeginSignupDeviceId: String? = null
    var lastConfirmCode: String? = null

    override suspend fun beginSignup(email: String, provider: Provider, deviceId: String): Result<PendingEnrollment> {
        lastBeginSignupEmail = email
        lastBeginSignupProvider = provider
        lastBeginSignupDeviceId = deviceId
        return beginSignupResult.map { pending ->
            pending.copy(
                email = email,
                providerId = provider.id.toString(),
            )
        }
    }

    override suspend fun confirmSignupCode(accountId: String, email: String, code: String, provider: Provider): Result<Unit> {
        lastConfirmCode = code
        return confirmSignupCodeResult
    }

    override suspend fun finalizeSignup(
        pendingEnrollment: PendingEnrollment,
        provider: Provider,
        deviceId: String,
        generatedCertificate: GeneratedCertificate,
    ): Result<Unit> = finalizeSignupResult
}

class FakeCertificateGenerator : CertificateGenerator {
    var result: Result<GeneratedCertificate> = Result.success(sampleGeneratedCertificate())

    override fun generate(email: String, pin: String?): GeneratedCertificate = result.getOrThrow()
}

class FakeAuthSessionService : AuthSessionService {
    var syncAndPollPendingSessionResult: Result<NotificationSession?> = Result.success(null)
    var authorizeResult: Result<Unit> = Result.success(Unit)
    var authorizeWithBiometricResult: Result<Unit> = Result.success(Unit)
    var cancelResult: Result<Unit> = Result.success(Unit)

    var lastAuthorizedPin: String? = null

    override suspend fun syncAndPollPendingSession(): Result<NotificationSession?> = syncAndPollPendingSessionResult

    override suspend fun authorize(session: NotificationSession, pin: String?): Result<Unit> {
        lastAuthorizedPin = pin
        return authorizeResult
    }

    override suspend fun authorizeWithBiometric(session: NotificationSession): Result<Unit> = authorizeWithBiometricResult

    override suspend fun cancel(session: NotificationSession): Result<Unit> = cancelResult
}

class FakeAccountManagementService : AccountManagementService {
    var deleteResult: Result<Unit> = Result.success(Unit)
    var devicesResult: Result<List<ManagedDevice>> = Result.success(emptyList())
    var deleteDeviceResult: Result<Unit> = Result.success(Unit)

    override suspend fun deleteAccount(account: Account): Result<Unit> = deleteResult

    override suspend fun getDevices(account: Account, currentDeviceId: String): Result<List<ManagedDevice>> = devicesResult

    override suspend fun deleteDevice(account: Account, deviceId: String, currentDeviceId: String): Result<Unit> = deleteDeviceResult
}

class FakeBiometricCertificateService : BiometricCertificateService {
    var enableResult: Result<Account>? = null
    var signResult: Result<String> = Result.success("signed-hash")
    var lastEnablePin: String? = null

    override suspend fun enableBiometric(account: Account, pin: String?): Result<Account> {
        lastEnablePin = pin
        return enableResult ?: Result.success(account.copy(hasFingerprint = true))
    }

    override fun sign(account: Account, message: String): Result<String> = signResult
}

class FakeAccountSigner : com.passi.cloud.passi_android.domain.auth.AccountSigner {
    var signResult: Result<String> = Result.success("fake-signed-hash")

    override fun sign(
        account: com.passi.cloud.passi_android.domain.model.Account,
        pin: String?,
        message: String,
    ): Result<String> = signResult
}

class FakeCertificateRotationService : CertificateRotationService {
    var rotateResult: Result<Account>? = null
    var lastUsedBiometric: Boolean? = null
    var lastOldPin: String? = null
    var lastNewPin: String? = null

    override suspend fun rotateCertificate(
        account: Account,
        oldPin: String?,
        newPin: String?,
        useBiometric: Boolean,
    ): Result<Account> {
        lastOldPin = oldPin
        lastNewPin = newPin
        lastUsedBiometric = useBiometric
        return rotateResult ?: Result.success(account.copy(thumbprint = "updated-thumbprint"))
    }
}

fun inMemoryAccountsRepository(): InMemoryAccountsRepository = InMemoryAccountsRepository()

fun inMemoryProvidersRepository(): InMemoryProvidersRepository = InMemoryProvidersRepository()

fun pendingEnrollmentStore(): PendingEnrollmentStore = PendingEnrollmentStore()

fun pendingSessionStore(): PendingSessionStore = PendingSessionStore()