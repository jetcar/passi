package com.passi.cloud.passi_android.data.notifications

import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.domain.notifications.DeviceUnlinkService
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository

class DeviceUnlinkRequestException(
    message: String,
    val retryable: Boolean,
) : IllegalStateException(message)

private data class UnlinkDeviceRequestDto(
    val UnlinkToken: String,
)

class BackendDeviceUnlinkService(
    private val apiClient: PassiApiClient,
    private val providersRepository: ProvidersRepository,
) : DeviceUnlinkService {
    override suspend fun unlinkDevice(apiBaseUrl: String, unlinkToken: String): Result<Unit> {
        if (apiBaseUrl.isBlank() || unlinkToken.isBlank()) {
            return Result.failure(IllegalArgumentException("Missing device unlink data"))
        }

        val provider = providersRepository.getProviders().firstOrNull {
            it.baseUrl.trimEnd('/').equals(apiBaseUrl.trimEnd('/'), ignoreCase = true)
        } ?: return Result.failure(IllegalArgumentException("Unknown notification provider"))

        val result = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.unlinkDevice,
            payload = UnlinkDeviceRequestDto(UnlinkToken = unlinkToken),
        )
        if (!result.isSuccessful) {
            val error = apiClient.extractErrorMessage(result.body)
            return Result.failure(
                DeviceUnlinkRequestException(
                    message = error ?: "Device unlink failed",
                    retryable = result.statusCode == 0 || result.statusCode >= 500,
                )
            )
        }

        return Result.success(Unit)
    }
}
