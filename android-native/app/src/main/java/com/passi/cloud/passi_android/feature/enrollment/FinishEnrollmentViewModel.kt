package com.passi.cloud.passi_android.feature.enrollment

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.local.PendingEnrollmentStore
import com.passi.cloud.passi_android.domain.enrollment.CertificateGenerator
import com.passi.cloud.passi_android.domain.enrollment.checkpoint
import com.passi.cloud.passi_android.domain.enrollment.certificate
import com.passi.cloud.passi_android.domain.enrollment.preparePendingDeviceKey
import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyRecoveryRequiredException
import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection
import kotlinx.coroutines.CancellationException
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import com.passi.cloud.passi_android.domain.service.EnrollmentService
import com.passi.cloud.passi_android.R
import com.passi.cloud.passi_android.localization.UiText
import com.passi.cloud.passi_android.localization.toUiText
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.util.UUID

data class FinishEnrollmentUiState(
    val email: String = "",
    val pin: String = "",
    val pinConfirmation: String = "",
    val pinError: UiText? = null,
    val pinConfirmationError: UiText? = null,
    val responseError: UiText? = null,
    val isMissingContext: Boolean = false,
    val isSubmitting: Boolean = false,
    val keyProtection: DeviceKeyProtection? = null,
    val isProtectionLocked: Boolean = false,
)

class FinishEnrollmentViewModel(
    private val pendingEnrollmentStore: PendingEnrollmentStore,
    private val accountsRepository: AccountsRepository,
    private val providersRepository: ProvidersRepository,
    private val enrollmentService: EnrollmentService,
    private val certificateGenerator: CertificateGenerator,
    private val deviceIdProvider: () -> String,
) : ViewModel() {
    private val _uiState = MutableStateFlow(FinishEnrollmentUiState())
    val uiState: StateFlow<FinishEnrollmentUiState> = _uiState.asStateFlow()

    init {
        val pending = pendingEnrollmentStore.read()
        _uiState.value = if (pending == null) {
            FinishEnrollmentUiState(
                isMissingContext = true,
                responseError = UiText.Resource(R.string.error_no_pending_signup),
            )
        } else {
            FinishEnrollmentUiState(email = pending.email)
        }
        if (pending != null) {
            viewModelScope.launch {
                val account = accountsRepository.getAccount(UUID.fromString(pending.accountId))
                val pendingKey = account?.pendingKey
                if (pendingKey != null) {
                    _uiState.value = _uiState.value.copy(
                        keyProtection = pendingKey.keyProtection
                            ?: DeviceKeyProtection.BIOMETRIC_OR_DEVICE_CREDENTIAL,
                        isProtectionLocked = true,
                    )
                }
            }
        }
    }

    fun onPinChanged(value: String) {
        _uiState.value = _uiState.value.copy(
            pin = value.take(12).filter(Char::isDigit),
            pinError = null,
            responseError = null,
        )
    }

    fun onPinConfirmationChanged(value: String) {
        _uiState.value = _uiState.value.copy(
            pinConfirmation = value.take(12).filter(Char::isDigit),
            pinConfirmationError = null,
            responseError = null,
        )
    }

    fun skip(onDone: () -> Unit) {
        finalizeEnrollment(pinLength = 0, onDone = onDone)
    }

    fun submit(onDone: () -> Unit) = finalizeEnrollment(pinLength = 0, onDone = onDone)

    fun onKeyProtectionSelected(protection: DeviceKeyProtection) {
        if (_uiState.value.isProtectionLocked) return
        _uiState.value = _uiState.value.copy(keyProtection = protection, responseError = null)
    }

    private fun finalizeEnrollment(pinLength: Int, onDone: () -> Unit) {
        val pending = pendingEnrollmentStore.read()
        if (pending == null) {
            _uiState.value = _uiState.value.copy(
                responseError = UiText.Resource(R.string.error_no_pending_signup),
                isMissingContext = true,
            )
            return
        }

        if (_uiState.value.isSubmitting) return
        if (pending.confirmationCode.isNullOrBlank()) {
            _uiState.value = _uiState.value.copy(responseError = UiText.Resource(R.string.error_confirmation_code_missing))
            return
        }
        val protection = _uiState.value.keyProtection
        if (protection == null) {
            _uiState.value = _uiState.value.copy(
                responseError = UiText.Resource(R.string.device_key_select_protection),
            )
            return
        }
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isSubmitting = true, responseError = null)
            var account = accountsRepository.getAccount(UUID.fromString(pending.accountId))
            if (account == null) {
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    responseError = UiText.Resource(R.string.error_pending_account_not_found),
                )
                return@launch
            }

            val provider = providersRepository.getProvider(account.providerId)
            if (provider == null) {
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    responseError = UiText.Resource(R.string.provider_not_found),
                )
                return@launch
            }

            val generatedCertificate = try {
                account = preparePendingDeviceKey(
                    account,
                    accountsRepository,
                    certificateGenerator,
                    protection,
                )
                val pendingKey = requireNotNull(account.pendingKey)
                _uiState.value = _uiState.value.copy(
                    keyProtection = pendingKey.keyProtection
                        ?: DeviceKeyProtection.BIOMETRIC_OR_DEVICE_CREDENTIAL,
                    isProtectionLocked = true,
                )
                pendingKey.certificate()
            } catch (cancelled: CancellationException) {
                throw cancelled
            } catch (error: Exception) {
                val refreshedAccount = try {
                    accountsRepository.getAccount(UUID.fromString(pending.accountId))
                } catch (_: Exception) {
                    null
                }
                val pendingKey = refreshedAccount?.pendingKey
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    keyProtection = pendingKey?.keyProtection
                        ?: if (pendingKey != null) DeviceKeyProtection.BIOMETRIC_OR_DEVICE_CREDENTIAL
                        else _uiState.value.keyProtection,
                    isProtectionLocked = pendingKey != null,
                    responseError = if (error is DeviceKeyRecoveryRequiredException) UiText.Resource(R.string.device_key_recovery_required)
                        else UiText.Resource(R.string.device_key_unavailable),
                )
                return@launch
            }

            val deviceId = deviceIdProvider()
            try {
                account = account.copy(pendingKey = requireNotNull(account.pendingKey).copy(submitted = true))
                accountsRepository.updateAccount(account)
            } catch (cancelled: CancellationException) {
                throw cancelled
            } catch (_: Exception) {
                _uiState.value = _uiState.value.copy(isSubmitting = false,
                    responseError = UiText.Resource(R.string.error_account_storage_retry))
                return@launch
            }
            val finalizeResult = enrollmentService.finalizeSignup(
                pendingEnrollment = pending,
                provider = provider,
                deviceId = deviceId,
                generatedCertificate = generatedCertificate,
            )

            finalizeResult.onSuccess { canonicalAccountId ->
                val finalizedAccount = account.copy(
                    id = canonicalAccountId,
                    deviceId = deviceId,
                    pinLength = 0,
                    keyAlias = generatedCertificate.keyAlias,
                    keySecurityLevel = generatedCertificate.keySecurityLevel,
                    keyProtection = generatedCertificate.keyProtection,
                    pendingKey = null,
                    hasFingerprint = false,
                    salt = generatedCertificate.salt,
                    privateCertBinary = generatedCertificate.privateCertBinary,
                    publicCertBinary = generatedCertificate.publicCertBinary,
                    thumbprint = generatedCertificate.thumbprint,
                    validFrom = generatedCertificate.validFrom,
                    validTo = generatedCertificate.validTo,
                )
                try {
                    accountsRepository.replaceAccount(account.id, finalizedAccount)
                } catch (_: Exception) {
                    _uiState.value = _uiState.value.copy(isSubmitting = false,
                        responseError = UiText.Resource(R.string.error_account_storage_retry))
                    return@onSuccess
                }
                pendingEnrollmentStore.clear()
                _uiState.value = _uiState.value.copy(isSubmitting = false)
                onDone()
            }.onFailure { error ->
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    responseError = error.toUiText(R.string.error_network),
                )
            }
        }
    }

    companion object {
        private const val MIN_PIN_LENGTH = 4

        fun factory(
            pendingEnrollmentStore: PendingEnrollmentStore,
            accountsRepository: AccountsRepository,
            providersRepository: ProvidersRepository,
            enrollmentService: EnrollmentService,
            certificateGenerator: CertificateGenerator,
            deviceIdProvider: () -> String,
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                FinishEnrollmentViewModel(
                    pendingEnrollmentStore = pendingEnrollmentStore,
                    accountsRepository = accountsRepository,
                    providersRepository = providersRepository,
                    enrollmentService = enrollmentService,
                    certificateGenerator = certificateGenerator,
                    deviceIdProvider = deviceIdProvider,
                )
            }
        }
    }
}
