package com.passi.cloud.passi_android.notifications

import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.google.gson.Gson
import com.passi.cloud.passi_android.MainActivity
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.R
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import java.util.UUID

private data class FirebaseNotificationPayload(
    val Sender: String? = null,
    val SessionId: String? = null,
    val ReturnHost: String? = null,
    val AccountGuid: String? = null,
    val DeviceId: String? = null,
    val UnlinkToken: String? = null,
    val ApiBaseUrl: String? = null,
)

class PassiFirebaseMessagingService : FirebaseMessagingService() {
    private val serviceScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private val gson = Gson()

    override fun onMessageReceived(message: RemoteMessage) {
        super.onMessageReceived(message)

        val title = message.notification?.title ?: message.data["title"] ?: getString(R.string.notification_login_title)
        val body = message.data["body"] ?: message.notification?.body
        val payload = body?.let { runCatching { gson.fromJson(it, FirebaseNotificationPayload::class.java) }.getOrNull() }

        MainActivity.ensureNotificationChannel(this)
        val container = (applicationContext as? PassiApplication)?.container
        val accountId = payload?.AccountGuid?.let { runCatching { UUID.fromString(it) }.getOrNull() }
        val accountIsMissing = container != null && accountId != null && runCatching {
            runBlocking { container.accountsRepository.getAccount(accountId) == null }
        }.getOrDefault(false)
        val currentDeviceId = runCatching { container?.preferences?.getOrCreateDeviceId() }.getOrNull()
        val targetsCurrentDevice = !payload?.DeviceId.isNullOrBlank() && payload.DeviceId == currentDeviceId
        val canUnlink = accountIsMissing &&
            targetsCurrentDevice &&
            !payload.UnlinkToken.isNullOrBlank() &&
            !payload.ApiBaseUrl.isNullOrBlank()

        if (canUnlink) {
            PassiNotifications.showMissingAccountNotification(
                context = this,
                apiBaseUrl = payload.ApiBaseUrl,
                unlinkToken = payload.UnlinkToken,
            )
            return
        }

        if (accountIsMissing) {
            return
        }

        PassiNotifications.showLoginNotification(
            context = this,
            title = title,
            contentText = payload?.ReturnHost ?: getString(R.string.notification_login_body),
        )
        LoginRequestLauncher.openIfUnlocked(this)
    }

    override fun onNewToken(token: String) {
        super.onNewToken(token)
        val container = (applicationContext as? PassiApplication)?.container ?: return
        serviceScope.launch {
            container.notificationTokenRegistrationService.registerToken(token)
        }
    }
}
