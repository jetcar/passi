package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.data.selection.SelectedProviderStore
import com.passi.cloud.passi_android.domain.model.ManagedDevice
import com.passi.cloud.passi_android.feature.account.AccountDevicesViewModel
import com.passi.cloud.passi_android.feature.account.AccountDetailViewModel
import com.passi.cloud.passi_android.feature.providers.ProviderEditorViewModel
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.advanceUntilIdle
import org.junit.Test
import java.time.Instant
import java.util.UUID

@OptIn(ExperimentalCoroutinesApi::class)
class ProviderAndAccountUseCaseTests : CoroutineViewModelTest() {
    @Test
    fun providerEditorSavesProviderConfiguration() = runViewModelTest {
        val providersRepository = inMemoryProvidersRepository()
        val viewModel = ProviderEditorViewModel(
            selectedProviderStore = SelectedProviderStore(),
            providersRepository = providersRepository,
        )
        var saved = false

        viewModel.update {
            it.copy(
                name = "custom",
                baseUrl = "https://custom.example.com",
                isDefault = false,
            )
        }
        viewModel.save { saved = true }
        advanceUntilIdle()

        assertThat(saved).isTrue()
        assertThat(providersRepository.getProviders().any { it.name == "custom" && it.baseUrl == "https://custom.example.com" }).isTrue()
    }

    @Test
    fun accountDetailDeletesAccountAndClearsSelection() = runViewModelTest {
        val accountsRepository = inMemoryAccountsRepository()
        val providersRepository = inMemoryProvidersRepository()
        val selectedAccountStore = SelectedAccountStore().apply {
            save(UUID.fromString(samplePendingEnrollment().accountId))
        }
        val viewModel = AccountDetailViewModel(
            selectedAccountStore = selectedAccountStore,
            accountsRepository = accountsRepository,
            providersRepository = providersRepository,
            accountManagementService = FakeAccountManagementService(),
            biometricCertificateService = FakeBiometricCertificateService(),
        )
        var deleted = false

        advanceUntilIdle()
        viewModel.delete { deleted = true }
        advanceUntilIdle()

        assertThat(deleted).isTrue()
        assertThat(selectedAccountStore.read()).isNull()
        assertThat(accountsRepository.getAccount(UUID.fromString(samplePendingEnrollment().accountId))).isNull()
    }

    @Test
    fun accountDetailRequiresPinBeforeEnablingBiometric() = runViewModelTest {
        val selectedAccountStore = SelectedAccountStore().apply {
            save(UUID.fromString(samplePendingEnrollment().accountId))
        }
        val accountsRepository = inMemoryAccountsRepository().also {
            it.updateAccount(it.getAccounts().first().copy(isConfirmed = true, pinLength = 4))
        }
        val viewModel = AccountDetailViewModel(
            selectedAccountStore = selectedAccountStore,
            accountsRepository = accountsRepository,
            providersRepository = inMemoryProvidersRepository(),
            accountManagementService = FakeAccountManagementService(),
            biometricCertificateService = FakeBiometricCertificateService(),
        )

        advanceUntilIdle()
        viewModel.onBiometricPinChanged("12")
        viewModel.enableBiometric()

        assertThat(viewModel.uiState.value.biometricPinError).isEqualTo("Invalid Pin")
    }

    @Test
    fun accountDetailEnablesBiometricAndUpdatesAccount() = runViewModelTest {
        val selectedAccountStore = SelectedAccountStore().apply {
            save(UUID.fromString(samplePendingEnrollment().accountId))
        }
        val accountsRepository = inMemoryAccountsRepository().also {
            it.updateAccount(it.getAccounts().first().copy(isConfirmed = true, pinLength = 4))
        }
        val biometricService = FakeBiometricCertificateService()
        val viewModel = AccountDetailViewModel(
            selectedAccountStore = selectedAccountStore,
            accountsRepository = accountsRepository,
            providersRepository = inMemoryProvidersRepository(),
            accountManagementService = FakeAccountManagementService(),
            biometricCertificateService = biometricService,
        )

        advanceUntilIdle()
        viewModel.onBiometricPinChanged("1234")
        viewModel.enableBiometric()
        advanceUntilIdle()

        assertThat(biometricService.lastEnablePin).isEqualTo("1234")
        assertThat(viewModel.uiState.value.account?.hasFingerprint).isTrue()
    }

    @Test
    fun accountDevicesLoadsRegisteredDevicesAndMarksCurrent() = runViewModelTest {
        val selectedAccountStore = SelectedAccountStore().apply {
            save(UUID.fromString(samplePendingEnrollment().accountId))
        }
        val accountManagementService = FakeAccountManagementService().apply {
            devicesResult = Result.success(
                listOf(
                    ManagedDevice(
                        deviceId = "current-device",
                        platform = "Android",
                        creationTime = Instant.parse("2026-04-09T10:15:30Z"),
                        isCurrent = true,
                    ),
                    ManagedDevice(
                        deviceId = "old-device",
                        platform = "iOS",
                        creationTime = Instant.parse("2026-04-08T10:15:30Z"),
                        isCurrent = false,
                    ),
                )
            )
        }
        val viewModel = AccountDevicesViewModel(
            selectedAccountStore = selectedAccountStore,
            accountsRepository = inMemoryAccountsRepository(),
            accountManagementService = accountManagementService,
            currentDeviceIdProvider = { "current-device" },
        )

        advanceUntilIdle()

        assertThat(viewModel.uiState.value.devices).hasSize(2)
        assertThat(viewModel.uiState.value.devices.first().isCurrent).isTrue()
        assertThat(viewModel.uiState.value.responseError).isNull()
    }

    @Test
    fun accountDevicesDeleteRemovesRemoteDeviceAndRefreshesState() = runViewModelTest {
        val selectedAccountStore = SelectedAccountStore().apply {
            save(UUID.fromString(samplePendingEnrollment().accountId))
        }
        val accountManagementService = FakeAccountManagementService().apply {
            devicesResult = Result.success(
                listOf(
                    ManagedDevice(
                        deviceId = "current-device",
                        platform = "Android",
                        creationTime = Instant.parse("2026-04-09T10:15:30Z"),
                        isCurrent = true,
                    ),
                    ManagedDevice(
                        deviceId = "old-device",
                        platform = "Android",
                        creationTime = Instant.parse("2026-04-08T10:15:30Z"),
                        isCurrent = false,
                    ),
                )
            )
            deleteDeviceResult = Result.success(Unit)
        }
        val viewModel = AccountDevicesViewModel(
            selectedAccountStore = selectedAccountStore,
            accountsRepository = inMemoryAccountsRepository(),
            accountManagementService = accountManagementService,
            currentDeviceIdProvider = { "current-device" },
        )

        advanceUntilIdle()
        accountManagementService.devicesResult = Result.success(
            listOf(
                ManagedDevice(
                    deviceId = "current-device",
                    platform = "Android",
                    creationTime = Instant.parse("2026-04-09T10:15:30Z"),
                    isCurrent = true,
                )
            )
        )

        viewModel.deleteDevice("old-device")
        advanceUntilIdle()

        assertThat(viewModel.uiState.value.devices).hasSize(1)
        assertThat(viewModel.uiState.value.devices.single().deviceId).isEqualTo("current-device")
        assertThat(viewModel.uiState.value.responseError).isNull()
    }
}