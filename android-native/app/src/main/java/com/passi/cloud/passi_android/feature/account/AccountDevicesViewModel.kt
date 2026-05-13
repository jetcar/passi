package com.passi.cloud.passi_android.feature.account

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.domain.account.AccountManagementService
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.ManagedDevice
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class AccountDevicesUiState(
    val account: Account? = null,
    val devices: List<ManagedDevice> = emptyList(),
    val isLoading: Boolean = true,
    val isRemoving: Boolean = false,
    val responseError: String? = null,
)

class AccountDevicesViewModel(
    private val selectedAccountStore: SelectedAccountStore,
    private val accountsRepository: AccountsRepository,
    private val accountManagementService: AccountManagementService,
    private val currentDeviceIdProvider: () -> String,
) : ViewModel() {
    private val _uiState = MutableStateFlow(AccountDevicesUiState())
    val uiState: StateFlow<AccountDevicesUiState> = _uiState.asStateFlow()

    init {
        refresh()
    }

    fun refresh() {
        viewModelScope.launch {
            val accountId = selectedAccountStore.read()
            val account = accountId?.let { accountsRepository.getAccount(it) }
            if (account == null) {
                _uiState.value = AccountDevicesUiState(isLoading = false, responseError = "Account not found")
                return@launch
            }

            _uiState.value = _uiState.value.copy(account = account, isLoading = true, responseError = null)
            accountManagementService.getDevices(account, currentDeviceIdProvider())
                .onSuccess { devices ->
                    _uiState.value = _uiState.value.copy(
                        account = account,
                        devices = devices.sortedWith(compareByDescending<ManagedDevice> { it.isCurrent }.thenByDescending { it.creationTime }),
                        isLoading = false,
                        responseError = null,
                    )
                }
                .onFailure { error ->
                    _uiState.value = _uiState.value.copy(
                        account = account,
                        isLoading = false,
                        responseError = error.message ?: "Network error. Try again",
                    )
                }
        }
    }

    fun deleteDevice(deviceId: String) {
        val account = _uiState.value.account ?: return
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isRemoving = true, responseError = null)
            accountManagementService.deleteDevice(account, deviceId, currentDeviceIdProvider())
                .onSuccess {
                    _uiState.value = _uiState.value.copy(isRemoving = false)
                    refresh()
                }
                .onFailure { error ->
                    _uiState.value = _uiState.value.copy(
                        isRemoving = false,
                        responseError = error.message ?: "Network error. Try again",
                    )
                }
        }
    }

    companion object {
        fun factory(
            selectedAccountStore: SelectedAccountStore,
            accountsRepository: AccountsRepository,
            accountManagementService: AccountManagementService,
            currentDeviceIdProvider: () -> String,
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                AccountDevicesViewModel(
                    selectedAccountStore = selectedAccountStore,
                    accountsRepository = accountsRepository,
                    accountManagementService = accountManagementService,
                    currentDeviceIdProvider = currentDeviceIdProvider,
                )
            }
        }
    }
}