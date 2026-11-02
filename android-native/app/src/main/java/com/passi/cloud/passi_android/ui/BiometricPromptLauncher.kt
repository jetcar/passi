package com.passi.cloud.passi_android.ui

import android.content.Context
import android.content.ContextWrapper
import androidx.biometric.BiometricManager
import androidx.biometric.BiometricPrompt
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.core.content.ContextCompat
import androidx.fragment.app.FragmentActivity
import com.passi.cloud.passi_android.R

@Composable
fun rememberBiometricPromptLauncher(
    title: String,
    subtitle: String,
    onSuccess: () -> Unit,
    onError: (String) -> Unit,
): () -> Unit {
    val context = LocalContext.current
    val activity = remember(context) { context.findActivity() }
    val unavailableMessage = stringResource(R.string.biometric_prompt_unavailable)
    val cancelledMessage = stringResource(R.string.biometric_authentication_cancelled)
    val cancelLabel = stringResource(R.string.cancel)
    val noHardwareMessage = stringResource(R.string.biometric_no_hardware)
    val hardwareUnavailableMessage = stringResource(R.string.biometric_hardware_unavailable)
    val noneEnrolledMessage = stringResource(R.string.biometric_none_enrolled)
    val authenticationUnavailableMessage = stringResource(R.string.biometric_authentication_unavailable)

    return remember(
        activity,
        title,
        subtitle,
        onSuccess,
        onError,
        unavailableMessage,
        cancelledMessage,
        cancelLabel,
        noHardwareMessage,
        hardwareUnavailableMessage,
        noneEnrolledMessage,
        authenticationUnavailableMessage,
    ) {
        fun launchPrompt() {
            val currentActivity = activity ?: run {
                onError(unavailableMessage)
                return
            }

            val manager = BiometricManager.from(currentActivity)
            val capability = manager.canAuthenticate(BiometricManager.Authenticators.BIOMETRIC_WEAK)
            if (capability != BiometricManager.BIOMETRIC_SUCCESS) {
                onError(
                    when (capability) {
                        BiometricManager.BIOMETRIC_ERROR_NO_HARDWARE -> noHardwareMessage
                        BiometricManager.BIOMETRIC_ERROR_HW_UNAVAILABLE -> hardwareUnavailableMessage
                        BiometricManager.BIOMETRIC_ERROR_NONE_ENROLLED -> noneEnrolledMessage
                        else -> authenticationUnavailableMessage
                    },
                )
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
                                -> cancelledMessage
                                else -> errString.toString()
                            }
                        )
                    }
                }
            )

            val promptInfo = BiometricPrompt.PromptInfo.Builder()
                .setTitle(title)
                .setSubtitle(subtitle)
                .setNegativeButtonText(cancelLabel)
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
