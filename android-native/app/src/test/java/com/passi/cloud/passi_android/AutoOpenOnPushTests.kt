package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.notifications.OverlayPermissionPrompt
import org.junit.Test

class AutoOpenOnPushTests {
    @Test
    fun overlayPromptOnlyWhenPermissionMissingAndNotYetAsked() {
        assertThat(OverlayPermissionPrompt.shouldPrompt(canDrawOverlays = true, alreadyAsked = false)).isFalse()
        assertThat(OverlayPermissionPrompt.shouldPrompt(canDrawOverlays = false, alreadyAsked = true)).isFalse()
        assertThat(OverlayPermissionPrompt.shouldPrompt(canDrawOverlays = true, alreadyAsked = true)).isFalse()
        assertThat(OverlayPermissionPrompt.shouldPrompt(canDrawOverlays = false, alreadyAsked = false)).isTrue()
    }
}
