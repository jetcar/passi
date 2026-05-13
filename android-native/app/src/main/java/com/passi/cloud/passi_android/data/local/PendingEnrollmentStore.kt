package com.passi.cloud.passi_android.data.local

import com.passi.cloud.passi_android.domain.service.PendingEnrollment

class PendingEnrollmentStore {
    private var current: PendingEnrollment? = null

    fun save(pendingEnrollment: PendingEnrollment) {
        current = pendingEnrollment
    }

    fun read(): PendingEnrollment? = current

    fun clear() {
        current = null
    }
}