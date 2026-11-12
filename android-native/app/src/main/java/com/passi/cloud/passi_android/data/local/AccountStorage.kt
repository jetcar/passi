package com.passi.cloud.passi_android.data.local

import com.passi.cloud.passi_android.domain.model.Account
import java.util.UUID

/** Each write returns only after its complete account snapshot is durably committed. */
interface AccountStorage {
    fun readAccounts(): List<Account>
    fun writeAccounts(accounts: List<Account>)
    fun deleteBiometricMaterial(accountId: UUID)
}
