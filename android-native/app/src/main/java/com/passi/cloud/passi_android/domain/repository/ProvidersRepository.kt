package com.passi.cloud.passi_android.domain.repository

import com.passi.cloud.passi_android.domain.model.Provider
import kotlinx.coroutines.flow.Flow
import java.util.UUID

interface ProvidersRepository {
    fun observeProviders(): Flow<List<Provider>>

    suspend fun getProviders(): List<Provider>

    suspend fun getProvider(providerId: UUID?): Provider?

    suspend fun saveProvider(provider: Provider)

    suspend fun deleteProvider(providerId: UUID)
}