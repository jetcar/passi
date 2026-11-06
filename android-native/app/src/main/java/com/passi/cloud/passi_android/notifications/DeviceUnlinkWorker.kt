package com.passi.cloud.passi_android.notifications

import android.content.Context
import androidx.work.CoroutineWorker
import androidx.work.WorkerParameters
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.data.notifications.DeviceUnlinkRequestException

class DeviceUnlinkWorker(
    appContext: Context,
    workerParameters: WorkerParameters,
) : CoroutineWorker(appContext, workerParameters) {
    override suspend fun doWork(): Result {
        val apiBaseUrl = inputData.getString(KEY_API_BASE_URL).orEmpty()
        val unlinkToken = inputData.getString(KEY_UNLINK_TOKEN).orEmpty()
        if (apiBaseUrl.isBlank() || unlinkToken.isBlank()) {
            return Result.failure()
        }

        val container = (applicationContext as? PassiApplication)?.container ?: return Result.failure()
        val unlinkResult = try {
            container.deviceUnlinkService.unlinkDevice(apiBaseUrl, unlinkToken)
        } catch (error: Throwable) {
            kotlin.Result.failure(error)
        }

        if (unlinkResult.isSuccess) {
            PassiNotifications.clearMissingAccountNotification(applicationContext, unlinkToken)
            return Result.success()
        }

        val error = unlinkResult.exceptionOrNull()
        if (runAttemptCount > 0 && error is DeviceUnlinkRequestException && !error.retryable) {
            // The previous attempt may have reached the server and lost its success response.
            PassiNotifications.clearMissingAccountNotification(applicationContext, unlinkToken)
            return Result.success()
        }

        return if (runAttemptCount < MAX_ATTEMPTS &&
            (error !is DeviceUnlinkRequestException || error.retryable)
        ) {
            Result.retry()
        } else {
            Result.failure()
        }
    }

    companion object {
        const val KEY_API_BASE_URL = "device_unlink_api_base_url"
        const val KEY_UNLINK_TOKEN = "device_unlink_token"
        const val MAX_ATTEMPTS = 3
    }
}
