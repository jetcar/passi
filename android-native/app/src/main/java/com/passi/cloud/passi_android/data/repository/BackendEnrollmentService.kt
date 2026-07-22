package com.passi.cloud.passi_android.data.repository

import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.data.remote.dto.SignupConfirmationRequestDto
import com.passi.cloud.passi_android.data.remote.dto.SignupConfirmationResponseDto
import com.passi.cloud.passi_android.data.remote.dto.SignupCheckRequestDto
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

        val errorMessage = apiClient.extractErrorMessage(result.body)

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

        val errorMessage = apiClient.extractErrorMessage(result.body)

        return Result.failure(IllegalStateException(errorMessage ?: "Network error. Try again"))
    }

    override suspend fun finalizeSignup(
        pendingEnrollment: PendingEnrollment,
        provider: Provider,
        deviceId: String,
        generatedCertificate: GeneratedCertificate,
    ): Result<UUID> {
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
            // Adopt the canonical GUID the server returns. For an existing account this differs from
            // the locally generated accountId; falling back to the local one keeps older backends
            // (which return no body) working.
            val canonicalGuid = apiClient
                .parseBody(result.body, SignupConfirmationResponseDto::class.java)
                ?.accountGuid
                ?.takeIf { it.isNotBlank() }
                ?.let { runCatching { UUID.fromString(it) }.getOrNull() }
                ?: UUID.fromString(pendingEnrollment.accountId)

            return Result.success(canonicalGuid)
        }

        val errorMessage = apiClient.extractErrorMessage(result.body)

        return Result.failure(IllegalStateException(errorMessage ?: "Network error. Try again"))
    }
}