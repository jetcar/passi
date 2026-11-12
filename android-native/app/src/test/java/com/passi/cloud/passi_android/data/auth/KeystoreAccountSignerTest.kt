package com.passi.cloud.passi_android.data.auth

import com.passi.cloud.passi_android.domain.model.Account
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import java.security.PrivateKey
import java.security.PublicKey
import java.security.Signature
import java.time.Instant
import java.util.UUID

class KeystoreAccountSignerTest {
    private val account = Account(
        id = UUID.randomUUID(), providerId = null, email = "test@example.com", deviceId = "test",
        isConfirmed = true, validFrom = Instant.EPOCH, validTo = Instant.MAX,
        keyAlias = "passi.account.test",
    )

    @Test fun deniedAuthorizationNeverUpdatesOrSigns() = runTest {
        for (reason in listOf("cancelled", "failed", "invalidated", "unavailable")) {
            val signature = RecordingSignature()
            val signer = KeystoreAccountSigner(SignatureAuthorization { error(reason) }) { signature }
            assertTrue(signer.sign(account, null, "challenge").isFailure)
            assertEquals(0, signature.updates)
            assertEquals(0, signature.signs)
        }
    }

    @Test fun unrelatedAuthorizedOperationCannotSign() = runTest {
        val expected = RecordingSignature()
        val unrelated = RecordingSignature()
        val signer = KeystoreAccountSigner(SignatureAuthorization { unrelated }) { expected }
        assertTrue(signer.sign(account, null, "challenge").isFailure)
        assertEquals(0, expected.signs)
        assertEquals(0, unrelated.signs)
    }

    @Test fun coroutineCancellationNeverSignsAndIsPropagated() = runTest {
        val signature = RecordingSignature()
        val signer = KeystoreAccountSigner(SignatureAuthorization { throw CancellationException() }) { signature }
        var cancelled = false
        try { signer.sign(account, null, "challenge") } catch (_: CancellationException) { cancelled = true }
        assertTrue(cancelled)
        assertEquals(0, signature.signs)
    }

    @Test fun successSignsOnlyTheAuthorizedOperationOnce() = runTest {
        val signature = RecordingSignature()
        var authorizations = 0
        val signer = KeystoreAccountSigner(SignatureAuthorization {
            assertTrue(it === signature)
            authorizations++
            it
        }) { signature }
        assertTrue(signer.sign(account, "ignored-legacy-pin", "challenge").isSuccess)
        assertEquals(1, authorizations)
        assertEquals(1, signature.updates)
        assertEquals(1, signature.signs)
    }

    private class RecordingSignature : Signature("SHA512withRSA") {
        var updates = 0
        var signs = 0
        init { initSign(object : PrivateKey {
            override fun getAlgorithm() = "RSA"
            override fun getFormat(): String? = null
            override fun getEncoded(): ByteArray? = null
        }) }
        override fun engineInitVerify(publicKey: PublicKey) = Unit
        override fun engineInitSign(privateKey: PrivateKey) = Unit
        override fun engineUpdate(b: Byte) { updates++ }
        override fun engineUpdate(b: ByteArray, off: Int, len: Int) { updates++ }
        override fun engineSign(): ByteArray { signs++; return byteArrayOf(1) }
        override fun engineVerify(sigBytes: ByteArray) = false
        @Deprecated("Legacy Signature API")
        override fun engineSetParameter(param: String, value: Any) = Unit
        @Deprecated("Legacy Signature API")
        override fun engineGetParameter(param: String): Any? = null
    }
}
