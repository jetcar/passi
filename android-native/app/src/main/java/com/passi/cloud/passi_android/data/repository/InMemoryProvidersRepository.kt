package com.passi.cloud.passi_android.data.repository

import com.passi.cloud.passi_android.domain.model.ApiPaths
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import java.util.UUID

class InMemoryProvidersRepository : ProvidersRepository {
    private val defaultProvider = Provider(
        id = UUID.fromString("7a22cc55-4d18-4e02-bfa5-f6ce4913fbfb"),
        name = "passi",
        baseUrl = "https://passi.cloud/passiapi",
        apiPaths = ApiPaths.defaultPaths(),
        isDefault = true,
    )

    private val providers = MutableStateFlow(
        listOf(defaultProvider)
    )

    override fun observeProviders(): Flow<List<Provider>> = providers.asStateFlow()

    override suspend fun getProviders(): List<Provider> = providers.value

    override suspend fun getProvider(providerId: UUID?): Provider? {
        return providers.value.firstOrNull { it.id == providerId }
            ?: providers.value.firstOrNull { it.isDefault }
    }

    override suspend fun saveProvider(provider: Provider) {
        providers.value = providers.value
            .filterNot { it.id == provider.id }
            .plus(provider)
            .sortedBy { it.name.lowercase() }
    }

    override suspend fun deleteProvider(providerId: UUID) {
        providers.value = providers.value.filterNot { it.id == providerId }
    }
}