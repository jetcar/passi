package com.passi.cloud.passi_android.data.remote.dto

import com.passi.cloud.passi_android.domain.model.ManagedDevice
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import java.time.Instant

@Serializable
data class AuthorizeRequestDto(
    @SerialName("SignedHash")
    val signedHash: String,
    @SerialName("PublicCertThumbprint")
    val publicCertThumbprint: String,
    @SerialName("SessionId")
    val sessionId: String,
)

@Serializable
data class SyncAccountsRequestDto(
    @SerialName("DeviceId")
    val deviceId: String,
    @SerialName("Guids")
    val guids: List<String>,
)

@Serializable
data class GetAllSessionRequestDto(
    @SerialName("DeviceId")
    val deviceId: String,
)

@Serializable
data class NotificationDto(
    @SerialName("Sender")
    val sender: String,
    @SerialName("ConfirmationColor")
    val confirmationColor: Any?,
    @SerialName("SessionId")
    val sessionId: String,
    @SerialName("ExpirationTime")
    val expirationTime: String,
    @SerialName("RandomString")
    val randomString: String,
    @SerialName("ReturnHost")
    val returnHost: String,
    @SerialName("AccountGuid")
    val accountGuid: String,
)

@Serializable
data class AccountMinDto(
    @SerialName("Username")
    val username: String,
    @SerialName("UserGuid")
    val userGuid: String,
)

@Serializable
data class ManageDevicesRequestDto(
    @SerialName("AccountGuid")
    val accountGuid: String,
    @SerialName("Thumbprint")
    val thumbprint: String,
    @SerialName("CurrentDeviceId")
    val currentDeviceId: String,
)

@Serializable
data class DeleteDeviceRequestDto(
    @SerialName("AccountGuid")
    val accountGuid: String,
    @SerialName("Thumbprint")
    val thumbprint: String,
    @SerialName("CurrentDeviceId")
    val currentDeviceId: String,
    @SerialName("DeviceId")
    val deviceId: String,
)

@Serializable
data class ManagedDeviceDto(
    @SerialName("DeviceId")
    val deviceId: String,
    @SerialName("Platform")
    val platform: String? = null,
    @SerialName("CreationTime")
    val creationTime: String,
    @SerialName("IsCurrent")
    val isCurrent: Boolean,
)

fun ManagedDeviceDto.toDomain(): ManagedDevice = ManagedDevice(
    deviceId = deviceId,
    platform = platform,
    creationTime = Instant.parse(creationTime),
    isCurrent = isCurrent,
)