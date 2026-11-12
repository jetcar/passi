package com.passi.cloud.passi_android.feature.enrollment

import android.os.Build
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.local.PendingEnrollmentStore
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.enrollment.supportsDeviceKeys
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
import java.time.Instant
import java.time.temporal.ChronoUnit
import java.util.UUID

data class AddAccountUiState(
    val providers: List<Provider> = emptyList(),
    val selectedProviderId: String? = null,
    val email: String = "",
    val emailError: UiText? = null,
    val responseError: UiText? = null,
    val isSubmitting: Boolean = false,
    val deviceKeySupported: Boolean = true,
)

class AddAccountViewModel(
    private val providersRepository: ProvidersRepository,
    private val accountsRepository: AccountsRepository,
    private val enrollmentService: EnrollmentService,
    private val pendingEnrollmentStore: PendingEnrollmentStore,
    private val deviceIdProvider: () -> String,
    private val deviceKeySupported: Boolean = true,
) : ViewModel() {
    private val _uiState = MutableStateFlow(AddAccountUiState(deviceKeySupported = deviceKeySupported))
    val uiState: StateFlow<AddAccountUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            val providers = providersRepository.getProviders()
            _uiState.value = _uiState.value.copy(
                providers = providers,
                selectedProviderId = providers.firstOrNull { it.isDefault }?.id?.toString() ?: providers.firstOrNull()?.id?.toString(),
            )
        }
    }

    fun onEmailChanged(value: String) {
        _uiState.value = _uiState.value.copy(email = value, emailError = null, responseError = null)
    }

    fun onProviderSelected(providerId: String) {
        _uiState.value = _uiState.value.copy(selectedProviderId = providerId)
    }

    fun submit(onSuccess: () -> Unit) {
        if (!deviceKeySupported) {
            _uiState.value = _uiState.value.copy(responseError = UiText.Resource(R.string.device_key_android_version_required))
            return
        }
        val email = _uiState.value.email.trim()
        if (!EMAIL_REGEX.matches(email)) {
            _uiState.value = _uiState.value.copy(emailError = UiText.Resource(R.string.error_invalid_email))
            return
        }

        val provider = _uiState.value.providers.firstOrNull { it.id.toString() == _uiState.value.selectedProviderId }
        if (provider == null) {
            _uiState.value = _uiState.value.copy(responseError = UiText.Resource(R.string.error_select_provider))
            return
        }

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isSubmitting = true, responseError = null)
            val result = enrollmentService.beginSignup(email, provider, deviceIdProvider())
            result.onSuccess { pending ->
                val account = Account(
                    id = UUID.fromString(pending.accountId),
                    providerId = UUID.fromString(pending.providerId),
                    email = pending.email,
                    deviceId = deviceIdProvider(),
                    isConfirmed = false,
                    validFrom = Instant.now(),
                    validTo = Instant.now().plus(365, ChronoUnit.DAYS),
                )
                accountsRepository.savePendingAccount(account)
                pendingEnrollmentStore.save(pending)
                _uiState.value = _uiState.value.copy(isSubmitting = false)
                onSuccess()
            }.onFailure { error ->
                _uiState.value = _uiState.value.copy(
                    isSubmitting = false,
                    responseError = error.toUiText(R.string.error_network),
                )
            }
        }
    }

    companion object {
        private val EMAIL_REGEX = Regex("^[A-Za-z0-9+_.-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$")

        fun factory(
            providersRepository: ProvidersRepository,
            accountsRepository: AccountsRepository,
            enrollmentService: EnrollmentService,
            pendingEnrollmentStore: PendingEnrollmentStore,
            deviceIdProvider: () -> String,
            deviceKeySupported: Boolean = supportsDeviceKeys(Build.VERSION.SDK_INT),
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                AddAccountViewModel(
                    providersRepository = providersRepository,
                    accountsRepository = accountsRepository,
                    enrollmentService = enrollmentService,
                    pendingEnrollmentStore = pendingEnrollmentStore,
                    deviceIdProvider = deviceIdProvider,
                    deviceKeySupported = deviceKeySupported,
                )
            }
        }
    }
}
