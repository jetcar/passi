package com.passi.cloud.passi_android

import android.app.KeyguardManager
import android.content.Context
import android.os.Build
import androidx.biometric.BiometricManager
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.passi.cloud.passi_android.data.crypto.AndroidKeystoreCertificateGenerator
import com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate
import com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection
import org.junit.Assert.assertFalse
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Assume.assumeTrue
import org.junit.Test
import org.junit.runner.RunWith
import java.security.KeyStore
import java.security.PrivateKey
import java.security.Signature
import java.time.Instant
import java.util.UUID

/** Runs on API 30+ hardware with a secure screen lock; an emulator is not hardware-security evidence. */
@RunWith(AndroidJUnit4::class)
class DeviceHeldKeyTest {
    @Test fun missingCheckpointKeyCannotBeValidated() {
        assumeTrue(Build.VERSION.SDK_INT >= Build.VERSION_CODES.R)
        val context = ApplicationProvider.getApplicationContext<Context>()
        val checkpoint = GeneratedCertificate(
            salt = null, privateCertBinary = null, publicCertBinary = "unused-public-placeholder",
            thumbprint = "unused-thumbprint", validFrom = Instant.EPOCH, validTo = Instant.MAX,
            keyAlias = AndroidKeystoreCertificateGenerator.ALIAS_PREFIX + UUID.randomUUID(),
            keySecurityLevel = "TEE",
        )
        val result = runCatching { AndroidKeystoreCertificateGenerator(context).validate(checkpoint) }
        assertTrue(result.isFailure)
        assertTrue(result.exceptionOrNull()?.message?.contains("missing") == true)
    }

    @Test fun deviceWithoutSecureScreenLockCannotCreateSigningKey() {
        assumeTrue(Build.VERSION.SDK_INT >= Build.VERSION_CODES.R)
        val context = ApplicationProvider.getApplicationContext<Context>()
        assumeTrue(!(context.getSystemService(Context.KEYGUARD_SERVICE) as KeyguardManager).isDeviceSecure)
        fun aliases(): Set<String> = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }
            .aliases().toList().filter { it.startsWith(AndroidKeystoreCertificateGenerator.ALIAS_PREFIX) }.toSet()
        val before = aliases()
        val attempted = runCatching {
            AndroidKeystoreCertificateGenerator(context).generate(
                "device-key-test@example.com",
                DeviceKeyProtection.DEVICE_CREDENTIAL,
            )
        }
        assertTrue("No secure screen lock must fail closed", attempted.isFailure)
        assertEquals("Failed generation must remove its key entry", before, aliases())
    }

    @Test fun privateKeyIsNotExportableAndCannotSignWithoutCryptoAuthorization() {
        assumeTrue(Build.VERSION.SDK_INT >= Build.VERSION_CODES.R)
        val context = ApplicationProvider.getApplicationContext<Context>()
        assumeTrue((context.getSystemService(Context.KEYGUARD_SERVICE) as KeyguardManager).isDeviceSecure)
        val generator = AndroidKeystoreCertificateGenerator(context)
        val protections = buildList {
            if (BiometricManager.from(context).canAuthenticate(BiometricManager.Authenticators.BIOMETRIC_STRONG) ==
                BiometricManager.BIOMETRIC_SUCCESS) add(DeviceKeyProtection.BIOMETRIC)
            add(DeviceKeyProtection.DEVICE_CREDENTIAL)
        }
        for (protection in protections) {
            val generated = generator.generate("device-key-test@example.com", protection)
            val alias = requireNotNull(generated.keyAlias)
            try {
                assertEquals(protection, generated.keyProtection)
                assertNull(generated.privateCertBinary)
                assertNull(generated.salt)
                val store = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }
                val privateKey = store.getKey(alias, null) as PrivateKey
                assertNull(privateKey.encoded)
                assertNull(privateKey.format)
                generator.validate(generated)
                assertTrue(runCatching {
                    generator.validate(generated.copy(publicCertBinary = "mismatched-public-certificate"))
                }.isFailure)
                assertTrue(runCatching {
                    generator.validate(generated.copy(thumbprint = "mismatched-thumbprint"))
                }.isFailure)
                // Even an unlocked device is insufficient. No BiometricPrompt/CryptoObject authorized this op.
                val unauthorized = runCatching {
                    Signature.getInstance("SHA512withRSA").apply {
                        initSign(privateKey)
                        update("synthetic instrumentation challenge".toByteArray())
                    }.sign()
                }
                assertTrue("An operation without user authorization must fail", unauthorized.isFailure)
            } finally {
                AndroidKeystoreCertificateGenerator.delete(alias)
            }
            val store = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }
            assertFalse(store.containsAlias(alias))
        }
    }
}
