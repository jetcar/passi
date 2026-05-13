package com.passi.cloud.passi_android.domain.auth

import com.passi.cloud.passi_android.domain.model.NotificationSession

interface AuthSessionService {
    suspend fun syncAndPollPendingSession(): Result<NotificationSession?>

    suspend fun authorize(session: NotificationSession, pin: String?): Result<Unit>

    suspend fun authorizeWithBiometric(session: NotificationSession): Result<Unit>

    suspend fun cancel(session: NotificationSession): Result<Unit>
}