package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.feature.certificate.UpdateCertificateViewModel
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.advanceUntilIdle
import org.junit.Test
import java.util.UUID

@OptIn(ExperimentalCoroutinesApi::class)
class CertificateUseCaseTests : CoroutineViewModelTest() {
    @Test
    fun updateCertificateRequiresBiometricConfirmationForBiometricOnlyAccount() = runViewModelTest {
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
        viewModel.rotate {}

        assertThat(viewModel.uiState.value.responseError).isEqualTo("Confirm biometric before rotating this certificate")
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
        viewModel.rotate {}

        assertThat(viewModel.uiState.value.oldPinError).isEqualTo("Invalid old pin")
    }

    @Test
    fun updateCertificateRotatesAndRefreshesBiometricMaterial() = runViewModelTest {
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
        var done = false

        advanceUntilIdle()
        viewModel.confirmBiometricForCurrentCertificate()
        viewModel.onNewPinChanged("5678")
        viewModel.onConfirmPinChanged("5678")
        viewModel.rotate { done = true }
        advanceUntilIdle()

        assertThat(done).isTrue()
        assertThat(rotationService.lastUsedBiometric).isTrue()
        assertThat(rotationService.lastNewPin).isEqualTo("5678")
        assertThat(biometricService.lastEnablePin).isEqualTo("5678")
    }
}