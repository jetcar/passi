package com.passi.cloud.passi_android.data.repository

import com.passi.cloud.passi_android.data.local.PassiPreferences
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.NotificationSession
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import java.util.UUID

class PersistentAccountsRepository(
    private val preferences: PassiPreferences,
) : AccountsRepository {
    private val accounts = MutableStateFlow(preferences.readAccounts())

    override fun observeAccounts(): Flow<List<Account>> = accounts.asStateFlow()

    override suspend fun getAccounts(): List<Account> = accounts.value

    override suspend fun getAccount(accountId: UUID): Account? {
        return accounts.value.firstOrNull { it.id == accountId }
    }

    override suspend fun savePendingAccount(account: Account) {
        accounts.value = accounts.value + account
        persist()
    }

    override suspend fun updateAccount(account: Account) {
        accounts.value = accounts.value.map { current ->
            if (current.id == account.id) account else current
        }
        persist()
    }

    override suspend fun deleteAccount(accountId: UUID) {
        accounts.value = accounts.value.filterNot { it.id == accountId }
        preferences.deleteBiometricMaterial(accountId)
        persist()
    }

    override suspend fun syncAccounts(): Result<Unit> = Result.success(Unit)

    override suspend fun pollPendingSession(): Result<NotificationSession?> = Result.success(null)

    private fun persist() {
        preferences.writeAccounts(accounts.value)
    }
}