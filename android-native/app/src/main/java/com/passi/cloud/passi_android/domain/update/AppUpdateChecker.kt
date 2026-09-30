package com.passi.cloud.passi_android.domain.update

interface AppUpdateChecker {
    suspend fun isUpdateAvailable(): Result<Boolean>
}
