package com.passi.cloud.passi_android.notifications

import android.app.Activity
import android.app.AlertDialog
import android.app.NotificationManager
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Build
import android.provider.Settings

/** Android 14+ may deny full-screen intents; then the user has to allow them in system settings. Ask once. */
object FullScreenIntentPrompt {
    private const val ANDROID_14 = 34
    private const val PREFS = "passi_ui"
    private const val KEY_ASKED = "full_screen_intent_asked"

    fun shouldPrompt(sdkInt: Int, canUseFullScreenIntent: Boolean, alreadyAsked: Boolean): Boolean =
        sdkInt >= ANDROID_14 && !canUseFullScreenIntent && !alreadyAsked

    fun maybePrompt(activity: Activity) {
        if (Build.VERSION.SDK_INT < ANDROID_14) return
        val prefs = activity.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
        val canUse = activity.getSystemService(NotificationManager::class.java).canUseFullScreenIntent()
        if (!shouldPrompt(Build.VERSION.SDK_INT, canUse, prefs.getBoolean(KEY_ASKED, false))) return

        prefs.edit().putBoolean(KEY_ASKED, true).apply()
        AlertDialog.Builder(activity)
            .setTitle("Show login requests right away")
            .setMessage("Allow Passi to use full-screen notifications so login requests open immediately, even when your phone is locked. You'll still need to unlock to approve.")
            .setPositiveButton("Open settings") { _, _ ->
                activity.startActivity(
                    Intent(Settings.ACTION_MANAGE_APP_USE_FULL_SCREEN_INTENT, Uri.parse("package:${activity.packageName}")),
                )
            }
            .setNegativeButton("Not now", null)
            .show()
    }

    fun resetAsked(context: Context) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().remove(KEY_ASKED).commit()
    }
}

/**
 * Android blocks a background app from starting its own activity, so a login push can only open Passi while
 * the phone is unlocked if the user allowed "Display over other apps". The full-screen intent only covers the
 * locked/screen-off case. Ask once.
 */
object OverlayPermissionPrompt {
    private const val PREFS = "passi_ui"
    private const val KEY_ASKED = "overlay_permission_asked"

    fun shouldPrompt(canDrawOverlays: Boolean, alreadyAsked: Boolean): Boolean =
        !canDrawOverlays && !alreadyAsked

    /** Returns true when the dialog was shown. */
    fun maybePrompt(activity: Activity): Boolean {
        val prefs = activity.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
        if (!shouldPrompt(Settings.canDrawOverlays(activity), prefs.getBoolean(KEY_ASKED, false))) return false

        prefs.edit().putBoolean(KEY_ASKED, true).apply()
        AlertDialog.Builder(activity)
            .setTitle("Open Passi for login requests")
            .setMessage("To open login requests automatically while you use your phone, allow Passi to \"Display over other apps\". Otherwise you'll need to tap the notification.")
            .setPositiveButton("Open settings") { _, _ ->
                activity.startActivity(
                    Intent(Settings.ACTION_MANAGE_OVERLAY_PERMISSION, Uri.parse("package:${activity.packageName}")),
                )
            }
            .setNegativeButton("Not now", null)
            .show()
        return true
    }

    fun resetAsked(context: Context) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().remove(KEY_ASKED).commit()
    }
}
