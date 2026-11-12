package com.passi.cloud.passi_android.data.biometric

import com.passi.cloud.passi_android.data.local.PassiPreferences
import com.passi.cloud.passi_android.data.local.StoredBiometricMaterial
import com.passi.cloud.passi_android.domain.biometric.BiometricCertificateService
import com.passi.cloud.passi_android.domain.model.Account
import java.io.ByteArrayInputStream
import java.security.KeyStore
import java.security.PrivateKey
import java.security.Signature
import java.util.Base64
import java.util.UUID

class LocalBiometricCertificateService(
    private val preferences: PassiPreferences,
    private val accountSigner: com.passi.cloud.passi_android.domain.auth.AccountSigner,
) : BiometricCertificateService {
    override suspend fun enableBiometric(account: Account, pin: String?): Result<Account> =
        Result.failure(IllegalStateException("Update the certificate to enable device-protected signing"))

    override suspend fun sign(account: Account, message: String): Result<String> {
        if (account.keyAlias != null) return accountSigner.sign(account, null, message)
        return runCatching {
            val material = preferences.readBiometricMaterial(account.id)
                ?: error("Biometric certificate not found")
            val keyStore = loadPkcs12(material.privateCertBinary, material.password)
            val alias = firstAlias(keyStore)
            val privateKey = keyStore.getKey(alias, material.password.toCharArray()) as? PrivateKey
                ?: error("Private key not found")

            val signature = Signature.getInstance("SHA512withRSA")
            signature.initSign(privateKey)
            signature.update(message.toByteArray(Charsets.UTF_8))
            Base64.getEncoder().encodeToString(signature.sign())
        }
    }

    private fun certificatePassword(account: Account, pin: String?): String {
        val salt = account.salt ?: error("Missing certificate salt")
        return salt + (pin ?: "")
    }

    private fun loadPkcs12(privateCertBinary: String, password: String): KeyStore {
        val keyStore = KeyStore.getInstance("PKCS12")
        ByteArrayInputStream(Base64.getDecoder().decode(privateCertBinary)).use { inputStream ->
            keyStore.load(inputStream, password.toCharArray())
        }
        return keyStore
    }

    private fun firstAlias(keyStore: KeyStore): String {
        val aliases = keyStore.aliases()
        return if (aliases.hasMoreElements()) aliases.nextElement() else error("Certificate alias not found")
    }
}