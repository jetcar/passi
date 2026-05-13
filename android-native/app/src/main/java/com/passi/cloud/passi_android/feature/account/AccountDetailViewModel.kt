package com.passi.cloud.passi_android.feature.account

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.domain.account.AccountManagementService
import com.passi.cloud.passi_android.domain.biometric.BiometricCertificateService
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class AccountDetailUiState(
    val account: Account? = null,
    val provider: Provider? = null,
    val biometricPin: String = "",
    val biometricPinError: String? = null,
    val responseError: String? = null,
    val isDeleting: Boolean = false,
    val isEnablingBiometric: Boolean = false,
)

class AccountDetailViewModel(
    private val selectedAccountStore: SelectedAccountStore,
    private val accountsRepository: AccountsRepository,
    private val providersRepository: ProvidersRepository,
    private val accountManagementService: AccountManagementService,
    private val biometricCertificateService: BiometricCertificateService,
) : ViewModel() {
    private val _uiState = MutableStateFlow(AccountDetailUiState())
    val uiState: StateFlow<AccountDetailUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            val accountId = selectedAccountStore.read()
            val account = accountId?.let { accountsRepository.getAccount(it) }
            val provider = account?.providerId?.let { providersRepository.getProvider(it) }
            _uiState.value = AccountDetailUiState(account = account, provider = provider)
        }
    }

    fun delete(onDeleted: () -> Unit) {
        val account = _uiState.value.account ?: return
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isDeleting = true, responseError = null)
            accountManagementService.deleteAccount(account)
                .onSuccess {
                    accountsRepository.deleteAccount(account.id)
                    selectedAccountStore.clear()
                    _uiState.value = _uiState.value.copy(isDeleting = false)
                    onDeleted()
                }
                .onFailure { error ->
                    _uiState.value = _uiState.value.copy(
                        isDeleting = false,
                        responseError = error.message ?: "Network error. Try again",
                    )
                }
        }
    }

    fun onBiometricPinChanged(value: String) {
        _uiState.value = _uiState.value.copy(
            biometricPin = value.filter(Char::isDigit),
            biometricPinError = null,
            responseError = null,
        )
    }

    fun onBiometricPromptError(message: String) {
        _uiState.value = _uiState.value.copy(responseError = message)
    }

    fun enableBiometric() {
        val account = _uiState.value.account ?: return
        if (account.hasFingerprint) {
            return
        }
        if (account.pinLength > 0 && _uiState.value.biometricPin.length < 4) {
            _uiState.value = _uiState.value.copy(biometricPinError = "Invalid Pin")
            return
        }

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isEnablingBiometric = true, responseError = null)
            biometricCertificateService.enableBiometric(
                account = account,
                pin = if (account.pinLength > 0) _uiState.value.biometricPin else null,
            ).onSuccess { updatedAccount ->
                accountsRepository.updateAccount(updatedAccount)
                _uiState.value = _uiState.value.copy(
                    account = updatedAccount,
                    biometricPin = "",
                    biometricPinError = null,
                    responseError = null,
                    isEnablingBiometric = false,
                )
            }.onFailure { error ->
                val message = error.message ?: "Biometric setup failed"
                _uiState.value = _uiState.value.copy(
                    isEnablingBiometric = false,
                    responseError = if (message == "Invalid Pin") null else message,
                    biometricPinError = if (message == "Invalid Pin") message else null,
                )
            }
        }
    }

    companion object {
        fun factory(
            selectedAccountStore: SelectedAccountStore,
            accountsRepository: AccountsRepository,
            providersRepository: ProvidersRepository,
            accountManagementService: AccountManagementService,
            biometricCertificateService: BiometricCertificateService,
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                AccountDetailViewModel(
                    selectedAccountStore = selectedAccountStore,
                    accountsRepository = accountsRepository,
                    providersRepository = providersRepository,
                    accountManagementService = accountManagementService,
                    biometricCertificateService = biometricCertificateService,
                )
            }
        }
    }
}