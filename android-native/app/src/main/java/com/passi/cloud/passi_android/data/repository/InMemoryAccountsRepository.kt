package com.passi.cloud.passi_android.data.repository

import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.NotificationSession
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import java.time.Instant
import java.time.temporal.ChronoUnit
import java.util.UUID

class InMemoryAccountsRepository : AccountsRepository {
    private val defaultProviderId = UUID.fromString("7a22cc55-4d18-4e02-bfa5-f6ce4913fbfb")

    private val accounts = MutableStateFlow(
        listOf(
            Account(
                id = UUID.fromString("4eeb9825-3028-4389-ac41-b6690b0edb9e"),
                providerId = defaultProviderId,
                email = "admin@passi.cloud",
                deviceId = "pending-device-id",
                isConfirmed = false,
                validFrom = Instant.now(),
                validTo = Instant.now().plus(365, ChronoUnit.DAYS),
            )
        )
    )

    override fun observeAccounts(): Flow<List<Account>> = accounts.asStateFlow()

    override suspend fun getAccounts(): List<Account> = accounts.value

    override suspend fun getAccount(accountId: UUID): Account? {
        return accounts.value.firstOrNull { it.id == accountId }
    }

    override suspend fun savePendingAccount(account: Account) {
        accounts.value = accounts.value + account
    }

    override suspend fun updateAccount(account: Account) {
        accounts.value = accounts.value.map { current ->
            if (current.id == account.id) account else current
        }
    }

    override suspend fun deleteAccount(accountId: UUID) {
        accounts.value = accounts.value.filterNot { it.id == accountId }
    }

    override suspend fun syncAccounts(): Result<Unit> = Result.success(Unit)

    override suspend fun pollPendingSession(): Result<NotificationSession?> = Result.success(null)
}