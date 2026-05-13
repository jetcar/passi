package com.passi.cloud.passi_android.feature.accounts

import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.auth.PendingSessionStore
import com.passi.cloud.passi_android.data.local.PendingEnrollmentStore
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.domain.account.AccountManagementService
import com.passi.cloud.passi_android.domain.auth.AuthSessionService
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import java.util.UUID
import com.passi.cloud.passi_android.domain.service.PendingEnrollment
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock

data class AccountsUiState(
    val versionLabel: String = "",
    val providers: List<Provider> = emptyList(),
    val accounts: List<Account> = emptyList(),
    val syncError: String? = null,
    val isSyncing: Boolean = false,
    val isForegroundPolling: Boolean = false,
    val isDeleteMode: Boolean = false,
    val revealedDeleteAccountIds: Set<java.util.UUID> = emptySet(),
    val isDeletingAccountIds: Set<java.util.UUID> = emptySet(),
    val deleteError: String? = null,
)

class AccountsViewModel(
    private val accountsRepository: AccountsRepository,
    private val providersRepository: ProvidersRepository,
    private val authSessionService: AuthSessionService,
    private val pendingSessionStore: PendingSessionStore,
    private val pendingEnrollmentStore: PendingEnrollmentStore,
    private val selectedAccountStore: SelectedAccountStore,
    private val accountManagementService: AccountManagementService,
) : ViewModel() {
    private val _uiState = MutableStateFlow(AccountsUiState())
    private val syncMutex = Mutex()
    private var lastOpenedPendingSessionId: UUID? = null

    val uiState: StateFlow<AccountsUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            combine(
                providersRepository.observeProviders(),
                accountsRepository.observeAccounts(),
            ) { providers, accounts ->
                AccountsUiState(
                    providers = providers,
                    accounts = accounts,
                )
            }.collect { state ->
                _uiState.value = _uiState.value.copy(
                    providers = state.providers,
                    accounts = state.accounts,
                )
            }
        }
    }

    fun sync(onPendingSession: () -> Unit) {
        viewModelScope.launch {
            runSync(
                updateBusyState = { isBusy ->
                    _uiState.value = _uiState.value.copy(isSyncing = isBusy, syncError = if (isBusy) null else _uiState.value.syncError)
                },
                onPendingSession = onPendingSession,
                surfaceErrors = true,
            )
        }
    }

    fun foregroundPoll(onPendingSession: () -> Unit) {
        if (_uiState.value.isForegroundPolling) {
            return
        }

        viewModelScope.launch {
            runSync(
                updateBusyState = { isBusy ->
                    _uiState.value = _uiState.value.copy(isForegroundPolling = isBusy)
                },
                onPendingSession = onPendingSession,
                surfaceErrors = false,
            )
        }
    }

    private suspend fun runSync(
        updateBusyState: (Boolean) -> Unit,
        onPendingSession: () -> Unit,
        surfaceErrors: Boolean,
    ) {
        if (syncMutex.isLocked) {
            return
        }

        syncMutex.withLock {
            updateBusyState(true)
            val result = authSessionService.syncAndPollPendingSession()
            result.onSuccess { session ->
                pendingSessionStore.save(session)
                updateBusyState(false)
                if (surfaceErrors) {
                    _uiState.value = _uiState.value.copy(syncError = null)
                }
                if (session == null) {
                    lastOpenedPendingSessionId = null
                    return@onSuccess
                }

                if (lastOpenedPendingSessionId != session.sessionId) {
                    lastOpenedPendingSessionId = session.sessionId
                    onPendingSession()
                }
            }.onFailure { error ->
                updateBusyState(false)
                if (surfaceErrors) {
                    _uiState.value = _uiState.value.copy(
                        syncError = error.message ?: "Network error. Try again",
                    )
                }
            }
        }
    }

    fun showMainError(message: String) {
        _uiState.value = _uiState.value.copy(syncError = message)
    }

    fun openAccount(account: Account, onOpenAccount: () -> Unit, onResumeEnrollment: () -> Unit) {
        if (!account.isConfirmed) {
            pendingEnrollmentStore.save(
                PendingEnrollment(
                    accountId = account.id.toString(),
                    email = account.email,
                    providerId = account.providerId.toString(),
                )
            )
            onResumeEnrollment()
            return
        }
        selectedAccountStore.save(account.id)
        onOpenAccount()
    }

    fun toggleDeleteMode() {
        val nextDeleteMode = !_uiState.value.isDeleteMode
        _uiState.value = _uiState.value.copy(
            isDeleteMode = nextDeleteMode,
            revealedDeleteAccountIds = emptySet(),
            deleteError = null,
        )
    }

    fun toggleDeleteForAccount(accountId: java.util.UUID) {
        if (!_uiState.value.isDeleteMode) {
            return
        }

        val revealed = _uiState.value.revealedDeleteAccountIds.toMutableSet()
        if (!revealed.add(accountId)) {
            revealed.remove(accountId)
        }

        _uiState.value = _uiState.value.copy(
            revealedDeleteAccountIds = revealed,
            deleteError = null,
        )
    }

    fun deleteAccount(account: Account) {
        viewModelScope.launch {
            val deletingAccountIds = _uiState.value.isDeletingAccountIds + account.id
            _uiState.value = _uiState.value.copy(
                isDeletingAccountIds = deletingAccountIds,
                deleteError = null,
            )

            val serverResult = accountManagementService.deleteAccount(account)
            accountsRepository.deleteAccount(account.id)
            _uiState.value = _uiState.value.copy(
                isDeletingAccountIds = _uiState.value.isDeletingAccountIds - account.id,
                revealedDeleteAccountIds = _uiState.value.revealedDeleteAccountIds - account.id,
                deleteError = serverResult.exceptionOrNull()?.message,
            )
        }
    }

    companion object {
        fun factory(
            accountsRepository: AccountsRepository,
            providersRepository: ProvidersRepository,
            authSessionService: AuthSessionService,
            pendingSessionStore: PendingSessionStore,
            pendingEnrollmentStore: PendingEnrollmentStore,
            selectedAccountStore: SelectedAccountStore,
            accountManagementService: AccountManagementService,
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                AccountsViewModel(
                    accountsRepository = accountsRepository,
                    providersRepository = providersRepository,
                    authSessionService = authSessionService,
                    pendingSessionStore = pendingSessionStore,
                    pendingEnrollmentStore = pendingEnrollmentStore,
                    selectedAccountStore = selectedAccountStore,
                    accountManagementService = accountManagementService,
                )
            }
        }
    }
}