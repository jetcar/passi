package com.passi.cloud.passi_android.feature.enrollment

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AddAccountRoute(
    onBack: () -> Unit,
    onSignupStarted: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: AddAccountViewModel = viewModel(
        factory = AddAccountViewModel.factory(
            providersRepository = application.container.providersRepository,
            accountsRepository = application.container.accountsRepository,
            enrollmentService = application.container.enrollmentService,
            pendingEnrollmentStore = application.container.pendingEnrollmentStore,
            deviceIdProvider = { application.container.preferences.getOrCreateDeviceId() },
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    AddAccountScreen(
        uiState = uiState,
        onEmailChanged = viewModel::onEmailChanged,
        onProviderSelected = viewModel::onProviderSelected,
        onSubmit = { viewModel.submit(onSignupStarted) },
        onBack = onBack,
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
internal fun AddAccountScreen(
    uiState: AddAccountUiState,
    onEmailChanged: (String) -> Unit,
    onProviderSelected: (String) -> Unit,
    onSubmit: () -> Unit,
    onBack: () -> Unit,
) {
    var expanded by remember { mutableStateOf(false) }
    val selectedProvider = uiState.providers.firstOrNull { it.id.toString() == uiState.selectedProviderId }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA))
            .padding(horizontal = 16.dp),
        verticalArrangement = Arrangement.Center,
    ) {
        ExposedDropdownMenuBox(
            expanded = expanded,
            onExpandedChange = { expanded = !expanded },
        ) {
            OutlinedTextField(
                value = selectedProvider?.name.orEmpty(),
                onValueChange = {},
                readOnly = true,
                textStyle = MaterialTheme.typography.bodyLarge.copy(textAlign = TextAlign.Center, color = Color.Black),
                label = { Text("IdentityProvider", color = Color.Black) },
                trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
                modifier = Modifier
                    .fillMaxWidth()
                    .menuAnchor(),
            )
            DropdownMenu(
                expanded = expanded,
                onDismissRequest = { expanded = false },
            ) {
                uiState.providers.forEach { provider ->
                    DropdownMenuItem(
                        text = { Text(provider.name) },
                        onClick = {
                            onProviderSelected(provider.id.toString())
                            expanded = false
                        }
                    )
                }
            }
        }

        Spacer(modifier = Modifier.padding(top = 8.dp))

        Text(
            text = uiState.responseError.orEmpty(),
            modifier = Modifier.fillMaxWidth(),
            textAlign = TextAlign.Center,
            color = Color.Black,
        )

        Text(
            text = uiState.emailError.orEmpty(),
            modifier = Modifier.fillMaxWidth(),
            textAlign = TextAlign.Center,
            color = Color.Black,
        )

        Text(
            text = "Email",
            modifier = Modifier.fillMaxWidth(),
            textAlign = TextAlign.Center,
            color = Color.Black,
        )

        OutlinedTextField(
            value = uiState.email,
            onValueChange = onEmailChanged,
            isError = uiState.emailError != null,
            textStyle = MaterialTheme.typography.bodyLarge.copy(color = Color.Black),
            modifier = Modifier
                .fillMaxWidth()
                .padding(top = 5.dp),
        )

        PillButton(
            text = "Register",
            enabled = !uiState.isSubmitting,
            onClick = onSubmit,
            showProgress = uiState.isSubmitting,
            modifier = Modifier
                .fillMaxWidth()
                .padding(top = 10.dp),
        )

        PillButton(
            text = "Cancel",
            onClick = onBack,
            modifier = Modifier
                .fillMaxWidth()
                .padding(top = 10.dp),
        )
    }
}

@Composable
private fun PillButton(
    text: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    enabled: Boolean = true,
    showProgress: Boolean = false,
) {
    Button(
        onClick = onClick,
        enabled = enabled,
        shape = RoundedCornerShape(100.dp),
        colors = ButtonDefaults.buttonColors(
            containerColor = Color.White,
            contentColor = Color.Black,
        ),
        modifier = modifier,
    ) {
        if (showProgress) {
            CircularProgressIndicator(modifier = Modifier.padding(4.dp), strokeWidth = 2.dp)
        } else {
            Text(text)
        }
    }
}