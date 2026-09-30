package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.feature.update.AppUpdateViewModel
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.advanceUntilIdle
import org.junit.Test

@OptIn(ExperimentalCoroutinesApi::class)
class AppUpdateUseCaseTests : CoroutineViewModelTest() {
    @Test
    fun bannerIsShownWhenNewVersionIsAvailable() = runViewModelTest {
        val checker = FakeAppUpdateChecker().apply { result = Result.success(true) }
        val viewModel = AppUpdateViewModel(checker)

        viewModel.check()
        advanceUntilIdle()

        assertThat(viewModel.uiState.value.showBanner).isTrue()
    }

    @Test
    fun bannerIsHiddenWhenAppIsUpToDate() = runViewModelTest {
        val checker = FakeAppUpdateChecker().apply { result = Result.success(false) }
        val viewModel = AppUpdateViewModel(checker)

        viewModel.check()
        advanceUntilIdle()

        assertThat(viewModel.uiState.value.showBanner).isFalse()
    }

    @Test
    fun bannerIsHiddenWhenCheckFails() = runViewModelTest {
        val checker = FakeAppUpdateChecker().apply { result = Result.failure(IllegalStateException("Play Store unavailable")) }
        val viewModel = AppUpdateViewModel(checker)

        viewModel.check()
        advanceUntilIdle()

        assertThat(viewModel.uiState.value.showBanner).isFalse()
    }

    @Test
    fun failedRecheckKeepsBannerFromEarlierSuccessfulCheck() = runViewModelTest {
        val checker = FakeAppUpdateChecker().apply { result = Result.success(true) }
        val viewModel = AppUpdateViewModel(checker)

        viewModel.check()
        advanceUntilIdle()
        checker.result = Result.failure(IllegalStateException("offline"))
        viewModel.check()
        advanceUntilIdle()

        assertThat(viewModel.uiState.value.showBanner).isTrue()
    }

    @Test
    fun dismissedBannerStaysHiddenAfterRecheck() = runViewModelTest {
        val checker = FakeAppUpdateChecker().apply { result = Result.success(true) }
        val viewModel = AppUpdateViewModel(checker)

        viewModel.check()
        advanceUntilIdle()
        viewModel.dismiss()
        viewModel.check()
        advanceUntilIdle()

        assertThat(viewModel.uiState.value.showBanner).isFalse()
        assertThat(checker.checkCount).isEqualTo(2)
    }

    @Test
    fun bannerDisappearsOnceUpdateIsInstalled() = runViewModelTest {
        val checker = FakeAppUpdateChecker().apply { result = Result.success(true) }
        val viewModel = AppUpdateViewModel(checker)

        viewModel.check()
        advanceUntilIdle()
        checker.result = Result.success(false)
        viewModel.check()
        advanceUntilIdle()

        assertThat(viewModel.uiState.value.showBanner).isFalse()
    }
}
