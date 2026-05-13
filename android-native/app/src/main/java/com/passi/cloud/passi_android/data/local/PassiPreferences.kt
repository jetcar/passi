package com.passi.cloud.passi_android.data.local

import android.content.Context
import android.content.SharedPreferences
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.ApiPaths
import com.passi.cloud.passi_android.domain.model.Provider
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import java.time.Instant
import java.util.UUID

private const val PREFERENCES_NAME = "passi_native"
private const val SECURE_PREFERENCES_NAME = "passi_native_secure"
private const val ACCOUNTS_KEY = "allAccounts"
private const val PROVIDERS_KEY = "providers"
private const val DEVICE_ID_KEY = "deviceId"
private const val BIOMETRIC_CERT_PREFIX = "biometric_cert_"
private const val BIOMETRIC_PASSWORD_PREFIX = "biometric_password_"

class PassiPreferences(
    context: Context,
    private val gson: Gson = Gson(),
) {
    private val preferences = context.getSharedPreferences(PREFERENCES_NAME, Context.MODE_PRIVATE)
    private val securePreferences: SharedPreferences by lazy {
        val masterKey = MasterKey.Builder(context)
            .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
            .build()

        EncryptedSharedPreferences.create(
            context,
            SECURE_PREFERENCES_NAME,
            masterKey,
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM,
        )
    }

    fun readAccounts(): List<Account> {
        val json = preferences.getString(ACCOUNTS_KEY, null) ?: return emptyList()
        val type = object : TypeToken<List<StoredAccount>>() {}.type
        val stored = gson.fromJson<List<StoredAccount>>(json, type) ?: return emptyList()
        return stored.map { it.toDomain() }
    }

    fun writeAccounts(accounts: List<Account>) {
        val json = gson.toJson(accounts.map { StoredAccount.fromDomain(it) })
        preferences.edit().putString(ACCOUNTS_KEY, json).apply()
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
        preferences.edit().putString(DEVICE_ID_KEY, newId).apply()
        return newId
    }

    fun readBiometricMaterial(accountId: UUID): StoredBiometricMaterial? {
        val securePrivateCertBinary = securePreferences.getString(BIOMETRIC_CERT_PREFIX + accountId, null)
        val securePassword = securePreferences.getString(BIOMETRIC_PASSWORD_PREFIX + accountId, null)
        if (!securePrivateCertBinary.isNullOrBlank() && !securePassword.isNullOrBlank()) {
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

        val migrated = StoredBiometricMaterial(
            privateCertBinary = privateCertBinary,
            password = password,
        )
        writeBiometricMaterial(accountId, migrated)
        preferences.edit()
            .remove(BIOMETRIC_CERT_PREFIX + accountId)
            .remove(BIOMETRIC_PASSWORD_PREFIX + accountId)
            .apply()
        return migrated
    }

    fun writeBiometricMaterial(accountId: UUID, material: StoredBiometricMaterial) {
        securePreferences.edit()
            .putString(BIOMETRIC_CERT_PREFIX + accountId, material.privateCertBinary)
            .putString(BIOMETRIC_PASSWORD_PREFIX + accountId, material.password)
            .apply()

        // Clean up any legacy plaintext copy after the secure write succeeds.
        preferences.edit()
            .remove(BIOMETRIC_CERT_PREFIX + accountId)
            .remove(BIOMETRIC_PASSWORD_PREFIX + accountId)
            .apply()
    }

    fun deleteBiometricMaterial(accountId: UUID) {
        securePreferences.edit()
            .remove(BIOMETRIC_CERT_PREFIX + accountId)
            .remove(BIOMETRIC_PASSWORD_PREFIX + accountId)
            .apply()

        preferences.edit()
            .remove(BIOMETRIC_CERT_PREFIX + accountId)
            .remove(BIOMETRIC_PASSWORD_PREFIX + accountId)
            .apply()
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
        )
    }
}