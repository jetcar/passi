package com.passi.cloud.passi_android.domain.model

import java.time.Instant
import java.util.UUID

data class NotificationSession(
    val sender: String,
    val confirmationColor: ConfirmationColor,
    val sessionId: UUID,
    val expirationTime: Instant,
    val randomString: String,
    val returnHost: String,
    val accountId: UUID,
    /** 2-digit number shown in the browser; null when the server predates number matching. */
    val confirmationNumber: Int? = null,
)