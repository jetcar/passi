package com.passi.cloud.passi_android.domain.enrollment

import com.passi.cloud.passi_android.domain.model.PendingDeviceKey
import java.time.Instant

fun GeneratedCertificate.checkpoint(): PendingDeviceKey {
    check(privateCertBinary == null && salt == null) { "Exportable keys are not accepted" }
    return PendingDeviceKey(
        requireNotNull(keyAlias), requireNotNull(keySecurityLevel), publicCertBinary,
        thumbprint, validFrom.toString(), validTo.toString(), submitted = false,
    )
}

fun PendingDeviceKey.certificate() = GeneratedCertificate(
    salt = null, privateCertBinary = null, publicCertBinary = publicCertBinary,
    thumbprint = thumbprint, validFrom = Instant.parse(validFrom), validTo = Instant.parse(validTo),
    keyAlias = keyAlias, keySecurityLevel = keySecurityLevel,
)
