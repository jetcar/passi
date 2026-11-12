package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.localization.UiText
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.feature.certificate.UpdateCertificateViewModel
import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection
import com.passi.cloud.passi_android.domain.enrollment.checkpoint
import com.passi.cloud.passi_android.domain.certificate.CertificateRotationService
import com.passi.cloud.passi_android.domain.model.Account
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.advanceUntilIdle
import org.junit.Test
import java.util.UUID

@OptIn(ExperimentalCoroutinesApi::class)
class CertificateUseCaseTests : CoroutineViewModelTest() {
    @Test
    fun pendingHistoricalKeyLocksItsActualProtection() = runViewModelTest {
        val accountId = UUID.fromString(samplePendingEnrollment().accountId)
        val accountsRepository = inMemoryAccountsRepository().also { repository ->
            val account = requireNotNull(repository.getAccount(accountId))
            repository.updateAccount(account.copy(
                isConfirmed = true,
                pendingKey = sampleGeneratedCertificate().checkpoint().copy(
                    keyProtection = null,
                    submitted = true,
                ),
            ))
        }
        val viewModel = UpdateCertificateViewModel(
            selectedAccountStore = SelectedAccountStore().apply { save(accountId) },
            accountsRepository = accountsRepository,
            certificateRotationService = FakeCertificateRotationService(),
            biometricCertificateService = FakeBiometricCertificateService(),
        )

        advanceUntilIdle()

        assertThat(viewModel.uiState.value.keyProtection)
            .isEqualTo(DeviceKeyProtection.BIOMETRIC_OR_DEVICE_CREDENTIAL)
        assertThat(viewModel.uiState.value.isProtectionLocked).isTrue()
        viewModel.onKeyProtectionSelected(DeviceKeyProtection.BIOMETRIC)
        assertThat(viewModel.uiState.value.keyProtection)
            .isEqualTo(DeviceKeyProtection.BIOMETRIC_OR_DEVICE_CREDENTIAL)
    }

    @Test
    fun failedRotationLocksProtectionStoredByPendingCheckpoint() = runViewModelTest {
        val accountId = UUID.fromString(samplePendingEnrollment().accountId)
        val accountsRepository = inMemoryAccountsRepository()
        val rotationService = object : CertificateRotationService {
            override suspend fun rotateCertificate(
                account: Account,
                oldPin: String?,
                newPin: String?,
                useBiometric: Boolean,
            ): Result<Account> {
                accountsRepository.updateAccount(account.copy(
                    pendingKey = sampleGeneratedCertificate()
                        .copy(keyProtection = DeviceKeyProtection.DEVICE_CREDENTIAL)
                        .checkpoint()
                        .copy(submitted = true),
                ))
                return Result.failure(IllegalStateException("network failure"))
            }
        }
        val viewModel = UpdateCertificateViewModel(
            selectedAccountStore = SelectedAccountStore().apply { save(accountId) },
            accountsRepository = accountsRepository,
            certificateRotationService = rotationService,
            biometricCertificateService = FakeBiometricCertificateService(),
        )

        advanceUntilIdle()
        viewModel.onKeyProtectionSelected(DeviceKeyProtection.BIOMETRIC)
        viewModel.rotate()
        advanceUntilIdle()

        assertThat(viewModel.uiState.value.keyProtection)
            .isEqualTo(DeviceKeyProtection.DEVICE_CREDENTIAL)
        assertThat(viewModel.uiState.value.isProtectionLocked).isTrue()
        viewModel.onKeyProtectionSelected(DeviceKeyProtection.BIOMETRIC)
        assertThat(viewModel.uiState.value.keyProtection)
            .isEqualTo(DeviceKeyProtection.DEVICE_CREDENTIAL)
    }

    @Test
    fun updateCertificateDoesNotUseStandaloneBiometricApproval() = runViewModelTest {
        val accountsRepository = inMemoryAccountsRepository().also {
            it.updateAccount(it.getAccounts().first().copy(isConfirmed = true, pinLength = 0, hasFingerprint = true))
        }
        val rotationService = FakeCertificateRotationService()
        val viewModel = UpdateCertificateViewModel(
            selectedAccountStore = SelectedAccountStore().apply { save(UUID.fromString(samplePendingEnrollment().accountId)) },
            accountsRepository = accountsRepository,
            certificateRotationService = rotationService,
            biometricCertificateService = FakeBiometricCertificateService(),
        )

        advanceUntilIdle()
        viewModel.onKeyProtectionSelected(DeviceKeyProtection.BIOMETRIC)
        viewModel.rotate()

        advanceUntilIdle()
        assertThat(viewModel.uiState.value.responseError).isNull()
        assertThat(viewModel.uiState.value.isComplete).isTrue()
        assertThat(rotationService.lastUsedBiometric).isTrue()
    }

    @Test
    fun updateCertificateRejectsShortOldPinForPinProtectedAccount() = runViewModelTest {
        val accountsRepository = inMemoryAccountsRepository().also {
            it.updateAccount(it.getAccounts().first().copy(isConfirmed = true, pinLength = 4))
        }
        val viewModel = UpdateCertificateViewModel(
            selectedAccountStore = SelectedAccountStore().apply { save(UUID.fromString(samplePendingEnrollment().accountId)) },
            accountsRepository = accountsRepository,
            certificateRotationService = FakeCertificateRotationService(),
            biometricCertificateService = FakeBiometricCertificateService(),
        )

        advanceUntilIdle()
        viewModel.onKeyProtectionSelected(DeviceKeyProtection.DEVICE_CREDENTIAL)
        viewModel.onOldPinChanged("12")
        viewModel.rotate()

        assertThat(viewModel.uiState.value.oldPinError).isEqualTo(UiText.Resource(R.string.error_invalid_old_pin))
    }

    @Test
    fun updateCertificateRequiresOldPinAndNeverCreatesBiometricCopy() = runViewModelTest {
        val accountsRepository = inMemoryAccountsRepository().also {
            it.updateAccount(it.getAccounts().first().copy(isConfirmed = true, pinLength = 4, hasFingerprint = true))
        }
        val rotationService = FakeCertificateRotationService()
        val biometricService = FakeBiometricCertificateService()
        val viewModel = UpdateCertificateViewModel(
            selectedAccountStore = SelectedAccountStore().apply { save(UUID.fromString(samplePendingEnrollment().accountId)) },
            accountsRepository = accountsRepository,
            certificateRotationService = rotationService,
            biometricCertificateService = biometricService,
        )
        advanceUntilIdle()
        viewModel.onKeyProtectionSelected(DeviceKeyProtection.DEVICE_CREDENTIAL)
        viewModel.confirmBiometricForCurrentCertificate()
        viewModel.onOldPinChanged("1234")
        viewModel.onNewPinChanged("5678")
        viewModel.onConfirmPinChanged("5678")
        viewModel.rotate()
        advanceUntilIdle()

        assertThat(viewModel.uiState.value.isComplete).isTrue()
        assertThat(rotationService.lastUsedBiometric).isFalse()
        assertThat(rotationService.lastNewPin).isNull()
        assertThat(rotationService.lastOldPin).isEqualTo("1234")
        assertThat(biometricService.lastEnablePin).isNull()
    }
}
