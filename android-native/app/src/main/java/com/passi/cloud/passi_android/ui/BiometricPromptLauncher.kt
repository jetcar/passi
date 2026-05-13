package com.passi.cloud.passi_android.ui

import android.content.Context
import android.content.ContextWrapper
import androidx.biometric.BiometricManager
import androidx.biometric.BiometricPrompt
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.ContextCompat
import androidx.fragment.app.FragmentActivity

@Composable
fun rememberBiometricPromptLauncher(
    title: String,
    subtitle: String,
    onSuccess: () -> Unit,
    onError: (String) -> Unit,
): () -> Unit {
    val context = LocalContext.current
    val activity = remember(context) { context.findActivity() }

    return remember(activity, title, subtitle, onSuccess, onError) {
        fun launchPrompt() {
            val currentActivity = activity ?: run {
                onError("Biometric prompt is unavailable")
                return
            }

            val manager = BiometricManager.from(currentActivity)
            val capability = manager.canAuthenticate(BiometricManager.Authenticators.BIOMETRIC_WEAK)
            if (capability != BiometricManager.BIOMETRIC_SUCCESS) {
                onError(capability.toErrorMessage())
                return
            }

            val prompt = BiometricPrompt(
                currentActivity,
                ContextCompat.getMainExecutor(currentActivity),
                object : BiometricPrompt.AuthenticationCallback() {
                    override fun onAuthenticationSucceeded(result: BiometricPrompt.AuthenticationResult) {
                        onSuccess()
                    }

                    override fun onAuthenticationError(errorCode: Int, errString: CharSequence) {
                        onError(
                            when (errorCode) {
                                BiometricPrompt.ERROR_NEGATIVE_BUTTON,
                                BiometricPrompt.ERROR_USER_CANCELED,
                                BiometricPrompt.ERROR_CANCELED,
                                BiometricPrompt.ERROR_TIMEOUT,
                                -> "Biometric authentication cancelled"
                                else -> errString.toString()
                            }
                        )
                    }
                }
            )

            val promptInfo = BiometricPrompt.PromptInfo.Builder()
                .setTitle(title)
                .setSubtitle(subtitle)
                .setNegativeButtonText("Cancel")
                .build()

            prompt.authenticate(promptInfo)
        }

        ::launchPrompt
    }
}

private fun Context.findActivity(): FragmentActivity? = when (this) {
    is FragmentActivity -> this
    is ContextWrapper -> baseContext.findActivity()
    else -> null
}

private fun Int.toErrorMessage(): String = when (this) {
    BiometricManager.BIOMETRIC_ERROR_NO_HARDWARE -> "No biometric hardware found"
    BiometricManager.BIOMETRIC_ERROR_HW_UNAVAILABLE -> "Biometric hardware is unavailable"
    BiometricManager.BIOMETRIC_ERROR_NONE_ENROLLED -> "No biometric credential is enrolled on this device"
    else -> "Biometric authentication is unavailable"
}