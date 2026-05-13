package com.passi.cloud.passi_android.domain.account

import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.ManagedDevice

interface AccountManagementService {
    suspend fun deleteAccount(account: Account): Result<Unit>

    suspend fun getDevices(account: Account, currentDeviceId: String): Result<List<ManagedDevice>>

    suspend fun deleteDevice(account: Account, deviceId: String, currentDeviceId: String): Result<Unit>
}