package com.passi.cloud.passi_android.domain.notifications

interface NotificationTokenRegistrationService {
    suspend fun registerToken(token: String): Result<Unit>
}