package com.passi.cloud.passi_android.data.selection

import java.util.UUID

class SelectedProviderStore {
    private var selectedProviderId: UUID? = null

    fun save(providerId: UUID?) {
        selectedProviderId = providerId
    }

    fun read(): UUID? = selectedProviderId

    fun clear() {
        selectedProviderId = null
    }
}