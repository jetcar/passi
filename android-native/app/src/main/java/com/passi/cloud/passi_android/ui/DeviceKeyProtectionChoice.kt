package com.passi.cloud.passi_android.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.passi.cloud.passi_android.R
import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection

@Composable
fun DeviceKeyProtectionChoice(
    selected: DeviceKeyProtection?,
    biometricAvailable: Boolean,
    deviceCredentialAvailable: Boolean,
    enabled: Boolean,
    onSelected: (DeviceKeyProtection) -> Unit,
) {
    Column(modifier = Modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(8.dp)) {
        Text(stringResource(R.string.device_key_choose_protection))
        if (selected == DeviceKeyProtection.BIOMETRIC_OR_DEVICE_CREDENTIAL) {
            OutlinedButton(
                onClick = {},
                modifier = Modifier.fillMaxWidth(),
                enabled = false,
            ) {
                Text(stringResource(R.string.device_key_biometric_or_phone_pin_selected))
            }
            return@Column
        }
        OutlinedButton(
            onClick = { onSelected(DeviceKeyProtection.BIOMETRIC) },
            modifier = Modifier.fillMaxWidth(),
            enabled = enabled && biometricAvailable,
        ) {
            Text(if (selected == DeviceKeyProtection.BIOMETRIC) {
                stringResource(R.string.device_key_biometric_selected)
            } else stringResource(R.string.device_key_biometric))
        }
        OutlinedButton(
            onClick = { onSelected(DeviceKeyProtection.DEVICE_CREDENTIAL) },
            modifier = Modifier.fillMaxWidth(),
            enabled = enabled && deviceCredentialAvailable,
        ) {
            Text(if (selected == DeviceKeyProtection.DEVICE_CREDENTIAL) {
                stringResource(R.string.device_key_phone_pin_selected)
            } else stringResource(R.string.device_key_phone_pin))
        }
        if (!biometricAvailable) Text(stringResource(R.string.device_key_biometric_unavailable))
        if (!deviceCredentialAvailable) Text(stringResource(R.string.device_key_phone_pin_unavailable))
    }
}
