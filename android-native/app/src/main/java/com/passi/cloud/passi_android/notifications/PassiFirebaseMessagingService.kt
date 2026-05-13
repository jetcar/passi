package com.passi.cloud.passi_android.notifications

import android.app.PendingIntent
import android.content.Intent
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.google.gson.Gson
import com.passi.cloud.passi_android.MainActivity
import com.passi.cloud.passi_android.PassiApplication
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import java.util.UUID

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
        showNotification(
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

    private fun openApplication() {
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP
            putExtra(MainActivity.EXTRA_OPEN_PENDING_SESSION, true)
        }
        startActivity(intent)
    }

    private fun showNotification(title: String, contentText: String) {
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP
            putExtra(MainActivity.EXTRA_OPEN_PENDING_SESSION, true)
        }
        val pendingIntent = PendingIntent.getActivity(
            this,
            0,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )

        val builder = NotificationCompat.Builder(this, MainActivity.NOTIFICATION_CHANNEL_ID)
            .setSmallIcon(android.R.drawable.sym_def_app_icon)
            .setContentTitle(title)
            .setContentText(contentText)
            .setPriority(NotificationCompat.PRIORITY_MAX)
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)
            .setFullScreenIntent(pendingIntent, true)

        NotificationManagerCompat.from(this).notify(UUID.randomUUID().hashCode(), builder.build())
    }
}