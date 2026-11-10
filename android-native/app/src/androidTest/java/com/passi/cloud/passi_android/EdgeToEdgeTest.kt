package com.passi.cloud.passi_android

import android.Manifest
import android.content.res.Configuration
import android.os.Build
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.hasSetTextAction
import androidx.compose.ui.test.junit4.createEmptyComposeRule
import androidx.compose.ui.test.onNodeWithContentDescription
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import androidx.test.core.app.ActivityScenario
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.uiautomator.UiDevice
import org.junit.After
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test

class EdgeToEdgeTest {
    @get:Rule val composeRule = createEmptyComposeRule()
    private val instrumentation = InstrumentationRegistry.getInstrumentation()
    private val context = instrumentation.targetContext
    private val device = UiDevice.getInstance(instrumentation)
    private lateinit var scenario: ActivityScenario<MainActivity>

    @Before
    fun launch() {
        context.getSharedPreferences("passi_ui", 0).edit()
            .putBoolean("overlay_permission_asked", true)
            .putBoolean("full_screen_intent_asked", true)
            .commit()
        if (Build.VERSION.SDK_INT >= 33) {
            instrumentation.uiAutomation.grantRuntimePermission(context.packageName, Manifest.permission.POST_NOTIFICATIONS)
        }
        device.setOrientationNatural()
        scenario = ActivityScenario.launch(MainActivity::class.java)
    }

    @After
    fun close() {
        scenario.close()
        device.unfreezeRotation()
    }

    @Test
    fun navigationActionsAvoidSystemBarsAndCutouts() {
        assertNavigationActionsSafe()
    }

    @Test
    fun navigationActionsAvoidSystemBarsAndCutoutsInLandscape() {
        device.setOrientationLeft()
        composeRule.waitUntil(10_000) {
            var landscape = false
            scenario.onActivity { landscape = it.resources.configuration.orientation == Configuration.ORIENTATION_LANDSCAPE }
            landscape
        }
        assertNavigationActionsSafe()
    }

    @Test
    fun keyboardDoesNotCoverEnrollmentControls() {
        composeRule.onNodeWithContentDescription("Add").performClick()
        composeRule.onNodeWithText("I agree").performClick()
        composeRule.onNode(hasSetTextAction()).performClick()
        composeRule.waitUntil(10_000) {
            var visible = false
            scenario.onActivity {
                visible = ViewCompat.getRootWindowInsets(it.window.decorView)
                    ?.isVisible(WindowInsetsCompat.Type.ime()) == true
            }
            visible
        }
        assertSafe(composeRule.onNode(hasSetTextAction()).assertIsDisplayed().fetchSemanticsNode().boundsInWindow)
        assertSafe(composeRule.onNodeWithText("Register").assertIsDisplayed().fetchSemanticsNode().boundsInWindow)
        assertSafe(composeRule.onNodeWithText("Cancel").assertIsDisplayed().fetchSemanticsNode().boundsInWindow)
    }

    private fun assertNavigationActionsSafe() {
        for (label in listOf("Menu", "Add", "Delete")) {
            assertSafe(composeRule.onNodeWithContentDescription(label).assertIsDisplayed().fetchSemanticsNode().boundsInWindow)
        }
    }

    private fun assertSafe(bounds: Rect) {
        scenario.onActivity { activity ->
            val view = activity.window.decorView
            val insets = requireNotNull(ViewCompat.getRootWindowInsets(view)).getInsets(
                WindowInsetsCompat.Type.systemBars() or WindowInsetsCompat.Type.displayCutout() or WindowInsetsCompat.Type.ime(),
            )
            assertTrue("Control overlaps left system inset: $bounds", bounds.left >= insets.left - 1)
            assertTrue("Control overlaps top system inset: $bounds", bounds.top >= insets.top - 1)
            assertTrue("Control overlaps right system inset: $bounds", bounds.right <= view.width - insets.right + 1)
            assertTrue("Control overlaps bottom system inset: $bounds", bounds.bottom <= view.height - insets.bottom + 1)
        }
    }
}
