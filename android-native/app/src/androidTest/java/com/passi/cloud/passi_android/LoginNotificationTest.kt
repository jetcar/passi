package com.passi.cloud.passi_android

import android.Manifest
import android.app.Activity
import android.app.KeyguardManager
import android.app.Notification
import android.app.NotificationManager
import android.content.pm.PackageManager
import android.os.Build
import android.os.PowerManager
import android.os.SystemClock
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.test.core.app.ActivityScenario
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.runner.lifecycle.ActivityLifecycleMonitorRegistry
import androidx.test.runner.lifecycle.Stage
import androidx.test.uiautomator.By
import androidx.test.uiautomator.UiDevice
import androidx.test.uiautomator.Until
import com.passi.cloud.passi_android.notifications.LoginRequestLauncher
import com.passi.cloud.passi_android.notifications.OverlayPermissionPrompt
import com.passi.cloud.passi_android.notifications.PassiNotifications
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class LoginNotificationTest {
    private val context = InstrumentationRegistry.getInstrumentation().targetContext
    private val manager = context.getSystemService(NotificationManager::class.java)

    @Before
    fun setUp() {
        if (Build.VERSION.SDK_INT >= 33) {
            InstrumentationRegistry.getInstrumentation().uiAutomation
                .grantRuntimePermission(context.packageName, Manifest.permission.POST_NOTIFICATIONS)
        }
        MainActivity.ensureNotificationChannel(context)
        manager.cancelAll()
        waitFor { activeCount() == 0 }
    }

    @After
    fun tearDown() = manager.cancelAll()

    @Test
    fun repeatedLoginPushReplacesPreviousNotificationInsteadOfStacking() {
        PassiNotifications.showLoginNotification(context, "Passi login", "mail.example.com")
        PassiNotifications.showLoginNotification(context, "Passi login", "passi.cloud")

        waitFor { activeCount() == 1 }
        assertEquals(1, activeCount())
    }

    @Test
    fun missingAccountNotificationOffersDeviceRemovalWithoutOpeningLogin() {
        PassiNotifications.showMissingAccountNotification(
            context = context,
            apiBaseUrl = "https://passi.cloud/passiapi",
            unlinkToken = "single-use-token",
        )

        waitFor { activeCount() == 1 }
        val posted = manager.activeNotifications.single().notification
        assertEquals("Account is not on this device", posted.extras.getString(Notification.EXTRA_TITLE))
        assertEquals("Remove device", posted.actions.single().title.toString())
        assertEquals(null, posted.fullScreenIntent)
    }

    @Test
    fun openingAppPreservesMissingAccountRemovalAction() {
        PassiNotifications.showMissingAccountNotification(
            context = context,
            apiBaseUrl = "https://passi.cloud/passiapi",
            unlinkToken = "single-use-token",
        )

        ActivityScenario.launch(MainActivity::class.java).use {
            waitFor { activeCount() == 1 }
            assertEquals("Remove device", manager.activeNotifications.single().notification.actions.single().title.toString())
        }
    }

    @Test
    fun missingAccountNotificationsForDifferentLinksDoNotReplaceEachOther() {
        PassiNotifications.showMissingAccountNotification(
            context = context,
            apiBaseUrl = "https://passi.cloud/passiapi",
            unlinkToken = "first-token",
        )
        PassiNotifications.showMissingAccountNotification(
            context = context,
            apiBaseUrl = "https://passi.cloud/passiapi",
            unlinkToken = "second-token",
        )

        waitFor { activeCount() == 2 }
        assertEquals(2, activeCount())
    }

    @Test
    fun openingAppClearsPendingNotifications() {
        // e.g. left in the tray by the FCM SDK while the app was in the background, or by older app versions
        NotificationManagerCompat.from(context).apply {
            notify(11, stale("first"))
            notify(12, stale("second"))
        }
        waitFor { activeCount() == 2 }
        assertEquals(2, activeCount())

        ActivityScenario.launch(MainActivity::class.java).use {
            waitFor { activeCount() == 0 }
            assertEquals(0, activeCount())
        }
    }

    @Test
    fun appDoesNotRequestFullScreenIntentPermission() {
        val info = context.packageManager.getPackageInfo(context.packageName, PackageManager.GET_PERMISSIONS)
        assertFalse(info.requestedPermissions.orEmpty().contains(Manifest.permission.USE_FULL_SCREEN_INTENT))
    }

    @Test
    fun appDeclaresOverlayPermissionSoPushCanOpenItWhileUnlocked() {
        val info = context.packageManager.getPackageInfo(context.packageName, PackageManager.GET_PERMISSIONS)
        assertTrue(info.requestedPermissions.orEmpty().contains(Manifest.permission.SYSTEM_ALERT_WINDOW))
    }

    @Test
    fun loginNotificationHasATapActionWithoutAFullScreenIntent() {
        PassiNotifications.showLoginNotification(context, "Passi login", "passi.cloud")
        waitFor { activeCount() == 1 }

        val posted = manager.activeNotifications.single()
        assertEquals(MainActivity.NOTIFICATION_CHANNEL_ID, posted.notification.channelId)
        assertEquals(Notification.VISIBILITY_PUBLIC, posted.notification.visibility)
        assertNotNull(posted.notification.contentIntent)
        assertNull(posted.notification.fullScreenIntent)
        assertEquals(Notification.CATEGORY_STATUS, posted.notification.category)
    }

    @Test
    fun loginChannelIsHighImportance() {
        // Lock-screen visibility comes from the notification (VISIBILITY_PUBLIC); Android ignores it on channels.
        val channel = manager.getNotificationChannel(MainActivity.NOTIFICATION_CHANNEL_ID)
        assertEquals(NotificationManager.IMPORTANCE_HIGH, channel.importance)
    }

    @Test
    fun loginPushStaysInNotificationsWhilePhoneIsLocked() {
        val keyguard = context.getSystemService(KeyguardManager::class.java)
        val power = context.getSystemService(PowerManager::class.java)
        val device = UiDevice.getInstance(InstrumentationRegistry.getInstrumentation())
        shell("appops set ${context.packageName} SYSTEM_ALERT_WINDOW allow")
        device.pressHome()
        shell("locksettings set-pin 1111")
        try {
            shell("input keyevent KEYCODE_SLEEP")
            waitFor { !power.isInteractive && keyguard.isDeviceLocked }
            assertTrue(keyguard.isDeviceLocked)

            PassiNotifications.showLoginNotification(context, "Passi login", "passi.cloud")
            LoginRequestLauncher.openIfUnlocked(context)
            waitFor { activeCount() == 1 }
            SystemClock.sleep(1_000)

            assertEquals(1, activeCount())
            assertFalse("Passi must not open above the lock screen", resumedActivity() is MainActivity)
            assertFalse("Passi must not wake the screen", power.isInteractive)

            shell("input keyevent KEYCODE_WAKEUP")
            assertTrue(keyguard.isKeyguardLocked)
            LoginRequestLauncher.openIfUnlocked(context)
            SystemClock.sleep(1_000)
            assertFalse("An awake locked phone must not open Passi", resumedActivity() is MainActivity)
            assertEquals(1, activeCount())

            // The notification's activity must remain behind keyguard until the user unlocks.
            device.openNotification()
            val notification = device.wait(Until.findObject(By.text("Passi login")), 5_000)
            assertNotNull("Login notification should be available on the lock screen", notification)
            notification.click()
            SystemClock.sleep(1_000)
            assertTrue(keyguard.isDeviceLocked)
            assertFalse("Challenge must not be visible before unlock", device.currentPackageName == context.packageName)

            // Complete the real unlock flow; clearing credentials cancels the pending notification action.
            shell("input text 1111")
            shell("input keyevent KEYCODE_ENTER")
            waitFor { !keyguard.isDeviceLocked && resumedActivity() is MainActivity }
            assertFalse("Phone should be unlocked", keyguard.isDeviceLocked)
            assertTrue("Notification tap must open Passi after unlock", resumedActivity() is MainActivity)
        } finally {
            shell("locksettings clear --old 1111")
            shell("input keyevent KEYCODE_WAKEUP")
            shell("wm dismiss-keyguard")
        }
    }

    @Test
    fun loginPushOpensAppWhenPhoneIsAwakeAndUnlocked() {
        val device = UiDevice.getInstance(InstrumentationRegistry.getInstrumentation())
        shell("appops set ${context.packageName} SYSTEM_ALERT_WINDOW allow")
        shell("input keyevent KEYCODE_WAKEUP")
        shell("wm dismiss-keyguard")
        device.pressHome()

        PassiNotifications.showLoginNotification(context, "Passi login", "passi.cloud")
        LoginRequestLauncher.openIfUnlocked(context)

        waitFor { resumedActivity() is MainActivity }
        assertTrue("An unlocked phone should open Passi automatically", resumedActivity() is MainActivity)
    }

    @Test
    fun appAsksOnceToAllowDisplayOverOtherAppsWhenDenied() {
        val device = UiDevice.getInstance(InstrumentationRegistry.getInstrumentation())
        OverlayPermissionPrompt.resetAsked(context)
        shell("appops set ${context.packageName} SYSTEM_ALERT_WINDOW default")
        ActivityScenario.launch(MainActivity::class.java).use {
            assertNotNull("prompt should appear", device.wait(Until.findObject(By.textContains("Display over other apps")), 5_000))
            device.findObject(By.text("Not now"))?.click()
        }
        ActivityScenario.launch(MainActivity::class.java).use {
            assertTrue("asked only once", device.wait(Until.gone(By.textContains("Display over other apps")), 3_000))
        }
    }

    private fun resumedActivity(): Activity? {
        var activity: Activity? = null
        InstrumentationRegistry.getInstrumentation().runOnMainSync {
            activity = ActivityLifecycleMonitorRegistry.getInstance().getActivitiesInStage(Stage.RESUMED).firstOrNull()
        }
        return activity
    }

    private fun shell(command: String) {
        InstrumentationRegistry.getInstrumentation().uiAutomation.executeShellCommand(command).close()
        SystemClock.sleep(500)
    }

    private fun stale(text: String) = NotificationCompat.Builder(context, MainActivity.NOTIFICATION_CHANNEL_ID)
        .setSmallIcon(android.R.drawable.sym_def_app_icon)
        .setContentTitle("Passi login")
        .setContentText(text)
        .build()

    private fun activeCount() = manager.activeNotifications.size

    private fun waitFor(timeoutMs: Long = 5_000, condition: () -> Boolean) {
        val deadline = SystemClock.uptimeMillis() + timeoutMs
        while (!condition() && SystemClock.uptimeMillis() < deadline) SystemClock.sleep(100)
    }
}
