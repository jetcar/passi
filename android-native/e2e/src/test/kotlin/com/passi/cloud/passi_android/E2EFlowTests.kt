package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.google.gson.FieldNamingPolicy
import com.google.gson.GsonBuilder
import com.google.gson.JsonParser
import com.passi.cloud.passi_android.data.crypto.BouncyCastleCertificateGenerator
import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.data.repository.BackendEnrollmentService
import com.passi.cloud.passi_android.domain.model.ApiPaths
import com.passi.cloud.passi_android.domain.model.Provider
import kotlinx.coroutines.runBlocking
import org.junit.AfterClass
import org.junit.Before
import org.junit.BeforeClass
import org.junit.Test
import java.io.InputStreamReader
import java.net.ServerSocket
import java.net.HttpURLConnection
import java.net.URL
import java.time.Duration
import java.util.UUID

class E2EFlowTests {

    companion object {
        private lateinit var networkName: String
        private lateinit var postgresName: String
        private lateinit var redisName: String
        private lateinit var mailhogName: String
        private lateinit var passiApiName: String
        private lateinit var passiApiImage: String
        private var mailhogHttpPort: Int = 0
        private var passiApiPort: Int = 0

        private lateinit var apiClient: PassiApiClient
        private lateinit var provider: Provider

        init {
            Runtime.getRuntime().addShutdownHook(Thread {
                stopContainers()
            })
        }

        @BeforeClass
        @JvmStatic
        fun startContainers() {
            cleanupStaleContainers()
            val suffix = UUID.randomUUID().toString().replace("-", "").take(12)
            networkName = "passi-e2e-$suffix"
            postgresName = "passi-e2e-postgres-$suffix"
            redisName = "passi-e2e-redis-$suffix"
            mailhogName = "passi-e2e-mailhog-$suffix"
            passiApiName = "passi-e2e-api-$suffix"
            passiApiImage = System.getProperty("passi.api.image", "passiwebapi-e2e:local")
            mailhogHttpPort = findFreePort()
            passiApiPort = findFreePort()

            buildLocalPassiApiImage()

            docker("network", "create", networkName)

            docker(
                "run", "-d",
                "--name", postgresName,
                "--network", networkName,
                "--network-alias", "postgres",
                "-e", "POSTGRES_PASSWORD=test1",
                "-e", "POSTGRES_USER=postgres",
                "-e", "POSTGRES_DB=Passi",
                "postgres:15.5",
            )
            waitForLog(postgresName, "database system is ready to accept connections", occurrences = 2)

            docker(
                "run", "-d",
                "--name", redisName,
                "--network", networkName,
                "--network-alias", "redis",
                "redis:7-alpine",
            )
            waitForLog(redisName, "Ready to accept connections")

            docker(
                "run", "-d",
                "--name", mailhogName,
                "--network", networkName,
                "--network-alias", "mailhog",
                "-p", "$mailhogHttpPort:8025",
                "mailhog/mailhog:latest",
            )
            waitForHttp("http://localhost:$mailhogHttpPort/api/v2/messages")

            docker(
                "run", "-d",
                "--name", passiApiName,
                "--network", networkName,
                "--network-alias", "passiwebapi",
                "-p", "$passiApiPort:5004",
                "-e", "DbHost=postgres",
                "-e", "DbPassword=test1",
                "-e", "DbPort=5432",
                "-e", "DbUser=postgres",
                "-e", "DbSslMode=Allow",
                "-e", "PassiDbName=Passi",
                "-e", "redis=redis",
                "-e", "redisPort=6379",
                "-e", "smtpHost=mailhog",
                "-e", "smtpPort=1025",
                "-e", "smtpUsername=test",
                "-e", "smtpPassword=test",
                "-e", "emailFrom=passi@test.com",
                "-e", "DoNotSendMail=false",
                "-e", "IsTest=true",
                "-e", "SmtpDisableSsl=true",
                "-e", "google-services-json-path=/nonexistent",
                passiApiImage,
            )
            waitForHttp("http://localhost:$passiApiPort/passiapi/health")

            apiClient = PassiApiClient(
                GsonBuilder()
                    .setFieldNamingPolicy(FieldNamingPolicy.UPPER_CAMEL_CASE)
                    .disableHtmlEscaping()
                    .create()
            )

            provider = Provider(
                id = UUID.fromString("7a22cc55-4d18-4e02-bfa5-f6ce4913fbfb"),
                name = "test",
                baseUrl = "http://localhost:$passiApiPort/passiapi",
                isDefault = true,
                apiPaths = ApiPaths.defaultPaths(),
            )
        }

        @AfterClass
        @JvmStatic
        fun stopContainers() {
            if (::passiApiName.isInitialized) dockerIgnoreErrors("rm", "-f", passiApiName)
            if (::mailhogName.isInitialized) dockerIgnoreErrors("rm", "-f", mailhogName)
            if (::redisName.isInitialized) dockerIgnoreErrors("rm", "-f", redisName)
            if (::postgresName.isInitialized) dockerIgnoreErrors("rm", "-f", postgresName)
            if (::networkName.isInitialized) dockerIgnoreErrors("network", "rm", networkName)
        }

        private fun cleanupStaleContainers() {
            try {
                val containerIds = docker("ps", "-a", "-q", "--filter", "name=passi-e2e-").trim()
                if (containerIds.isNotEmpty()) {
                    val ids = containerIds.lines().map { it.trim() }.filter { it.isNotEmpty() }
                    dockerIgnoreErrors("rm", "-f", *ids.toTypedArray())
                }
                val networkIds = docker("network", "ls", "-q", "--filter", "name=passi-e2e-").trim()
                if (networkIds.isNotEmpty()) {
                    networkIds.lines().map { it.trim() }.filter { it.isNotEmpty() }.forEach { id ->
                        dockerIgnoreErrors("network", "rm", id)
                    }
                }
            } catch (_: Exception) {
            }
        }

        private fun docker(vararg args: String): String {
            val processBuilder = ProcessBuilder(listOf("docker", *args))
                .redirectErrorStream(true)
            processBuilder.environment().remove("DOCKER_API_VERSION")
            processBuilder.environment().remove("DOCKER_HOST")
            processBuilder.environment()["DOCKER_CONTEXT"] = "desktop-linux"

            val process = processBuilder.start()
            val output = process.inputStream.bufferedReader().readText().trim()
            val exitCode = process.waitFor()
            if (exitCode != 0) {
                throw IllegalStateException("docker ${args.joinToString(" ")} failed: $output")
            }
            return output
        }

        private fun dockerIgnoreErrors(vararg args: String) {
            try {
                docker(*args)
            } catch (_: IllegalStateException) {
            }
        }

        private fun buildLocalPassiApiImage() {
            val repoRoot = System.getProperty("passi.repo.root")
                ?: throw IllegalStateException("Missing passi.repo.root system property")

            // Reuse existing repository Docker build chain:
            // 1) root Dockerfile builds jetcar/common_image:latest
            // 2) passiwebapi/Dockerfile copies published output from that image
            runProcess(
                listOf(
                    "docker", "build",
                    ".",
                    "-f", "$repoRoot/Dockerfile",
                    "-t", "jetcar/common_image:latest",
                    "-t", "common_image:dev",
                ),
                repoRoot,
                "docker build for common_image failed",
                sanitizeDockerEnv = true,
            )

            runProcess(
                listOf(
                    "docker", "build",
                    ".",
                    "-f", "$repoRoot/passiwebapi/Dockerfile",
                    "-t", passiApiImage,
                ),
                repoRoot,
                "docker build for PassiApi failed",
                sanitizeDockerEnv = true,
            )
        }

        private fun runProcess(
            command: List<String>,
            workingDirectory: String,
            failurePrefix: String,
            sanitizeDockerEnv: Boolean = false,
        ): String {
            println("[E2E] Running: ${command.joinToString(" ")}")
            val processBuilder = ProcessBuilder(command)
                .directory(java.io.File(workingDirectory))
                .redirectErrorStream(true)

            if (sanitizeDockerEnv) {
                processBuilder.environment().remove("DOCKER_API_VERSION")
                processBuilder.environment().remove("DOCKER_HOST")
                processBuilder.environment()["DOCKER_CONTEXT"] = "desktop-linux"
            }

            val process = processBuilder.start()
            val output = process.inputStream.bufferedReader().readText().trim()
            val exitCode = process.waitFor()
            println("[E2E] Exit $exitCode: ${command.first()}")
            if (output.isNotEmpty()) println(output)
            if (exitCode != 0) {
                throw IllegalStateException("$failurePrefix (exit $exitCode): $output")
            }
            return output
        }

        private fun waitForLog(containerName: String, text: String, occurrences: Int = 1, timeout: Duration = Duration.ofMinutes(2)) {
            println("[E2E] Waiting for '$text' in $containerName...")
            val deadline = System.currentTimeMillis() + timeout.toMillis()
            while (System.currentTimeMillis() < deadline) {
                val logs = docker("logs", containerName)
                if (Regex(Regex.escape(text)).findAll(logs).count() >= occurrences) {
                    println("[E2E] $containerName ready")
                    return
                }
                Thread.sleep(1000)
            }
            val logs = try { docker("logs", containerName) } catch (_: Exception) { "(unavailable)" }
            throw AssertionError("Container $containerName did not emit '$text' within $timeout\nLogs:\n$logs")
        }

        private fun waitForHttp(url: String, timeout: Duration = Duration.ofMinutes(2)) {
            println("[E2E] Waiting for $url ...")
            val deadline = System.currentTimeMillis() + timeout.toMillis()
            var lastError = ""
            while (System.currentTimeMillis() < deadline) {
                try {
                    val connection = URL(url).openConnection() as HttpURLConnection
                    connection.connectTimeout = 5000
                    connection.readTimeout = 5000
                    connection.requestMethod = "GET"
                    val code = connection.responseCode
                    connection.disconnect()
                    if (code in 200..299) {
                        println("[E2E] $url healthy (HTTP $code)")
                        return
                    }
                    lastError = "HTTP $code"
                } catch (e: Exception) {
                    lastError = e.message ?: e.javaClass.simpleName
                }
                Thread.sleep(1000)
            }
            val containerName = if (url.contains("8025")) mailhogName else passiApiName
            val logs = try { docker("logs", containerName) } catch (_: Exception) { "(unavailable)" }
            throw AssertionError("Endpoint $url did not become healthy within $timeout (last error: $lastError)\nContainer logs:\n$logs")
        }

        private fun findFreePort(): Int {
            ServerSocket(0).use { socket ->
                socket.reuseAddress = true
                return socket.localPort
            }
        }
    }

    @Before
    fun clearMailhog() {
        val conn = URL("http://localhost:$mailhogHttpPort/api/v1/messages")
            .openConnection() as HttpURLConnection
        conn.requestMethod = "DELETE"
        conn.responseCode
        conn.disconnect()
    }

    private data class SyncAccountsRequest(
        val deviceId: String,
        val guids: List<String>,
    )

    private data class GetAllSessionRequest(
        val deviceId: String,
    )

    private data class DeviceTokenUpdateRequest(
        val deviceId: String,
        val token: String,
        val platform: String,
    )

    private data class StartLoginRequest(
        val username: String,
        val clientId: String,
        val randomString: String,
        val checkColor: Int,
        val returnUrl: String,
    )

    private data class AuthorizeRequest(
        val signedHash: String,
        val publicCertThumbprint: String,
        val sessionId: String,
    )

    private data class RegisteredAccount(
        val accountGuid: String,
        val thumbprint: String,
    )

    private data class ActiveSessionSnapshot(
        val sessionId: String,
        val accountGuid: String,
    )

    private fun waitForEmailCode(targetEmail: String, timeoutSeconds: Int = 30): String {
        val apiUrl = "http://localhost:$mailhogHttpPort/api/v2/messages"
        val deadline = System.currentTimeMillis() + timeoutSeconds * 1000L
        var lastResponse = ""
        while (System.currentTimeMillis() < deadline) {
            val json = (URL(apiUrl).openConnection() as HttpURLConnection)
                .inputStream.use { InputStreamReader(it).readText() }
            lastResponse = json

            val parsed = runCatching { JsonParser.parseString(json).asJsonObject }.getOrNull()
            val items = parsed?.getAsJsonArray("items")
            if (items != null) {
                for (itemElement in items) {
                    val item = itemElement.asJsonObject
                    val content = item.getAsJsonObject("Content") ?: continue
                    val headers = content.getAsJsonObject("Headers")
                    val toArray = headers?.getAsJsonArray("To")
                    val matchesRecipient = toArray
                        ?.mapNotNull { header -> header.takeUnless { it.isJsonNull }?.asString }
                        ?.any { header -> header.contains(targetEmail, ignoreCase = true) }
                        ?: false

                    if (!matchesRecipient) {
                        continue
                    }

                    val subject = headers
                        ?.getAsJsonArray("Subject")
                        ?.firstOrNull()
                        ?.takeUnless { it.isJsonNull }
                        ?.asString
                        .orEmpty()
                    val subjectMatch = Regex("""Passi code (\d+)""").find(subject)
                    if (subjectMatch != null) {
                        return subjectMatch.groupValues[1]
                    }

                    val body = content.get("Body")?.takeUnless { it.isJsonNull }?.asString.orEmpty()
                    val bodyMatch = Regex("""(?:<b>|\\u003cb\\u003e)(\d+)(?:</b>|\\u003c/b\\u003e)""").find(body)
                    if (bodyMatch != null) {
                        return bodyMatch.groupValues[1]
                    }
                }
            }

            Thread.sleep(500)
        }

        val apiLogs = try {
            docker("logs", passiApiName)
        } catch (_: Exception) {
            ""
        }
        val mailhogLogs = try {
            docker("logs", mailhogName)
        } catch (_: Exception) {
            ""
        }
        throw AssertionError(
            "No confirmation code email for '$targetEmail' received within ${timeoutSeconds}s\n" +
                "MailHog response: $lastResponse\n" +
                "PassiApi logs:\n$apiLogs\n" +
                "MailHog logs:\n$mailhogLogs"
        )
    }

    @Test
    fun fullRegistrationFlow() = runBlocking {
        val email = "test-${UUID.randomUUID()}@example.com"
        val deviceId = UUID.randomUUID().toString()
        val pin = "1234"
        val enrollmentService = BackendEnrollmentService(apiClient)

        println("[TEST] fullRegistrationFlow: email=$email deviceId=$deviceId")

        println("[TEST] Step 1: beginSignup")
        val beginResult = enrollmentService.beginSignup(email, provider, deviceId)
        println("[TEST] beginSignup result: isSuccess=${beginResult.isSuccess} value=${beginResult.getOrNull()} error=${beginResult.exceptionOrNull()?.message}")
        assertThat(beginResult.isSuccess).isTrue()
        val pending = beginResult.getOrThrow()

        println("[TEST] Step 2: waitForEmailCode")
        val code = waitForEmailCode(email)
        println("[TEST] Got code: $code")

        println("[TEST] Step 3: confirmSignupCode")
        val confirmResult = enrollmentService.confirmSignupCode(
            accountId = pending.accountId,
            email = email,
            code = code,
            provider = provider,
        )
        println("[TEST] confirmSignupCode result: isSuccess=${confirmResult.isSuccess} error=${confirmResult.exceptionOrNull()?.message}")
        assertThat(confirmResult.isSuccess).isTrue()

        println("[TEST] Step 4: finalizeSignup")
        val confirmedPending = pending.copy(confirmationCode = code)
        val cert = BouncyCastleCertificateGenerator().generate(email, pin)
        val finalizeResult = enrollmentService.finalizeSignup(
            pendingEnrollment = confirmedPending,
            provider = provider,
            deviceId = deviceId,
            generatedCertificate = cert,
        )
        println("[TEST] finalizeSignup result: isSuccess=${finalizeResult.isSuccess} error=${finalizeResult.exceptionOrNull()?.message}")
        assertThat(finalizeResult.isSuccess).isTrue()
        println("[TEST] fullRegistrationFlow DONE")
    }

    @Test
    fun registrationFailsWithWrongConfirmationCode() = runBlocking {
        val email = "test-${UUID.randomUUID()}@example.com"
        val deviceId = UUID.randomUUID().toString()
        val enrollmentService = BackendEnrollmentService(apiClient)

        println("[TEST] registrationFailsWithWrongConfirmationCode: email=$email")

        println("[TEST] Step 1: beginSignup")
        val beginResult = enrollmentService.beginSignup(email, provider, deviceId)
        println("[TEST] beginSignup result: isSuccess=${beginResult.isSuccess} error=${beginResult.exceptionOrNull()?.message}")
        assertThat(beginResult.isSuccess).isTrue()
        val pending = beginResult.getOrThrow()

        println("[TEST] Step 2: confirmSignupCode with wrong code 000000")
        val confirmResult = enrollmentService.confirmSignupCode(
            accountId = pending.accountId,
            email = email,
            code = "000000",
            provider = provider,
        )
        println("[TEST] confirmSignupCode result: isFailure=${confirmResult.isFailure} error=${confirmResult.exceptionOrNull()?.message}")
        assertThat(confirmResult.isFailure).isTrue()
        println("[TEST] registrationFailsWithWrongConfirmationCode DONE")
    }

    @Test
    fun registrationResendsConfirmationForExistingEmail() = runBlocking {
        val email = "duplicate-${UUID.randomUUID()}@example.com"
        val deviceId = UUID.randomUUID().toString()
        val pin = "1234"
        val enrollmentService = BackendEnrollmentService(apiClient)

        println("[TEST] registrationResendsConfirmationForExistingEmail: email=$email")

        println("[TEST] Step 1: first beginSignup")
        val firstBegin = enrollmentService.beginSignup(email, provider, deviceId)
        println("[TEST] firstBegin result: isSuccess=${firstBegin.isSuccess} error=${firstBegin.exceptionOrNull()?.message}")
        assertThat(firstBegin.isSuccess).isTrue()
        val pending = firstBegin.getOrThrow()

        println("[TEST] Step 2: waitForEmailCode (first)")
        val code = waitForEmailCode(email)
        println("[TEST] Got code: $code")

        println("[TEST] Step 3: confirmSignupCode")
        val confirmResult = enrollmentService.confirmSignupCode(pending.accountId, email, code, provider)
        println("[TEST] confirmSignupCode result: isSuccess=${confirmResult.isSuccess} error=${confirmResult.exceptionOrNull()?.message}")
        assertThat(confirmResult.isSuccess).isTrue()

        println("[TEST] Step 4: finalizeSignup")
        val cert = BouncyCastleCertificateGenerator().generate(email, pin)
        val finalizeResult = enrollmentService.finalizeSignup(
            pending.copy(confirmationCode = code),
            provider,
            deviceId,
            cert,
        )
        println("[TEST] finalizeSignup result: isSuccess=${finalizeResult.isSuccess} error=${finalizeResult.exceptionOrNull()?.message}")
        assertThat(finalizeResult.isSuccess).isTrue()

        println("[TEST] Step 5: clearMailhog")
        clearMailhog()

        println("[TEST] Step 6: second beginSignup (duplicate email)")
        val secondBegin = enrollmentService.beginSignup(email, provider, UUID.randomUUID().toString())
        println("[TEST] secondBegin result: isSuccess=${secondBegin.isSuccess} error=${secondBegin.exceptionOrNull()?.message}")
        assertThat(secondBegin.isSuccess).isTrue()

        println("[TEST] Step 7: waitForEmailCode (resent)")
        val resentCode = waitForEmailCode(email)
        println("[TEST] Got resent code: $resentCode")
        assertThat(resentCode).isNotEmpty()
        println("[TEST] registrationResendsConfirmationForExistingEmail DONE")
    }

    @Test
    fun sameEmailOnAnotherDevice_keepsFirstDeviceAccountSynced() = runBlocking {
        val email = "multidevice-${UUID.randomUUID()}@example.com"
        val pin = "1234"
        val deviceA = UUID.randomUUID().toString()
        val deviceB = UUID.randomUUID().toString()
        val enrollmentService = BackendEnrollmentService(apiClient)

        println("[TEST] sameEmailOnAnotherDevice_keepsFirstDeviceAccountSynced: email=$email")

        println("[TEST] Step 1: register device A")
        val registrationA = registerAndFinalizeAccount(
            enrollmentService = enrollmentService,
            email = email,
            deviceId = deviceA,
            pin = pin,
        )

        println("[TEST] Step 2: verify device A sync includes account A")
        val initialSyncedA = syncAccountGuids(deviceA, listOf(registrationA.accountGuid))
        println("[TEST] Device A initial synced guids: $initialSyncedA")
        assertThat(initialSyncedA).contains(registrationA.accountGuid)

        println("[TEST] Step 3: clear inbox and register same email on device B")
        clearMailhog()
        val registrationB = registerAndFinalizeAccount(
            enrollmentService = enrollmentService,
            email = email,
            deviceId = deviceB,
            pin = pin,
        )
        println("[TEST] Device B registered guid: ${registrationB.accountGuid}")

        println("[TEST] Step 4: verify both devices stay active (not REMOVED)")
        val syncedAfterSecondRegistrationA = syncAccountGuids(deviceA, listOf(registrationA.accountGuid, registrationB.accountGuid))
        println("[TEST] Device A synced guids after device B registration: $syncedAfterSecondRegistrationA")
        assertThat(syncedAfterSecondRegistrationA).isNotEmpty()
        assertThat(syncedAfterSecondRegistrationA).contains(registrationA.accountGuid)
        val syncedAfterSecondRegistrationB = syncAccountGuids(deviceB, listOf(registrationA.accountGuid, registrationB.accountGuid))
        println("[TEST] Device B synced guids after registration: $syncedAfterSecondRegistrationB")
        assertThat(syncedAfterSecondRegistrationB).isNotEmpty()
        assertThat(syncedAfterSecondRegistrationB).contains(registrationA.accountGuid)

        println("[TEST] Step 5: register push tokens for both devices")
        val tokenA = "token-A-${UUID.randomUUID()}"
        val tokenB = "token-B-${UUID.randomUUID()}"
        updateDeviceToken(deviceA, tokenA)
        updateDeviceToken(deviceB, tokenB)

        println("[TEST] Step 6: initiate login request")
        clearMailhog()
        val startedSessionId = startLoginSession(email)
        println("[TEST] Started session id: $startedSessionId")

        println("[TEST] Step 7: verify both devices receive notifications")
        waitForNotificationEmails(expectedCount = 2, timeoutSeconds = 30)

        println("[TEST] Step 8: verify started session is visible from both devices")
        val activeA = waitForActiveSession(deviceA, expectedSessionId = startedSessionId)
        val activeB = waitForActiveSession(deviceB, expectedSessionId = startedSessionId)
        println("[TEST] Device A active session: $activeA")
        println("[TEST] Device B active session: $activeB")
        assertThat(activeA.sessionId).isEqualTo(startedSessionId)
        assertThat(activeB.sessionId).isEqualTo(startedSessionId)

        println("[TEST] Step 9: authorize from device A and verify check returns pass")
        authorizeSession(activeA.sessionId, registrationA.thumbprint)
        val checkStatus = checkLoginSessionStatus(activeA.sessionId)
        println("[TEST] /api/Auth/check status: $checkStatus")
        assertThat(checkStatus).isEqualTo(200)

        println("[TEST] sameEmailOnAnotherDevice_keepsFirstDeviceAccountSynced DONE")
    }

    private suspend fun registerAndFinalizeAccount(
        enrollmentService: BackendEnrollmentService,
        email: String,
        deviceId: String,
        pin: String,
    ): RegisteredAccount {
        val beginResult = enrollmentService.beginSignup(email, provider, deviceId)
        println("[TEST] beginSignup($deviceId): isSuccess=${beginResult.isSuccess} error=${beginResult.exceptionOrNull()?.message}")
        assertThat(beginResult.isSuccess).isTrue()
        val pending = beginResult.getOrThrow()

        val code = waitForEmailCode(email)
        println("[TEST] confirmation code for $deviceId: $code")

        val confirmResult = enrollmentService.confirmSignupCode(
            accountId = pending.accountId,
            email = email,
            code = code,
            provider = provider,
        )
        println("[TEST] confirmSignupCode($deviceId): isSuccess=${confirmResult.isSuccess} error=${confirmResult.exceptionOrNull()?.message}")
        assertThat(confirmResult.isSuccess).isTrue()

        val cert = BouncyCastleCertificateGenerator().generate(email, pin)
        val finalizeResult = enrollmentService.finalizeSignup(
            pendingEnrollment = pending.copy(confirmationCode = code),
            provider = provider,
            deviceId = deviceId,
            generatedCertificate = cert,
        )
        println("[TEST] finalizeSignup($deviceId): isSuccess=${finalizeResult.isSuccess} error=${finalizeResult.exceptionOrNull()?.message}")
        assertThat(finalizeResult.isSuccess).isTrue()

        return RegisteredAccount(
            accountGuid = pending.accountId,
            thumbprint = cert.thumbprint,
        )
    }

    private suspend fun syncAccountGuids(deviceId: String, guids: List<String>): List<String> {
        val response = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.syncAccounts,
            payload = SyncAccountsRequest(deviceId = deviceId, guids = guids),
        )
        println("[TEST] syncAccountGuids($deviceId): status=${response.statusCode} body=${response.body}")
        assertThat(response.isSuccessful).isTrue()

        return JsonParser.parseString(response.body)
            .asJsonArray
            .mapNotNull { element ->
                val obj = element.asJsonObject
                obj.get("userGuid")?.takeUnless { it.isJsonNull }?.asString
            }
    }

    private suspend fun updateDeviceToken(deviceId: String, token: String) {
        val response = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.tokenUpdate,
            payload = DeviceTokenUpdateRequest(
                deviceId = deviceId,
                token = token,
                platform = "android",
            ),
        )
        println("[TEST] updateDeviceToken($deviceId): status=${response.statusCode}")
        assertThat(response.isSuccessful).isTrue()
    }

    private suspend fun startLoginSession(email: String): String {
        val response = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = "/api/Auth/Start",
            payload = StartLoginRequest(
                username = email,
                clientId = "e2e-client",
                randomString = UUID.randomUUID().toString().replace("-", ""),
                checkColor = 1,
                returnUrl = "https://example.test/callback",
            ),
        )
        println("[TEST] startLoginSession: status=${response.statusCode} body=${response.body}")
        assertThat(response.isSuccessful).isTrue()

        val payload = JsonParser.parseString(response.body).asJsonObject
        return payload.readString("sessionId", "SessionId")
            ?: throw IllegalStateException("SessionId missing in Start response: ${response.body}")
    }

    private suspend fun waitForActiveSession(deviceId: String, expectedSessionId: String, timeoutSeconds: Int = 30): ActiveSessionSnapshot {
        val deadline = System.currentTimeMillis() + timeoutSeconds * 1000L
        var lastBody = ""
        while (System.currentTimeMillis() < deadline) {
            val response = apiClient.postJson(
                baseUrl = provider.baseUrl,
                path = provider.apiPaths.checkForStartedSessions,
                payload = GetAllSessionRequest(deviceId = deviceId),
            )
            if (response.isSuccessful && response.body.isNotBlank() && response.body != "null") {
                lastBody = response.body
                val obj = JsonParser.parseString(response.body).asJsonObject
                val sessionId = obj.readString("sessionId", "SessionId")
                val accountGuid = obj.readString("accountGuid", "AccountGuid")
                if (sessionId == expectedSessionId && accountGuid != null) {
                    return ActiveSessionSnapshot(sessionId = sessionId, accountGuid = accountGuid)
                }
            }
            Thread.sleep(500)
        }
        throw AssertionError("No active session for device $deviceId within ${timeoutSeconds}s. lastBody=$lastBody")
    }

    private suspend fun authorizeSession(sessionId: String, thumbprint: String) {
        val response = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.authorize,
            payload = AuthorizeRequest(
                signedHash = "e2e-signed-${UUID.randomUUID()}",
                publicCertThumbprint = thumbprint,
                sessionId = sessionId,
            ),
        )
        println("[TEST] authorizeSession: status=${response.statusCode} body=${response.body}")
        assertThat(response.isSuccessful).isTrue()
    }

    private fun checkLoginSessionStatus(sessionId: String): Int {
        val url = URL("${provider.baseUrl}/api/Auth/check?sessionId=$sessionId")
        val connection = (url.openConnection() as HttpURLConnection).apply {
            requestMethod = "GET"
            connectTimeout = 15_000
            readTimeout = 15_000
        }
        return try {
            connection.responseCode
        } finally {
            connection.disconnect()
        }
    }

    private fun waitForNotificationEmails(expectedCount: Int, timeoutSeconds: Int = 30) {
        val apiUrl = "http://localhost:$mailhogHttpPort/api/v2/messages"
        val deadline = System.currentTimeMillis() + timeoutSeconds * 1000L
        var lastResponse = ""
        while (System.currentTimeMillis() < deadline) {
            val json = (URL(apiUrl).openConnection() as HttpURLConnection)
                .inputStream.use { InputStreamReader(it).readText() }
            lastResponse = json
            val count = Regex(""""Subject":\["Passi notification [^"]+"\]""")
                .findAll(json)
                .count()
            if (count >= expectedCount) {
                println("[TEST] Notification email count: $count")
                return
            }
            Thread.sleep(500)
        }
        throw AssertionError("Expected at least $expectedCount notification emails within ${timeoutSeconds}s. Mailhog=$lastResponse")
    }

    private fun com.google.gson.JsonObject.readString(vararg keys: String): String? {
        for (key in keys) {
            val value = this.get(key)
            if (value != null && !value.isJsonNull) {
                return value.asString
            }
        }
        return null
    }
}
