package com.passi.cloud.passi_android.domain.model

import java.time.Instant
import java.util.UUID

data class Account(
    val id: UUID,
    val providerId: UUID?,
    val email: String,
    val deviceId: String,
    val isConfirmed: Boolean,
    val thumbprint: String? = null,
    val validFrom: Instant,
    val validTo: Instant,
    val inactive: Boolean = false,
    val salt: String? = null,
    val privateCertBinary: String? = null,
    val publicCertBinary: String? = null,
    val pinLength: Int = 0,
    val hasFingerprint: Boolean = false,
) {
    val isActive: Boolean
        get() = !inactive
}