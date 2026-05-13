package com.passi.cloud.passi_android.feature.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.auth.PendingSessionStore
import com.passi.cloud.passi_android.domain.auth.AuthSessionService
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class SessionPinUiState(
    val account: Account? = null,
    val email: String = "",
    val pin: String = "",
    val pinLength: Int = 4,
    val hasFingerprint: Boolean = false,
    val responseError: String? = null,
    val isSubmitting: Boolean = false,
)

class SessionPinViewModel(
    private val pendingSessionStore: PendingSessionStore,
    private val accountsRepository: AccountsRepository,
    private val authSessionService: AuthSessionService,
) : ViewModel() {
    private val _uiState = MutableStateFlow(SessionPinUiState())
    val uiState: StateFlow<SessionPinUiState> = _uiState.asStateFlow()

    init {
        val session = pendingSessionStore.read()
        if (session == null) {
            _uiState.value = SessionPinUiState(responseError = "No pending session found")
        } else {
            viewModelScope.launch {
                val account = accountsRepository.getAccount(session.accountId)
                _uiState.value = SessionPinUiState(
                    account = account,
                    email = account?.email.orEmpty(),
                    pinLength = account?.pinLength ?: 4,
                    hasFingerprint = account?.hasFingerprint == true,
                )
            }
        }
    }

    fun onPinChanged(value: String) {
        _uiState.value = _uiState.value.copy(pin = value.filter(Char::isDigit), responseError = null)
    }

    fun submit(onAuthorized: () -> Unit) {
        val session = pendingSessionStore.read() ?: run {
            _uiState.value = _uiState.value.copy(responseError = "No pending session found")
            return
        }

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isSubmitting = true)
            authSessionService.authorize(session, _uiState.value.pin)
                .onSuccess {
                    pendingSessionStore.clear()
                    _uiState.value = _uiState.value.copy(isSubmitting = false)
                    onAuthorized()
                }
                .onFailure { error ->
                    _uiState.value = _uiState.value.copy(
                        isSubmitting = false,
                        responseError = error.message ?: "Network error. Try again",
                    )
                }
        }
    }

    fun onBiometricPromptError(message: String) {
        _uiState.value = _uiState.value.copy(responseError = message, isSubmitting = false)
    }

    fun authorizeWithBiometric(onAuthorized: () -> Unit) {
        val session = pendingSessionStore.read() ?: run {
            _uiState.value = _uiState.value.copy(responseError = "No pending session found")
            return
        }

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isSubmitting = true, responseError = null)
            authSessionService.authorizeWithBiometric(session)
                .onSuccess {
                    pendingSessionStore.clear()
                    _uiState.value = _uiState.value.copy(isSubmitting = false)
                    onAuthorized()
                }
                .onFailure { error ->
                    _uiState.value = _uiState.value.copy(
                        isSubmitting = false,
                        responseError = error.message ?: "Network error. Try again",
                    )
                }
        }
    }

    fun cancel(onCancelled: () -> Unit) {
        val session = pendingSessionStore.read() ?: run {
            onCancelled()
            return
        }

        viewModelScope.launch {
            authSessionService.cancel(session)
            pendingSessionStore.clear()
            onCancelled()
        }
    }

    companion object {
        fun factory(
            pendingSessionStore: PendingSessionStore,
            accountsRepository: AccountsRepository,
            authSessionService: AuthSessionService,
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                SessionPinViewModel(
                    pendingSessionStore = pendingSessionStore,
                    accountsRepository = accountsRepository,
                    authSessionService = authSessionService,
                )
            }
        }
    }
}