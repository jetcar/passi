package com.passi.cloud.passi_android.feature.providers

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.selection.SelectedProviderStore
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class ProviderDetailUiState(
    val provider: Provider? = null,
)

class ProviderDetailViewModel(
    private val selectedProviderStore: SelectedProviderStore,
    private val providersRepository: ProvidersRepository,
) : ViewModel() {
    private val _uiState = MutableStateFlow(ProviderDetailUiState())
    val uiState: StateFlow<ProviderDetailUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            val provider = selectedProviderStore.read()?.let { providersRepository.getProvider(it) }
            _uiState.value = ProviderDetailUiState(provider = provider)
        }
    }

    companion object {
        fun factory(
            selectedProviderStore: SelectedProviderStore,
            providersRepository: ProvidersRepository,
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                ProviderDetailViewModel(
                    selectedProviderStore = selectedProviderStore,
                    providersRepository = providersRepository,
                )
            }
        }
    }
}