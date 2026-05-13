package com.passi.cloud.passi_android.data.auth

import com.passi.cloud.passi_android.domain.auth.AccountSigner
import com.passi.cloud.passi_android.domain.model.Account
import java.io.ByteArrayInputStream
import java.security.KeyStore
import java.security.PrivateKey
import java.security.Signature
import java.util.Base64

class Pkcs12AccountSigner : AccountSigner {
    override fun sign(account: Account, pin: String?, message: String): Result<String> {
        return runCatching {
            val privateCertBinary = account.privateCertBinary ?: error("Missing private certificate")
            val salt = account.salt ?: error("Missing certificate salt")
            val password = salt + (pin ?: "")
            val pkcs12Bytes = Base64.getDecoder().decode(privateCertBinary)

            val keyStore = KeyStore.getInstance("PKCS12")
            ByteArrayInputStream(pkcs12Bytes).use { inputStream ->
                keyStore.load(inputStream, password.toCharArray())
            }

            val aliases = keyStore.aliases()
            val alias = if (aliases.hasMoreElements()) aliases.nextElement() else null
                ?: error("Certificate alias not found")
            val privateKey = keyStore.getKey(alias, password.toCharArray()) as? PrivateKey
                ?: error("Private key not found")

            val signature = Signature.getInstance("SHA512withRSA")
            signature.initSign(privateKey)
            signature.update(message.toByteArray(Charsets.UTF_8))

            Base64.getEncoder().encodeToString(signature.sign())
        }
    }
}