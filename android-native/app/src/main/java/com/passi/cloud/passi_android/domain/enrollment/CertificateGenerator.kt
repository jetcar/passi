package com.passi.cloud.passi_android.domain.enrollment

interface CertificateGenerator {
    fun generate(email: String, protection: DeviceKeyProtection): GeneratedCertificate

    /** Fail closed unless the provider can reopen this exact usable private key. */
    fun validate(certificate: GeneratedCertificate) { error("Device key validation is unavailable") }

    /** Dispose only a freshly generated key which no durable account checkpoint owns. */
    fun dispose(certificate: GeneratedCertificate) { error("Device key disposal is unavailable") }
}
