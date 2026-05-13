package com.passi.cloud.passi_android.data.selection

import java.util.UUID

class SelectedAccountStore {
    private var selectedAccountId: UUID? = null

    fun save(accountId: UUID?) {
        selectedAccountId = accountId
    }

    fun read(): UUID? = selectedAccountId

    fun clear() {
        selectedAccountId = null
    }
}