package com.passi.cloud.passi_android.notifications

import android.content.Intent
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.google.gson.Gson
import com.passi.cloud.passi_android.MainActivity
import com.passi.cloud.passi_android.PassiApplication
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch

private data class FirebaseNotificationPayload(
    val Sender: String? = null,
    val SessionId: String? = null,
    val ReturnHost: String? = null,
    val AccountGuid: String? = null,
)

class PassiFirebaseMessagingService : FirebaseMessagingService() {
    private val serviceScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private val gson = Gson()

    override fun onMessageReceived(message: RemoteMessage) {
        super.onMessageReceived(message)

        val title = message.notification?.title ?: message.data["title"] ?: "Passi login"
        val body = message.data["body"] ?: message.notification?.body
        val payload = body?.let { runCatching { gson.fromJson(it, FirebaseNotificationPayload::class.java) }.getOrNull() }

        MainActivity.ensureNotificationChannel(this)
        openApplication()
        PassiNotifications.showLoginNotification(
            context = this,
            title = title,
            contentText = payload?.ReturnHost ?: "Open Passi to review the request",
        )
    }

    override fun onNewToken(token: String) {
        super.onNewToken(token)
        val container = (applicationContext as? PassiApplication)?.container ?: return
        serviceScope.launch {
            container.notificationTokenRegistrationService.registerToken(token)
        }
    }

    // Android lets this through only if the user allowed "Display over other apps" (see OverlayPermissionPrompt);
    // otherwise it is silently blocked and the notification's full-screen intent / tap opens the app instead.
    private fun openApplication() {
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP
            putExtra(MainActivity.EXTRA_OPEN_PENDING_SESSION, true)
        }
        startActivity(intent)
    }
}
