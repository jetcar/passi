package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.feature.enrollment.AddAccountViewModel
import com.passi.cloud.passi_android.feature.enrollment.ConfirmCodeViewModel
import com.passi.cloud.passi_android.feature.enrollment.FinishEnrollmentViewModel
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.advanceUntilIdle
import org.junit.Test

@OptIn(ExperimentalCoroutinesApi::class)
class EnrollmentUseCaseTests : CoroutineViewModelTest() {
    @Test
    fun addAccountRejectsInvalidEmail() = runViewModelTest {
        val viewModel = AddAccountViewModel(
            providersRepository = inMemoryProvidersRepository(),
            accountsRepository = inMemoryAccountsRepository(),
            enrollmentService = FakeEnrollmentService(),
            pendingEnrollmentStore = pendingEnrollmentStore(),
            deviceIdProvider = { "device-1" },
        )

        advanceUntilIdle()
        viewModel.onEmailChanged("not-an-email")
        viewModel.submit {}

        assertThat(viewModel.uiState.value.emailError).isEqualTo("invalid Email")
    }

    @Test
    fun addAccountStartsSignupAndStoresPendingEnrollment() = runViewModelTest {
        val providersRepository = inMemoryProvidersRepository()
        val accountsRepository = inMemoryAccountsRepository()
        val enrollmentStore = pendingEnrollmentStore()
        val enrollmentService = FakeEnrollmentService()
        var wasSuccessful = false

        val viewModel = AddAccountViewModel(
            providersRepository = providersRepository,
            accountsRepository = accountsRepository,
            enrollmentService = enrollmentService,
            pendingEnrollmentStore = enrollmentStore,
            deviceIdProvider = { "device-1" },
        )

        advanceUntilIdle()
        viewModel.onEmailChanged("new.user@example.com")
        viewModel.submit { wasSuccessful = true }
        advanceUntilIdle()

        assertThat(wasSuccessful).isTrue()
        assertThat(enrollmentStore.read()).isNotNull()
        assertThat(accountsRepository.getAccounts().any { it.email == "new.user@example.com" }).isTrue()
    }

    @Test
    fun confirmCodeMarksPendingAccountConfirmed() = runViewModelTest {
        val enrollmentStore = pendingEnrollmentStore().apply {
            save(samplePendingEnrollment())
        }
        val accountsRepository = inMemoryAccountsRepository()
        val providersRepository = inMemoryProvidersRepository()
        val enrollmentService = FakeEnrollmentService()
        var completed = false

        val viewModel = ConfirmCodeViewModel(
            pendingEnrollmentStore = enrollmentStore,
            enrollmentService = enrollmentService,
            accountsRepository = accountsRepository,
            providersRepository = providersRepository,
        )

        viewModel.onCodeChanged("123456")
        viewModel.submit { completed = true }
        advanceUntilIdle()

        assertThat(completed).isTrue()
        assertThat(accountsRepository.getAccounts().first { it.id.toString() == samplePendingEnrollment().accountId }.isConfirmed).isTrue()
        assertThat(enrollmentStore.read()?.confirmationCode).isEqualTo("123456")
    }

    @Test
    fun confirmCodeRejectsIncompleteCode() = runViewModelTest {
        val viewModel = ConfirmCodeViewModel(
            pendingEnrollmentStore = pendingEnrollmentStore().apply { save(samplePendingEnrollment()) },
            enrollmentService = FakeEnrollmentService(),
            accountsRepository = inMemoryAccountsRepository(),
            providersRepository = inMemoryProvidersRepository(),
        )

        viewModel.onCodeChanged("123")
        viewModel.submit {}
        advanceUntilIdle()

        assertThat(viewModel.uiState.value.responseError).isEqualTo("Enter the 6-digit confirmation code")
    }

    @Test
    fun finishEnrollmentRejectsShortPin() = runViewModelTest {
        val viewModel = FinishEnrollmentViewModel(
            pendingEnrollmentStore = pendingEnrollmentStore().apply { save(samplePendingEnrollment()) },
            accountsRepository = inMemoryAccountsRepository(),
            providersRepository = inMemoryProvidersRepository(),
            enrollmentService = FakeEnrollmentService(),
            certificateGenerator = FakeCertificateGenerator(),
            deviceIdProvider = { "device-1" },
        )

        viewModel.onPinChanged("123")
        viewModel.onPinConfirmationChanged("123")
        viewModel.submit {}

        assertThat(viewModel.uiState.value.pinError).isEqualTo("Pin1 should be min 4 numbers")
    }

    @Test
    fun finishEnrollmentStoresCertificateAndClearsPendingContext() = runViewModelTest {
        val enrollmentStore = pendingEnrollmentStore().apply { save(samplePendingEnrollment()) }
        val accountsRepository = inMemoryAccountsRepository()
        val viewModel = FinishEnrollmentViewModel(
            pendingEnrollmentStore = enrollmentStore,
            accountsRepository = accountsRepository,
            providersRepository = inMemoryProvidersRepository(),
            enrollmentService = FakeEnrollmentService(),
            certificateGenerator = FakeCertificateGenerator(),
            deviceIdProvider = { "device-finished" },
        )
        var completed = false

        viewModel.onPinChanged("1234")
        viewModel.onPinConfirmationChanged("1234")
        viewModel.submit { completed = true }
        advanceUntilIdle()

        val updatedAccount = accountsRepository.getAccounts().first { it.id.toString() == samplePendingEnrollment().accountId }
        assertThat(completed).isTrue()
        assertThat(enrollmentStore.read()).isNull()
        assertThat(updatedAccount.deviceId).isEqualTo("device-finished")
        assertThat(updatedAccount.pinLength).isEqualTo(4)
        assertThat(updatedAccount.thumbprint).isEqualTo("thumbprint-123")
    }
}