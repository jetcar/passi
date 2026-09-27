package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.notifications.FullScreenIntentPrompt
import org.junit.Test

class LockScreenApprovalTests {
    @Test
    fun fullScreenPermissionPromptOnlyOnAndroid14PlusWhenMissingAndNotYetAsked() {
        assertThat(FullScreenIntentPrompt.shouldPrompt(sdkInt = 33, canUseFullScreenIntent = false, alreadyAsked = false)).isFalse()
        assertThat(FullScreenIntentPrompt.shouldPrompt(sdkInt = 34, canUseFullScreenIntent = true, alreadyAsked = false)).isFalse()
        assertThat(FullScreenIntentPrompt.shouldPrompt(sdkInt = 34, canUseFullScreenIntent = false, alreadyAsked = true)).isFalse()
        assertThat(FullScreenIntentPrompt.shouldPrompt(sdkInt = 34, canUseFullScreenIntent = false, alreadyAsked = false)).isTrue()
        assertThat(FullScreenIntentPrompt.shouldPrompt(sdkInt = 37, canUseFullScreenIntent = false, alreadyAsked = false)).isTrue()
    }
}
