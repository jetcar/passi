package com.passi.cloud.passi_android.data.repository

import com.passi.cloud.passi_android.data.local.AccountStorage
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.NotificationSession
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.emitAll
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import java.util.UUID

class PersistentAccountsRepository(
    private val preferences: AccountStorage,
    private val deleteKey: (String) -> Unit,
) : AccountsRepository {
    private val accounts = MutableStateFlow(preferences.readAccounts())
    private val mutex = Mutex()

    private var startupCleanupComplete = false

    override fun observeAccounts(): Flow<List<Account>> = flow {
        withStoreLock { }
        emitAll(accounts.asStateFlow())
    }
    override suspend fun getAccounts(): List<Account> = withStoreLock { accounts.value }
    override suspend fun getAccount(accountId: UUID): Account? = withStoreLock { accounts.value.firstOrNull { it.id == accountId } }

    override suspend fun savePendingAccount(account: Account) = withStoreLock {
        persist(accounts.value.filterNot { it.id == account.id } + account)
    }

    override suspend fun updateAccount(account: Account) = withStoreLock {
        val previous = accounts.value.firstOrNull { it.id == account.id } ?: error("Account not found")
        // Stale sync/UI writes must never restore a retired/exportable key or discard a pending rotation.
        check(previous.keyAlias == null || account.keyAlias == previous.keyAlias ||
            (previous.pendingKey != null && account.keyAlias == previous.pendingKey.keyAlias)) { "Stale account key update" }
        val samePendingKey = previous.pendingKey?.let { old -> account.pendingKey?.let { next ->
            old.copy(submitted = next.submitted) == next && (!old.mayHaveBeenSubmitted || next.mayHaveBeenSubmitted)
        } } == true
        check(previous.pendingKey == null || samePendingKey ||
            account.keyAlias == previous.pendingKey.keyAlias) { "Pending rotation cannot be discarded" }
        persist(accounts.value.map { if (it.id == account.id) account else it })
        if (account.legacyCleanupPending) runCatching { cleanLegacy(account) }
        Unit
    }

    override suspend fun discardUnsubmittedPendingKey(accountId: UUID, expectedAlias: String) = withStoreLock {
        val current = accounts.value.firstOrNull { it.id == accountId } ?: error("Account not found")
        val pending = current.pendingKey ?: return@withStoreLock
        check(pending.keyAlias == expectedAlias && !pending.mayHaveBeenSubmitted) { "Submitted keys require account recovery" }
        // Checkpoint the discarded alias before deletion. A failure can be retried by later cleanup/deletion.
        val discarded = current.copy(pendingKey = null,
            retiredKeyAliases = (current.retiredKeyAliases.orEmpty() + pending.keyAlias).distinct())
        persist(accounts.value.map { if (it.id == accountId) discarded else it })
        runCatching {
            deleteKey(pending.keyAlias)
            persist(accounts.value.map { if (it.id == accountId) it.copy(
                retiredKeyAliases = it.retiredKeyAliases.orEmpty().filterNot { alias -> alias == pending.keyAlias }) else it })
        }
        Unit
    }

    override suspend fun replaceAccount(previousId: UUID, account: Account) = withStoreLock {
        val replaced = accounts.value.filter { it.id == previousId || it.id == account.id }
        val obsoleteAliases = replaced.flatMap {
            listOfNotNull(it.keyAlias, it.pendingKey?.keyAlias, it.cleanupKeyAlias) + it.retiredKeyAliases.orEmpty()
        }.distinct().filterNot { it == account.keyAlias }
        val finalized = account.copy(legacyCleanupPending = true,
            cleanupKeyAlias = null, retiredKeyAliases = obsoleteAliases,
            cleanupAccountIds = (replaced.flatMap { it.cleanupAccountIds.orEmpty() } +
                listOf(previousId.toString(), account.id.toString())).distinct())
        persist(accounts.value.filterNot { it.id == previousId || it.id == account.id } + finalized)
        runCatching { cleanLegacy(finalized) }
        Unit
    }

    override suspend fun deleteAccount(accountId: UUID) = withStoreLock {
        accounts.value.firstOrNull { it.id == accountId }?.let { account ->
            (listOfNotNull(account.keyAlias, account.pendingKey?.keyAlias, account.cleanupKeyAlias) +
                account.retiredKeyAliases.orEmpty()).distinct().forEach(deleteKey)
            account.cleanupAccountIds.orEmpty().map(UUID::fromString).filterNot { it == accountId }
                .forEach(preferences::deleteBiometricMaterial)
        }
        preferences.deleteBiometricMaterial(accountId)
        persist(accounts.value.filterNot { it.id == accountId })
    }

    override suspend fun syncAccounts(): Result<Unit> = Result.success(Unit)
    override suspend fun pollPendingSession(): Result<NotificationSession?> = Result.success(null)

    private fun cleanLegacy(account: Account) {
        check(account.keyAlias != null && account.privateCertBinary == null)
        preferences.deleteBiometricMaterial(account.id)
        account.cleanupAccountIds.orEmpty().map(UUID::fromString).filterNot { it == account.id }
            .forEach(preferences::deleteBiometricMaterial)
        (listOfNotNull(account.cleanupKeyAlias) + account.retiredKeyAliases.orEmpty())
            .distinct().filterNot { it == account.keyAlias }.forEach(deleteKey)
        persist(accounts.value.map {
            if (it.id == account.id) it.copy(legacyCleanupPending = false, cleanupKeyAlias = null, retiredKeyAliases = null,
                cleanupAccountIds = null) else it
        })
    }

    private fun persist(updated: List<Account>) {
        check(updated.none { it.keyAlias != null && (it.privateCertBinary != null || it.salt != null || it.pinLength != 0) }) {
            "Device-held accounts cannot contain exportable key material"
        }
        preferences.writeAccounts(updated) // checked synchronous disk commit BEFORE publishing in memory
        accounts.value = updated
    }

    private suspend fun <T> withStoreLock(block: () -> T): T = withContext(Dispatchers.IO) {
        mutex.withLock {
            if (!startupCleanupComplete) {
                accounts.value.filter { it.legacyCleanupPending }.forEach { runCatching { cleanLegacy(it) } }
                startupCleanupComplete = true
            }
            block()
        }
    }
}
