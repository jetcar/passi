package com.passi.cloud.passi_android.ui

import android.os.Build
import androidx.biometric.BiometricManager
import androidx.biometric.BiometricPrompt
import androidx.core.content.ContextCompat
import androidx.fragment.app.FragmentActivity
import com.passi.cloud.passi_android.R
import com.passi.cloud.passi_android.data.auth.SignatureAuthorization
import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection
import kotlinx.coroutines.CancellableContinuation
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withContext
import java.lang.ref.WeakReference
import java.security.Signature
import kotlin.coroutines.resumeWithException

/** Activity attachment and all prompt callbacks run on the main thread. No authorization is cached. */
class DeviceKeyAuthorization : SignatureAuthorization {
    private var activity = WeakReference<FragmentActivity>(null)
    private var pending: CancellableContinuation<Signature>? = null
    private var prompt: BiometricPrompt? = null

    fun attach(activity: FragmentActivity) {
        if (this.activity.get() !== activity) cancelPending()
        this.activity = WeakReference(activity)
    }

    fun detach(activity: FragmentActivity) {
        if (this.activity.get() !== activity) return
        cancelPending()
        this.activity.clear()
    }

    private fun cancelPending() {
        val continuation = pending
        pending = null
        prompt?.cancelAuthentication()
        prompt = null
        if (continuation?.isActive == true) {
            continuation.resumeWithException(IllegalStateException("Device authorization cancelled"))
        }
    }

    override suspend fun authorize(
        signature: Signature,
        protection: DeviceKeyProtection,
    ): Signature = withContext(Dispatchers.Main.immediate) {
        check(Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) { "Device authorization requires Android 11 or later" }
        check(pending == null) { "Another device authorization is already in progress" }
        val host = activity.get()?.takeUnless { it.isFinishing || it.isDestroyed }
            ?: error("Open Passi to authorize use of the device signing key")
        val authenticators = when (protection) {
            DeviceKeyProtection.BIOMETRIC -> BiometricManager.Authenticators.BIOMETRIC_STRONG
            DeviceKeyProtection.DEVICE_CREDENTIAL -> BiometricManager.Authenticators.DEVICE_CREDENTIAL
            DeviceKeyProtection.BIOMETRIC_OR_DEVICE_CREDENTIAL ->
                BiometricManager.Authenticators.BIOMETRIC_STRONG or BiometricManager.Authenticators.DEVICE_CREDENTIAL
        }
        check(BiometricManager.from(host).canAuthenticate(authenticators) == BiometricManager.BIOMETRIC_SUCCESS) {
            "Set up a secure device screen lock to use the signing key"
        }
        suspendCancellableCoroutine { continuation ->
            pending = continuation
            val callback = object : BiometricPrompt.AuthenticationCallback() {
                private fun complete(result: Result<Signature>) {
                    if (pending !== continuation) return
                    pending = null
                    prompt = null
                    if (continuation.isActive) continuation.resumeWith(result)
                }

                override fun onAuthenticationSucceeded(result: BiometricPrompt.AuthenticationResult) {
                    if (result.cryptoObject?.signature !== signature) {
                        complete(Result.failure(IllegalStateException("Device authorization did not authorize this operation")))
                    } else {
                        complete(Result.success(signature))
                    }
                }

                override fun onAuthenticationError(errorCode: Int, errString: CharSequence) {
                    complete(Result.failure(IllegalStateException("Device authorization cancelled or unavailable ($errorCode)")))
                }

                override fun onAuthenticationFailed() {
                    val failedPrompt = prompt
                    complete(Result.failure(IllegalStateException("Device authorization failed")))
                    failedPrompt?.cancelAuthentication()
                }
            }
            val currentPrompt = BiometricPrompt(host, ContextCompat.getMainExecutor(host), callback)
            prompt = currentPrompt
            continuation.invokeOnCancellation {
                ContextCompat.getMainExecutor(host).execute {
                    if (pending === continuation) cancelPending()
                }
            }
            try {
                val infoBuilder = BiometricPrompt.PromptInfo.Builder()
                    .setTitle(host.getString(R.string.device_key_authorize_title))
                    .setSubtitle(host.getString(when (protection) {
                        DeviceKeyProtection.BIOMETRIC -> R.string.device_key_authorize_biometric_subtitle
                        DeviceKeyProtection.DEVICE_CREDENTIAL -> R.string.device_key_authorize_phone_pin_subtitle
                        DeviceKeyProtection.BIOMETRIC_OR_DEVICE_CREDENTIAL -> R.string.device_key_authorize_subtitle
                    }))
                    .setAllowedAuthenticators(authenticators)
                    .setConfirmationRequired(true)
                if (protection == DeviceKeyProtection.BIOMETRIC) {
                    infoBuilder.setNegativeButtonText(host.getString(R.string.cancel))
                }
                val info = infoBuilder.build()
                // Credential + CryptoObject is supported starting at API 30, never use a standalone prompt.
                // https://developer.android.com/reference/androidx/biometric/BiometricPrompt#authenticate(androidx.biometric.BiometricPrompt.PromptInfo,androidx.biometric.BiometricPrompt.CryptoObject)
                currentPrompt.authenticate(info, BiometricPrompt.CryptoObject(signature))
            } catch (failure: Exception) {
                if (pending === continuation) {
                    pending = null
                    prompt = null
                    currentPrompt.cancelAuthentication()
                    if (continuation.isActive) continuation.resumeWithException(failure)
                }
            }
        }
    }
}
