package com.passi.cloud.passi_android.feature.account

import android.os.Build
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
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
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.R
import com.passi.cloud.passi_android.domain.enrollment.supportsDeviceKeys
import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection
import com.passi.cloud.passi_android.ui.IPhoneConfirmationSheet
import com.passi.cloud.passi_android.ui.rememberBiometricPromptLauncher
import com.passi.cloud.passi_android.localization.asString

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AccountDetailRoute(
    onUpdateCertificate: () -> Unit,
    onManageDevices: () -> Unit,
    onBack: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: AccountDetailViewModel = viewModel(
        factory = AccountDetailViewModel.factory(
            selectedAccountStore = application.container.selectedAccountStore,
            accountsRepository = application.container.accountsRepository,
            providersRepository = application.container.providersRepository,
            accountManagementService = application.container.accountManagementService,
            biometricCertificateService = application.container.biometricCertificateService,
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    var showDeleteConfirmation by remember { mutableStateOf(false) }
    val launchBiometricPrompt = rememberBiometricPromptLauncher(
        title = stringResource(R.string.biometric_enable_title),
        subtitle = stringResource(R.string.biometric_enable_subtitle),
        onSuccess = viewModel::enableBiometric,
        onError = viewModel::onBiometricPromptError,
    )

    AccountDetailScreen(
        uiState = uiState,
        showDeleteConfirmation = showDeleteConfirmation,
        onBiometricPinChanged = viewModel::onBiometricPinChanged,
        onLaunchBiometric = launchBiometricPrompt,
        onUpdateCertificate = onUpdateCertificate,
        onManageDevices = onManageDevices,
        onBack = onBack,
        onShowDeleteConfirmation = { showDeleteConfirmation = true },
        onDeleteConfirmed = {
            showDeleteConfirmation = false
            viewModel.delete(onBack)
        },
        onDismissDeleteConfirmation = { showDeleteConfirmation = false },
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
internal fun AccountDetailScreen(
    uiState: AccountDetailUiState,
    showDeleteConfirmation: Boolean,
    onBiometricPinChanged: (String) -> Unit,
    onLaunchBiometric: () -> Unit,
    onUpdateCertificate: () -> Unit,
    onManageDevices: () -> Unit,
    onBack: () -> Unit,
    onShowDeleteConfirmation: () -> Unit,
    onDeleteConfirmed: () -> Unit,
    onDismissDeleteConfirmation: () -> Unit,
    deviceKeySupported: Boolean = supportsDeviceKeys(Build.VERSION.SDK_INT),
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA)),
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(Color(0xFF2196F3))
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
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 12.dp, vertical = 16.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            Text(
                text = uiState.responseError.asString(),
                modifier = Modifier.fillMaxWidth(),
                color = MaterialTheme.colorScheme.error,
                textAlign = TextAlign.Center,
            )

            uiState.account?.let { account ->
                DetailLabelValue(label = stringResource(R.string.current_user), value = account.email)
                DetailLabelValue(label = stringResource(R.string.certificate_thumbprint), value = account.thumbprint.orEmpty())
                DetailLabelValue(label = stringResource(R.string.certificate_valid_from), value = account.validFrom.toString())
                DetailLabelValue(label = stringResource(R.string.certificate_valid_to), value = account.validTo.toString())
                DetailLabelValue(
                    label = stringResource(R.string.certificate_protection),
                    value = stringResource(when {
                        account.keyAlias == null -> R.string.certificate_protection_legacy
                        account.keyProtection == DeviceKeyProtection.BIOMETRIC -> R.string.device_key_biometric
                        account.keyProtection == DeviceKeyProtection.DEVICE_CREDENTIAL -> R.string.device_key_phone_pin
                        else -> R.string.certificate_protection_biometric_or_phone_pin
                    }),
                )
                DetailLabelValue(label = stringResource(R.string.identity_provider), value = uiState.provider?.name.orEmpty())

                ActionButton(
                    text = stringResource(R.string.update_certificate),
                    onClick = onUpdateCertificate,
                    enabled = deviceKeySupported,
                )

                Text(stringResource(when {
                    !deviceKeySupported -> R.string.device_key_android_version_required
                    account.keyAlias == null -> R.string.device_key_migrate
                    else -> R.string.device_key_explanation
                }))

                ActionButton(
                    text = stringResource(R.string.manage_devices),
                    onClick = onManageDevices,
                )

                ActionButton(
                    text = stringResource(
                        if (uiState.isDeleting) R.string.deleting_account else R.string.delete_account,
                    ),
                    onClick = onShowDeleteConfirmation,
                    enabled = !uiState.isDeleting,
                )
            } ?: Text(
                text = stringResource(R.string.account_not_found),
                color = Color.Black,
                style = MaterialTheme.typography.bodyLarge,
            )

            Spacer(modifier = Modifier.weight(1f, fill = false))
        }
    }

    if (showDeleteConfirmation) {
        IPhoneConfirmationSheet(
            title = stringResource(R.string.delete_account_title),
            message = stringResource(R.string.delete_account_message),
            confirmLabel = stringResource(R.string.delete_account),
            onConfirm = onDeleteConfirmed,
            onDismiss = onDismissDeleteConfirmation,
        )
    }
}

@Composable
private fun DetailLabelValue(
    label: String,
    value: String,
) {
    Text(
        text = label,
        color = Color.Black,
        modifier = Modifier.padding(horizontal = 20.dp, vertical = 2.dp),
        style = MaterialTheme.typography.bodyLarge,
    )
    Text(
        text = value,
        color = Color.Black,
        modifier = Modifier.padding(horizontal = 20.dp, vertical = 2.dp),
        style = MaterialTheme.typography.bodyLarge,
        fontWeight = FontWeight.Medium,
    )
}

@Composable
private fun ActionButton(
    text: String,
    onClick: () -> Unit,
    enabled: Boolean = true,
    showProgress: Boolean = false,
) {
    Button(
        onClick = onClick,
        enabled = enabled,
        colors = ButtonDefaults.buttonColors(
            containerColor = Color.White,
            contentColor = Color.Black,
        ),
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 10.dp),
    ) {
        if (showProgress) {
            CircularProgressIndicator(modifier = Modifier.padding(4.dp), strokeWidth = 2.dp)
        } else {
            Text(text)
        }
    }
}
