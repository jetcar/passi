package com.passi.cloud.passi_android

import android.content.SharedPreferences
import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.data.local.PassiPreferences
import kotlinx.coroutines.runBlocking
import org.junit.Test
import java.lang.reflect.Proxy
import java.util.UUID
import java.util.concurrent.CountDownLatch
import java.util.concurrent.Executors
import java.util.concurrent.TimeUnit
import java.util.concurrent.TimeoutException

class AccountPreferencesCompatibilityTests {
    private val id = UUID.fromString("9ea21bd1-69c6-4f98-a63d-5bf7d2202657")
    private val oldJson = """[{"id":"$id","providerId":null,"email":"user@example.com","deviceId":"old-device","isConfirmed":true,"thumbprint":"old-thumbprint","validFrom":"2026-01-01T00:00:00Z","validTo":"2027-01-01T00:00:00Z","inactive":false,"salt":"old-salt","privateCertBinary":"legacy-pkcs12","publicCertBinary":"public-cert","pinLength":4,"hasFingerprint":true}]"""

    @Test fun oldAccountJsonWithoutAnyDeviceKeyFieldsPreservesLegacyAccess() {
        val plain = PreferencesDouble("plain").apply { values["allAccounts"] = oldJson }
        val secure = PreferencesDouble("secure")
        val preferences = PassiPreferences(plain.api, { secure.api })
        val account = preferences.readAccounts().single()
        assertThat(account.id).isEqualTo(id)
        assertThat(account.privateCertBinary).isEqualTo("legacy-pkcs12")
        assertThat(account.salt).isEqualTo("old-salt")
        assertThat(account.pinLength).isEqualTo(4)
        assertThat(account.hasFingerprint).isTrue()
        assertThat(account.keyAlias).isNull()
        assertThat(account.pendingKey).isNull()
        assertThat(account.legacyCleanupPending).isFalse()
        assertThat(account.retiredKeyAliases).isNull()
        assertThat(account.cleanupAccountIds).isNull()
        preferences.writeAccounts(listOf(account))
        assertThat(preferences.readAccounts().single()).isEqualTo(account)
    }

    @Test fun historicalPendingJsonWithoutSubmissionFieldIsConservativelyAmbiguous() {
        val checkpoint = """, "pendingKey":{"keyAlias":"passi.account.old-pending","keySecurityLevel":"TEE","publicCertBinary":"public","thumbprint":"pending-thumb","validFrom":"2026-01-01T00:00:00Z","validTo":"2027-01-01T00:00:00Z"}"""
        val plain = PreferencesDouble("plain").apply { values["allAccounts"] = oldJson.dropLast(2) + checkpoint + "}]" }
        val secure = PreferencesDouble("secure")
        val preferences = PassiPreferences(plain.api, { secure.api })
        val account = preferences.readAccounts().single()
        assertThat(account.pendingKey!!.submitted).isNull()
        assertThat(account.pendingKey!!.mayHaveBeenSubmitted).isTrue()
        preferences.writeAccounts(listOf(account.copy(pendingKey = account.pendingKey!!.copy(submitted = false))))
        assertThat(preferences.readAccounts().single().pendingKey!!.submitted).isFalse()
        preferences.writeAccounts(listOf(account.copy(pendingKey = account.pendingKey!!.copy(submitted = true))))
        assertThat(preferences.readAccounts().single().pendingKey!!.submitted).isTrue()
    }

    @Test fun plaintextBiometricMigrationCommitsEncryptedCopyBeforeRemovingPlaintextOffCallerThread() = runBlocking<Unit> {
        val caller = Thread.currentThread()
        val events = mutableListOf<String>()
        val plain = plaintext(events)
        val secure = PreferencesDouble("secure", events)
        val preferences = PassiPreferences(plain.api, { secure.api })
        val material = preferences.readBiometricMaterial(id)!!
        assertThat(material.privateCertBinary).isEqualTo("old-biometric-pkcs12")
        assertThat(material.password).isEqualTo("old-password")
        assertThat(events).containsExactly("secure.commit", "plain.commit").inOrder()
        assertThat(plain.values).doesNotContainKey("biometric_cert_$id")
        assertThat(secure.persisted["biometric_cert_$id"]).isEqualTo(material.privateCertBinary)
        assertThat(secure.persisted["biometric_password_$id"]).isEqualTo(material.password)
        assertThat(plain.commitThreads + secure.commitThreads).doesNotContain(caller)
        preferences.readBiometricMaterial(id)
        assertThat(events).hasSize(2)
    }

    @Test fun failedEncryptedCommitNeverDeletesPlaintextEvenWhenItsMemoryCacheChanged() = runBlocking<Unit> {
        val events = mutableListOf<String>()
        val plain = plaintext(events)
        val secure = PreferencesDouble("secure", events).apply { failCommit = true }
        val preferences = PassiPreferences(plain.api, { secure.api })
        repeat(2) {
            assertThat(runCatching { preferences.readBiometricMaterial(id) }.isFailure).isTrue()
            assertThat(plain.values["biometric_cert_$id"]).isEqualTo("old-biometric-pkcs12")
            assertThat(secure.persisted).isEmpty()
        }
        assertThat(events).containsExactly("secure.commit", "secure.commit")
        secure.failCommit = false
        assertThat(preferences.readBiometricMaterial(id)).isNotNull()
        assertThat(plain.values).doesNotContainKey("biometric_cert_$id")
        assertThat(secure.persisted["biometric_password_$id"]).isEqualTo("old-password")
    }

    @Test fun failedPlaintextRemovalRetainsEncryptedCopyAndResumesAfterRestart() = runBlocking<Unit> {
        val events = mutableListOf<String>()
        val plain = plaintext(events).apply { failCommit = true }
        val secure = PreferencesDouble("secure", events)
        assertThat(runCatching { PassiPreferences(plain.api, { secure.api }).readBiometricMaterial(id) }.isFailure).isTrue()
        assertThat(secure.persisted["biometric_cert_$id"]).isEqualTo("old-biometric-pkcs12")
        plain.values.clear()
        plain.values.putAll(plain.persisted)
        plain.failCommit = false
        assertThat(PassiPreferences(plain.api, { secure.api }).readBiometricMaterial(id)).isNotNull()
        assertThat(plain.persisted).doesNotContainKey("biometric_cert_$id")
        assertThat(plain.persisted).doesNotContainKey("biometric_password_$id")
    }

    @Test fun concurrentCleanupWaitsForMigrationAndCannotLeaveResurrectedMaterial() {
        val plain = plaintext(mutableListOf())
        val secure = PreferencesDouble("secure")
        val reader = PassiPreferences(plain.api, { secure.api })
        val cleaner = PassiPreferences(plain.api, { secure.api })
        val migrationReachedCommit = CountDownLatch(1)
        val releaseMigration = CountDownLatch(1)
        val cleanupStarted = CountDownLatch(1)
        secure.beforeCommit = { changes ->
            if (changes["biometric_cert_$id"] != null) {
                migrationReachedCommit.countDown()
                check(releaseMigration.await(5, TimeUnit.SECONDS))
            }
        }
        val executor = Executors.newFixedThreadPool(2)
        try {
            val migration = executor.submit { runBlocking { reader.readBiometricMaterial(id) } }
            assertThat(migrationReachedCommit.await(5, TimeUnit.SECONDS)).isTrue()
            val cleanup = executor.submit {
                cleanupStarted.countDown()
                cleaner.deleteBiometricMaterial(id)
            }
            assertThat(cleanupStarted.await(5, TimeUnit.SECONDS)).isTrue()
            assertThat(runCatching { cleanup.get(200, TimeUnit.MILLISECONDS) }.exceptionOrNull())
                .isInstanceOf(TimeoutException::class.java)
            releaseMigration.countDown()
            migration.get(5, TimeUnit.SECONDS)
            cleanup.get(5, TimeUnit.SECONDS)
            assertThat(secure.persisted).isEmpty()
            assertThat(plain.persisted).isEmpty()
            assertThat(runBlocking { reader.readBiometricMaterial(id) }).isNull()
        } finally {
            releaseMigration.countDown()
            executor.shutdownNow()
        }
    }

    private fun plaintext(events: MutableList<String>) = PreferencesDouble("plain", events).apply {
        values["biometric_cert_$id"] = "old-biometric-pkcs12"
        values["biometric_password_$id"] = "old-password"
        persisted.putAll(values)
    }

    /** Models Android's important commit(false) behavior: memory can change while disk did not. */
    private class PreferencesDouble(private val name: String, private val events: MutableList<String> = mutableListOf()) {
        val values = mutableMapOf<String, Any?>()
        val persisted = mutableMapOf<String, Any?>()
        val commitThreads = mutableListOf<Thread>()
        var failCommit = false
        var beforeCommit: ((Map<String, Any?>) -> Unit)? = null
        val api = Proxy.newProxyInstance(SharedPreferences::class.java.classLoader, arrayOf(SharedPreferences::class.java)) { _, method, args ->
            when (method.name) {
                "getString" -> values[args!![0]] ?: args[1]
                "contains" -> values.containsKey(args!![0])
                "getAll" -> values.toMap()
                "edit" -> editor()
                else -> error("Unexpected preference method ${method.name}")
            }
        } as SharedPreferences

        private fun editor(): SharedPreferences.Editor {
            val changes = mutableMapOf<String, Any?>()
            lateinit var editor: SharedPreferences.Editor
            editor = Proxy.newProxyInstance(SharedPreferences.Editor::class.java.classLoader, arrayOf(SharedPreferences.Editor::class.java)) { _, method, args ->
                when (method.name) {
                    "putString" -> { changes[args!![0] as String] = args[1]; editor }
                    "remove" -> { changes[args!![0] as String] = null; editor }
                    "commit" -> {
                        beforeCommit?.invoke(changes)
                        commitThreads.add(Thread.currentThread())
                        events.add("$name.commit")
                        changes.forEach { (key, value) -> if (value == null) values.remove(key) else values[key] = value }
                        if (!failCommit) { persisted.clear(); persisted.putAll(values) }
                        !failCommit
                    }
                    else -> error("Unexpected editor method ${method.name}")
                }
            } as SharedPreferences.Editor
            return editor
        }
    }
}
