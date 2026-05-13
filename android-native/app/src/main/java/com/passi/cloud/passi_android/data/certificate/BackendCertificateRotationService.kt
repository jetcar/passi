package com.passi.cloud.passi_android.data.certificate

import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.domain.auth.AccountSigner
import com.passi.cloud.passi_android.domain.biometric.BiometricCertificateService
import com.passi.cloud.passi_android.domain.certificate.CertificateRotationService
import com.passi.cloud.passi_android.domain.enrollment.CertificateGenerator
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository

private data class CertificateUpdateRequestDto(
    val PublicCert: String,
    val ParentCertThumbprint: String,
    val ParentCertHashSignature: String,
    val DeviceId: String,
)

class BackendCertificateRotationService(
    private val apiClient: PassiApiClient,
    private val providersRepository: ProvidersRepository,
    private val certificateGenerator: CertificateGenerator,
    private val accountSigner: AccountSigner,
    private val biometricCertificateService: BiometricCertificateService,
) : CertificateRotationService {
    override suspend fun rotateCertificate(
        account: Account,
        oldPin: String?,
        newPin: String?,
        useBiometric: Boolean,
    ): Result<Account> {
        val provider = providersRepository.getProvider(account.providerId)
            ?: return Result.failure(IllegalStateException("Provider not found"))
        val parentThumbprint = account.thumbprint
            ?: return Result.failure(IllegalStateException("Current certificate thumbprint is missing"))

        val generatedCertificate = runCatching {
            certificateGenerator.generate(account.email, newPin)
        }.getOrElse { error ->
            return Result.failure(IllegalStateException(error.message ?: "Certificate generation failed"))
        }

        val parentSignature = if (useBiometric) {
            biometricCertificateService.sign(account, generatedCertificate.publicCertBinary)
                .getOrElse { error ->
                    return Result.failure(IllegalStateException(error.message ?: "Biometric signing failed"))
                }
        } else {
            accountSigner.sign(account, oldPin, generatedCertificate.publicCertBinary)
                .getOrElse {
                    return Result.failure(IllegalStateException(if (account.pinLength > 0) "Invalid old pin" else "Certificate signing failed"))
                }
        }

        val result = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.updateCertificate,
            payload = CertificateUpdateRequestDto(
                PublicCert = generatedCertificate.publicCertBinary,
                ParentCertThumbprint = parentThumbprint,
                ParentCertHashSignature = parentSignature,
                DeviceId = account.deviceId,
            )
        )

        if (!result.isSuccessful) {
            val message = apiClient.extractErrorMessage(result.body)
            return Result.failure(IllegalStateException(message ?: "Network error. Try again"))
        }

        return Result.success(
            account.copy(
                salt = generatedCertificate.salt,
                privateCertBinary = generatedCertificate.privateCertBinary,
                publicCertBinary = generatedCertificate.publicCertBinary,
                thumbprint = generatedCertificate.thumbprint,
                validFrom = generatedCertificate.validFrom,
                validTo = generatedCertificate.validTo,
                pinLength = newPin?.length ?: 0,
            )
        )
    }
}