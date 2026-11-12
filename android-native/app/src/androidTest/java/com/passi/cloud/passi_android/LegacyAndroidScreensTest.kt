package com.passi.cloud.passi_android

import android.content.Context
import androidx.compose.material3.MaterialTheme
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithContentDescription
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.test.core.app.ApplicationProvider
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.feature.accounts.AccountsScreen
import com.passi.cloud.passi_android.feature.accounts.AccountsUiState
import com.passi.cloud.passi_android.feature.certificate.UpdateCertificateScreen
import com.passi.cloud.passi_android.feature.certificate.UpdateCertificateUiState
import com.passi.cloud.passi_android.feature.enrollment.AddAccountScreen
import com.passi.cloud.passi_android.feature.enrollment.AddAccountUiState
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import java.time.Instant
import java.util.UUID

class LegacyAndroidScreensTest {
    @get:Rule val compose = createComposeRule()
    private val context: Context get() = ApplicationProvider.getApplicationContext()
    private val account = Account(UUID.randomUUID(), null, "legacy@example.com", "device", true,
        validFrom = Instant.EPOCH, validTo = Instant.MAX, privateCertBinary = "legacy-pkcs12")

    @Test fun unsupportedPlatformHidesActionableMigrationNoticeAndDisablesAdd() {
        compose.setContent {
            MaterialTheme {
                AccountsScreen(AccountsUiState(accounts = listOf(account), deviceKeySupported = false), "test",
                    onAddAccount = { error("Unsupported registration") }, onOpenProviders = {},
                    onToggleDeleteMode = {}, onSync = {}, onOpenAccount = {},
                    onUpdateCertificate = { error("Unsupported migration") }, onRevealDelete = {}, onDelete = {})
            }
        }
        compose.onNodeWithText(context.getString(R.string.device_key_android_version_required)).assertExists()
        compose.onNodeWithTag("certificate-update-notice").assertDoesNotExist()
        compose.onNodeWithTag("certificate-update-action").assertDoesNotExist()
        compose.onNodeWithContentDescription(context.getString(R.string.add)).assertIsNotEnabled()
    }

    @Test fun unsupportedRegistrationScreenOffersOnlyExplanationAndExit() {
        var exited = false
        compose.setContent {
            MaterialTheme {
                AddAccountScreen(AddAccountUiState(deviceKeySupported = false), {}, {},
                    onSubmit = { error("Unsupported registration") }, onBack = { exited = true })
            }
        }
        compose.onNodeWithText(context.getString(R.string.device_key_android_version_required)).assertExists()
        compose.onNodeWithText(context.getString(R.string.register)).assertDoesNotExist()
        compose.onNodeWithText(context.getString(R.string.cancel)).performClick()
        assertTrue(exited)
    }

    @Test fun unsupportedMigrationScreenDoesNotOfferPinOrUpdate() {
        compose.setContent {
            MaterialTheme {
                UpdateCertificateScreen(UpdateCertificateUiState(account = account, deviceKeySupported = false),
                    {}, {}, {}, {}, onRotate = { error("Unsupported migration") }, onDone = {}, onCancel = {})
            }
        }
        compose.onNodeWithText(context.getString(R.string.device_key_android_version_required)).assertExists()
        compose.onNodeWithText(context.getString(R.string.update_certificate)).assertDoesNotExist()
        compose.onNodeWithText(context.getString(R.string.old_pin)).assertDoesNotExist()
    }
}
