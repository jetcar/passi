package com.passi.cloud.passi_android.localization

import androidx.annotation.StringRes
import androidx.compose.runtime.Composable
import androidx.compose.ui.res.stringResource
import com.passi.cloud.passi_android.R
import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyRecoveryRequiredException

sealed interface UiText {
    data class Resource(
        @StringRes val id: Int,
        val arguments: List<Any> = emptyList(),
    ) : UiText

    data class Dynamic(val value: String) : UiText
}

@Composable
fun UiText?.asString(): String = when (this) {
    null -> ""
    is UiText.Dynamic -> value
    is UiText.Resource -> stringResource(id, *arguments.toTypedArray())
}

fun Throwable.toUiText(@StringRes fallback: Int): UiText {
    if (this is DeviceKeyRecoveryRequiredException) return UiText.Resource(R.string.device_key_recovery_required)
    val resource = when (message) {
        "Account not found" -> R.string.account_not_found
        "Account storage failed", "Device storage failed" -> R.string.error_account_storage_retry
        "Provider not found" -> R.string.provider_not_found
        "Invalid Pin", "Invalid PIN" -> R.string.error_invalid_pin
        "Invalid old pin", "Invalid old PIN" -> R.string.error_invalid_old_pin
        "Biometric setup failed" -> R.string.error_biometric_setup
        "Certificate generation failed" -> R.string.error_certificate_generation
        "Network error. Try again" -> R.string.error_network
        "Certificate thumbprint is missing", "Current certificate thumbprint is missing" -> R.string.error_certificate_thumbprint_missing
        "Failed to parse devices" -> R.string.error_devices_parse
        "Biometric signing failed" -> R.string.error_biometric_signing
        "Certificate signing failed" -> R.string.error_certificate_signing
        "Token update failed" -> R.string.error_token_update
        "Unexpected error. Try again" -> R.string.error_unexpected
        "Missing confirmation code" -> R.string.error_confirmation_code_missing
        else -> null
    }
    return when {
        resource != null -> UiText.Resource(resource)
        !message.isNullOrBlank() -> UiText.Dynamic(message.orEmpty())
        else -> UiText.Resource(fallback)
    }
}
