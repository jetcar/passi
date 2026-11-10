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
import androidx.core.view.WindowCompat
import androidx.appcompat.app.AppCompatActivity
import com.passi.cloud.passi_android.ui.PassiApp
import com.passi.cloud.passi_android.notifications.OverlayPermissionPrompt
import com.passi.cloud.passi_android.notifications.PassiNotifications

class MainActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        WindowCompat.enableEdgeToEdge(window)
        // Passi currently uses a light Compose theme, regardless of the system theme.
        WindowCompat.getInsetsController(window, window.decorView).apply {
            isAppearanceLightStatusBars = true
            isAppearanceLightNavigationBars = true
        }
        ensureNotificationChannel(this)
        requestNotificationPermissionIfNeeded()
        OverlayPermissionPrompt.maybePrompt(this)
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
        // Notification taps use Android's normal unlock flow before the challenge is visible.
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
                context.getString(R.string.notification_channel_name),
                NotificationManager.IMPORTANCE_HIGH,
            ).apply {
                description = context.getString(R.string.notification_channel_description)
            }
            val manager = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            manager.createNotificationChannel(channel)
        }
    }
}
