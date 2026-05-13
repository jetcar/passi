package com.passi.cloud.passi_android.data.notifications

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class NotificationOpenStore {
    private val _openRequests = MutableStateFlow(0L)
    val openRequests: StateFlow<Long> = _openRequests.asStateFlow()

    fun requestOpen() {
        _openRequests.value += 1
    }
}