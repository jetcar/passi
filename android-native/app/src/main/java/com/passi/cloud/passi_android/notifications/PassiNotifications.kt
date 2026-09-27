package com.passi.cloud.passi_android.notifications

import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import com.passi.cloud.passi_android.MainActivity

object PassiNotifications {
    /** One login request is relevant at a time, so each new push replaces the previous one instead of stacking. */
    const val LOGIN_NOTIFICATION_ID = 1001

    /** Removes everything Passi has in the notification list, incl. entries the FCM SDK posted for us. */
    fun clearAll(context: Context) = NotificationManagerCompat.from(context).cancelAll()

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
}
