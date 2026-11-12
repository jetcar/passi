package com.passi.cloud.passi_android.domain.auth

import com.passi.cloud.passi_android.domain.model.Account

interface AccountSigner {
    suspend fun sign(account: Account, pin: String?, message: String): Result<String>
}