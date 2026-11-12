package com.passi.cloud.passi_android.data.crypto

import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyInfo
import android.security.keystore.KeyProperties
import androidx.annotation.RequiresApi
import com.passi.cloud.passi_android.domain.enrollment.CertificateGenerator
import com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate
import org.bouncycastle.asn1.x500.X500NameBuilder
import org.bouncycastle.asn1.x500.style.BCStyle
import java.math.BigInteger
import java.security.GeneralSecurityException
import java.security.KeyFactory
import java.security.KeyPairGenerator
import java.security.KeyStore
import java.security.MessageDigest
import java.security.PrivateKey
import java.security.ProviderException
import java.security.SecureRandom
import java.security.Signature
import java.security.cert.X509Certificate
import java.time.Instant
import java.time.temporal.ChronoUnit
import java.util.Base64
import java.util.Date
import java.util.UUID
import javax.security.auth.x500.X500Principal

/** Generates only device-held keys. The legacy application PIN is deliberately not a key credential. */
class AndroidKeystoreCertificateGenerator(context: Context) : CertificateGenerator {
    private val context = context.applicationContext

    override fun dispose(certificate: GeneratedCertificate) {
        delete(requireNotNull(certificate.keyAlias) { "Missing device signing key alias" })
    }

    override fun validate(certificate: GeneratedCertificate) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.R) error("Android 11 or later is required")
        check(certificate.privateCertBinary == null && certificate.salt == null) {
            "Exportable signing material is not accepted"
        }
        val alias = requireNotNull(certificate.keyAlias) { "Missing device signing key alias" }
        require(alias.startsWith(ALIAS_PREFIX)) { "Unexpected signing key alias" }
        val keyStore = store()
        val key = keyStore.getKey(alias, null) as? PrivateKey
            ?: throw GeneralSecurityException("Pending device signing key is missing; recover the account")
        requireSecureSigningKey(key)
        val stored = keyStore.getCertificate(alias) as? X509Certificate
            ?: throw GeneralSecurityException("Pending device certificate is missing")
        val publicBytes = stored.encoded
        check(Base64.getEncoder().encodeToString(publicBytes) == certificate.publicCertBinary) {
            "Pending certificate does not match the device signing key"
        }
        check(MessageDigest.getInstance("SHA-1").digest(publicBytes)
            .joinToString("") { "%02X".format(it) } == certificate.thumbprint &&
            stored.notBefore.toInstant() == certificate.validFrom &&
            stored.notAfter.toInstant() == certificate.validTo
        ) { "Pending certificate metadata does not match the device signing key" }
        // Detect key invalidation before sending a request that can retire the old certificate.
        // This only initializes an operation; it never grants authorization or produces a signature.
        // https://developer.android.com/reference/android/security/keystore/KeyPermanentlyInvalidatedException
        Signature.getInstance("SHA512withRSA").initSign(key)
    }

    override fun generate(email: String, pin: String?): GeneratedCertificate {
        require(pin.isNullOrEmpty()) { "An application PIN cannot protect a device-held signing key" }
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.R) {
            error("Device-held signing requires Android 11 or later and a device screen lock")
        }
        val alias = ALIAS_PREFIX + UUID.randomUUID()
        val strongBoxAvailable = context.packageManager.hasSystemFeature(PackageManager.FEATURE_STRONGBOX_KEYSTORE)
        return generateWithHardwareFallback(
            strongBoxAvailable = strongBoxAvailable,
            generate = { strongBox -> generate(alias, email, strongBox) },
            cleanup = { delete(alias) },
        )
    }

    @RequiresApi(Build.VERSION_CODES.R)
    private fun generate(alias: String, email: String, strongBox: Boolean): GeneratedCertificate {
        val validFrom = Instant.now().minus(1, ChronoUnit.DAYS)
        val validTo = Instant.now().plus(365, ChronoUnit.DAYS)
        val subject = X500Principal(X500NameBuilder(BCStyle.INSTANCE)
            .addRDN(BCStyle.CN, email.replace("@", "")).build().encoded)
        val spec = KeyGenParameterSpec.Builder(alias, KeyProperties.PURPOSE_SIGN)
            .setKeySize(2048)
            .setDigests(KeyProperties.DIGEST_SHA512)
            .setSignaturePaddings(KeyProperties.SIGNATURE_PADDING_RSA_PKCS1)
            .setUserAuthenticationRequired(true)
            .setUserAuthenticationParameters(0, AUTHENTICATORS)
            // Credential fallback deliberately preserves keys across biometric enrollment changes.
            // Screen-lock removal/reset still invalidates the key; recovery re-enrolls a fresh key.
            .setInvalidatedByBiometricEnrollment(false)
            .setIsStrongBoxBacked(strongBox)
            .setCertificateSubject(subject)
            .setCertificateSerialNumber(BigInteger(1, ByteArray(16).also { SecureRandom().nextBytes(it) }))
            .setCertificateNotBefore(Date.from(validFrom))
            .setCertificateNotAfter(Date.from(validTo))
            .build()
        val generator = KeyPairGenerator.getInstance(KeyProperties.KEY_ALGORITHM_RSA, KEYSTORE)
        generator.initialize(spec)
        val pair = generator.generateKeyPair()
        val securityLevel = requireSecureSigningKey(pair.private, strongBox)
        val certificate = store().getCertificate(alias) as X509Certificate
        // An auth-per-use key cannot self-sign during creation. Android's placeholder X.509
        // certificate is a public-key container; Passi validates dates/CN and challenge signatures.
        // https://developer.android.com/reference/android/security/keystore/KeyGenParameterSpec
        val publicBytes = certificate.encoded
        return GeneratedCertificate(
            salt = null,
            privateCertBinary = null,
            publicCertBinary = Base64.getEncoder().encodeToString(publicBytes),
            thumbprint = MessageDigest.getInstance("SHA-1").digest(publicBytes)
                .joinToString("") { "%02X".format(it) },
            validFrom = certificate.notBefore.toInstant(),
            validTo = certificate.notAfter.toInstant(),
            keyAlias = alias,
            keySecurityLevel = securityLevel,
        )
    }

    companion object {
        const val KEYSTORE = "AndroidKeyStore"
        const val ALIAS_PREFIX = "passi.account."
        const val AUTHENTICATORS = KeyProperties.AUTH_BIOMETRIC_STRONG or KeyProperties.AUTH_DEVICE_CREDENTIAL

        fun delete(alias: String) {
            require(alias.startsWith(ALIAS_PREFIX)) { "Unexpected signing key alias" }
            store().deleteEntry(alias)
        }

        internal fun store(): KeyStore = KeyStore.getInstance(KEYSTORE).apply { load(null) }

        @Suppress("DEPRECATION")
        internal fun requireSecureSigningKey(key: PrivateKey, requestedStrongBox: Boolean = false): String {
            if (Build.VERSION.SDK_INT < Build.VERSION_CODES.R) error("Android 11 or later is required")
            val info = KeyFactory.getInstance(key.algorithm, KEYSTORE).getKeySpec(key, KeyInfo::class.java)
            // https://developer.android.com/reference/android/security/keystore/KeyInfo
            val level = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                when (info.securityLevel) {
                    KeyProperties.SECURITY_LEVEL_STRONGBOX -> "STRONGBOX"
                    KeyProperties.SECURITY_LEVEL_TRUSTED_ENVIRONMENT -> "TEE"
                    else -> throw GeneralSecurityException("Hardware-backed signing is unavailable")
                }
            } else {
                if (!info.isInsideSecureHardware) throw GeneralSecurityException("Hardware-backed signing is unavailable")
                if (requestedStrongBox) "STRONGBOX" else "HARDWARE"
            }
            if (key.encoded != null || key.format != null || info.keySize != 2048 ||
                info.purposes != KeyProperties.PURPOSE_SIGN ||
                info.digests.toSet() != setOf(KeyProperties.DIGEST_SHA512) ||
                info.signaturePaddings.toSet() != setOf(KeyProperties.SIGNATURE_PADDING_RSA_PKCS1) ||
                !info.isUserAuthenticationRequired ||
                !info.isUserAuthenticationRequirementEnforcedBySecureHardware ||
                info.userAuthenticationType != AUTHENTICATORS ||
                info.userAuthenticationValidityDurationSeconds != -1
            ) throw GeneralSecurityException("Signing key does not satisfy device authorization policy")
            return level
        }
    }
}

/** Every failed attempt cleans its alias; only supported provider failures permit a TEE retry. */
internal fun <T> generateWithHardwareFallback(
    strongBoxAvailable: Boolean,
    generate: (strongBox: Boolean) -> T,
    cleanup: () -> Unit,
): T {
    fun cleanFailedAttempt(failure: Throwable) {
        try {
            cleanup()
        } catch (cleanupFailure: Throwable) {
            if (cleanupFailure !== failure) failure.addSuppressed(cleanupFailure)
            // A failed deletion must not be followed by another attempt using the same alias.
            throw failure
        }
    }
    if (strongBoxAvailable) {
        // SHA-512 is not a required StrongBox digest. Do not relax the key's policy for TEE.
        // https://developer.android.com/privacy-and-security/keystore#StrongBoxKeyMint
        try {
            return generate(true)
        } catch (failure: Throwable) {
            cleanFailedAttempt(failure)
            if (failure !is GeneralSecurityException && failure !is ProviderException) throw failure
        }
    }
    try {
        return generate(false)
    } catch (failure: Throwable) {
        cleanFailedAttempt(failure)
        throw failure
    }
}
