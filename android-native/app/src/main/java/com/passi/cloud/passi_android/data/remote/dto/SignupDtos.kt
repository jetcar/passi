package com.passi.cloud.passi_android.data.remote.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class SignupRequestDto(
    @SerialName("Email")
    val email: String,
    @SerialName("DeviceId")
    val deviceId: String,
    @SerialName("UserGuid")
    val userGuid: String,
)

@Serializable
data class SignupCheckRequestDto(
    @SerialName("Username")
    val username: String,
    @SerialName("Code")
    val code: String,
)

@Serializable
data class SignupConfirmationRequestDto(
    @SerialName("Email")
    val email: String,
    @SerialName("Code")
    val code: String,
    @SerialName("PublicCert")
    val publicCert: String,
    @SerialName("Guid")
    val guid: String,
    @SerialName("DeviceId")
    val deviceId: String,
)