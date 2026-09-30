package com.passi.cloud.passi_android.feature.update

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.domain.update.AppUpdateChecker
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class AppUpdateUiState(
    val isUpdateAvailable: Boolean = false,
    val isDismissed: Boolean = false,
) {
    val showBanner: Boolean get() = isUpdateAvailable && !isDismissed
}

class AppUpdateViewModel(
    private val appUpdateChecker: AppUpdateChecker,
) : ViewModel() {
    private val _uiState = MutableStateFlow(AppUpdateUiState())

    val uiState: StateFlow<AppUpdateUiState> = _uiState.asStateFlow()

    fun check() {
        viewModelScope.launch {
            // A failed check (offline, not installed from Google Play) keeps whatever was known before.
            appUpdateChecker.isUpdateAvailable().onSuccess { available ->
                _uiState.value = _uiState.value.copy(isUpdateAvailable = available)
            }
        }
    }

    fun dismiss() {
        _uiState.value = _uiState.value.copy(isDismissed = true)
    }

    companion object {
        fun factory(appUpdateChecker: AppUpdateChecker): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                AppUpdateViewModel(appUpdateChecker = appUpdateChecker)
            }
        }
    }
}
