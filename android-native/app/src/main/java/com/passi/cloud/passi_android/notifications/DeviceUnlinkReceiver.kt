package com.passi.cloud.passi_android.notifications

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import androidx.work.BackoffPolicy
import androidx.work.Constraints
import androidx.work.ExistingWorkPolicy
import androidx.work.NetworkType
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.workDataOf
import java.util.concurrent.TimeUnit

class DeviceUnlinkReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        val apiBaseUrl = intent.getStringExtra(EXTRA_API_BASE_URL).orEmpty()
        val unlinkToken = intent.getStringExtra(EXTRA_UNLINK_TOKEN).orEmpty()
        if (apiBaseUrl.isBlank() || unlinkToken.isBlank()) {
            return
        }

        val request = OneTimeWorkRequestBuilder<DeviceUnlinkWorker>()
            .setInputData(
                workDataOf(
                    DeviceUnlinkWorker.KEY_API_BASE_URL to apiBaseUrl,
                    DeviceUnlinkWorker.KEY_UNLINK_TOKEN to unlinkToken,
                )
            )
            .setConstraints(Constraints.Builder().setRequiredNetworkType(NetworkType.CONNECTED).build())
            .setBackoffCriteria(BackoffPolicy.EXPONENTIAL, 10, TimeUnit.SECONDS)
            .build()

        WorkManager.getInstance(context).enqueueUniqueWork(
            "device-unlink-${unlinkToken.hashCode()}",
            ExistingWorkPolicy.KEEP,
            request,
        )
    }

    companion object {
        private const val EXTRA_API_BASE_URL = "device_unlink_api_base_url"
        private const val EXTRA_UNLINK_TOKEN = "device_unlink_token"

        fun intent(context: Context, apiBaseUrl: String, unlinkToken: String): Intent =
            Intent(context, DeviceUnlinkReceiver::class.java).apply {
                putExtra(EXTRA_API_BASE_URL, apiBaseUrl)
                putExtra(EXTRA_UNLINK_TOKEN, unlinkToken)
            }
    }
}
