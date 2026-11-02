package com.passi.cloud.passi_android.feature.certificate

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.domain.biometric.BiometricCertificateService
import com.passi.cloud.passi_android.domain.certificate.CertificateRotationService
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import com.passi.cloud.passi_android.R
import com.passi.cloud.passi_android.localization.UiText
import com.passi.cloud.passi_android.localization.toUiText
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class UpdateCertificateUiState(
    val account: Account? = null,
    val oldPin: String = "",
    val newPin: String = "",
    val confirmPin: String = "",
    val useBiometricForCurrentCertificate: Boolean = false,
    val oldPinError: UiText? = null,
    val newPinError: UiText? = null,
    val confirmPinError: UiText? = null,
    val responseError: UiText? = null,
    val isSubmitting: Boolean = false,
)

class UpdateCertificateViewModel(
    private val selectedAccountStore: SelectedAccountStore,
    private val accountsRepository: AccountsRepository,
    private val certificateRotationService: CertificateRotationService,
    private val biometricCertificateService: BiometricCertificateService,
) : ViewModel() {
    private val _uiState = MutableStateFlow(UpdateCertificateUiState())
    val uiState: StateFlow<UpdateCertificateUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            val accountId = selectedAccountStore.read()
            val account = accountId?.let { accountsRepository.getAccount(it) }
            _uiState.value = _uiState.value.copy(account = account)
        }
    }

    fun onOldPinChanged(value: String) {
        _uiState.value = _uiState.value.copy(oldPin = digits(value), oldPinError = null, responseError = null)
    }

    fun onNewPinChanged(value: String) {
        _uiState.value = _uiState.value.copy(newPin = digits(value), newPinError = null, responseError = null)
    }

    fun onConfirmPinChanged(value: String) {
        _uiState.value = _uiState.value.copy(confirmPin = digits(value), confirmPinError = null, responseError = null)
    }

    fun onBiometricPromptError(message: String) {
        _uiState.value = _uiState.value.copy(responseError = UiText.Dynamic(message))
    }

    fun confirmBiometricForCurrentCertificate() {
        _uiState.value = _uiState.value.copy(
            useBiometricForCurrentCertificate = true,
            oldPinError = null,
            responseError = null,
        )
    }

    fun rotate(onDone: () -> Unit) {
        val account = _uiState.value.account ?: return

        if (account.hasFingerprint && !_uiState.value.useBiometricForCurrentCertificate && account.pinLength == 0) {
            _uiState.value = _uiState.value.copy(
                responseError = UiText.Resource(R.string.error_biometric_required_for_rotation),
            )
            return
        }

        if (account.pinLength > 0 && !_uiState.value.useBiometricForCurrentCertificate && _uiState.value.oldPin.length < 4) {
            _uiState.value = _uiState.value.copy(oldPinError = UiText.Resource(R.string.error_invalid_old_pin))
            return
        }

        val wantsPin = _uiState.value.newPin.isNotEmpty() || _uiState.value.confirmPin.isNotEmpty()
        if (wantsPin) {
            if (_uiState.value.newPin.length < 4) {
                _uiState.value = _uiState.value.copy(
                    newPinError = UiText.Resource(R.string.error_pin_length, listOf(4)),
                )
                return
            }
            if (_uiState.value.newPin != _uiState.value.confirmPin) {
                _uiState.value = _uiState.value.copy(
                    confirmPinError = UiText.Resource(R.string.error_pin_mismatch),
                )
                return
            }
        }

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isSubmitting = true, responseError = null)
            certificateRotationService.rotateCertificate(
                account = account,
                oldPin = if (account.pinLength > 0 && !_uiState.value.useBiometricForCurrentCertificate) _uiState.value.oldPin else null,
                newPin = if (wantsPin) _uiState.value.newPin else null,
                useBiometric = _uiState.value.useBiometricForCurrentCertificate,
            ).onSuccess { updatedAccount ->
                val accountWithBiometric = if (updatedAccount.hasFingerprint) {
                    biometricCertificateService.enableBiometric(
                        account = updatedAccount,
                        pin = if (wantsPin) _uiState.value.newPin else null,
                    ).getOrDefault(updatedAccount)
                } else {
                    updatedAccount
                }
                accountsRepository.updateAccount(accountWithBiometric)
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    account = accountWithBiometric,
                    useBiometricForCurrentCertificate = false,
                )
                onDone()
            }.onFailure { error ->
                val isInvalidOldPin = error.message == "Invalid old pin" || error.message == "Invalid old PIN"
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    responseError = if (isInvalidOldPin) null else error.toUiText(R.string.error_network),
                    oldPinError = if (isInvalidOldPin) UiText.Resource(R.string.error_invalid_old_pin) else null,
                )
            }
        }
    }

    private fun digits(value: String): String = value.filter(Char::isDigit)

    companion object {
        fun factory(
            selectedAccountStore: SelectedAccountStore,
            accountsRepository: AccountsRepository,
            certificateRotationService: CertificateRotationService,
            biometricCertificateService: BiometricCertificateService,
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                UpdateCertificateViewModel(
                    selectedAccountStore = selectedAccountStore,
                    accountsRepository = accountsRepository,
                    certificateRotationService = certificateRotationService,
                    biometricCertificateService = biometricCertificateService,
                )
            }
        }
    }
}
