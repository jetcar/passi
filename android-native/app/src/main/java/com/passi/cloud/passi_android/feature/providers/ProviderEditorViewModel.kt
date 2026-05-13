package com.passi.cloud.passi_android.feature.providers

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.passi.cloud.passi_android.data.selection.SelectedProviderStore
import com.passi.cloud.passi_android.domain.model.ApiPaths
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.util.UUID

data class ProviderEditorUiState(
    val id: UUID? = null,
    val name: String = "",
    val baseUrl: String = "https://",
    val isDefault: Boolean = false,
    val signup: String = ApiPaths.defaultPaths().signup,
    val signupConfirmation: String = ApiPaths.defaultPaths().signupConfirmation,
    val signupCheck: String = ApiPaths.defaultPaths().signupCheck,
    val tokenUpdate: String = ApiPaths.defaultPaths().tokenUpdate,
    val cancelCheck: String = ApiPaths.defaultPaths().cancelCheck,
    val authorize: String = ApiPaths.defaultPaths().authorize,
    val time: String = ApiPaths.defaultPaths().time,
    val updateCertificate: String = ApiPaths.defaultPaths().updateCertificate,
    val checkForStartedSessions: String = ApiPaths.defaultPaths().checkForStartedSessions,
    val syncAccounts: String = ApiPaths.defaultPaths().syncAccounts,
    val deleteAccount: String = ApiPaths.defaultPaths().deleteAccount,
    val listDevices: String = ApiPaths.defaultPaths().listDevices,
    val deleteDevice: String = ApiPaths.defaultPaths().deleteDevice,
)

class ProviderEditorViewModel(
    private val selectedProviderStore: SelectedProviderStore,
    private val providersRepository: ProvidersRepository,
) : ViewModel() {
    private val _uiState = MutableStateFlow(ProviderEditorUiState())
    val uiState: StateFlow<ProviderEditorUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            val selectedProvider = selectedProviderStore.read()?.let { providersRepository.getProvider(it) }
            if (selectedProvider != null) {
                _uiState.value = selectedProvider.toEditorState()
            }
        }
    }

    fun update(transform: (ProviderEditorUiState) -> ProviderEditorUiState) {
        _uiState.value = transform(_uiState.value)
    }

    fun save(onSaved: () -> Unit) {
        viewModelScope.launch {
            val state = _uiState.value
            providersRepository.saveProvider(
                Provider(
                    id = state.id ?: UUID.randomUUID(),
                    name = state.name,
                    baseUrl = state.baseUrl,
                    isDefault = state.isDefault,
                    apiPaths = ApiPaths(
                        signup = state.signup,
                        signupConfirmation = state.signupConfirmation,
                        signupCheck = state.signupCheck,
                        tokenUpdate = state.tokenUpdate,
                        cancelCheck = state.cancelCheck,
                        authorize = state.authorize,
                        time = state.time,
                        updateCertificate = state.updateCertificate,
                        checkForStartedSessions = state.checkForStartedSessions,
                        syncAccounts = state.syncAccounts,
                        deleteAccount = state.deleteAccount,
                        listDevices = state.listDevices,
                        deleteDevice = state.deleteDevice,
                    )
                )
            )
            onSaved()
        }
    }

    companion object {
        fun factory(
            selectedProviderStore: SelectedProviderStore,
            providersRepository: ProvidersRepository,
        ): ViewModelProvider.Factory = viewModelFactory {
            initializer {
                ProviderEditorViewModel(
                    selectedProviderStore = selectedProviderStore,
                    providersRepository = providersRepository,
                )
            }
        }
    }
}

private fun Provider.toEditorState(): ProviderEditorUiState = ProviderEditorUiState(
    id = id,
    name = name,
    baseUrl = baseUrl,
    isDefault = isDefault,
    signup = apiPaths.signup,
    signupConfirmation = apiPaths.signupConfirmation,
    signupCheck = apiPaths.signupCheck,
    tokenUpdate = apiPaths.tokenUpdate,
    cancelCheck = apiPaths.cancelCheck,
    authorize = apiPaths.authorize,
    time = apiPaths.time,
    updateCertificate = apiPaths.updateCertificate,
    checkForStartedSessions = apiPaths.checkForStartedSessions,
    syncAccounts = apiPaths.syncAccounts,
    deleteAccount = apiPaths.deleteAccount,
    listDevices = apiPaths.listDevices,
    deleteDevice = apiPaths.deleteDevice,
)