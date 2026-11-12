package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.data.local.AccountStorage
import com.passi.cloud.passi_android.data.repository.PersistentAccountsRepository
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.PendingDeviceKey
import kotlinx.coroutines.runBlocking
import org.junit.Test
import java.time.Instant
import java.util.UUID

class PersistentKeyLifecycleTests {
    private fun legacy() = Account(UUID.randomUUID(), UUID.randomUUID(), "user@example.com", "device", true,
        thumbprint = "old", validFrom = Instant.parse("2026-01-01T00:00:00Z"),
        validTo = Instant.parse("2027-01-01T00:00:00Z"), salt = "salt", privateCertBinary = "pkcs12",
        publicCertBinary = "old-public", pinLength = 4, hasFingerprint = true)

    private fun pending(alias: String = "passi.account.pending") = PendingDeviceKey(alias, "TEE",
        "new-public", "new", "2026-01-01T00:00:00Z", "2027-01-01T00:00:00Z")

    private fun migrated(old: Account, alias: String = "passi.account.new") = old.copy(
        keyAlias = alias, keySecurityLevel = "TEE", privateCertBinary = null, salt = null,
        publicCertBinary = "new-public", thumbprint = "new", pinLength = 0, hasFingerprint = false,
        pendingKey = null)

    @Test fun failedDurableCommitDoesNotPublishChangedAccount() = runBlocking<Unit> {
        val old = legacy()
        val storage = MemoryStorage(listOf(old))
        val repository = PersistentAccountsRepository(storage) { error("Unexpected key cleanup") }
        storage.failWrites = true
        assertThat(runCatching { repository.updateAccount(old.copy(pendingKey = pending())) }.isFailure).isTrue()
        assertThat(repository.getAccount(old.id)).isEqualTo(old)
        assertThat(storage.accounts).containsExactly(old)
        assertThat(storage.cleanedBiometrics).isEmpty()
    }

    @Test fun startupCleansConfirmedMigrationWithoutDeletingActiveKey() = runBlocking<Unit> {
        val old = legacy()
        val accepted = migrated(old).copy(legacyCleanupPending = true, cleanupKeyAlias = "passi.account.retired")
        val storage = MemoryStorage(listOf(accepted))
        val deleted = mutableListOf<String>()
        val repository = PersistentAccountsRepository(storage, deleted::add)
        val cleaned = repository.getAccount(old.id)!!
        assertThat(deleted).containsExactly("passi.account.retired")
        assertThat(storage.cleanedBiometrics).containsExactly(old.id)
        assertThat(cleaned.keyAlias).isEqualTo(accepted.keyAlias)
        assertThat(cleaned.privateCertBinary).isNull()
        assertThat(cleaned.legacyCleanupPending).isFalse()
        assertThat(cleaned.cleanupKeyAlias).isNull()
        assertThat(storage.accounts).containsExactly(cleaned)
    }

    @Test fun startupDoesNotCleanUnacceptedPendingMigrationOrLegacyAccount() = runBlocking<Unit> {
        val old = legacy().copy(pendingKey = pending())
        val storage = MemoryStorage(listOf(old))
        val deleted = mutableListOf<String>()
        val repository = PersistentAccountsRepository(storage, deleted::add)
        assertThat(repository.getAccount(old.id)).isEqualTo(old)
        assertThat(storage.cleanedBiometrics).isEmpty()
        assertThat(deleted).isEmpty()
        assertThat(storage.writeCount).isEqualTo(0)
    }

    @Test fun corruptedLegacyCleanupMarkerCannotDeleteLegacyMaterial() = runBlocking<Unit> {
        val old = legacy().copy(legacyCleanupPending = true)
        val storage = MemoryStorage(listOf(old))
        val deleted = mutableListOf<String>()
        val repository = PersistentAccountsRepository(storage, deleted::add)
        assertThat(repository.getAccount(old.id)).isEqualTo(old)
        assertThat(storage.cleanedBiometrics).isEmpty()
        assertThat(deleted).isEmpty()
    }

    @Test fun secureCleanupFailureRetainsMarkerAndRetriesOnNextStartup() = runBlocking<Unit> {
        val accepted = migrated(legacy()).copy(legacyCleanupPending = true, cleanupKeyAlias = "passi.account.retired")
        val storage = MemoryStorage(listOf(accepted)).apply { failBiometricCleanup = true }
        val deleted = mutableListOf<String>()
        val first = PersistentAccountsRepository(storage, deleted::add)
        assertThat(first.getAccount(accepted.id)).isEqualTo(accepted)
        assertThat(deleted).isEmpty()
        storage.failBiometricCleanup = false
        val restarted = PersistentAccountsRepository(storage, deleted::add)
        assertThat(restarted.getAccount(accepted.id)!!.legacyCleanupPending).isFalse()
        assertThat(deleted).containsExactly("passi.account.retired")
        assertThat(storage.cleanedBiometrics).containsExactly(accepted.id)
    }

    @Test fun cleanupMarkerCommitFailureKeepsNewKeyAndCleanupIsIdempotent() = runBlocking<Unit> {
        val accepted = migrated(legacy()).copy(legacyCleanupPending = true, cleanupKeyAlias = "passi.account.retired")
        val storage = MemoryStorage(listOf(accepted)).apply { failWrites = true }
        val deleted = mutableListOf<String>()
        val first = PersistentAccountsRepository(storage, deleted::add)
        assertThat(first.getAccount(accepted.id)).isEqualTo(accepted)
        assertThat(storage.accounts).containsExactly(accepted)
        storage.failWrites = false
        val restarted = PersistentAccountsRepository(storage, deleted::add)
        assertThat(restarted.getAccount(accepted.id)!!.keyAlias).isEqualTo(accepted.keyAlias)
        assertThat(restarted.getAccount(accepted.id)!!.legacyCleanupPending).isFalse()
        assertThat(deleted).containsExactly("passi.account.retired", "passi.account.retired")
        assertThat(deleted).doesNotContain(accepted.keyAlias)
    }

    @Test fun deletionRemovesCurrentPendingAndRetiredAliasesAndBiometricCopies() = runBlocking<Unit> {
        // A cleanup alias is also cleaned on explicit deletion even if a previous attempt was interrupted.
        val account = migrated(legacy()).copy(pendingKey = pending(), cleanupKeyAlias = "passi.account.retired",
            retiredKeyAliases = listOf("passi.account.older", "passi.account.retired"))
        val storage = MemoryStorage(listOf(account))
        val deleted = mutableListOf<String>()
        val repository = PersistentAccountsRepository(storage, deleted::add)
        repository.deleteAccount(account.id)
        assertThat(deleted).containsExactly(account.keyAlias, account.pendingKey!!.keyAlias,
            account.cleanupKeyAlias, "passi.account.older")
        assertThat(storage.cleanedBiometrics).containsExactly(account.id)
        assertThat(repository.getAccounts()).isEmpty()
        assertThat(storage.accounts).isEmpty()
        repository.deleteAccount(account.id)
        assertThat(deleted).hasSize(4)
    }

    @Test fun staleSnapshotCannotRestoreLegacyKeyAfterMigration() = runBlocking<Unit> {
        val old = legacy()
        val accepted = migrated(old)
        val storage = MemoryStorage(listOf(accepted))
        val repository = PersistentAccountsRepository(storage) { error("Unexpected cleanup") }
        assertThat(runCatching { repository.updateAccount(old.copy(inactive = false)) }.isFailure).isTrue()
        assertThat(repository.getAccount(old.id)).isEqualTo(accepted)
        assertThat(storage.accounts).containsExactly(accepted)
    }

    @Test fun staleSnapshotCannotDiscardOrReplacePendingRotation() = runBlocking<Unit> {
        val old = legacy()
        val checkpoint = old.copy(pendingKey = pending())
        val storage = MemoryStorage(listOf(checkpoint))
        val repository = PersistentAccountsRepository(storage) { error("Unexpected cleanup") }
        assertThat(runCatching { repository.updateAccount(old) }.isFailure).isTrue()
        assertThat(runCatching { repository.updateAccount(old.copy(pendingKey = pending("passi.account.other"))) }.isFailure).isTrue()
        assertThat(repository.getAccount(old.id)).isEqualTo(checkpoint)
        assertThat(storage.accounts).containsExactly(checkpoint)
    }

    @Test fun canonicalEnrollmentReplacementCleansSupersededKeyAfterDurableCommit() = runBlocking<Unit> {
        val canonical = migrated(legacy(), "passi.account.superseded")
        val temporary = legacy().copy(id = UUID.randomUUID(), isConfirmed = false, pendingKey = pending())
        val finalized = migrated(temporary, temporary.pendingKey!!.keyAlias).copy(id = canonical.id, isConfirmed = true)
        val storage = MemoryStorage(listOf(canonical, temporary))
        val deleted = mutableListOf<String>()
        val repository = PersistentAccountsRepository(storage) { alias ->
            assertThat(storage.accounts).hasSize(1)
            assertThat(storage.accounts.single().keyAlias).isEqualTo(finalized.keyAlias)
            deleted.add(alias)
        }
        repository.replaceAccount(temporary.id, finalized)
        assertThat(repository.getAccount(temporary.id)).isNull()
        assertThat(repository.getAccount(canonical.id)!!.keyAlias).isEqualTo(finalized.keyAlias)
        assertThat(repository.getAccount(canonical.id)!!.legacyCleanupPending).isFalse()
        assertThat(deleted).containsExactly(canonical.keyAlias)
        assertThat(storage.cleanedBiometrics).containsExactly(canonical.id, temporary.id)
    }

    @Test fun failedCanonicalReplacementCommitLeavesBothAccountsAndKeysUntouched() = runBlocking<Unit> {
        val canonical = migrated(legacy(), "passi.account.superseded")
        val temporary = legacy().copy(isConfirmed = false, pendingKey = pending())
        val storage = MemoryStorage(listOf(canonical, temporary)).apply { failWrites = true }
        val deleted = mutableListOf<String>()
        val repository = PersistentAccountsRepository(storage, deleted::add)
        val finalized = migrated(temporary, temporary.pendingKey!!.keyAlias).copy(id = canonical.id)
        assertThat(runCatching { repository.replaceAccount(temporary.id, finalized) }.isFailure).isTrue()
        assertThat(repository.getAccounts()).containsExactly(canonical, temporary)
        assertThat(storage.accounts).containsExactly(canonical, temporary)
        assertThat(deleted).isEmpty()
        assertThat(storage.cleanedBiometrics).isEmpty()
    }

    @Test fun interruptedCanonicalCleanupResumesEverySupersededAliasAfterRestart() = runBlocking<Unit> {
        val canonical = migrated(legacy(), "passi.account.superseded").copy(
            pendingKey = pending("passi.account.abandoned-pending"),
            cleanupKeyAlias = "passi.account.prior-cleanup",
            retiredKeyAliases = listOf("passi.account.older", "passi.account.prior-cleanup"))
        val temporary = legacy().copy(isConfirmed = false, pendingKey = pending())
        val finalized = migrated(temporary, temporary.pendingKey!!.keyAlias).copy(id = canonical.id)
        val storage = MemoryStorage(listOf(canonical, temporary))
        val firstDeletes = mutableListOf<String>()
        val repository = PersistentAccountsRepository(storage) { alias ->
            if (alias == canonical.pendingKey!!.keyAlias) error("Keystore temporarily unavailable")
            firstDeletes.add(alias)
        }
        repository.replaceAccount(temporary.id, finalized)
        val checkpoint = repository.getAccount(canonical.id)!!
        assertThat(checkpoint.keyAlias).isEqualTo(finalized.keyAlias)
        assertThat(checkpoint.legacyCleanupPending).isTrue()
        assertThat(checkpoint.retiredKeyAliases).containsExactly(canonical.keyAlias,
            canonical.pendingKey!!.keyAlias, canonical.cleanupKeyAlias, "passi.account.older")
        assertThat(storage.accounts).containsExactly(checkpoint)
        assertThat(firstDeletes).containsExactly(canonical.keyAlias)

        val retriedDeletes = mutableListOf<String>()
        val restarted = PersistentAccountsRepository(storage, retriedDeletes::add)
        restarted.getAccounts() // Startup cleanup runs on IO at the first suspend access.
        assertThat(retriedDeletes).containsExactly(canonical.keyAlias, canonical.pendingKey!!.keyAlias,
            canonical.cleanupKeyAlias, "passi.account.older")
        assertThat(retriedDeletes).doesNotContain(finalized.keyAlias)
        assertThat(restarted.getAccount(canonical.id)!!.legacyCleanupPending).isFalse()
        assertThat(restarted.getAccount(canonical.id)!!.retiredKeyAliases).isNull()
        assertThat(restarted.getAccount(canonical.id)!!.keyAlias).isEqualTo(finalized.keyAlias)
    }

    @Test fun submittedStateCannotBeRolledBackAndOnlyUnsubmittedKeysCanBeDiscarded() = runBlocking<Unit> {
        val account = legacy().copy(pendingKey = pending().copy(submitted = false))
        val storage = MemoryStorage(listOf(account))
        val deleted = mutableListOf<String>()
        val repository = PersistentAccountsRepository(storage, deleted::add)
        val submitted = account.copy(pendingKey = account.pendingKey!!.copy(submitted = true))
        repository.updateAccount(submitted)
        assertThat(runCatching { repository.updateAccount(account) }.isFailure).isTrue()
        assertThat(runCatching { repository.discardUnsubmittedPendingKey(account.id, account.pendingKey!!.keyAlias) }.isFailure).isTrue()
        assertThat(repository.getAccount(account.id)).isEqualTo(submitted)
        assertThat(deleted).isEmpty()
    }

    @Test fun unsubmittedDiscardCommitsBeforeDeletingItsAlias() = runBlocking<Unit> {
        val account = legacy().copy(pendingKey = pending().copy(submitted = false))
        val storage = MemoryStorage(listOf(account))
        val deleted = mutableListOf<String>()
        val repository = PersistentAccountsRepository(storage) { alias ->
            assertThat(storage.accounts.single().pendingKey).isNull()
            assertThat(storage.accounts.single().retiredKeyAliases).contains(alias)
            deleted.add(alias)
        }
        repository.discardUnsubmittedPendingKey(account.id, account.pendingKey!!.keyAlias)
        assertThat(repository.getAccount(account.id)!!.privateCertBinary).isEqualTo(account.privateCertBinary)
        assertThat(repository.getAccount(account.id)!!.pendingKey).isNull()
        assertThat(deleted).containsExactly(account.pendingKey!!.keyAlias)
    }

    @Test fun allDurableMutationsAndCleanupRunOffCallingThread() = runBlocking<Unit> {
        val caller = Thread.currentThread()
        val account = migrated(legacy()).copy(legacyCleanupPending = true, cleanupKeyAlias = "passi.account.retired")
        val storage = MemoryStorage(listOf(account))
        val keyThreads = mutableListOf<Thread>()
        val repository = PersistentAccountsRepository(storage) { keyThreads.add(Thread.currentThread()) }
        val cleaned = repository.getAccount(account.id)!!
        repository.updateAccount(cleaned.copy(inactive = true))
        repository.deleteAccount(account.id)
        assertThat(storage.mutationThreads).isNotEmpty()
        assertThat(storage.mutationThreads).doesNotContain(caller)
        assertThat(keyThreads).isNotEmpty()
        assertThat(keyThreads).doesNotContain(caller)
    }

    private class MemoryStorage(initial: List<Account>) : AccountStorage {
        var accounts = initial
        var failWrites = false
        var failBiometricCleanup = false
        var writeCount = 0
        val cleanedBiometrics = mutableListOf<UUID>()
        val mutationThreads = mutableListOf<Thread>()
        override fun readAccounts(): List<Account> = accounts
        override fun writeAccounts(accounts: List<Account>) {
            mutationThreads.add(Thread.currentThread())
            writeCount++
            check(!failWrites) { "Disk commit failed" }
            this.accounts = accounts.toList()
        }
        override fun deleteBiometricMaterial(accountId: UUID) {
            mutationThreads.add(Thread.currentThread())
            check(!failBiometricCleanup) { "Encrypted preference storage unavailable" }
            cleanedBiometrics.add(accountId)
        }
    }
}
