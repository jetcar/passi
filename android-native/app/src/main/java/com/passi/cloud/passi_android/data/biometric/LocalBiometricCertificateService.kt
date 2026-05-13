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
) : BiometricCertificateService {
    override suspend fun enableBiometric(account: Account, pin: String?): Result<Account> {
        return runCatching {
            val sourcePassword = certificatePassword(account, pin)
            val keyStore = loadPkcs12(
                privateCertBinary = account.privateCertBinary ?: error("Missing private certificate"),
                password = sourcePassword,
            )
            val alias = firstAlias(keyStore)
            val certificate = keyStore.getCertificate(alias) ?: error("Certificate not found")
            val privateKey = keyStore.getKey(alias, sourcePassword.toCharArray()) as? PrivateKey
                ?: error("Private key not found")

            val biometricPassword = UUID.randomUUID().toString()
            val biometricKeyStore = KeyStore.getInstance("PKCS12")
            biometricKeyStore.load(null, null)
            biometricKeyStore.setKeyEntry(alias, privateKey, biometricPassword.toCharArray(), arrayOf(certificate))

            val exportBuffer = java.io.ByteArrayOutputStream()
            exportBuffer.use { output ->
                biometricKeyStore.store(output, biometricPassword.toCharArray())
            }

            preferences.writeBiometricMaterial(
                accountId = account.id,
                material = StoredBiometricMaterial(
                    privateCertBinary = Base64.getEncoder().encodeToString(exportBuffer.toByteArray()),
                    password = biometricPassword,
                ),
            )

            account.copy(hasFingerprint = true)
        }.recoverCatching { error ->
            throw IllegalStateException(if (account.pinLength > 0) "Invalid Pin" else (error.message ?: "Biometric setup failed"))
        }
    }

    override fun sign(account: Account, message: String): Result<String> {
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