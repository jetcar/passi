package com.passi.cloud.passi_android.feature.enrollment

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
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
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
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

@Composable
fun FinishEnrollmentRoute(
    onCancel: () -> Unit,
    onDone: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: FinishEnrollmentViewModel = viewModel(
        factory = FinishEnrollmentViewModel.factory(
            pendingEnrollmentStore = application.container.pendingEnrollmentStore,
            accountsRepository = application.container.accountsRepository,
            providersRepository = application.container.providersRepository,
            enrollmentService = application.container.enrollmentService,
            certificateGenerator = application.container.certificateGenerator,
            deviceIdProvider = { application.container.preferences.getOrCreateDeviceId() },
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    FinishEnrollmentScreen(
        uiState = uiState,
        onPinChanged = viewModel::onPinChanged,
        onPinConfirmationChanged = viewModel::onPinConfirmationChanged,
        onSkip = { viewModel.skip(onDone) },
        onSubmit = { viewModel.submit(onDone) },
    )
}

@Composable
internal fun FinishEnrollmentScreen(
    uiState: FinishEnrollmentUiState,
    onPinChanged: (String) -> Unit,
    onPinConfirmationChanged: (String) -> Unit,
    onSkip: () -> Unit,
    onSubmit: () -> Unit,
) {
    var activeField by remember { mutableStateOf("pin") }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA)),
        contentAlignment = Alignment.Center,
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            if (uiState.responseError != null) {
                Text(
                    text = uiState.responseError.orEmpty(),
                    color = Color.Black,
                    textAlign = TextAlign.Center,
                )
            }

            Text("Secure account by pin.", color = Color.Black)

            if (uiState.pinError != null) {
                Text(uiState.pinError.orEmpty(), color = Color.Black, textAlign = TextAlign.Center)
            }
            Text("Pin", color = Color.Black, textAlign = TextAlign.Center)
            PinFieldRow(
                maskedValue = "●".repeat(uiState.pin.length),
                hasError = uiState.pinError != null,
                isActive = activeField == "pin",
                onClear = { onPinChanged("") },
                onTap = { activeField = "pin" },
            )

            if (uiState.pinConfirmationError != null) {
                Text(uiState.pinConfirmationError.orEmpty(), color = Color.Black, textAlign = TextAlign.Center)
            }
            Text("Pin confirmation", color = Color.Black, textAlign = TextAlign.Center)
            PinFieldRow(
                maskedValue = "●".repeat(uiState.pinConfirmation.length),
                hasError = uiState.pinConfirmationError != null,
                isActive = activeField == "pinConfirmation",
                onClear = { onPinConfirmationChanged("") },
                onTap = { activeField = "pinConfirmation" },
            )

            NumberPad(onKey = { key ->
                when (key) {
                    "del" -> {
                        if (activeField == "pin") onPinChanged(uiState.pin.dropLast(1))
                        else onPinConfirmationChanged(uiState.pinConfirmation.dropLast(1))
                    }
                    "confirm" -> {
                        if (activeField == "pin") activeField = "pinConfirmation"
                        else if (!uiState.isSubmitting && !uiState.isMissingContext) onSubmit()
                    }
                    else -> {
                        if (activeField == "pin") onPinChanged(uiState.pin + key)
                        else onPinConfirmationChanged(uiState.pinConfirmation + key)
                    }
                }
            })

            Spacer(modifier = Modifier.height(4.dp))

            Button(
                onClick = onSkip,
                enabled = !uiState.isSubmitting && !uiState.isMissingContext,
                shape = RoundedCornerShape(100.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color.White,
                    contentColor = Color.Black,
                ),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text("Skip")
            }
        }
    }
}

@Composable
private fun PinFieldRow(
    maskedValue: String,
    hasError: Boolean,
    isActive: Boolean,
    onClear: () -> Unit,
    onTap: () -> Unit,
) {
    val bgColor = if (hasError) Color.Red else Color(0xFF7FFFD4)
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onTap)
            .then(if (isActive) Modifier.border(2.dp, Color.Black) else Modifier)
            .background(bgColor)
            .padding(horizontal = 8.dp, vertical = 4.dp),
    ) {
        Text(
            text = maskedValue.ifEmpty { " " },
            color = Color.Black,
            fontWeight = FontWeight.Bold,
            fontSize = 15.sp,
            textAlign = TextAlign.Center,
            modifier = Modifier
                .fillMaxWidth()
                .padding(end = 36.dp)
                .align(Alignment.Center),
        )
        TextButton(
            onClick = onClear,
            modifier = Modifier.align(Alignment.CenterEnd),
        ) {
            Text("X", color = Color.Black)
        }
    }
}

