package com.passi.cloud.passi_android.data.repository

import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.data.remote.dto.SignupCheckRequestDto
import com.passi.cloud.passi_android.data.remote.dto.SignupConfirmationRequestDto
import com.passi.cloud.passi_android.data.remote.dto.SignupRequestDto
import com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.service.EnrollmentService
import com.passi.cloud.passi_android.domain.service.PendingEnrollment
import java.util.UUID

class BackendEnrollmentService(
    private val apiClient: PassiApiClient,
) : EnrollmentService {
    override suspend fun beginSignup(email: String, provider: Provider, deviceId: String): Result<PendingEnrollment> {
        val accountId = UUID.randomUUID().toString()
        val result = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.signup,
            payload = SignupRequestDto(
                email = email,
                deviceId = deviceId,
                userGuid = accountId,
            )
        )

        if (result.isSuccessful) {
            return Result.success(
                PendingEnrollment(
                    accountId = accountId,
                    email = email,
                    providerId = provider.id.toString(),
                )
            )
        }

        val errorMessage = if (result.statusCode == 400) apiClient.extractErrorMessage(result.body) else null
        return Result.failure(IllegalStateException(errorMessage ?: "Network error. Try again"))
    }

    override suspend fun confirmSignupCode(accountId: String, email: String, code: String, provider: Provider): Result<Unit> {
        val result = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.signupCheck,
            payload = SignupCheckRequestDto(
                username = email,
                code = code,
            )
        )

        if (result.isSuccessful) {
            return Result.success(Unit)
        }

        val errorMessage = if (result.statusCode == 400) apiClient.extractErrorMessage(result.body) else null
        return Result.failure(IllegalStateException(errorMessage ?: "Network error. Try again"))
    }

    override suspend fun finalizeSignup(
        pendingEnrollment: PendingEnrollment,
        provider: Provider,
        deviceId: String,
        generatedCertificate: GeneratedCertificate,
    ): Result<Unit> {
        val code = pendingEnrollment.confirmationCode
            ?: return Result.failure(IllegalStateException("Missing confirmation code"))

        val result = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.signupConfirmation,
            payload = SignupConfirmationRequestDto(
                email = pendingEnrollment.email,
                code = code,
                publicCert = generatedCertificate.publicCertBinary,
                guid = pendingEnrollment.accountId,
                deviceId = deviceId,
            )
        )

        if (result.isSuccessful) {
            return Result.success(Unit)
        }

        val errorMessage = if (result.statusCode == 400) apiClient.extractErrorMessage(result.body) else null
        return Result.failure(IllegalStateException(errorMessage ?: "Network error. Try again"))
    }
}
