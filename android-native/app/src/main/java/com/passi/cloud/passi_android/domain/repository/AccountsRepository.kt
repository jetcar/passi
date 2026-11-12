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

    suspend fun discardUnsubmittedPendingKey(accountId: UUID, expectedAlias: String) {
        val current = getAccount(accountId) ?: error("Account not found")
        val pending = current.pendingKey ?: return
        check(pending.keyAlias == expectedAlias && !pending.mayHaveBeenSubmitted) { "Submitted keys require account recovery" }
        updateAccount(current.copy(pendingKey = null,
            retiredKeyAliases = (current.retiredKeyAliases.orEmpty() + pending.keyAlias).distinct()))
    }

    suspend fun replaceAccount(previousId: UUID, account: Account) {
        if (previousId == account.id) updateAccount(account) else {
            savePendingAccount(account)
            deleteAccount(previousId)
        }
    }

    suspend fun syncAccounts(): Result<Unit>

    suspend fun pollPendingSession(): Result<NotificationSession?>
}
