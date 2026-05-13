package com.passi.cloud.passi_android.feature.auth

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableLongStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.domain.model.ConfirmationColor
import com.passi.cloud.passi_android.ui.rememberBiometricPromptLauncher
import java.time.Instant
import java.time.Duration
import kotlinx.coroutines.delay

private const val BIOMETRIC_CANCELLED_MESSAGE = "Biometric authentication cancelled"

@Composable
fun SessionChallengeRoute(
    onCancel: (String?) -> Unit,
    onRequirePin: () -> Unit,
    onAuthorized: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: SessionChallengeViewModel = viewModel(
        factory = SessionChallengeViewModel.factory(
            pendingSessionStore = application.container.pendingSessionStore,
            accountsRepository = application.container.accountsRepository,
            authSessionService = application.container.authSessionService,
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val launchBiometricPrompt = rememberBiometricPromptLauncher(
        title = "Approve session",
        subtitle = "Confirm your biometric credential to approve this session",
        onSuccess = { viewModel.authorizeWithBiometric(onAuthorized) },
        onError = { message ->
            if (message == BIOMETRIC_CANCELLED_MESSAGE) {
                viewModel.cancel(onCancel)
            } else {
                viewModel.onBiometricPromptError(message)
            }
        },
    )

    var timeLeftSeconds by remember { mutableLongStateOf(0L) }
    LaunchedEffect(uiState.session?.expirationTime) {
        val expiration = uiState.session?.expirationTime ?: return@LaunchedEffect
        while (true) {
            val remaining = Duration.between(Instant.now(), expiration).seconds
            timeLeftSeconds = maxOf(0, remaining)
            if (remaining <= 0) break
            delay(1000)
        }
    }

    SessionChallengeScreen(
        uiState = uiState,
        timeLeftSeconds = timeLeftSeconds,
        onColorSelected = { color ->
            viewModel.onColorSelected(
                color = color,
                onRequirePin = onRequirePin,
                onRequireBiometric = launchBiometricPrompt,
                onAuthorized = onAuthorized,
            )
        },
        onCancel = { viewModel.cancel(onCancel) },
    )
}

@Composable
internal fun SessionChallengeScreen(
    uiState: SessionChallengeUiState,
    timeLeftSeconds: Long,
    onColorSelected: (ConfirmationColor) -> Unit,
    onCancel: () -> Unit,
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA))
            .padding(16.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Column(
            modifier = Modifier.fillMaxWidth(),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Text(
                text = "${timeLeftSeconds}s",
                color = Color.Black,
                fontSize = 18.sp,
                textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxWidth(),
            )

            uiState.session?.let { session ->
                Text(
                    text = session.sender,
                    color = Color.Black,
                    fontWeight = FontWeight.Bold,
                    textAlign = TextAlign.Center,
                    modifier = Modifier.fillMaxWidth(),
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis,
                )
                Text(
                    text = session.returnHost,
                    color = Color.Black,
                    textAlign = TextAlign.Center,
                    modifier = Modifier.fillMaxWidth(),
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis,
                )
            }

            if (uiState.colorError != null) {
                Text(
                    text = uiState.colorError.orEmpty(),
                    color = Color.Black,
                    textAlign = TextAlign.Center,
                    modifier = Modifier.fillMaxWidth(),
                )
            }

            if (uiState.responseError != null) {
                Text(
                    text = uiState.responseError.orEmpty(),
                    color = Color.Black,
                    textAlign = TextAlign.Center,
                    modifier = Modifier.fillMaxWidth(),
                )
            }

            Spacer(modifier = Modifier.height(12.dp))

            Row(
                horizontalArrangement = Arrangement.spacedBy(20.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                uiState.colorOptions.forEach { color ->
                    Button(
                        onClick = { onColorSelected(color) },
                        enabled = uiState.isButtonEnabled && !uiState.isLoading,
                        modifier = Modifier
                            .testTag("session-color-${color.name.lowercase()}")
                            .size(68.dp)
                            .border(1.dp, Color.Black.copy(alpha = 0.2f), CircleShape),
                        shape = CircleShape,
                        colors = ButtonDefaults.buttonColors(
                            containerColor = color.toComposeColor(),
                            disabledContainerColor = color.toComposeColor().copy(alpha = 0.5f),
                        ),
                        contentPadding = PaddingValues(0.dp),
                    ) { }
                }
            }

            Spacer(modifier = Modifier.height(12.dp))

            Text(
                text = "Tap the matching color",
                color = Color.Black.copy(alpha = 0.75f),
                fontSize = 13.sp,
                textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxWidth(),
            )

            Button(
                onClick = onCancel,
                enabled = !uiState.isLoading,
                shape = RoundedCornerShape(100.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Color.White, contentColor = Color.Black),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text("Cancel")
            }
        }
    }
}

private fun ConfirmationColor.toComposeColor(): Color = when (this) {
    ConfirmationColor.BLUE -> Color.Blue
    ConfirmationColor.GREEN -> Color.Green
    ConfirmationColor.RED -> Color.Red
    ConfirmationColor.YELLOW -> Color.Yellow
}