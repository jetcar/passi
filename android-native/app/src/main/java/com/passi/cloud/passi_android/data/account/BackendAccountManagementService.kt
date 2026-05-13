package com.passi.cloud.passi_android.data.account

import com.google.gson.FieldNamingPolicy
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.google.gson.reflect.TypeToken
import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.data.remote.dto.DeleteDeviceRequestDto
import com.passi.cloud.passi_android.data.remote.dto.ManagedDeviceDto
import com.passi.cloud.passi_android.data.remote.dto.ManageDevicesRequestDto
import com.passi.cloud.passi_android.data.remote.dto.toDomain
import com.passi.cloud.passi_android.domain.account.AccountManagementService
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.ManagedDevice
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import java.security.MessageDigest
import java.util.Base64

class BackendAccountManagementService(
    private val apiClient: PassiApiClient,
    private val providersRepository: ProvidersRepository,
    private val gson: Gson = GsonBuilder()
        .setFieldNamingPolicy(FieldNamingPolicy.UPPER_CAMEL_CASE)
        .disableHtmlEscaping()
        .create(),
) : AccountManagementService {
    override suspend fun deleteAccount(account: Account): Result<Unit> {
        val provider = providersRepository.getProvider(account.providerId)
            ?: return Result.failure(IllegalStateException("Provider not found"))
        val thumbprint = resolveThumbprint(account)
        if (thumbprint == null) {
            // No certificate was ever registered server-side for unconfirmed accounts; local removal is safe.
            return if (!account.isConfirmed) {
                Result.success(Unit)
            } else {
                Result.failure(IllegalStateException("Certificate thumbprint is missing"))
            }
        }

        val result = apiClient.delete(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.deleteAccount + "?accountGuid=${account.id}&thumbprint=${thumbprint}",
        )

        return if (result.isSuccessful) {
            Result.success(Unit)
        } else {
            Result.failure(IllegalStateException(apiClient.extractErrorMessage(result.body) ?: "Network error. Try again"))
        }
    }

    override suspend fun getDevices(account: Account, currentDeviceId: String): Result<List<ManagedDevice>> {
        val provider = providersRepository.getProvider(account.providerId)
            ?: return Result.failure(IllegalStateException("Provider not found"))
        val thumbprint = resolveThumbprint(account)
            ?: return Result.failure(IllegalStateException("Certificate thumbprint is missing"))

        val result = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.listDevices,
            payload = ManageDevicesRequestDto(
                accountGuid = account.id.toString(),
                thumbprint = thumbprint,
                currentDeviceId = currentDeviceId,
            ),
        )

        if (!result.isSuccessful) {
            return Result.failure(IllegalStateException(apiClient.extractErrorMessage(result.body) ?: "Network error. Try again"))
        }

        val type = object : TypeToken<List<ManagedDeviceDto>>() {}.type
        val devices = runCatching {
            gson.fromJson<List<ManagedDeviceDto>>(result.body, type).orEmpty().map { it.toDomain() }
        }.getOrElse {
            return Result.failure(IllegalStateException("Failed to parse devices"))
        }

        return Result.success(devices)
    }

    override suspend fun deleteDevice(account: Account, deviceId: String, currentDeviceId: String): Result<Unit> {
        val provider = providersRepository.getProvider(account.providerId)
            ?: return Result.failure(IllegalStateException("Provider not found"))
        val thumbprint = resolveThumbprint(account)
            ?: return Result.failure(IllegalStateException("Certificate thumbprint is missing"))

        val result = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.deleteDevice,
            payload = DeleteDeviceRequestDto(
                accountGuid = account.id.toString(),
                thumbprint = thumbprint,
                currentDeviceId = currentDeviceId,
                deviceId = deviceId,
            ),
        )

        return if (result.isSuccessful) {
            Result.success(Unit)
        } else {
            Result.failure(IllegalStateException(apiClient.extractErrorMessage(result.body) ?: "Network error. Try again"))
        }
    }

    private fun resolveThumbprint(account: Account): String? {
        if (account.thumbprint != null) return account.thumbprint
        val publicCertBinary = account.publicCertBinary ?: return null
        return runCatching {
            val publicBytes = Base64.getDecoder().decode(publicCertBinary)
            MessageDigest.getInstance("SHA-1")
                .digest(publicBytes)
                .joinToString(separator = "") { byte -> "%02X".format(byte) }
        }.getOrNull()
    }
}