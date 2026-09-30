package com.passi.cloud.passi_android.data.update

import android.content.Context
import com.google.android.play.core.appupdate.AppUpdateManagerFactory
import com.google.android.play.core.install.model.UpdateAvailability
import com.google.android.play.core.ktx.requestAppUpdateInfo
import com.passi.cloud.passi_android.domain.update.AppUpdateChecker
import kotlinx.coroutines.CancellationException

// Asks Google Play whether a newer version is published. Only works for installs that came from Google Play;
// sideloaded/debug builds get a failure, which callers treat as "no update".
class PlayAppUpdateChecker(
    context: Context,
) : AppUpdateChecker {
    private val appUpdateManager = AppUpdateManagerFactory.create(context.applicationContext)

    override suspend fun isUpdateAvailable(): Result<Boolean> = try {
        val info = appUpdateManager.requestAppUpdateInfo()
        Result.success(info.updateAvailability() == UpdateAvailability.UPDATE_AVAILABLE)
    } catch (error: CancellationException) {
        throw error
    } catch (error: Exception) {
        Result.failure(error)
    }
}
