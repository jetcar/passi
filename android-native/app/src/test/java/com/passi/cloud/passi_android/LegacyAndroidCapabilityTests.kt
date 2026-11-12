package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.data.auth.PendingSessionStore
import com.passi.cloud.passi_android.data.local.PendingEnrollmentStore
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.domain.enrollment.supportsDeviceKeys
import com.passi.cloud.passi_android.feature.accounts.AccountsViewModel
import com.passi.cloud.passi_android.feature.certificate.UpdateCertificateViewModel
import com.passi.cloud.passi_android.feature.enrollment.AddAccountViewModel
import com.passi.cloud.passi_android.localization.UiText
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.advanceUntilIdle
import org.junit.Test

@OptIn(ExperimentalCoroutinesApi::class)
class LegacyAndroidCapabilityTests : CoroutineViewModelTest() {
    @Test fun deviceCredentialCryptoPolicyStartsAtAndroid11() {
        assertThat(supportsDeviceKeys(28)).isFalse()
        assertThat(supportsDeviceKeys(29)).isFalse()
        assertThat(supportsDeviceKeys(30)).isTrue()
        assertThat(supportsDeviceKeys(36)).isTrue()
    }

    @Test fun unsupportedAndroidNeverStartsRegistration() = runViewModelTest {
        for (api in listOf(28, 29)) {
            val service = FakeEnrollmentService()
            val pending = PendingEnrollmentStore()
            val model = AddAccountViewModel(inMemoryProvidersRepository(), inMemoryAccountsRepository(),
                service, pending, { "device" }, deviceKeySupported = supportsDeviceKeys(api))
            advanceUntilIdle()
            model.onEmailChanged("test@example.com")
            model.submit { error("Unsupported registration must not navigate") }
            advanceUntilIdle()
            assertThat(service.lastBeginSignupEmail).isNull()
            assertThat(pending.read()).isNull()
            assertThat(model.uiState.value.deviceKeySupported).isFalse()
            assertThat(model.uiState.value.responseError)
                .isEqualTo(UiText.Resource(R.string.device_key_android_version_required))
        }
    }

    @Test fun unsupportedAndroidKeepsLegacyAccountAccessWithoutMigrationNotice() = runViewModelTest {
        val repository = inMemoryAccountsRepository()
        val legacy = repository.getAccounts().first().copy(isConfirmed = true, privateCertBinary = "legacy", keyAlias = null)
        repository.updateAccount(legacy)
        val pending = PendingEnrollmentStore()
        val selected = SelectedAccountStore()
        val model = AccountsViewModel(repository, inMemoryProvidersRepository(), FakeAuthSessionService(),
            PendingSessionStore(), pending, selected, FakeAccountManagementService(), deviceKeySupported = false)
        advanceUntilIdle()
        assertThat(model.uiState.value.accountsRequiringCertificateUpdate).isEmpty()
        var opened = false
        model.openAccount(legacy, { opened = true }, { error("Legacy account must remain accessible") })
        assertThat(opened).isTrue()
        assertThat(selected.read()).isEqualTo(legacy.id)
        model.updateCertificate(legacy) { error("Unsupported migration must not navigate") }
        model.openAccount(legacy.copy(isConfirmed = false), {}, { error("Unsupported enrollment must not resume") })
        assertThat(pending.read()).isNull()
        assertThat(model.uiState.value.syncError)
            .isEqualTo(UiText.Resource(R.string.device_key_android_version_required))
    }

    @Test fun unsupportedAndroidBlocksDirectMigrationEntryBeforeServiceCall() = runViewModelTest {
        val repository = inMemoryAccountsRepository()
        val account = repository.getAccounts().first()
        val service = FakeCertificateRotationService()
        val model = UpdateCertificateViewModel(SelectedAccountStore().apply { save(account.id) },
            repository, service, FakeBiometricCertificateService(), deviceKeySupported = false)
        advanceUntilIdle()
        model.onOldPinChanged("1234")
        model.rotate { error("Unsupported migration must not finish") }
        advanceUntilIdle()
        assertThat(service.lastOldPin).isNull()
        assertThat(service.lastUsedBiometric).isNull()
        assertThat(model.uiState.value.responseError)
            .isEqualTo(UiText.Resource(R.string.device_key_android_version_required))
    }
}
