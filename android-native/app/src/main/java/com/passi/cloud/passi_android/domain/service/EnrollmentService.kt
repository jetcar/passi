package com.passi.cloud.passi_android.domain.service

import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate

data class PendingEnrollment(
    val accountId: String,
    val email: String,
    val providerId: String,
    val confirmationCode: String? = null,
)

interface EnrollmentService {
    suspend fun beginSignup(email: String, provider: Provider, deviceId: String): Result<PendingEnrollment>

    suspend fun confirmSignupCode(accountId: String, email: String, code: String, provider: Provider): Result<Unit>

    suspend fun finalizeSignup(
        pendingEnrollment: PendingEnrollment,
        provider: Provider,
        deviceId: String,
        generatedCertificate: GeneratedCertificate,
    ): Result<Unit>
}