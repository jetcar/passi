package com.passi.cloud.passi_android

import android.app.Application
import android.util.Log
import com.google.firebase.FirebaseApp
import com.google.firebase.messaging.FirebaseMessaging
import com.passi.cloud.passi_android.data.DefaultPassiAppContainer
import com.passi.cloud.passi_android.data.PassiAppContainer
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

class PassiApplication : Application() {
	companion object {
		private const val TAG = "PassiApplication"
		private const val MAX_STARTUP_TOKEN_ATTEMPTS = 3
		private const val STARTUP_TOKEN_RETRY_DELAY_MS = 5_000L
		private const val AUTHENTICATION_FAILED = "AUTHENTICATION_FAILED"
	}

	private val applicationScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

	val container: PassiAppContainer by lazy {
		DefaultPassiAppContainer(this)
	}

	override fun onCreate() {
		super.onCreate()

		// Ensure Firebase is initialized; ignore if already auto-initialized by google-services plugin
		runCatching { FirebaseApp.initializeApp(this) }

		if (FirebaseApp.getApps(this).isEmpty()) {
			Log.w(TAG, "Firebase is not configured; skipping startup token registration")
			return
		}

		fetchAndRegisterStartupToken()
	}

	private fun fetchAndRegisterStartupToken(attempt: Int = 1) {
		FirebaseMessaging.getInstance().token
			.addOnSuccessListener { token ->
				applicationScope.launch {
					val result = container.notificationTokenRegistrationService.registerToken(token)
					if (result.isFailure) {
						Log.w(
							TAG,
							"Failed to register Firebase token at startup: ${result.exceptionOrNull()?.message.orEmpty()}",
						)
					}
				}
			}
			.addOnFailureListener { error ->
				if (isAuthenticationFailure(error)) {
					Log.i(
						TAG,
						"Skipping Firebase startup token fetch because authentication failed; token will be requested again later.",
					)
					return@addOnFailureListener
				}

				if (attempt >= MAX_STARTUP_TOKEN_ATTEMPTS) {
					Log.w(TAG, "Failed to fetch Firebase token at startup after $attempt attempts", error)
					return@addOnFailureListener
				}

				applicationScope.launch {
					delay(STARTUP_TOKEN_RETRY_DELAY_MS * attempt)
					fetchAndRegisterStartupToken(attempt + 1)
				}
			}
	}

	private fun isAuthenticationFailure(error: Throwable): Boolean {
		var current: Throwable? = error
		while (current != null) {
			if (current.message?.contains(AUTHENTICATION_FAILED, ignoreCase = true) == true) {
				return true
			}
			current = current.cause
		}
		return false
	}
}