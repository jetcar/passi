package com.passi.cloud.passi_android.feature.providers

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.R

private val EditorBlue = Color(0xFF2196F3)

@Composable
fun ProviderEditorRoute(
    onBack: () -> Unit,
    onSaved: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: ProviderEditorViewModel = viewModel(
        factory = ProviderEditorViewModel.factory(
            selectedProviderStore = application.container.selectedProviderStore,
            providersRepository = application.container.providersRepository,
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA)),
    ) {
        // Blue header with back arrow
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(EditorBlue)
                .statusBarsPadding()
                .padding(top = 4.dp, bottom = 8.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            IconButton(onClick = onBack) {
                Icon(
                    imageVector = Icons.AutoMirrored.Filled.KeyboardArrowLeft,
                    contentDescription = stringResource(R.string.back),
                    tint = Color.White,
                    modifier = Modifier.size(28.dp),
                )
            }
        }

        Column(
            modifier = Modifier
                .weight(1f)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 16.dp, vertical = 8.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            ProviderField(stringResource(R.string.provider_name), uiState.name) { value -> viewModel.update { it.copy(name = value) } }
            ProviderField(stringResource(R.string.provider_base_url), uiState.baseUrl) { value -> viewModel.update { it.copy(baseUrl = value) } }
            ProviderField(stringResource(R.string.provider_signup), uiState.signup) { value -> viewModel.update { it.copy(signup = value) } }
            ProviderField(stringResource(R.string.provider_signup_confirmation), uiState.signupConfirmation) { value -> viewModel.update { it.copy(signupConfirmation = value) } }
            ProviderField(stringResource(R.string.provider_signup_check), uiState.signupCheck) { value -> viewModel.update { it.copy(signupCheck = value) } }
            ProviderField(stringResource(R.string.provider_token_update), uiState.tokenUpdate) { value -> viewModel.update { it.copy(tokenUpdate = value) } }
            ProviderField(stringResource(R.string.provider_cancel_check), uiState.cancelCheck) { value -> viewModel.update { it.copy(cancelCheck = value) } }
            ProviderField(stringResource(R.string.provider_authorize), uiState.authorize) { value -> viewModel.update { it.copy(authorize = value) } }
            ProviderField(stringResource(R.string.provider_time), uiState.time) { value -> viewModel.update { it.copy(time = value) } }
            ProviderField(stringResource(R.string.provider_update_certificate), uiState.updateCertificate) { value -> viewModel.update { it.copy(updateCertificate = value) } }
            ProviderField(stringResource(R.string.provider_active_session), uiState.checkForStartedSessions) { value -> viewModel.update { it.copy(checkForStartedSessions = value) } }
            ProviderField(stringResource(R.string.provider_sync_accounts), uiState.syncAccounts) { value -> viewModel.update { it.copy(syncAccounts = value) } }
            ProviderField(stringResource(R.string.provider_delete_account), uiState.deleteAccount) { value -> viewModel.update { it.copy(deleteAccount = value) } }
            ProviderField(stringResource(R.string.provider_list_devices), uiState.listDevices) { value -> viewModel.update { it.copy(listDevices = value) } }
            ProviderField(stringResource(R.string.provider_delete_device), uiState.deleteDevice) { value -> viewModel.update { it.copy(deleteDevice = value) } }

            Button(
                onClick = { viewModel.update { it.copy(isDefault = !it.isDefault) } },
                shape = RoundedCornerShape(100.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(stringResource(if (uiState.isDefault) R.string.provider_is_default else R.string.provider_mark_default))
            }

            Button(
                onClick = { viewModel.save(onSaved) },
                shape = RoundedCornerShape(100.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(stringResource(R.string.save))
            }

            Button(
                onClick = onBack,
                shape = RoundedCornerShape(100.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(stringResource(R.string.cancel))
            }
        }
    }
}

@Composable
private fun ProviderField(label: String, value: String, onValueChanged: (String) -> Unit) {
    OutlinedTextField(
        value = value,
        onValueChange = onValueChanged,
        label = { Text(label) },
        modifier = Modifier.fillMaxWidth(),
    )
}
