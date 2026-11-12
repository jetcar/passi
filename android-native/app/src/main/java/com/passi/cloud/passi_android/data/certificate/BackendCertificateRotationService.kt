package com.passi.cloud.passi_android.data.certificate

import com.google.gson.annotations.SerializedName
import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.domain.auth.AccountSigner
import com.passi.cloud.passi_android.domain.certificate.CertificateRotationService
import com.passi.cloud.passi_android.domain.enrollment.CertificateGenerator
import com.passi.cloud.passi_android.domain.enrollment.checkpoint
import com.passi.cloud.passi_android.domain.enrollment.certificate
import com.passi.cloud.passi_android.domain.enrollment.preparePendingDeviceKey
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import java.time.Instant

private data class CertificateUpdateResponseDto(
    @SerializedName(value = "PublicCert", alternate = ["publicCert"]) val publicCert: String?,
)

private data class CertificateUpdateRequestDto(
    val PublicCert: String,
    val ParentCertThumbprint: String,
    val ParentCertHashSignature: String,
    val DeviceId: String,
    val RetireParent: Boolean = true,
)

class BackendCertificateRotationService(
    private val apiClient: PassiApiClient,
    private val providersRepository: ProvidersRepository,
    private val certificateGenerator: CertificateGenerator,
    private val accountSigner: AccountSigner,
    private val accountsRepository: AccountsRepository,
) : CertificateRotationService {
    private val mutex = Mutex()

    override suspend fun rotateCertificate(
        account: Account, oldPin: String?, newPin: String?, useBiometric: Boolean,
    ): Result<Account> = mutex.withLock {
        try {
            require(newPin.isNullOrEmpty()) { "Use the device screen lock instead of a Passi PIN" }
            var current = accountsRepository.getAccount(account.id) ?: error("Account not found")
            // A stale screen must not create another successor after a completed retry.
            if (current.thumbprint != account.thumbprint) return@withLock Result.success(current)
            val provider = providersRepository.getProvider(current.providerId) ?: error("Provider not found")
            val parent = requireNotNull(current.thumbprint) { "Current certificate thumbprint is missing" }
            current = preparePendingDeviceKey(current, accountsRepository, certificateGenerator) {
                // Validate the legacy PIN before creating a replacement. This signature is discarded.
                if (current.keyAlias == null) {
                    accountSigner.sign(current, oldPin, "Passi:UnlockForMigration:v1")
                        .getOrElse { error("Invalid old pin") }
                }
            }
            val pending = requireNotNull(current.pendingKey)
            // Retirement is signed, not an unsigned flag. Old servers reject it safely.
            val message = "Passi:RetireParent:v2\n$parent\n${current.deviceId}\n${pending.publicCertBinary}"
            val signature = accountSigner.sign(current, oldPin, message).getOrThrow()
            current = current.copy(pendingKey = pending.copy(submitted = true))
            accountsRepository.updateAccount(current)
            val result = apiClient.postJson(provider.baseUrl, provider.apiPaths.updateCertificate,
                CertificateUpdateRequestDto(pending.publicCertBinary, parent, signature, current.deviceId))
            val accepted = apiClient.parseBody(result.body, CertificateUpdateResponseDto::class.java)
            check(result.isSuccessful && accepted?.publicCert == pending.publicCertBinary) {
                "Certificate update not confirmed. Retry with the same account."
            }
            val updated = current.copy(
                keyAlias = pending.keyAlias, keySecurityLevel = pending.keySecurityLevel,
                publicCertBinary = pending.publicCertBinary, thumbprint = pending.thumbprint,
                validFrom = Instant.parse(pending.validFrom), validTo = Instant.parse(pending.validTo),
                salt = null, privateCertBinary = null, pinLength = 0, hasFingerprint = false,
                pendingKey = null, legacyCleanupPending = true, cleanupKeyAlias = current.keyAlias,
                retiredKeyAliases = (current.retiredKeyAliases.orEmpty() + listOfNotNull(current.cleanupKeyAlias)).distinct(),
            )
            accountsRepository.updateAccount(updated)
            Result.success(accountsRepository.getAccount(updated.id) ?: updated)
        } catch (error: CancellationException) {
            throw error
        } catch (error: Exception) {
            Result.failure(error)
        }
    }
}
