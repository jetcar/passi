package com.passi.cloud.passi_android.domain.enrollment

import java.time.Instant

data class GeneratedCertificate(
    val salt: String,
    val privateCertBinary: String,
    val publicCertBinary: String,
    val thumbprint: String,
    val validFrom: Instant,
    val validTo: Instant,
)