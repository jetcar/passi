package com.passi.cloud.passi_android

import android.Manifest
import android.content.pm.PackageManager
import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.content.Intent
import android.os.Build
import android.os.Bundle
import androidx.activity.compose.setContent
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.fragment.app.FragmentActivity
import com.passi.cloud.passi_android.ui.PassiApp
import com.passi.cloud.passi_android.notifications.FullScreenIntentPrompt
import com.passi.cloud.passi_android.notifications.PassiNotifications

class MainActivity : FragmentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        ensureNotificationChannel(this)
        requestNotificationPermissionIfNeeded()
        FullScreenIntentPrompt.maybePrompt(this)
        handleIntent(intent)
        setContent {
            PassiApp()
        }
    }

    override fun onResume() {
        super.onResume()
        // The app is open now, so pending login notifications are stale; clear the whole list.
        PassiNotifications.clearAll(this)
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        handleIntent(intent)
    }

    private fun handleIntent(intent: Intent?) {
        val fromLoginNotification = intent?.getBooleanExtra(EXTRA_OPEN_PENDING_SESSION, false) == true
        // Show the login challenge over the lock screen (and wake it) only when opened for a login request;
        // approving still requires unlocking (see UnlockGate in SessionChallengeRoute).
        setShowWhenLocked(fromLoginNotification)
        setTurnScreenOn(fromLoginNotification)
        if (fromLoginNotification) {
            (application as PassiApplication).container.notificationOpenStore.requestOpen()
        }
    }

    private fun requestNotificationPermissionIfNeeded() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) {
            return
        }
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS) == PackageManager.PERMISSION_GRANTED) {
            return
        }
        ActivityCompat.requestPermissions(this, arrayOf(Manifest.permission.POST_NOTIFICATIONS), NOTIFICATION_PERMISSION_REQUEST_CODE)
    }

    companion object {
        const val EXTRA_OPEN_PENDING_SESSION = "open_pending_session"
        const val NOTIFICATION_CHANNEL_ID = "passi_notification_channel_id"
        private const val NOTIFICATION_PERMISSION_REQUEST_CODE = 1001

        fun ensureNotificationChannel(context: Context) {
            if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) {
                return
            }

            val channel = NotificationChannel(
                NOTIFICATION_CHANNEL_ID,
                "Passi Notifications",
                NotificationManager.IMPORTANCE_HIGH,
            ).apply {
                description = "Authentication requests for Passi"
            }
            val manager = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            manager.createNotificationChannel(channel)
        }
    }
}