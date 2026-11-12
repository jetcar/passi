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
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.biometric.BiometricManager
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.R
import com.passi.cloud.passi_android.ui.NumberPad
import com.passi.cloud.passi_android.ui.rememberBiometricPromptLauncher
import com.passi.cloud.passi_android.localization.asString
import com.passi.cloud.passi_android.ui.DeviceKeyProtectionChoice

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
    val biometricManager = BiometricManager.from(LocalContext.current)
    val launchBiometricPrompt = rememberBiometricPromptLauncher(
        title = stringResource(R.string.biometric_update_certificate_title),
        subtitle = stringResource(R.string.biometric_update_certificate_subtitle),
        onSuccess = viewModel::confirmBiometricForCurrentCertificate,
        onError = viewModel::onBiometricPromptError,
    )

    UpdateCertificateScreen(
        uiState = uiState,
        onOldPinChanged = viewModel::onOldPinChanged,
        onNewPinChanged = viewModel::onNewPinChanged,
        onConfirmPinChanged = viewModel::onConfirmPinChanged,
        onLaunchBiometric = launchBiometricPrompt,
        onRotate = viewModel::rotate,
        biometricAvailable = biometricManager.canAuthenticate(
            BiometricManager.Authenticators.BIOMETRIC_STRONG,
        ) == BiometricManager.BIOMETRIC_SUCCESS,
        deviceCredentialAvailable = biometricManager.canAuthenticate(
            BiometricManager.Authenticators.DEVICE_CREDENTIAL,
        ) == BiometricManager.BIOMETRIC_SUCCESS,
        onKeyProtectionSelected = viewModel::onKeyProtectionSelected,
        onDone = onBack,
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
    onDone: () -> Unit,
    onCancel: () -> Unit,
    biometricAvailable: Boolean = true,
    deviceCredentialAvailable: Boolean = true,
    onKeyProtectionSelected: (com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection) -> Unit = {},
) {
    val account = uiState.account
    Column(modifier = Modifier.fillMaxSize().padding(24.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        if (!uiState.deviceKeySupported) {
            Text(stringResource(R.string.device_key_android_version_required))
            TextButton(onClick = onCancel) { Text(stringResource(R.string.cancel)) }
            return@Column
        }
        account?.let {
            Text(
                text = "${stringResource(R.string.email)}: ${it.email}",
                fontWeight = FontWeight.Bold,
            )
        }
        Text(stringResource(R.string.device_key_explanation))
        uiState.responseError?.let { Text(it.asString()) }
        uiState.oldPinError?.let { Text(it.asString()) }
        if (uiState.isComplete) {
            Text(
                text = stringResource(R.string.certificate_updated_successfully),
                color = Color(0xFF2E7D32),
                fontWeight = FontWeight.Bold,
            )
            Button(onClick = onDone) { Text(stringResource(R.string.done)) }
            return@Column
        }
        if (account?.keyAlias == null && (account?.pinLength ?: 0) > 0) {
            androidx.compose.material3.OutlinedTextField(
                value = uiState.oldPin, onValueChange = onOldPinChanged,
                label = { Text(stringResource(R.string.old_pin)) },
                visualTransformation = androidx.compose.ui.text.input.PasswordVisualTransformation(),
                keyboardOptions = androidx.compose.foundation.text.KeyboardOptions(
                    keyboardType = androidx.compose.ui.text.input.KeyboardType.NumberPassword),
                enabled = !uiState.isSubmitting,
            )
        }
        DeviceKeyProtectionChoice(
            selected = uiState.keyProtection,
            biometricAvailable = biometricAvailable,
            deviceCredentialAvailable = deviceCredentialAvailable,
            enabled = !uiState.isSubmitting && !uiState.isProtectionLocked,
            onSelected = onKeyProtectionSelected,
        )
        Button(
            onClick = onRotate,
            enabled = account != null && !uiState.isSubmitting && uiState.keyProtection != null,
        ) {
            if (uiState.isSubmitting) {
                CircularProgressIndicator(
                    modifier = Modifier.size(20.dp),
                    strokeWidth = 2.dp,
                )
            } else {
                Text(stringResource(R.string.update_certificate))
            }
        }
        TextButton(onClick = onCancel, enabled = !uiState.isSubmitting) { Text(stringResource(R.string.cancel)) }
    }
}
