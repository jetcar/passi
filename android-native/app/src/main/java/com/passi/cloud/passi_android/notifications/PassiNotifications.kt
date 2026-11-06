package com.passi.cloud.passi_android.notifications

import android.app.PendingIntent
import android.app.NotificationManager
import android.content.Context
import android.content.Intent
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import com.passi.cloud.passi_android.MainActivity
import java.util.UUID

object PassiNotifications {
    /** One login request is relevant at a time, so each new push replaces the previous one instead of stacking. */
    const val LOGIN_NOTIFICATION_ID = 1001
    private const val MISSING_ACCOUNT_NOTIFICATION_ID = 1002
    private const val MISSING_ACCOUNT_TAG_PREFIX = "passi_missing_account_"

    /** Removes stale login entries while preserving missing-account removal actions. */
    fun clearAll(context: Context) {
        val manager = context.getSystemService(NotificationManager::class.java)
        manager.activeNotifications
            .filterNot { it.tag?.startsWith(MISSING_ACCOUNT_TAG_PREFIX) == true }
            .forEach { manager.cancel(it.tag, it.id) }
    }

    fun clearLoginNotification(context: Context) =
        NotificationManagerCompat.from(context).cancel(LOGIN_NOTIFICATION_ID)

    fun clearMissingAccountNotification(context: Context, unlinkToken: String) =
        NotificationManagerCompat.from(context).cancel(missingAccountTag(unlinkToken), MISSING_ACCOUNT_NOTIFICATION_ID)

    fun showLoginNotification(context: Context, title: String, contentText: String) {
        val intent = Intent(context, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP
            putExtra(MainActivity.EXTRA_OPEN_PENDING_SESSION, true)
        }
        val pendingIntent = PendingIntent.getActivity(
            context,
            0,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )

        val builder = NotificationCompat.Builder(context, MainActivity.NOTIFICATION_CHANNEL_ID)
            .setSmallIcon(android.R.drawable.sym_def_app_icon)
            .setContentTitle(title)
            .setContentText(contentText)
            .setPriority(NotificationCompat.PRIORITY_MAX)
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)
            .setFullScreenIntent(pendingIntent, true)
            .setVisibility(NotificationCompat.VISIBILITY_PUBLIC)
            .setCategory(NotificationCompat.CATEGORY_CALL)

        NotificationManagerCompat.from(context).notify(LOGIN_NOTIFICATION_ID, builder.build())
    }

    fun showMissingAccountNotification(
        context: Context,
        apiBaseUrl: String,
        unlinkToken: String,
    ) {
        val builder = NotificationCompat.Builder(context, MainActivity.NOTIFICATION_CHANNEL_ID)
            .setSmallIcon(android.R.drawable.sym_def_app_icon)
            .setContentTitle("Account is not on this device")
            .setContentText("This device is still linked to an account that is no longer installed")
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setAutoCancel(false)
            .setVisibility(NotificationCompat.VISIBILITY_PRIVATE)

        if (apiBaseUrl.isNotBlank() && unlinkToken.isNotBlank()) {
            val unlinkIntent = PendingIntent.getBroadcast(
                context,
                unlinkToken.hashCode(),
                DeviceUnlinkReceiver.intent(context, apiBaseUrl, unlinkToken),
                PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
            )
            builder.addAction(0, "Remove device", unlinkIntent)
        }

        NotificationManagerCompat.from(context).notify(
            missingAccountTag(unlinkToken),
            MISSING_ACCOUNT_NOTIFICATION_ID,
            builder.build(),
        )
    }

    private fun missingAccountTag(unlinkToken: String): String =
        MISSING_ACCOUNT_TAG_PREFIX + UUID.nameUUIDFromBytes(unlinkToken.toByteArray(Charsets.UTF_8))
}
