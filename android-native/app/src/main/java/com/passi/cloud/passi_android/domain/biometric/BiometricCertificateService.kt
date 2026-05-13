package com.passi.cloud.passi_android.domain.biometric

import com.passi.cloud.passi_android.domain.model.Account

interface BiometricCertificateService {
    suspend fun enableBiometric(account: Account, pin: String?): Result<Account>

    fun sign(account: Account, message: String): Result<String>
}