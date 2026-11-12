package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.localization.UiText
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.feature.certificate.UpdateCertificateViewModel
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.advanceUntilIdle
import org.junit.Test
import java.util.UUID

@OptIn(ExperimentalCoroutinesApi::class)
class CertificateUseCaseTests : CoroutineViewModelTest() {
    @Test
    fun updateCertificateDoesNotUseStandaloneBiometricApproval() = runViewModelTest {
        val accountsRepository = inMemoryAccountsRepository().also {
            it.updateAccount(it.getAccounts().first().copy(isConfirmed = true, pinLength = 0, hasFingerprint = true))
        }
        val viewModel = UpdateCertificateViewModel(
            selectedAccountStore = SelectedAccountStore().apply { save(UUID.fromString(samplePendingEnrollment().accountId)) },
            accountsRepository = accountsRepository,
            certificateRotationService = FakeCertificateRotationService(),
            biometricCertificateService = FakeBiometricCertificateService(),
        )

        advanceUntilIdle()
        viewModel.rotate()

        advanceUntilIdle()
        assertThat(viewModel.uiState.value.responseError).isNull()
        assertThat(viewModel.uiState.value.isComplete).isTrue()
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
