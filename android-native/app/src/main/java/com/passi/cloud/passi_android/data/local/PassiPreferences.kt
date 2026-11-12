package com.passi.cloud.passi_android.data.local

import android.content.Context
import android.content.SharedPreferences
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.passi.cloud.passi_android.domain.model.PendingDeviceKey
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.ApiPaths
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import java.time.Instant
import java.util.UUID
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

private const val PREFERENCES_NAME = "passi_native"
private const val SECURE_PREFERENCES_NAME = "passi_native_secure"
private const val ACCOUNTS_KEY = "allAccounts"
private const val PROVIDERS_KEY = "providers"
private const val DEVICE_ID_KEY = "deviceId"
private const val BIOMETRIC_CERT_PREFIX = "biometric_cert_"
private const val BIOMETRIC_PASSWORD_PREFIX = "biometric_password_"
// Shared by every wrapper for these preference files: migration must never resurrect deleted material.
private val biometricStorageLock = Any()

class PassiPreferences internal constructor(
    private val preferences: SharedPreferences,
    private val securePreferencesFactory: () -> SharedPreferences,
    private val gson: Gson = Gson(),
) : AccountStorage {
    constructor(context: Context, gson: Gson = Gson()) : this(
        context.getSharedPreferences(PREFERENCES_NAME, Context.MODE_PRIVATE),
        {
        val masterKey = MasterKey.Builder(context.applicationContext)
            .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
            .build()

        EncryptedSharedPreferences.create(
            context.applicationContext,
            SECURE_PREFERENCES_NAME,
            masterKey,
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM,
        )
        }, gson,
    )
    private val securePreferences: SharedPreferences by lazy(securePreferencesFactory)

    override fun readAccounts(): List<Account> {
        val json = preferences.getString(ACCOUNTS_KEY, null) ?: return emptyList()
        val type = object : TypeToken<List<StoredAccount>>() {}.type
        val stored = gson.fromJson<List<StoredAccount>>(json, type) ?: return emptyList()
        return stored.map { it.toDomain() }
    }

    override fun writeAccounts(accounts: List<Account>) {
        val json = gson.toJson(accounts.map { StoredAccount.fromDomain(it) })
        check(preferences.edit().putString(ACCOUNTS_KEY, json).commit()) { "Account storage failed" }
    }

    fun readProviders(): List<Provider> {
        val json = preferences.getString(PROVIDERS_KEY, null) ?: return emptyList()
        val type = object : TypeToken<List<StoredProvider>>() {}.type
        val stored = gson.fromJson<List<StoredProvider>>(json, type) ?: return emptyList()
        return stored.map { it.toDomain() }
    }

    fun writeProviders(providers: List<Provider>) {
        val json = gson.toJson(providers.map { StoredProvider.fromDomain(it) })
        preferences.edit().putString(PROVIDERS_KEY, json).apply()
    }

    fun getOrCreateDeviceId(): String {
        val existing = preferences.getString(DEVICE_ID_KEY, null)
        if (!existing.isNullOrBlank()) {
            return existing
        }

        val newId = UUID.randomUUID().toString()
        check(preferences.edit().putString(DEVICE_ID_KEY, newId).commit()) { "Device storage failed" }
        return newId
    }

    suspend fun readBiometricMaterial(accountId: UUID): StoredBiometricMaterial? = withContext(Dispatchers.IO) {
        synchronized(biometricStorageLock) { readBiometricMaterialLocked(accountId) }
    }

    private fun readBiometricMaterialLocked(accountId: UUID): StoredBiometricMaterial? {
        val securePrivateCertBinary = securePreferences.getString(BIOMETRIC_CERT_PREFIX + accountId, null)
        val securePassword = securePreferences.getString(BIOMETRIC_PASSWORD_PREFIX + accountId, null)
        if (!securePrivateCertBinary.isNullOrBlank() && !securePassword.isNullOrBlank()) {
            // Retry plaintext deletion after a crash between the secure commit and deletion.
            // A failed SharedPreferences.commit can still update its in-memory cache; recommit
            // before trusting that cache as a durable replacement for any remaining plaintext.
            if (preferences.contains(BIOMETRIC_CERT_PREFIX + accountId) || preferences.contains(BIOMETRIC_PASSWORD_PREFIX + accountId)) {
                check(securePreferences.edit().putString(BIOMETRIC_CERT_PREFIX + accountId, securePrivateCertBinary)
                    .putString(BIOMETRIC_PASSWORD_PREFIX + accountId, securePassword).commit()) { "Biometric migration failed" }
            }
            deletePlaintextBiometricMaterial(accountId)
            return StoredBiometricMaterial(
                privateCertBinary = securePrivateCertBinary,
                password = securePassword,
            )
        }

        val privateCertBinary = preferences.getString(BIOMETRIC_CERT_PREFIX + accountId, null)
        val password = preferences.getString(BIOMETRIC_PASSWORD_PREFIX + accountId, null)
        if (privateCertBinary.isNullOrBlank() || password.isNullOrBlank()) {
            return null
        }

        check(securePreferences.edit()
            .putString(BIOMETRIC_CERT_PREFIX + accountId, privateCertBinary)
            .putString(BIOMETRIC_PASSWORD_PREFIX + accountId, password)
            .commit()) { "Biometric migration failed" }
        deletePlaintextBiometricMaterial(accountId)
        return StoredBiometricMaterial(privateCertBinary, password)
    }

    private fun deletePlaintextBiometricMaterial(accountId: UUID) {
        if (!preferences.contains(BIOMETRIC_CERT_PREFIX + accountId) &&
            !preferences.contains(BIOMETRIC_PASSWORD_PREFIX + accountId)) return
        check(preferences.edit().remove(BIOMETRIC_CERT_PREFIX + accountId)
            .remove(BIOMETRIC_PASSWORD_PREFIX + accountId).commit()) { "Biometric migration cleanup failed" }
    }

    override fun deleteBiometricMaterial(accountId: UUID) = synchronized(biometricStorageLock) {
        check(securePreferences.edit()
            .remove(BIOMETRIC_CERT_PREFIX + accountId)
            .remove(BIOMETRIC_PASSWORD_PREFIX + accountId)
            .commit()) { "Biometric cleanup failed" }
        check(preferences.edit()
            .remove(BIOMETRIC_CERT_PREFIX + accountId)
            .remove(BIOMETRIC_PASSWORD_PREFIX + accountId)
            .commit()) { "Biometric cleanup failed" }
    }

}

data class StoredBiometricMaterial(
    val privateCertBinary: String,
    val password: String,
)

private data class StoredAccount(
    val id: String,
    val providerId: String?,
    val email: String,
    val deviceId: String,
    val isConfirmed: Boolean,
    val thumbprint: String?,
    val validFrom: String,
    val validTo: String,
    val inactive: Boolean,
    val salt: String?,
    val privateCertBinary: String?,
    val publicCertBinary: String?,
    val pinLength: Int,
    val hasFingerprint: Boolean,
    val keyAlias: String? = null,
    val keySecurityLevel: String? = null,
    val keyProtection: DeviceKeyProtection? = null,
    val pendingKey: PendingDeviceKey? = null,
    val legacyCleanupPending: Boolean = false,
    val cleanupKeyAlias: String? = null,
    val retiredKeyAliases: List<String>? = null,
    val cleanupAccountIds: List<String>? = null,
) {
    fun toDomain(): Account = Account(
        id = UUID.fromString(id),
        providerId = providerId?.let(UUID::fromString),
        email = email,
        deviceId = deviceId,
        isConfirmed = isConfirmed,
        thumbprint = thumbprint,
        validFrom = Instant.parse(validFrom),
        validTo = Instant.parse(validTo),
        inactive = inactive,
        salt = salt,
        privateCertBinary = privateCertBinary,
        publicCertBinary = publicCertBinary,
        pinLength = pinLength,
        hasFingerprint = hasFingerprint,
        keyAlias = keyAlias,
        keySecurityLevel = keySecurityLevel,
        keyProtection = keyProtection,
        pendingKey = pendingKey,
        legacyCleanupPending = legacyCleanupPending,
        cleanupKeyAlias = cleanupKeyAlias,
        retiredKeyAliases = retiredKeyAliases,
        cleanupAccountIds = cleanupAccountIds,
    )

    companion object {
        fun fromDomain(account: Account): StoredAccount = StoredAccount(
            id = account.id.toString(),
            providerId = account.providerId?.toString(),
            email = account.email,
            deviceId = account.deviceId,
            isConfirmed = account.isConfirmed,
            thumbprint = account.thumbprint,
            validFrom = account.validFrom.toString(),
            validTo = account.validTo.toString(),
            inactive = account.inactive,
            salt = account.salt,
            privateCertBinary = account.privateCertBinary,
            publicCertBinary = account.publicCertBinary,
            pinLength = account.pinLength,
            hasFingerprint = account.hasFingerprint,
            keyAlias = account.keyAlias,
            keySecurityLevel = account.keySecurityLevel,
            keyProtection = account.keyProtection,
            pendingKey = account.pendingKey,
            legacyCleanupPending = account.legacyCleanupPending,
            cleanupKeyAlias = account.cleanupKeyAlias,
            retiredKeyAliases = account.retiredKeyAliases,
            cleanupAccountIds = account.cleanupAccountIds,
        )
    }
}

private data class StoredProvider(
    val id: String,
    val name: String,
    val baseUrl: String,
    val isDefault: Boolean,
    val signup: String,
    val signupConfirmation: String,
    val signupCheck: String,
    val tokenUpdate: String,
    val cancelCheck: String,
    val authorize: String,
    val time: String,
    val updateCertificate: String,
    val checkForStartedSessions: String,
    val syncAccounts: String,
    val deleteAccount: String,
    val listDevices: String? = null,
    val deleteDevice: String? = null,
    val unlinkDevice: String? = null,
) {
    fun toDomain(): Provider = Provider(
        id = UUID.fromString(id),
        name = name,
        baseUrl = baseUrl,
        apiPaths = ApiPaths(
            signup = signup,
            signupConfirmation = signupConfirmation,
            signupCheck = signupCheck,
            tokenUpdate = tokenUpdate,
            cancelCheck = cancelCheck,
            authorize = authorize,
            time = time,
            updateCertificate = updateCertificate,
            checkForStartedSessions = checkForStartedSessions,
            syncAccounts = syncAccounts,
            deleteAccount = deleteAccount,
            listDevices = listDevices.orEmpty().ifBlank { ApiPaths.defaultPaths().listDevices },
            deleteDevice = deleteDevice.orEmpty().ifBlank { ApiPaths.defaultPaths().deleteDevice },
            unlinkDevice = unlinkDevice.orEmpty().ifBlank { ApiPaths.defaultPaths().unlinkDevice },
        ),
        isDefault = isDefault,
    )

    companion object {
        fun fromDomain(provider: Provider): StoredProvider = StoredProvider(
            id = provider.id.toString(),
            name = provider.name,
            baseUrl = provider.baseUrl,
            isDefault = provider.isDefault,
            signup = provider.apiPaths.signup,
            signupConfirmation = provider.apiPaths.signupConfirmation,
            signupCheck = provider.apiPaths.signupCheck,
            tokenUpdate = provider.apiPaths.tokenUpdate,
            cancelCheck = provider.apiPaths.cancelCheck,
            authorize = provider.apiPaths.authorize,
            time = provider.apiPaths.time,
            updateCertificate = provider.apiPaths.updateCertificate,
            checkForStartedSessions = provider.apiPaths.checkForStartedSessions,
            syncAccounts = provider.apiPaths.syncAccounts,
            deleteAccount = provider.apiPaths.deleteAccount,
            listDevices = provider.apiPaths.listDevices,
            deleteDevice = provider.apiPaths.deleteDevice,
            unlinkDevice = provider.apiPaths.unlinkDevice,
        )
    }
}
