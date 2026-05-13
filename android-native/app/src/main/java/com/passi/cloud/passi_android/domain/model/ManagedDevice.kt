package com.passi.cloud.passi_android.domain.model

import java.time.Instant

data class ManagedDevice(
    val deviceId: String,
    val platform: String?,
    val creationTime: Instant,
    val isCurrent: Boolean,
) {
    val displayName: String
        get() = when {
            isCurrent -> "This device"
            !platform.isNullOrBlank() -> platform
            else -> "Registered device"
        }

    val shortIdentifier: String
        get() = if (deviceId.length <= 10) deviceId else deviceId.take(8)
}