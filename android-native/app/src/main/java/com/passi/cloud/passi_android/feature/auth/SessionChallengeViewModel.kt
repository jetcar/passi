package com.passi.cloud.passi_android.feature.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.auth.PendingSessionStore
import com.passi.cloud.passi_android.domain.auth.AuthSessionService
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.ConfirmationColor
import com.passi.cloud.passi_android.domain.model.NotificationSession
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class SessionChallengeUiState(
    val session: NotificationSession? = null,
    val account: Account? = null,
    val colorOptions: List<ConfirmationColor> = emptyList(),
    /** 2-digit choices (correct + decoys); empty when the server sent no number and colors are used. */
    val numberOptions: List<Int> = emptyList(),
    val responseError: String? = null,
    val colorError: String? = null,
    val isButtonEnabled: Boolean = false,
    val isLoading: Boolean = false,
)

class SessionChallengeViewModel(
    private val pendingSessionStore: PendingSessionStore,
    private val accountsRepository: AccountsRepository,
    private val authSessionService: AuthSessionService,
) : ViewModel() {
    private val _uiState = MutableStateFlow(SessionChallengeUiState())
    val uiState: StateFlow<SessionChallengeUiState> = _uiState.asStateFlow()

    init {
        val session = pendingSessionStore.read()
        if (session == null) {
            _uiState.value = SessionChallengeUiState(responseError = "No pending session found")
        } else {
            viewModelScope.launch {
                val account = accountsRepository.getAccount(session.accountId)
                if (account == null) {
                    _uiState.value = SessionChallengeUiState(
                        session = session,
                        responseError = "Account not found for this session",
                    )
                } else {
                    _uiState.value = SessionChallengeUiState(
                        session = session,
                        account = account,
                        colorOptions = buildColorOptions(session.confirmationColor),
                        numberOptions = session.confirmationNumber?.let(::buildNumberOptions).orEmpty(),
                        isButtonEnabled = true,
                    )
                }
            }
        }
    }

    fun onColorSelected(
        color: ConfirmationColor,
        onRequirePin: () -> Unit,
        onRequireBiometric: () -> Unit,
        onAuthorized: () -> Unit,
    ) = onChallengeAnswered(
        isCorrect = { it.confirmationColor == color },
        errorMessage = "Invalid confirmation color. Go back and try again.",
        onRequirePin = onRequirePin,
        onRequireBiometric = onRequireBiometric,
        onAuthorized = onAuthorized,
    )

    fun onNumberSelected(
        number: Int,
        onRequirePin: () -> Unit,
        onRequireBiometric: () -> Unit,
        onAuthorized: () -> Unit,
    ) = onChallengeAnswered(
        isCorrect = { it.confirmationNumber == number },
        errorMessage = "Invalid confirmation number. Go back and try again.",
        onRequirePin = onRequirePin,
        onRequireBiometric = onRequireBiometric,
        onAuthorized = onAuthorized,
    )

    private fun onChallengeAnswered(
        isCorrect: (NotificationSession) -> Boolean,
        errorMessage: String,
        onRequirePin: () -> Unit,
        onRequireBiometric: () -> Unit,
        onAuthorized: () -> Unit,
    ) {
        val session = _uiState.value.session
        if (session == null) {
            _uiState.value = _uiState.value.copy(responseError = "No pending session found")
            return
        }

        val account = _uiState.value.account
        if (account == null) {
            _uiState.value = _uiState.value.copy(responseError = "Account not found for this session")
            return
        }

        if (!isCorrect(session)) {
            _uiState.value = _uiState.value.copy(
                isButtonEnabled = false,
                colorError = errorMessage,
            )
            return
        }

        if (account.pinLength > 0) {
            onRequirePin()
            return
        }

        if (account.hasFingerprint) {
            onRequireBiometric()
            return
        }

        authorizeWithoutPin(onAuthorized)
    }

    fun onBiometricPromptError(message: String) {
        _uiState.value = _uiState.value.copy(responseError = message, isLoading = false)
    }

    fun authorizeWithBiometric(onAuthorized: () -> Unit) {
        val session = _uiState.value.session ?: return
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, responseError = null)
            authSessionService.authorizeWithBiometric(session)
                .onSuccess {
                    pendingSessionStore.clear()
                    _uiState.value = _uiState.value.copy(isLoading = false)
                    onAuthorized()
                }
                .onFailure { error ->
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        responseError = error.message ?: "Network error. Try again",
                    )
                }
        }
    }

    private fun authorizeWithoutPin(onAuthorized: () -> Unit) {
        val session = _uiState.value.session ?: return

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, responseError = null)
            authSessionService.authorize(session, pin = null)
                .onSuccess {
                    pendingSessionStore.clear()
                    _uiState.value = _uiState.value.copy(isLoading = false)
                    onAuthorized()
                }
                .onFailure { error ->
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        responseError = error.message ?: "Network error. Try again",
                    )
                }
        }
    }

    fun cancel(onCancelled: (String?) -> Unit) {
        val session = _uiState.value.session ?: run {
            onCancelled("No pending session found")
            return
        }

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, responseError = null)
            val cancelResult = authSessionService.cancel(session)
            pendingSessionStore.clear()
            _uiState.value = _uiState.value.copy(isLoading = false)
            onCancelled(cancelResult.exceptionOrNull()?.message)
        }
    }

    private fun buildNumberOptions(correctNumber: Int): List<Int> {
        val decoys = (10..99).filterNot { it == correctNumber }.shuffled().take(2).toMutableList()
        decoys.add((0..2).random(), correctNumber)
        return decoys
    }

    private fun buildColorOptions(correctColor: ConfirmationColor): List<ConfirmationColor> {
        val remaining = ConfirmationColor.entries.filterNot { it == correctColor }.shuffled().take(2).toMutableList()
        remaining.add((0..2).random(), correctColor)
        return remaining
    }

    companion object {
        fun factory(
            pendingSessionStore: PendingSessionStore,
            accountsRepository: AccountsRepository,
            authSessionService: AuthSessionService,
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                SessionChallengeViewModel(
                    pendingSessionStore = pendingSessionStore,
                    accountsRepository = accountsRepository,
                    authSessionService = authSessionService,
                )
            }
        }
    }
}