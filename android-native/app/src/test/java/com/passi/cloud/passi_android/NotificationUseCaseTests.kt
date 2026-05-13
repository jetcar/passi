package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.google.gson.Gson
import com.passi.cloud.passi_android.data.notifications.BackendNotificationTokenRegistrationService
import com.passi.cloud.passi_android.data.notifications.NotificationOpenStore
import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.domain.model.ApiPaths
import com.passi.cloud.passi_android.domain.model.Provider
import kotlinx.coroutines.ExperimentalCoroutinesApi
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.Test
import java.util.UUID

@OptIn(ExperimentalCoroutinesApi::class)
class NotificationUseCaseTests : CoroutineViewModelTest() {
    @Test
    fun notificationOpenStoreIncrementsOnRequest() = runViewModelTest {
        val store = NotificationOpenStore()

        assertThat(store.openRequests.value).isEqualTo(0L)

        store.requestOpen()
        store.requestOpen()

        assertThat(store.openRequests.value).isEqualTo(2L)
    }

    @Test
    fun notificationTokenRegistrationPostsTokenToProviderEndpoint() = runViewModelTest {
        val server = MockWebServer()
        server.enqueue(MockResponse().setResponseCode(200).setBody("{}"))
        server.start()

        try {
            val providersRepository = inMemoryProvidersRepository().apply {
                saveProvider(
                    Provider(
                        id = UUID.fromString("1e41fdd4-9b7a-4cdc-80d6-4ff94f4bb6d2"),
                        name = "local",
                        baseUrl = server.url("/").toString().trimEnd('/'),
                        isDefault = false,
                        apiPaths = ApiPaths.defaultPaths(),
                    )
                )
            }

            val service = BackendNotificationTokenRegistrationService(
                apiClient = PassiApiClient(Gson()),
                providersRepository = providersRepository,
                deviceIdProvider = { "device-123" },
            )

            val result = service.registerToken("token-abc")
            val request = server.takeRequest()
            val requestBody = request.body.readUtf8()

            assertThat(result.isSuccess).isTrue()
            assertThat(request.path).isEqualTo("/api/Token/Update")
            assertThat(requestBody).contains("token-abc")
            assertThat(requestBody).contains("device-123")
            assertThat(requestBody).contains("Android")
        } finally {
            server.shutdown()
        }
    }

    @Test
    fun notificationTokenRegistrationReturnsFailureForBadRequest() = runViewModelTest {
        val server = MockWebServer()
        server.enqueue(MockResponse().setResponseCode(400).setBody("{\"errors\":\"token invalid\"}"))
        server.start()

        try {
            val providersRepository = inMemoryProvidersRepository().apply {
                saveProvider(
                    Provider(
                        id = UUID.fromString("8410d995-c312-4b2f-9f33-8d71db854dd4"),
                        name = "local",
                        baseUrl = server.url("/").toString().trimEnd('/'),
                        isDefault = false,
                        apiPaths = ApiPaths.defaultPaths(),
                    )
                )
            }
            val service = BackendNotificationTokenRegistrationService(
                apiClient = PassiApiClient(Gson()),
                providersRepository = providersRepository,
                deviceIdProvider = { "device-123" },
            )

            val result = service.registerToken("token-abc")

            assertThat(result.isFailure).isTrue()
            assertThat(result.exceptionOrNull()?.message).isEqualTo("token invalid")
        } finally {
            server.shutdown()
        }
    }
}