package com.passi.cloud.passi_android.domain.enrollment

interface CertificateGenerator {
    fun generate(email: String, pin: String?): GeneratedCertificate
}