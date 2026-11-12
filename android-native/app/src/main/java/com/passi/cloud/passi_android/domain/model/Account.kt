package com.passi.cloud.passi_android.domain.model

import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection
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
    val keyAlias: String? = null,
    val keySecurityLevel: String? = null,
    val keyProtection: DeviceKeyProtection? = null,
    val pendingKey: PendingDeviceKey? = null,
    val legacyCleanupPending: Boolean = false,
    val cleanupKeyAlias: String? = null,
    val retiredKeyAliases: List<String>? = null,
    val cleanupAccountIds: List<String>? = null,
) {
    val isActive: Boolean
        get() = !inactive

    val requiresCertificateUpdate: Boolean
        get() = isActive && isConfirmed && keyAlias == null && privateCertBinary != null
}

/** Public-only durable checkpoint. Dates are strings so persistence needs no Instant adapter. */
data class PendingDeviceKey(
    val keyAlias: String,
    val keySecurityLevel: String,
    val publicCertBinary: String,
    val thumbprint: String,
    val validFrom: String,
    val validTo: String,
    val keyProtection: DeviceKeyProtection? = null,
    // Null is an older checkpoint whose submission outcome is unknown; never discard it.
    val submitted: Boolean? = null,
) {
    val mayHaveBeenSubmitted: Boolean get() = submitted != false
}
