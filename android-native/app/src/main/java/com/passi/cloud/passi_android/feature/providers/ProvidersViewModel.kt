package com.passi.cloud.passi_android.feature.providers

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.selection.SelectedProviderStore
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import com.passi.cloud.passi_android.R
import com.passi.cloud.passi_android.localization.UiText
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class ProvidersUiState(
    val providers: List<Provider> = emptyList(),
    val responseError: UiText? = null,
)

class ProvidersViewModel(
    private val providersRepository: ProvidersRepository,
    private val selectedProviderStore: SelectedProviderStore,
) : ViewModel() {
    private val _uiState = MutableStateFlow(ProvidersUiState())
    val uiState: StateFlow<ProvidersUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            providersRepository.observeProviders().collect { providers ->
                _uiState.value = ProvidersUiState(providers = providers.sortedBy { it.name.lowercase() })
            }
        }
    }

    fun openProvider(provider: Provider, onOpen: () -> Unit) {
        selectedProviderStore.save(provider.id)
        onOpen()
    }

    fun addProvider(onAdd: () -> Unit) {
        selectedProviderStore.clear()
        onAdd()
    }

    fun deleteProvider(provider: Provider) {
        viewModelScope.launch {
            if (provider.isDefault && _uiState.value.providers.count { it.isDefault } == 1) {
                _uiState.value = _uiState.value.copy(
                    responseError = UiText.Resource(R.string.default_provider_cannot_delete),
                )
                return@launch
            }
            providersRepository.deleteProvider(provider.id)
        }
    }

    companion object {
        fun factory(
            providersRepository: ProvidersRepository,
            selectedProviderStore: SelectedProviderStore,
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                ProvidersViewModel(
                    providersRepository = providersRepository,
                    selectedProviderStore = selectedProviderStore,
                )
            }
        }
    }
}
