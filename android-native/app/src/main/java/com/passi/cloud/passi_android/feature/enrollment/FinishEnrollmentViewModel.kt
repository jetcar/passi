package com.passi.cloud.passi_android.feature.enrollment

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.local.PendingEnrollmentStore
import com.passi.cloud.passi_android.domain.enrollment.CertificateGenerator
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import com.passi.cloud.passi_android.domain.service.EnrollmentService
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.util.UUID

data class FinishEnrollmentUiState(
    val email: String = "",
    val pin: String = "",
    val pinConfirmation: String = "",
    val pinError: String? = null,
    val pinConfirmationError: String? = null,
    val responseError: String? = null,
    val isMissingContext: Boolean = false,
    val isSubmitting: Boolean = false,
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
                responseError = "No pending signup found",
            )
        } else {
            FinishEnrollmentUiState(email = pending.email)
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

    fun submit(onDone: () -> Unit) {
        val pin = _uiState.value.pin
        val pinConfirmation = _uiState.value.pinConfirmation

        if (pin.length < MIN_PIN_LENGTH) {
            _uiState.value = _uiState.value.copy(
                pinError = "Pin1 should be min $MIN_PIN_LENGTH numbers",
            )
            return
        }

        if (pin != pinConfirmation) {
            _uiState.value = _uiState.value.copy(
                pinConfirmationError = "Pin2 doesn't match with Pin1",
            )
            return
        }

        finalizeEnrollment(pinLength = pin.length, onDone = onDone)
    }

    private fun finalizeEnrollment(pinLength: Int, onDone: () -> Unit) {
        val pending = pendingEnrollmentStore.read()
        if (pending == null) {
            _uiState.value = _uiState.value.copy(responseError = "No pending signup found", isMissingContext = true)
            return
        }

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isSubmitting = true, responseError = null)
            val account = accountsRepository.getAccount(UUID.fromString(pending.accountId))
            if (account == null) {
                _uiState.value = _uiState.value.copy(isSubmitting = false, responseError = "Pending account not found")
                return@launch
            }

            val provider = providersRepository.getProvider(account.providerId)
            if (provider == null) {
                _uiState.value = _uiState.value.copy(isSubmitting = false, responseError = "Provider not found")
                return@launch
            }

            val pin = if (pinLength == 0) null else _uiState.value.pin
            val generatedCertificate = runCatching {
                certificateGenerator.generate(account.email, pin)
            }.getOrElse { error ->
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    responseError = error.message ?: "Certificate generation failed",
                )
                return@launch
            }

            val deviceId = deviceIdProvider()
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
                    pinLength = pinLength,
                    salt = generatedCertificate.salt,
                    privateCertBinary = generatedCertificate.privateCertBinary,
                    publicCertBinary = generatedCertificate.publicCertBinary,
                    thumbprint = generatedCertificate.thumbprint,
                    validFrom = generatedCertificate.validFrom,
                    validTo = generatedCertificate.validTo,
                )
                if (canonicalAccountId == account.id) {
                    accountsRepository.updateAccount(finalizedAccount)
                } else {
                    // The server keeps a canonical GUID for an already-existing account and ignores
                    // the one this device generated. Move the local record onto the canonical GUID so
                    // login push notifications (keyed on it) resolve to this account instead of
                    // failing with "Account not found for this session".
                    accountsRepository.deleteAccount(account.id)
                    accountsRepository.savePendingAccount(finalizedAccount)
                }
                pendingEnrollmentStore.clear()
                _uiState.value = _uiState.value.copy(isSubmitting = false)
                onDone()
            }.onFailure { error ->
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    responseError = error.message ?: "Network error. Try again",
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