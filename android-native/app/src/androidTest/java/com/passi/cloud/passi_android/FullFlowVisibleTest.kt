package com.passi.cloud.passi_android

import androidx.compose.ui.test.SemanticsMatcher
import androidx.compose.ui.test.hasSetTextAction
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithContentDescription
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performTextClearance
import androidx.compose.ui.test.performTextInput
import androidx.test.platform.app.InstrumentationRegistry
import androidx.test.uiautomator.By
import androidx.test.uiautomator.UiDevice
import androidx.test.uiautomator.Until
import com.google.gson.JsonParser
import com.passi.cloud.passi_android.domain.model.ApiPaths
import kotlinx.coroutines.runBlocking
import org.junit.Rule
import org.junit.Test
import java.net.HttpURLConnection
import java.net.URL
import java.util.UUID

class FullFlowVisibleTest {
    @get:Rule
    val composeRule = createAndroidComposeRule<MainActivity>()

    @Test
    fun registrationAndAuthorization_fullVisibleFlow() {
        val baseUrl = instrumentationArg("BASE_URL", "http://10.0.2.2:5004/passiapi")
        val mailhogUrl = instrumentationArg("MAILHOG_URL", "http://10.0.2.2:8025")
        val email = "visible-flow-${UUID.randomUUID()}@example.com"

        updateDefaultProvider(baseUrl)
        allowNotificationsIfPrompted()

        waitForContentDescription("Add", "accounts screen")

        composeRule.onNodeWithContentDescription("Add").performClick()
        composeRule.onNodeWithText("I agree").performClick()

        enterIntoFirstEditableField(email)
        waitForText(email, "typed email", timeoutMs = 10_000L)
        composeRule.onNodeWithText("Register").performClick()

        // Confirm code screen.
        waitForText("Confirmation Code", "confirmation code screen", timeoutMs = 60_000L)
        val code = waitForEmailCode(mailhogUrl, email)
        tapDigits(code)
        composeRule.onNodeWithText("→").performClick()

        // Finish enrollment screen (PIN + confirmation).
        waitForText("Secure account by pin.", "finish enrollment screen", timeoutMs = 60_000L)
        tapDigits("1234")
        composeRule.onNodeWithText("→").performClick()
        tapDigits("1234")
        composeRule.onNodeWithText("→").performClick()

        waitForContentDescription("Add", "accounts screen after enrollment")
        composeRule.onNodeWithText(email).assertExists()

        // Create an auth request and approve it through the UI.
        val sessionId = startLoginSession(baseUrl, email)
        waitForTag("accounts-sync-action", "accounts sync action")
        composeRule.onNodeWithTag("accounts-sync-action", useUnmergedTree = true).performClick()
        waitForTag("session-color-blue", "session challenge screen")
        composeRule.onNodeWithTag("session-color-blue", useUnmergedTree = true).performClick()

        waitForText("Pin", "session pin screen")
        tapDigits("1234")
        composeRule.onNodeWithText("→").performClick()

        waitForSessionCheckPass(baseUrl, sessionId)
        waitForContentDescription("Add", "accounts screen after authorization")
    }

    private fun updateDefaultProvider(baseUrl: String) {
        val app = composeRule.activity.application as PassiApplication
        runBlocking {
            val repository = app.container.providersRepository
            val current = repository.getProviders().firstOrNull()
                ?: throw IllegalStateException("No providers configured")
            repository.saveProvider(
                current.copy(
                    baseUrl = baseUrl,
                    isDefault = true,
                    apiPaths = ApiPaths.defaultPaths(),
                )
            )
        }
    }

    private fun tapDigits(value: String) {
        value.forEach { digit ->
            composeRule.onAllNodesWithText(digit.toString())[0].performClick()
        }
    }

    private fun startLoginSession(baseUrl: String, email: String): String {
        val payload = """
            {
              "Username":"$email",
              "ClientId":"visible-e2e",
              "RandomString":"${UUID.randomUUID().toString().replace("-", "")}",
              "CheckColor":1,
              "ReturnUrl":"https://example.test/callback"
            }
        """.trimIndent()

        val response = postJson("$baseUrl/api/Auth/Start", payload)
        if (response.code !in 200..299) {
            throw AssertionError("Failed to start login session: ${response.code} ${response.body}")
        }

        val json = JsonParser.parseString(response.body).asJsonObject
        return json.get("sessionId")?.asString
            ?: json.get("SessionId")?.asString
            ?: throw AssertionError("SessionId missing in start response: ${response.body}")
    }

    private fun waitForSessionCheckPass(baseUrl: String, sessionId: String, timeoutMs: Long = 30_000L) {
        val deadline = System.currentTimeMillis() + timeoutMs
        while (System.currentTimeMillis() < deadline) {
            val conn = (URL("$baseUrl/api/Auth/check?sessionId=$sessionId").openConnection() as HttpURLConnection).apply {
                requestMethod = "GET"
                connectTimeout = 5_000
                readTimeout = 5_000
            }
            val code = conn.responseCode
            conn.disconnect()
            if (code == 200) {
                return
            }
            Thread.sleep(400)
        }
        throw AssertionError("Session check did not return 200 for sessionId=$sessionId")
    }

    private fun waitForEmailCode(mailhogBaseUrl: String, targetEmail: String, timeoutMs: Long = 60_000L): String {
        val deadline = System.currentTimeMillis() + timeoutMs
        var lastResponse = ""

        while (System.currentTimeMillis() < deadline) {
            val conn = (URL("$mailhogBaseUrl/api/v2/messages").openConnection() as HttpURLConnection).apply {
                requestMethod = "GET"
                connectTimeout = 5_000
                readTimeout = 5_000
            }
            val responseBody = conn.inputStream.bufferedReader().use { it.readText() }
            conn.disconnect()
            lastResponse = responseBody

            val root = runCatching { JsonParser.parseString(responseBody).asJsonObject }.getOrNull()
            val items = root?.getAsJsonArray("items")
            if (items != null) {
                for (item in items) {
                    val obj = item.asJsonObject
                    val content = obj.getAsJsonObject("Content") ?: continue
                    val headers = content.getAsJsonObject("Headers")
                    val to = headers?.getAsJsonArray("To")
                        ?.mapNotNull { el -> el.takeUnless { it.isJsonNull }?.asString }
                        .orEmpty()

                    if (to.none { it.contains(targetEmail, ignoreCase = true) }) {
                        continue
                    }

                    val subject = headers?.getAsJsonArray("Subject")
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

            Thread.sleep(400)
        }

        throw AssertionError("No confirmation code email for $targetEmail. Last MailHog response: $lastResponse")
    }

    private fun postJson(url: String, payload: String): HttpResult {
        val conn = (URL(url).openConnection() as HttpURLConnection).apply {
            requestMethod = "POST"
            doOutput = true
            connectTimeout = 8_000
            readTimeout = 8_000
            setRequestProperty("Content-Type", "application/json")
        }

        conn.outputStream.bufferedWriter().use { it.write(payload) }

        val stream = if (conn.responseCode in 200..299) conn.inputStream else conn.errorStream
        val body = stream?.bufferedReader()?.use { it.readText() }.orEmpty()
        val result = HttpResult(conn.responseCode, body)
        conn.disconnect()
        return result
    }

    private fun waitForNode(matcher: SemanticsMatcher, label: String, timeoutMs: Long = 20_000L) {
        composeRule.waitUntil(timeoutMs) {
            runCatching {
                composeRule.onNode(matcher, useUnmergedTree = true).assertExists()
            }.isSuccess
        }
        composeRule.onNode(matcher, useUnmergedTree = true).assertExists("Expected $label")
    }

    private fun enterIntoFirstEditableField(value: String, timeoutMs: Long = 20_000L) {
        val editableNodes = composeRule.onAllNodes(hasSetTextAction(), useUnmergedTree = true)
        composeRule.waitUntil(timeoutMs) {
            val hasEditable = runCatching { editableNodes.fetchSemanticsNodes().isNotEmpty() }.getOrDefault(false)
            if (!hasEditable) {
                clickAllowIfVisible()
            }
            hasEditable
        }

        val count = editableNodes.fetchSemanticsNodes().size
        for (index in (count - 1) downTo 0) {
            val typed = runCatching {
                editableNodes[index].performTextClearance()
                editableNodes[index].performTextInput(value)
            }.isSuccess
            if (typed) {
                return
            }
        }

        throw AssertionError("Could not enter text into any editable field")
    }

    private fun waitForText(text: String, label: String, timeoutMs: Long = 20_000L) {
        composeRule.waitUntil(timeoutMs) {
            runCatching {
                composeRule.onNodeWithText(text).assertExists()
            }.isSuccess
        }
        composeRule.onNodeWithText(text).assertExists("Expected $label")
    }

    private fun waitForTag(tag: String, label: String, timeoutMs: Long = 20_000L) {
        composeRule.waitUntil(timeoutMs) {
            val hasNode = runCatching {
                composeRule.onNodeWithTag(tag, useUnmergedTree = true).assertExists()
            }.isSuccess
            if (!hasNode) {
                clickAllowIfVisible()
            }
            hasNode
        }
        composeRule.onNodeWithTag(tag, useUnmergedTree = true).assertExists("Expected $label")
    }

    private fun waitForContentDescription(description: String, label: String, timeoutMs: Long = 20_000L) {
        composeRule.waitUntil(timeoutMs) {
            val hasNode = runCatching {
                composeRule.onNodeWithContentDescription(description).assertExists()
            }.isSuccess
            if (!hasNode) {
                clickAllowIfVisible()
            }
            hasNode
        }
        composeRule.onNodeWithContentDescription(description).assertExists("Expected $label")
    }

    private fun instrumentationArg(name: String, fallback: String): String {
        val args = InstrumentationRegistry.getArguments()
        return args.getString(name) ?: fallback
    }

    private fun allowNotificationsIfPrompted() {
        val deadline = System.currentTimeMillis() + 20_000
        while (System.currentTimeMillis() < deadline) {
            if (clickAllowIfVisible()) {
                return
            }

            Thread.sleep(300)
        }
    }

    private fun clickAllowIfVisible(): Boolean {
        val device = UiDevice.getInstance(InstrumentationRegistry.getInstrumentation())
        val allowButton =
            device.findObject(By.res("com.android.permissioncontroller:id/permission_allow_button"))
                ?: device.findObject(By.res("com.android.permissioncontroller:id/permission_allow_foreground_only_button"))
                ?: device.findObject(By.res("com.android.permissioncontroller:id/permission_allow_one_time_button"))
                ?: device.findObject(By.text("Allow"))
                ?: device.findObject(By.text("ALLOW"))
                ?: device.findObject(By.textContains("Allow"))
                ?: device.findObject(By.res("android:id/button1"))

        if (allowButton == null) {
            return false
        }

        allowButton.click()
        device.waitForIdle()
        return true
    }

    private data class HttpResult(
        val code: Int,
        val body: String,
    )
}
