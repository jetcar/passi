package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.notifications.LoginRequestLauncher
import org.junit.Test

class LockScreenApprovalTests {
    @Test
    fun loginPushOpensOnlyOnAnAwakeUnlockedPhone() {
        assertThat(LoginRequestLauncher.shouldOpen(true, false, false)).isTrue()
    }

    @Test
    fun loginPushDoesNotOpenOverTheLockScreenEvenWhenDeviceIsTrusted() {
        assertThat(LoginRequestLauncher.shouldOpen(true, true, false)).isFalse()
        assertThat(LoginRequestLauncher.shouldOpen(true, true, true)).isFalse()
    }

    @Test
    fun loginPushDoesNotOpenWhenDeviceAuthenticationIsRequired() {
        assertThat(LoginRequestLauncher.shouldOpen(true, false, true)).isFalse()
    }

    @Test
    fun loginPushDoesNotWakeASleepingPhoneEvenWithoutAScreenLock() {
        for (keyguardLocked in listOf(false, true)) {
            for (deviceLocked in listOf(false, true)) {
                assertThat(LoginRequestLauncher.shouldOpen(false, keyguardLocked, deviceLocked)).isFalse()
            }
        }
    }
}
