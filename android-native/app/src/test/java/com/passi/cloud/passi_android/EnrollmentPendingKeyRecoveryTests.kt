package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.domain.enrollment.CertificateGenerator
import com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate
import com.passi.cloud.passi_android.domain.enrollment.checkpoint
import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.service.EnrollmentService
import com.passi.cloud.passi_android.domain.service.PendingEnrollment
import com.passi.cloud.passi_android.feature.enrollment.ConfirmCodeViewModel
import com.passi.cloud.passi_android.feature.enrollment.FinishEnrollmentViewModel
import com.passi.cloud.passi_android.localization.UiText
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.advanceUntilIdle
import org.junit.Test
import java.util.UUID

@OptIn(ExperimentalCoroutinesApi::class)
class EnrollmentPendingKeyRecoveryTests : CoroutineViewModelTest() {
    @Test
    fun existingPendingKeyLocksItsProtectionChoice() = runViewModelTest {
        val pending = samplePendingEnrollment(confirmationCode = "123456")
        val repository = inMemoryAccountsRepository()
        val account = requireNotNull(repository.getAccount(UUID.fromString(pending.accountId)))
        repository.updateAccount(account.copy(
            pendingKey = sampleGeneratedCertificate()
                .copy(keyProtection = DeviceKeyProtection.DEVICE_CREDENTIAL)
                .checkpoint()
                .copy(submitted = true),
        ))
        val viewModel = FinishEnrollmentViewModel(
            pendingEnrollmentStore().apply { save(pending) },
            repository,
            inMemoryProvidersRepository(),
            FakeEnrollmentService(),
            CountingGenerator(),
            { "device" },
        )

        advanceUntilIdle()

        assertThat(viewModel.uiState.value.keyProtection)
            .isEqualTo(DeviceKeyProtection.DEVICE_CREDENTIAL)
        assertThat(viewModel.uiState.value.isProtectionLocked).isTrue()
        viewModel.onKeyProtectionSelected(DeviceKeyProtection.BIOMETRIC)
        assertThat(viewModel.uiState.value.keyProtection)
            .isEqualTo(DeviceKeyProtection.DEVICE_CREDENTIAL)
    }

    @Test
    fun missingConfirmationCodeStopsBeforeCreatingOrSubmittingKey() = runViewModelTest {
        val pending = samplePendingEnrollment()
        val repository = inMemoryAccountsRepository()
        val generator = CountingGenerator()
        val viewModel = FinishEnrollmentViewModel(pendingEnrollmentStore().apply { save(pending) }, repository,
            inMemoryProvidersRepository(), FakeEnrollmentService(), generator, { "device" })
        viewModel.onKeyProtectionSelected(DeviceKeyProtection.DEVICE_CREDENTIAL)
        viewModel.submit {}
        advanceUntilIdle()
        assertThat(viewModel.uiState.value.responseError).isEqualTo(UiText.Resource(R.string.error_confirmation_code_missing))
        assertThat(generator.generated).isEqualTo(0)
        assertThat(repository.getAccount(UUID.fromString(pending.accountId))?.pendingKey).isNull()
    }

    @Test
    fun resumePrefillsConfirmationCodeAndUsesItWithoutRetyping() = runViewModelTest {
        val pending = samplePendingEnrollment(confirmationCode = "123456")
        val store = pendingEnrollmentStore().apply { save(pending) }
        val repository = inMemoryAccountsRepository()
        val account = requireNotNull(repository.getAccount(UUID.fromString(pending.accountId)))
        repository.updateAccount(account.copy(pendingKey = sampleGeneratedCertificate().checkpoint().copy(submitted = true)))
        val service = FakeEnrollmentService()
        val viewModel = ConfirmCodeViewModel(store, service, repository, inMemoryProvidersRepository())
        var finished = false
        assertThat(viewModel.uiState.value.code).isEqualTo("123456")
        viewModel.submit { finished = true }
        advanceUntilIdle()
        assertThat(finished).isTrue()
        assertThat(store.read()?.confirmationCode).isEqualTo("123456")
        assertThat(service.lastConfirmCode).isNull()
    }

    @Test
    fun backAfterMistypedCheckpointCodeAllowsCorrection() = runViewModelTest {
        val pending = samplePendingEnrollment()
        val store = pendingEnrollmentStore().apply { save(pending) }
        val repository = inMemoryAccountsRepository()
        val account = requireNotNull(repository.getAccount(UUID.fromString(pending.accountId)))
        val checkpoint = sampleGeneratedCertificate().checkpoint().copy(submitted = true)
        repository.updateAccount(account.copy(pendingKey = checkpoint))
        val service = FakeEnrollmentService()
        val first = ConfirmCodeViewModel(store, service, repository, inMemoryProvidersRepository())
        first.onCodeChanged("999999")
        first.submit {}
        advanceUntilIdle()
        assertThat(store.read()?.confirmationCode).isEqualTo("999999")

        // Returning from finalization recreates the code screen with the last attempted code.
        val returned = ConfirmCodeViewModel(store, service, repository, inMemoryProvidersRepository())
        assertThat(returned.uiState.value.code).isEqualTo("999999")
        returned.onCodeChanged("123456")
        returned.submit {}
        advanceUntilIdle()
        assertThat(store.read()?.confirmationCode).isEqualTo("123456")
        assertThat(repository.getAccount(account.id)?.pendingKey).isEqualTo(checkpoint)
        assertThat(service.lastConfirmCode).isNull()
    }

    @Test
    fun submissionIsDurableBeforeSignupAndFailureRetriesExactCertificate() = runViewModelTest {
        val pending = samplePendingEnrollment(confirmationCode = "123456")
        val store = pendingEnrollmentStore().apply { save(pending) }
        val repository = inMemoryAccountsRepository()
        val generator = CountingGenerator()
        val submitted = mutableListOf<GeneratedCertificate>()
        val service = RecordingService { generatedCertificate ->
                val checkpoint = requireNotNull(repository.getAccount(UUID.fromString(pending.accountId))?.pendingKey)
                assertThat(checkpoint.submitted).isTrue()
                assertThat(checkpoint.publicCertBinary).isEqualTo(generatedCertificate.publicCertBinary)
                submitted += generatedCertificate
                Result.failure(IllegalStateException("Lost response"))
        }
        val viewModel = FinishEnrollmentViewModel(store, repository, inMemoryProvidersRepository(), service, generator, { "device" })
        viewModel.onKeyProtectionSelected(DeviceKeyProtection.BIOMETRIC)
        viewModel.submit {}
        advanceUntilIdle()
        viewModel.submit {}
        advanceUntilIdle()
        assertThat(generator.generated).isEqualTo(1)
        assertThat(submitted).hasSize(2)
        assertThat(submitted[0]).isEqualTo(submitted[1])
        assertThat(store.read()).isEqualTo(pending)
    }

    @Test
    fun deadSubmittedKeyRequiresRecoveryWithoutReplacingOrSendingIt() = runViewModelTest {
        val pending = samplePendingEnrollment(confirmationCode = "123456")
        val store = pendingEnrollmentStore().apply { save(pending) }
        val repository = inMemoryAccountsRepository()
        val account = requireNotNull(repository.getAccount(UUID.fromString(pending.accountId)))
        val oldCheckpoint = sampleGeneratedCertificate().checkpoint().copy(submitted = true)
        repository.updateAccount(account.copy(pendingKey = oldCheckpoint))
        val generator = CountingGenerator().apply { invalidAlias = oldCheckpoint.keyAlias }
        var calls = 0
        val service = RecordingService {
                calls++
                Result.success(account.id)
        }
        val viewModel = FinishEnrollmentViewModel(store, repository, inMemoryProvidersRepository(), service, generator, { "device" })
        viewModel.onKeyProtectionSelected(DeviceKeyProtection.DEVICE_CREDENTIAL)
        viewModel.submit {}
        advanceUntilIdle()
        assertThat(viewModel.uiState.value.responseError).isEqualTo(UiText.Resource(R.string.device_key_recovery_required))
        assertThat(generator.generated).isEqualTo(0)
        assertThat(calls).isEqualTo(0)
        assertThat(repository.getAccount(account.id)?.pendingKey).isEqualTo(oldCheckpoint)
    }

    @Test
    fun deadUnsubmittedSignupKeyIsRegenerated() = runViewModelTest {
        val pending = samplePendingEnrollment(confirmationCode = "123456")
        val store = pendingEnrollmentStore().apply { save(pending) }
        val repository = inMemoryAccountsRepository()
        val account = requireNotNull(repository.getAccount(UUID.fromString(pending.accountId)))
        val oldCheckpoint = sampleGeneratedCertificate().checkpoint()
        repository.updateAccount(account.copy(pendingKey = oldCheckpoint))
        val generator = CountingGenerator().apply { invalidAlias = oldCheckpoint.keyAlias }
        val viewModel = FinishEnrollmentViewModel(store, repository, inMemoryProvidersRepository(), FakeEnrollmentService(), generator, { "device" })
        viewModel.onKeyProtectionSelected(DeviceKeyProtection.BIOMETRIC)
        viewModel.submit {}
        advanceUntilIdle()
        assertThat(generator.generated).isEqualTo(1)
        assertThat(repository.getAccount(account.id)?.keyAlias).isEqualTo("replacement-1")
        assertThat(viewModel.uiState.value.responseError).isNull()
    }

    private class RecordingService(
        val onFinalize: suspend (GeneratedCertificate) -> Result<UUID>,
    ) : EnrollmentService by FakeEnrollmentService() {
        override suspend fun finalizeSignup(pendingEnrollment: PendingEnrollment, provider: Provider,
            deviceId: String, generatedCertificate: GeneratedCertificate): Result<UUID> = onFinalize(generatedCertificate)
    }

    private class CountingGenerator : CertificateGenerator {
        var generated = 0
        var invalidAlias: String? = null
        override fun generate(
            email: String,
            protection: com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection,
        ): GeneratedCertificate {
            generated++
            return sampleGeneratedCertificate().copy(
                keyAlias = "replacement-$generated",
                publicCertBinary = "public-$generated",
                keyProtection = protection,
            )
        }
        override fun validate(certificate: GeneratedCertificate) {
            check(certificate.keyAlias != invalidAlias) { "Invalidated key" }
        }
    }
}
