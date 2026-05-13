package com.passi.cloud.passi_android.domain.model

import java.util.UUID

data class Provider(
    val id: UUID,
    val name: String,
    val baseUrl: String,
    val apiPaths: ApiPaths,
    val isDefault: Boolean,
)
