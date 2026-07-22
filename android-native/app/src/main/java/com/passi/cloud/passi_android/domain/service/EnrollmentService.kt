package com.passi.cloud.passi_android.domain.service

import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate
import java.util.UUID

data class PendingEnrollment(
    val accountId: String,
    val email: String,
    val providerId: String,
    val confirmationCode: String? = null,
)

interface EnrollmentService {
    suspend fun beginSignup(email: String, provider: Provider, deviceId: String): Result<PendingEnrollment>

    suspend fun confirmSignupCode(accountId: String, email: String, code: String, provider: Provider): Result<Unit>

    /**
     * Finalizes enrollment and returns the canonical account GUID the server persisted. For an
     * existing account this can differ from [PendingEnrollment.accountId]; the caller must store the
     * local account under the returned GUID so login push notifications match.
     */
    suspend fun finalizeSignup(
        pendingEnrollment: PendingEnrollment,
        provider: Provider,
        deviceId: String,
        generatedCertificate: GeneratedCertificate,
    ): Result<UUID>
}