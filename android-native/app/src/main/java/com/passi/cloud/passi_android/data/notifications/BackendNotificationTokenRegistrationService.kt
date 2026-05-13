package com.passi.cloud.passi_android.data.notifications

import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.domain.notifications.NotificationTokenRegistrationService
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository

private data class DeviceTokenUpdateRequestDto(
    val Token: String,
    val DeviceId: String,
    val Platform: String,
)

class BackendNotificationTokenRegistrationService(
    private val apiClient: PassiApiClient,
    private val providersRepository: ProvidersRepository,
    private val deviceIdProvider: () -> String,
) : NotificationTokenRegistrationService {
    override suspend fun registerToken(token: String): Result<Unit> {
        val providers = providersRepository.getProviders()
        if (providers.isEmpty()) {
            return Result.success(Unit)
        }

        val deviceId = deviceIdProvider()
        providers.forEach { provider ->
            val result = apiClient.postJson(
                baseUrl = provider.baseUrl,
                path = provider.apiPaths.tokenUpdate,
                payload = DeviceTokenUpdateRequestDto(
                    Token = token,
                    DeviceId = deviceId,
                    Platform = "Android",
                ),
            )

            if (!result.isSuccessful) {
                val errorMessage = apiClient.extractErrorMessage(result.body)
                return Result.failure(IllegalStateException(errorMessage ?: "Token update failed"))
            }
        }

        return Result.success(Unit)
    }
}