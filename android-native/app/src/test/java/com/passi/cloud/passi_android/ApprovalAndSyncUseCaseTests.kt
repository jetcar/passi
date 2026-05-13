package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.data.auth.PendingSessionStore
import com.passi.cloud.passi_android.data.local.PendingEnrollmentStore
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.feature.accounts.AccountsViewModel
import com.passi.cloud.passi_android.feature.auth.SessionChallengeViewModel
import com.passi.cloud.passi_android.feature.auth.SessionPinViewModel
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.advanceUntilIdle
import org.junit.Test
import java.util.UUID

@OptIn(ExperimentalCoroutinesApi::class)
class ApprovalAndSyncUseCaseTests : CoroutineViewModelTest() {
    @Test
    fun openAccountStoresSelectionAndTriggersNavigation() = runViewModelTest {
        val selectedAccountStore = SelectedAccountStore()
        val pendingEnrollmentStore = PendingEnrollmentStore()
        val accountsRepository = inMemoryAccountsRepository()
        val confirmedAccount = accountsRepository.getAccounts().first().copy(isConfirmed = true)
        accountsRepository.updateAccount(confirmedAccount)
        val viewModel = AccountsViewModel(
            accountsRepository = accountsRepository,
            providersRepository = inMemoryProvidersRepository(),
            authSessionService = FakeAuthSessionService(),
            pendingSessionStore = PendingSessionStore(),
            pendingEnrollmentStore = pendingEnrollmentStore,
            selectedAccountStore = selectedAccountStore,
            accountManagementService = FakeAccountManagementService(),
        )
        var opened = false

        viewModel.openAccount(confirmedAccount, onOpenAccount = { opened = true }, onResumeEnrollment = {})

        assertThat(opened).isTrue()
        assertThat(selectedAccountStore.read()).isEqualTo(confirmedAccount.id)
    }

    @Test
    fun openUnconfirmedAccount_triggersResumeEnrollment() = runViewModelTest {
        val accountsRepository = inMemoryAccountsRepository()
        val unconfirmedAccount = accountsRepository.getAccounts().first()
        val pendingEnrollmentStore = PendingEnrollmentStore()
        val viewModel = AccountsViewModel(
            accountsRepository = accountsRepository,
            providersRepository = inMemoryProvidersRepository(),
            authSessionService = FakeAuthSessionService(),
            pendingSessionStore = PendingSessionStore(),
            pendingEnrollmentStore = pendingEnrollmentStore,
            selectedAccountStore = SelectedAccountStore(),
            accountManagementService = FakeAccountManagementService(),
        )
        var resumed = false

        viewModel.openAccount(unconfirmedAccount, onOpenAccount = {}, onResumeEnrollment = { resumed = true })

        assertThat(resumed).isTrue()
        assertThat(pendingEnrollmentStore.read()?.accountId).isEqualTo(unconfirmedAccount.id.toString())
    }

    @Test
    fun accountsSyncStoresPendingSessionAndTriggersNavigation() = runViewModelTest {
        val authService = FakeAuthSessionService().apply {
            syncAndPollPendingSessionResult = Result.success(sampleSession())
        }
        val sessionStore = PendingSessionStore()
        val pendingEnrollmentStore = PendingEnrollmentStore()
        val viewModel = AccountsViewModel(
            accountsRepository = inMemoryAccountsRepository(),
            providersRepository = inMemoryProvidersRepository(),
            authSessionService = authService,
            pendingSessionStore = sessionStore,
            pendingEnrollmentStore = pendingEnrollmentStore,
            selectedAccountStore = SelectedAccountStore(),
            accountManagementService = FakeAccountManagementService(),
        )
        var opened = false

        viewModel.sync { opened = true }
        advanceUntilIdle()

        assertThat(opened).isTrue()
        assertThat(sessionStore.read()).isNotNull()
    }

    @Test
    fun sessionChallengeRejectsWrongColor() = runViewModelTest {
        val accountsRepository = inMemoryAccountsRepository()
        val sessionStore = PendingSessionStore().apply { save(sampleSession(confirmationColor = com.passi.cloud.passi_android.domain.model.ConfirmationColor.BLUE)) }
        val viewModel = SessionChallengeViewModel(
            pendingSessionStore = sessionStore,
            accountsRepository = accountsRepository,
            authSessionService = FakeAuthSessionService(),
        )

        advanceUntilIdle()
        viewModel.onColorSelected(
            color = com.passi.cloud.passi_android.domain.model.ConfirmationColor.RED,
            onRequirePin = {},
            onRequireBiometric = {},
            onAuthorized = {},
        )

        assertThat(viewModel.uiState.value.colorError).isEqualTo("Invalid confirmation color. Go back and try again.")
        assertThat(viewModel.uiState.value.isButtonEnabled).isFalse()
    }

    @Test
    fun sessionChallengeRoutesPinProtectedAccountToPinFlow() = runViewModelTest {
        val accountsRepository = inMemoryAccountsRepository().also {
            it.updateAccount(it.getAccounts().first().copy(isConfirmed = true, pinLength = 4))
        }
        val viewModel = SessionChallengeViewModel(
            pendingSessionStore = PendingSessionStore().apply { save(sampleSession()) },
            accountsRepository = accountsRepository,
            authSessionService = FakeAuthSessionService(),
        )
        var pinRequested = false

        advanceUntilIdle()
        viewModel.onColorSelected(
            color = com.passi.cloud.passi_android.domain.model.ConfirmationColor.BLUE,
            onRequirePin = { pinRequested = true },
            onRequireBiometric = {},
            onAuthorized = {},
        )

        assertThat(pinRequested).isTrue()
    }

    @Test
    fun sessionChallengeRoutesBiometricAccountToBiometricFlow() = runViewModelTest {
        val accountsRepository = inMemoryAccountsRepository().also {
            it.updateAccount(it.getAccounts().first().copy(isConfirmed = true, pinLength = 0, hasFingerprint = true))
        }
        val viewModel = SessionChallengeViewModel(
            pendingSessionStore = PendingSessionStore().apply { save(sampleSession()) },
            accountsRepository = accountsRepository,
            authSessionService = FakeAuthSessionService(),
        )
        var biometricRequested = false

        advanceUntilIdle()
        viewModel.onColorSelected(
            color = com.passi.cloud.passi_android.domain.model.ConfirmationColor.BLUE,
            onRequirePin = {},
            onRequireBiometric = { biometricRequested = true },
            onAuthorized = {},
        )

        assertThat(biometricRequested).isTrue()
    }

    @Test
    fun sessionPinSubmitsPinAuthorization() = runViewModelTest {
        val sessionStore = PendingSessionStore().apply { save(sampleSession()) }
        val authService = FakeAuthSessionService()
        val accountsRepository = inMemoryAccountsRepository().also {
            it.updateAccount(it.getAccounts().first().copy(isConfirmed = true, pinLength = 4))
        }
        val viewModel = SessionPinViewModel(
            pendingSessionStore = sessionStore,
            accountsRepository = accountsRepository,
            authSessionService = authService,
        )
        var authorized = false

        advanceUntilIdle()
        viewModel.onPinChanged("1234")
        viewModel.submit { authorized = true }
        advanceUntilIdle()

        assertThat(authorized).isTrue()
        assertThat(authService.lastAuthorizedPin).isEqualTo("1234")
        assertThat(sessionStore.read()).isNull()
    }

    @Test
    fun sessionPinCancelClearsPendingSession() = runViewModelTest {
        val sessionStore = PendingSessionStore().apply { save(sampleSession()) }
        val viewModel = SessionPinViewModel(
            pendingSessionStore = sessionStore,
            accountsRepository = inMemoryAccountsRepository(),
            authSessionService = FakeAuthSessionService(),
        )
        var cancelled = false

        advanceUntilIdle()
        viewModel.cancel { cancelled = true }
        advanceUntilIdle()

        assertThat(cancelled).isTrue()
        assertThat(sessionStore.read()).isNull()
    }
}