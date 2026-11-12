package com.passi.cloud.passi_android.data.crypto

import com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate
import org.bouncycastle.asn1.x500.X500Name
import org.bouncycastle.cert.jcajce.JcaX509CertificateConverter
import org.bouncycastle.cert.jcajce.JcaX509v3CertificateBuilder
import org.bouncycastle.jce.provider.BouncyCastleProvider
import org.bouncycastle.operator.jcajce.JcaContentSignerBuilder
import java.io.ByteArrayOutputStream
import java.math.BigInteger
import java.security.KeyPairGenerator
import java.security.KeyStore
import java.security.MessageDigest
import java.security.Provider
import java.security.Security
import java.time.Instant
import java.time.temporal.ChronoUnit
import java.util.Base64
import java.util.Date
import java.util.UUID

class BouncyCastleCertificateGenerator {
    private val provider: Provider = BouncyCastleProvider()

    init {
        if (Security.getProvider(provider.name) !== provider) {
            Security.addProvider(provider)
        }
    }

    fun generate(email: String, pin: String?): GeneratedCertificate {
        val keyPairGenerator = KeyPairGenerator.getInstance(KEY_ALGORITHM)
        keyPairGenerator.initialize(KEY_SIZE)
        val keyPair = keyPairGenerator.generateKeyPair()

        val validFrom = Instant.now().minus(1, ChronoUnit.DAYS)
        val validTo = Instant.now().plus(365, ChronoUnit.DAYS)
        val subject = X500Name("CN=${email.replace("@", "")}")
        val serialNumber = BigInteger.valueOf(System.currentTimeMillis())

        val certificateBuilder = JcaX509v3CertificateBuilder(
            subject,
            serialNumber,
            Date.from(validFrom),
            Date.from(validTo),
            subject,
            keyPair.public,
        )
        val contentSigner = JcaContentSignerBuilder(SIGNATURE_ALGORITHM)
            .setProvider(provider)
            .build(keyPair.private)
        val x509Certificate = JcaX509CertificateConverter()
            .setProvider(provider)
            .getCertificate(certificateBuilder.build(contentSigner))

        val salt = UUID.randomUUID().toString()
        val password = salt + (pin ?: "")
        val pkcs12Bytes = exportPkcs12(password, keyPair.private, x509Certificate)
        val publicBytes = x509Certificate.encoded

        return GeneratedCertificate(
            salt = salt,
            privateCertBinary = Base64.getEncoder().encodeToString(pkcs12Bytes),
            publicCertBinary = Base64.getEncoder().encodeToString(publicBytes),
            thumbprint = sha1Hex(publicBytes),
            validFrom = x509Certificate.notBefore.toInstant(),
            validTo = x509Certificate.notAfter.toInstant(),
        )
    }

    private fun exportPkcs12(
        password: String,
        privateKey: java.security.PrivateKey,
        certificate: java.security.cert.X509Certificate,
    ): ByteArray {
        val keyStore = KeyStore.getInstance(KEYSTORE_TYPE)
        keyStore.load(null, null)
        keyStore.setKeyEntry(KEY_ALIAS, privateKey, password.toCharArray(), arrayOf(certificate))

        return ByteArrayOutputStream().use { outputStream ->
            keyStore.store(outputStream, password.toCharArray())
            outputStream.toByteArray()
        }
    }

    private fun sha1Hex(bytes: ByteArray): String {
        return MessageDigest.getInstance(THUMBPRINT_ALGORITHM)
            .digest(bytes)
            .joinToString(separator = "") { byte -> "%02X".format(byte) }
    }

    private companion object {
        const val KEY_ALGORITHM = "RSA"
        const val SIGNATURE_ALGORITHM = "SHA256withRSA"
        const val THUMBPRINT_ALGORITHM = "SHA-1"
        const val KEYSTORE_TYPE = "PKCS12"
        const val KEY_ALIAS = "passi"
        const val KEY_SIZE = 2048
    }
}
