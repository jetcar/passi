package com.passi.cloud.passi_android.feature.certificate

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
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
import com.passi.cloud.passi_android.ui.rememberBiometricPromptLauncher

@Composable
fun UpdateCertificateRoute(
    onBack: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: UpdateCertificateViewModel = viewModel(
        factory = UpdateCertificateViewModel.factory(
            selectedAccountStore = application.container.selectedAccountStore,
            accountsRepository = application.container.accountsRepository,
            certificateRotationService = application.container.certificateRotationService,
            biometricCertificateService = application.container.biometricCertificateService,
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val account = uiState.account
    val launchBiometricPrompt = rememberBiometricPromptLauncher(
        title = "Update certificate",
        subtitle = "Confirm your biometric credential to rotate this certificate",
        onSuccess = viewModel::confirmBiometricForCurrentCertificate,
        onError = viewModel::onBiometricPromptError,
    )

    UpdateCertificateScreen(
        uiState = uiState,
        onOldPinChanged = viewModel::onOldPinChanged,
        onNewPinChanged = viewModel::onNewPinChanged,
        onConfirmPinChanged = viewModel::onConfirmPinChanged,
        onLaunchBiometric = launchBiometricPrompt,
        onRotate = { viewModel.rotate(onBack) },
        onCancel = onBack,
    )
}

@Composable
internal fun UpdateCertificateScreen(
    uiState: UpdateCertificateUiState,
    onOldPinChanged: (String) -> Unit,
    onNewPinChanged: (String) -> Unit,
    onConfirmPinChanged: (String) -> Unit,
    onLaunchBiometric: () -> Unit,
    onRotate: () -> Unit,
    onCancel: () -> Unit,
) {
    val account = uiState.account

    val firstField = if (account != null && account.pinLength > 0 && !uiState.useBiometricForCurrentCertificate) "old" else "new"
    var activeField by remember { mutableStateOf(firstField) }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA))
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
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

        if (account != null && account.pinLength > 0 && !uiState.useBiometricForCurrentCertificate) {
            if (uiState.oldPinError != null) {
                Text(uiState.oldPinError.orEmpty(), color = Color.Black, textAlign = TextAlign.Center)
            }
            Text("Old Pin", color = Color.Black, textAlign = TextAlign.Center)
            CertPinFieldRow(
                maskedValue = "●".repeat(uiState.oldPin.length),
                hasError = uiState.oldPinError != null,
                isActive = activeField == "old",
                onClear = { onOldPinChanged("") },
                onTap = { activeField = "old" },
            )
            Spacer(modifier = Modifier.height(8.dp))
        }

        if (account?.hasFingerprint == true) {
            Button(
                onClick = onLaunchBiometric,
                enabled = !uiState.isSubmitting,
                shape = RoundedCornerShape(100.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color.White,
                    contentColor = Color.Black,
                ),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(
                    if (uiState.useBiometricForCurrentCertificate) {
                        "Biometric confirmed for current certificate"
                    } else {
                        "Use biometric for current certificate"
                    }
                )
            }
            Spacer(modifier = Modifier.height(8.dp))
        }

        if (uiState.newPinError != null) {
            Text(uiState.newPinError.orEmpty(), color = Color.Black, textAlign = TextAlign.Center)
        }
        Text("Pin", color = Color.Black, textAlign = TextAlign.Center)
        CertPinFieldRow(
            maskedValue = "●".repeat(uiState.newPin.length),
            hasError = uiState.newPinError != null,
            isActive = activeField == "new",
            onClear = { onNewPinChanged("") },
            onTap = { activeField = "new" },
        )
        Spacer(modifier = Modifier.height(8.dp))

        if (uiState.confirmPinError != null) {
            Text(uiState.confirmPinError.orEmpty(), color = Color.Black, textAlign = TextAlign.Center)
        }
        Text("Pin confirmation", color = Color.Black, textAlign = TextAlign.Center)
        CertPinFieldRow(
            maskedValue = "●".repeat(uiState.confirmPin.length),
            hasError = uiState.confirmPinError != null,
            isActive = activeField == "confirm",
            onClear = { onConfirmPinChanged("") },
            onTap = { activeField = "confirm" },
        )
        Spacer(modifier = Modifier.height(8.dp))

        NumberPad(onKey = { key ->
            when (key) {
                "del" -> when (activeField) {
                    "old" -> onOldPinChanged(uiState.oldPin.dropLast(1))
                    "new" -> onNewPinChanged(uiState.newPin.dropLast(1))
                    else -> onConfirmPinChanged(uiState.confirmPin.dropLast(1))
                }
                "confirm" -> when (activeField) {
                    "old" -> activeField = "new"
                    "new" -> activeField = "confirm"
                    else -> if (!uiState.isSubmitting) onRotate()
                }
                else -> when (activeField) {
                    "old" -> onOldPinChanged(uiState.oldPin + key)
                    "new" -> onNewPinChanged(uiState.newPin + key)
                    else -> onConfirmPinChanged(uiState.confirmPin + key)
                }
            }
        })

        Spacer(modifier = Modifier.height(8.dp))

        Button(
            onClick = onCancel,
            shape = RoundedCornerShape(100.dp),
            colors = ButtonDefaults.buttonColors(
                containerColor = Color.White,
                contentColor = Color.Black,
            ),
            modifier = Modifier.fillMaxWidth(),
        ) {
            Text("Cancel")
        }
    }
}

@Composable
private fun CertPinFieldRow(
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

