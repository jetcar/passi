package com.passi.cloud.passi_android.feature.auth

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.ui.NumberPad
import com.passi.cloud.passi_android.ui.rememberBiometricPromptLauncher

private val SessionPinBlue = Color(0xFF2196F3)
private const val BIOMETRIC_CANCELLED_MESSAGE = "Biometric authentication cancelled"

@Composable
fun SessionPinRoute(
    onCancel: () -> Unit,
    onAuthorized: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: SessionPinViewModel = viewModel(
        factory = SessionPinViewModel.factory(
            pendingSessionStore = application.container.pendingSessionStore,
            accountsRepository = application.container.accountsRepository,
            authSessionService = application.container.authSessionService,
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val launchedBiometric = remember { mutableStateOf(false) }
    val launchBiometricPrompt = rememberBiometricPromptLauncher(
        title = "Confirm by biometric",
        subtitle = "Use your biometric credential instead of the PIN",
        onSuccess = { viewModel.authorizeWithBiometric(onAuthorized) },
        onError = { message ->
            if (message == BIOMETRIC_CANCELLED_MESSAGE) {
                viewModel.cancel(onCancel)
            } else {
                viewModel.onBiometricPromptError(message)
            }
        },
    )

    LaunchedEffect(uiState.hasFingerprint) {
        if (uiState.hasFingerprint && !launchedBiometric.value) {
            launchedBiometric.value = true
            launchBiometricPrompt()
        }
    }

    SessionPinScreen(
        uiState = uiState,
        onPinChanged = viewModel::onPinChanged,
        onSubmit = { viewModel.submit(onAuthorized) },
        onCancel = { viewModel.cancel(onCancel) },
    )
}

@Composable
internal fun SessionPinScreen(
    uiState: SessionPinUiState,
    onPinChanged: (String) -> Unit,
    onSubmit: () -> Unit,
    onCancel: () -> Unit,
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA)),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        // Blue header row with email
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(SessionPinBlue)
                .padding(horizontal = 16.dp, vertical = 12.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Text(
                text = uiState.email,
                color = Color.White,
                fontWeight = FontWeight.Bold,
                fontSize = 16.sp,
                textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxWidth(),
            )
        }

        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 16.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            if (uiState.responseError != null) {
                Text(
                    text = uiState.responseError.orEmpty(),
                    color = Color.Black,
                    textAlign = TextAlign.Center,
                )
                Spacer(modifier = Modifier.height(8.dp))
            }

            Text("Pin", color = Color.Black, textAlign = TextAlign.Center)
            Spacer(modifier = Modifier.height(4.dp))

            // PIN display field (aquamarine background, masked)
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .border(2.dp, Color.Black)
                    .background(Color(0xFF7FFFD4))
                    .padding(horizontal = 8.dp, vertical = 4.dp),
            ) {
                Text(
                    text = "●".repeat(uiState.pin.length).ifEmpty { " " },
                    color = Color.Black,
                    fontWeight = FontWeight.Bold,
                    fontSize = 15.sp,
                    modifier = Modifier.align(Alignment.CenterStart),
                )
                TextButton(
                    onClick = { onPinChanged("") },
                    modifier = Modifier.align(Alignment.CenterEnd),
                ) {
                    Text("X", color = Color.Black)
                }
            }

            Spacer(modifier = Modifier.height(8.dp))

            NumberPad(onKey = { key ->
                when (key) {
                    "del" -> onPinChanged(uiState.pin.dropLast(1))
                    "confirm" -> if (!uiState.isSubmitting) onSubmit()
                    else -> onPinChanged(uiState.pin + key)
                }
            })

            Spacer(modifier = Modifier.height(8.dp))

            Button(
                onClick = onCancel,
                shape = RoundedCornerShape(100.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text("Cancel")
            }
        }
    }
}