package com.passi.cloud.passi_android.domain.certificate

import com.passi.cloud.passi_android.domain.model.Account

interface CertificateRotationService {
    suspend fun rotateCertificate(
        account: Account,
        oldPin: String?,
        newPin: String?,
        useBiometric: Boolean,
    ): Result<Account>
}