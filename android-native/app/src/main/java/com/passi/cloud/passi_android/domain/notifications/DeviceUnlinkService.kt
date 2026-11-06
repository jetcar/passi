package com.passi.cloud.passi_android.domain.notifications

interface DeviceUnlinkService {
    suspend fun unlinkDevice(apiBaseUrl: String, unlinkToken: String): Result<Unit>
}
