package com.passi.cloud.passi_android.feature.enrollment

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.local.PendingEnrollmentStore
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import com.passi.cloud.passi_android.domain.service.EnrollmentService
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.util.UUID

data class ConfirmCodeUiState(
    val email: String = "",
    val code: String = "",
    val responseError: String? = null,
    val isSubmitting: Boolean = false,
    val isMissingContext: Boolean = false,
)

class ConfirmCodeViewModel(
    private val pendingEnrollmentStore: PendingEnrollmentStore,
    private val enrollmentService: EnrollmentService,
    private val accountsRepository: AccountsRepository,
    private val providersRepository: ProvidersRepository,
) : ViewModel() {
    private val requiredCodeLength = 6

    private val _uiState = MutableStateFlow(ConfirmCodeUiState())
    val uiState: StateFlow<ConfirmCodeUiState> = _uiState.asStateFlow()

    init {
        val pending = pendingEnrollmentStore.read()
        _uiState.value = if (pending == null) {
            ConfirmCodeUiState(isMissingContext = true, responseError = "No pending signup found")
        } else {
            ConfirmCodeUiState(email = pending.email)
        }
    }

    fun onCodeChanged(value: String) {
        _uiState.value = _uiState.value.copy(code = value.take(6).filter(Char::isDigit), responseError = null)
    }

    fun submit(onSuccess: () -> Unit) {
        val pending = pendingEnrollmentStore.read()
        if (pending == null) {
            _uiState.value = _uiState.value.copy(responseError = "No pending signup found", isMissingContext = true)
            return
        }
        if (_uiState.value.code.length != requiredCodeLength) {
            _uiState.value = _uiState.value.copy(
                responseError = "Enter the 6-digit confirmation code",
            )
            return
        }

        viewModelScope.launch {
            val provider = providersRepository.getProvider(UUID.fromString(pending.providerId))
            if (provider == null) {
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    responseError = "Provider not found",
                )
                return@launch
            }
            _uiState.value = _uiState.value.copy(isSubmitting = true, responseError = null)
            val result = enrollmentService.confirmSignupCode(
                accountId = pending.accountId,
                email = pending.email,
                code = _uiState.value.code,
                provider = provider,
            )
            result.onSuccess {
                val account = accountsRepository.getAccount(UUID.fromString(pending.accountId))
                if (account != null) {
                    accountsRepository.updateAccount(account.copy(isConfirmed = true))
                }
                pendingEnrollmentStore.save(pending.copy(confirmationCode = _uiState.value.code))
                _uiState.value = _uiState.value.copy(isSubmitting = false)
                onSuccess()
            }.onFailure { error ->
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    responseError = error.message ?: "Network error. Try again",
                )
            }
        }
    }

    companion object {
        fun factory(
            pendingEnrollmentStore: PendingEnrollmentStore,
            enrollmentService: EnrollmentService,
            accountsRepository: AccountsRepository,
            providersRepository: ProvidersRepository,
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                ConfirmCodeViewModel(
                    pendingEnrollmentStore = pendingEnrollmentStore,
                    enrollmentService = enrollmentService,
                    accountsRepository = accountsRepository,
                    providersRepository = providersRepository,
                )
            }
        }
    }
}