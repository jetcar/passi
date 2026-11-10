package com.passi.cloud.passi_android.notifications

import android.app.Activity
import android.app.AlertDialog
import android.app.KeyguardManager
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.PowerManager
import android.provider.Settings
import android.util.Log
import com.passi.cloud.passi_android.MainActivity
import com.passi.cloud.passi_android.R

/** A push may bring Passi forward only while the user is already using an unlocked phone. */
object LoginRequestLauncher {
    fun shouldOpen(isInteractive: Boolean, isKeyguardLocked: Boolean, isDeviceLocked: Boolean): Boolean =
        isInteractive && !isKeyguardLocked && !isDeviceLocked

    fun openIfUnlocked(context: Context) {
        val power = context.getSystemService(PowerManager::class.java) ?: return
        val keyguard = context.getSystemService(KeyguardManager::class.java) ?: return
        if (!shouldOpen(power.isInteractive, keyguard.isKeyguardLocked, keyguard.isDeviceLocked)) return

        val intent = Intent(context, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP
            putExtra(MainActivity.EXTRA_OPEN_PENDING_SESSION, true)
        }
        // Background launches require Android's permission (e.g. Display over other apps).
        // The notification is already posted and remains available if Android blocks the launch.
        try {
            context.startActivity(intent)
        } catch (error: SecurityException) {
            Log.w("LoginRequestLauncher", "Automatic opening was blocked; use the login notification", error)
        }
    }
}

/**
 * Android blocks a background app from starting its own activity, so a login push can only open Passi while
 * the phone is unlocked if the user allowed "Display over other apps". Locked or sleeping phones receive
 * a notification to tap after unlocking. Ask once.
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
            .setTitle(activity.getString(R.string.permission_overlay_title))
            .setMessage(activity.getString(R.string.permission_overlay_message))
            .setPositiveButton(activity.getString(R.string.open_settings)) { _, _ ->
                activity.startActivity(
                    Intent(Settings.ACTION_MANAGE_OVERLAY_PERMISSION, Uri.parse("package:${activity.packageName}")),
                )
            }
            .setNegativeButton(activity.getString(R.string.not_now), null)
            .show()
        return true
    }

    fun resetAsked(context: Context) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().remove(KEY_ASKED).commit()
    }
}
