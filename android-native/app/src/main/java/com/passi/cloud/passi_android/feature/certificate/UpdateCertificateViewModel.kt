package com.passi.cloud.passi_android.feature.certificate

import android.os.Build
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.domain.biometric.BiometricCertificateService
import com.passi.cloud.passi_android.domain.certificate.CertificateRotationService
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.enrollment.supportsDeviceKeys
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
    val deviceKeySupported: Boolean = true,
)

class UpdateCertificateViewModel(
    private val selectedAccountStore: SelectedAccountStore,
    private val accountsRepository: AccountsRepository,
    private val certificateRotationService: CertificateRotationService,
    private val biometricCertificateService: BiometricCertificateService,
    private val deviceKeySupported: Boolean = true,
) : ViewModel() {
    private val _uiState = MutableStateFlow(UpdateCertificateUiState(deviceKeySupported = deviceKeySupported))
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
        if (!deviceKeySupported) {
            _uiState.value = _uiState.value.copy(responseError = UiText.Resource(R.string.device_key_android_version_required))
            return
        }
        val account = _uiState.value.account ?: return

        if (_uiState.value.isSubmitting) return
        if (account.keyAlias == null && account.pinLength > 0 && _uiState.value.oldPin.length < account.pinLength) {
            _uiState.value = _uiState.value.copy(oldPinError = UiText.Resource(R.string.error_invalid_old_pin))
            return
        }

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isSubmitting = true, responseError = null)
            certificateRotationService.rotateCertificate(
                account = account,
                oldPin = if (account.keyAlias == null) _uiState.value.oldPin else null,
                newPin = null,
                useBiometric = false,
            ).onSuccess { updatedAccount ->
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    account = updatedAccount,
                    oldPin = "",
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
            deviceKeySupported: Boolean = supportsDeviceKeys(Build.VERSION.SDK_INT),
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                UpdateCertificateViewModel(
                    selectedAccountStore = selectedAccountStore,
                    accountsRepository = accountsRepository,
                    certificateRotationService = certificateRotationService,
                    biometricCertificateService = biometricCertificateService,
                    deviceKeySupported = deviceKeySupported,
                )
            }
        }
    }
}
