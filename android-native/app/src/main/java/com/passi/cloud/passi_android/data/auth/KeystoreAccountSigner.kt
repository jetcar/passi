package com.passi.cloud.passi_android.data.auth

import com.passi.cloud.passi_android.data.crypto.AndroidKeystoreCertificateGenerator
import com.passi.cloud.passi_android.domain.auth.AccountSigner
import com.passi.cloud.passi_android.domain.model.Account
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.currentCoroutineContext
import kotlinx.coroutines.ensureActive
import java.security.PrivateKey
import java.security.Signature
import java.util.Base64

/** Only an authorization of this exact initialized operation may release its signature. */
fun interface SignatureAuthorization {
    suspend fun authorize(signature: Signature): Signature
}

class KeystoreAccountSigner internal constructor(
    private val authorization: SignatureAuthorization,
    private val initialize: (Account) -> Signature,
) : AccountSigner {
    constructor(authorization: SignatureAuthorization) : this(authorization, ::initializeDeviceSignature)

    override suspend fun sign(account: Account, pin: String?, message: String): Result<String> = try {
        val signature = initialize(account)
        val authorized = authorization.authorize(signature)
        check(authorized === signature) { "Authorization returned a different signing operation" }
        currentCoroutineContext().ensureActive()
        authorized.update(message.toByteArray(Charsets.UTF_8))
        Result.success(Base64.getEncoder().encodeToString(authorized.sign()))
    } catch (cancelled: CancellationException) {
        throw cancelled
    } catch (failure: Exception) {
        Result.failure(failure)
    }
}

private fun initializeDeviceSignature(account: Account): Signature {
    val alias = account.keyAlias ?: error("Account requires migration to a device-held key")
    require(alias.startsWith(AndroidKeystoreCertificateGenerator.ALIAS_PREFIX)) { "Unexpected signing key alias" }
    val key = AndroidKeystoreCertificateGenerator.store().getKey(alias, null) as? PrivateKey
        ?: error("Device signing key is missing; recover the account on this device")
    AndroidKeystoreCertificateGenerator.requireSecureSigningKey(key)
    return Signature.getInstance("SHA512withRSA").apply { initSign(key) }
}
