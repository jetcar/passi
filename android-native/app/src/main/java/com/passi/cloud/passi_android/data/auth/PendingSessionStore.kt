package com.passi.cloud.passi_android.data.auth

import com.passi.cloud.passi_android.domain.model.NotificationSession

class PendingSessionStore {
    private var current: NotificationSession? = null

    fun save(session: NotificationSession?) {
        current = session
    }

    fun read(): NotificationSession? = current

    fun clear() {
        current = null
    }
}