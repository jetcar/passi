package com.passi.cloud.passi_android.domain.repository

import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.NotificationSession
import kotlinx.coroutines.flow.Flow
import java.util.UUID

interface AccountsRepository {
    fun observeAccounts(): Flow<List<Account>>

    suspend fun getAccounts(): List<Account>

    suspend fun getAccount(accountId: UUID): Account?

    suspend fun savePendingAccount(account: Account)

    suspend fun updateAccount(account: Account)

    suspend fun deleteAccount(accountId: UUID)

    suspend fun syncAccounts(): Result<Unit>

    suspend fun pollPendingSession(): Result<NotificationSession?>
}