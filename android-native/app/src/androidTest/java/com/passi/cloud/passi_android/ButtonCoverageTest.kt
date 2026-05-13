package com.passi.cloud.passi_android

import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithContentDescription
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performTextInput
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.ApiPaths
import com.passi.cloud.passi_android.domain.model.ConfirmationColor
import com.passi.cloud.passi_android.domain.model.ManagedDevice
import com.passi.cloud.passi_android.domain.model.NotificationSession
import com.passi.cloud.passi_android.domain.model.Provider
import com.passi.cloud.passi_android.feature.account.AccountDetailScreen
import com.passi.cloud.passi_android.feature.account.AccountDetailUiState
import com.passi.cloud.passi_android.feature.account.AccountDevicesScreen
import com.passi.cloud.passi_android.feature.account.AccountDevicesUiState
import com.passi.cloud.passi_android.feature.accounts.AccountsScreen
import com.passi.cloud.passi_android.feature.accounts.AccountsUiState
import com.passi.cloud.passi_android.feature.auth.SessionChallengeScreen
import com.passi.cloud.passi_android.feature.auth.SessionChallengeUiState
import com.passi.cloud.passi_android.feature.auth.SessionPinScreen
import com.passi.cloud.passi_android.feature.auth.SessionPinUiState
import com.passi.cloud.passi_android.feature.certificate.UpdateCertificateScreen
import com.passi.cloud.passi_android.feature.certificate.UpdateCertificateUiState
import com.passi.cloud.passi_android.feature.enrollment.AddAccountScreen
import com.passi.cloud.passi_android.feature.enrollment.AddAccountUiState
import com.passi.cloud.passi_android.feature.enrollment.ConfirmCodeScreen
import com.passi.cloud.passi_android.feature.enrollment.ConfirmCodeUiState
import com.passi.cloud.passi_android.feature.enrollment.FinishEnrollmentScreen
import com.passi.cloud.passi_android.feature.enrollment.FinishEnrollmentUiState
import com.passi.cloud.passi_android.feature.enrollment.TermsRoute
import java.time.Instant
import java.time.temporal.ChronoUnit
import java.util.UUID
import org.junit.Rule
import org.junit.Test

class ButtonCoverageTest {
    @get:Rule
    val composeRule = createComposeRule()

    @Test
    fun termsButtonsTriggerCallbacks() {
        var agreed = 0
        var cancelled = 0

        composeRule.setPassiContent {
            TermsRoute(
                onAgree = { agreed++ },
                onCancel = { cancelled++ },
            )
        }

        composeRule.onNodeWithText("I agree").performClick()
        composeRule.onNodeWithText("I don't agree").performClick()

        assert(agreed == 1)
        assert(cancelled == 1)
    }

    @Test
    fun addAccountButtonsTriggerRegisterAndCancel() {
        var submitted = 0
        var cancelled = 0
        val provider = sampleProvider()

        composeRule.setPassiContent {
            var state by remember {
                mutableStateOf(
                    AddAccountUiState(
                        providers = listOf(provider),
                        selectedProviderId = provider.id.toString(),
                    )
                )
            }

            AddAccountScreen(
                uiState = state,
                onEmailChanged = { state = state.copy(email = it) },
                onProviderSelected = { state = state.copy(selectedProviderId = it) },
                onSubmit = { submitted++ },
                onBack = { cancelled++ },
            )
        }

        composeRule.onNodeWithText("Register").performClick()
        composeRule.onNodeWithText("Cancel").performClick()

        assert(submitted == 1)
        assert(cancelled == 1)
    }

    @Test
    fun confirmCodeButtonsUpdateCodeAndCancel() {
        var cancelled = 0
        var submitted = 0

        composeRule.setPassiContent {
            var state by remember { mutableStateOf(ConfirmCodeUiState(email = "user@example.com")) }

            ConfirmCodeScreen(
                uiState = state,
                onCodeChanged = { state = state.copy(code = it.take(6)) },
                onSubmit = { submitted++ },
                onCancel = { cancelled++ },
            )
        }

        composeRule.onNodeWithText("1").performClick()
        composeRule.onNodeWithText("2").performClick()
        composeRule.onNodeWithText("→").performClick()
        composeRule.onNodeWithText("12").assertExists()
        composeRule.onNodeWithText("Cancel").performClick()

        assert(submitted == 1)
        assert(cancelled == 1)
    }

    @Test
    fun finishEnrollmentButtonsHandlePinEntryClearAndSkip() {
        var skipped = 0
        var submitted = 0

        composeRule.setPassiContent {
            var state by remember { mutableStateOf(FinishEnrollmentUiState(email = "user@example.com")) }

            FinishEnrollmentScreen(
                uiState = state,
                onPinChanged = { state = state.copy(pin = it.take(12)) },
                onPinConfirmationChanged = { state = state.copy(pinConfirmation = it.take(12)) },
                onSkip = { skipped++ },
                onSubmit = { submitted++ },
            )
        }

        composeRule.onNodeWithText("1").performClick()
        composeRule.onNodeWithText("→").performClick()
        composeRule.onNodeWithText("2").performClick()
        composeRule.onAllNodesWithText("X")[1].performClick()
        composeRule.onNodeWithText("Skip").performClick()

        assert(skipped == 1)
        assert(submitted == 0)
    }

    @Test
    fun sessionPinButtonsHandleEntryClearSubmitAndCancel() {
        var submitted = 0
        var cancelled = 0

        composeRule.setPassiContent {
            var state by remember { mutableStateOf(SessionPinUiState(email = "user@example.com")) }

            SessionPinScreen(
                uiState = state,
                onPinChanged = { state = state.copy(pin = it.take(12)) },
                onSubmit = { submitted++ },
                onCancel = { cancelled++ },
            )
        }

        composeRule.onNodeWithText("1").performClick()
        composeRule.onAllNodesWithText("X")[0].performClick()
        composeRule.onNodeWithText("4").performClick()
        composeRule.onNodeWithText("→").performClick()
        composeRule.onNodeWithText("Cancel").performClick()

        assert(submitted == 1)
        assert(cancelled == 1)
    }

    @Test
    fun sessionChallengeButtonsTriggerAllColorButtonsAndCancel() {
        val clickedColors = mutableListOf<ConfirmationColor>()
        var cancelled = 0

        composeRule.setPassiContent {
            SessionChallengeScreen(
                uiState = SessionChallengeUiState(
                    session = sampleSession(),
                    account = sampleAccount(),
                    colorOptions = ConfirmationColor.entries,
                    isButtonEnabled = true,
                ),
                timeLeftSeconds = 25,
                onColorSelected = { clickedColors += it },
                onCancel = { cancelled++ },
            )
        }

        composeRule.onNodeWithTag("session-color-blue").performClick()
        composeRule.onNodeWithTag("session-color-green").performClick()
        composeRule.onNodeWithTag("session-color-red").performClick()
        composeRule.onNodeWithTag("session-color-yellow").performClick()
        composeRule.onNodeWithText("Cancel").performClick()

        assert(clickedColors == ConfirmationColor.entries)
        assert(cancelled == 1)
    }

    @Test
    fun updateCertificateButtonsTriggerBiometricRotateClearAndCancel() {
        var biometric = 0
        var rotated = 0
        var cancelled = 0

        composeRule.setPassiContent {
            var state by remember {
                mutableStateOf(
                    UpdateCertificateUiState(
                        account = sampleAccount(pinLength = 4, hasFingerprint = true),
                    )
                )
            }

            UpdateCertificateScreen(
                uiState = state,
                onOldPinChanged = { state = state.copy(oldPin = it) },
                onNewPinChanged = { state = state.copy(newPin = it) },
                onConfirmPinChanged = { state = state.copy(confirmPin = it) },
                onLaunchBiometric = { biometric++ },
                onRotate = { rotated++ },
                onCancel = { cancelled++ },
            )
        }

        composeRule.onNodeWithText("Use biometric for current certificate").performClick()
        composeRule.onNodeWithText("1").performClick()
        composeRule.onNodeWithText("→").performClick()
        composeRule.onNodeWithText("2").performClick()
        composeRule.onNodeWithText("→").performClick()
        composeRule.onNodeWithText("3").performClick()
        composeRule.onAllNodesWithText("X")[2].performClick()
        composeRule.onNodeWithText("Cancel").performClick()

        assert(biometric == 1)
        assert(rotated == 0)
        assert(cancelled == 1)
    }

    @Test
    fun accountDetailButtonsTriggerActionsAndDeleteSheetButtons() {
        var updated = 0
        var managed = 0
        var backed = 0
        var biometric = 0
        var confirmedDelete = 0
        var dismissedDelete = 0

        composeRule.setPassiContent {
            var showDeleteConfirmation by remember { mutableStateOf(false) }
            var state by remember {
                mutableStateOf(
                    AccountDetailUiState(
                        account = sampleAccount(pinLength = 4, hasFingerprint = false),
                        provider = sampleProvider(),
                    )
                )
            }

            AccountDetailScreen(
                uiState = state,
                showDeleteConfirmation = showDeleteConfirmation,
                onBiometricPinChanged = { state = state.copy(biometricPin = it) },
                onLaunchBiometric = { biometric++ },
                onUpdateCertificate = { updated++ },
                onManageDevices = { managed++ },
                onBack = { backed++ },
                onShowDeleteConfirmation = { showDeleteConfirmation = true },
                onDeleteConfirmed = {
                    showDeleteConfirmation = false
                    confirmedDelete++
                },
                onDismissDeleteConfirmation = {
                    showDeleteConfirmation = false
                    dismissedDelete++
                },
            )
        }

        composeRule.onNodeWithContentDescription("Back").performClick()
        composeRule.onNodeWithText("Update Certificate").performClick()
        composeRule.onNodeWithText("Add Fingerprint").performClick()
        composeRule.onNodeWithText("Manage devices").performClick()
        composeRule.onNodeWithText("Delete account").performClick()
        composeRule.onNodeWithText("Cancel").performClick()
        composeRule.onNodeWithText("Delete account").performClick()
        composeRule.onNodeWithText("Delete Account").performClick()

        assert(backed == 1)
        assert(updated == 1)
        assert(biometric == 1)
        assert(managed == 1)
        assert(dismissedDelete == 1)
        assert(confirmedDelete == 1)
    }

    @Test
    fun accountDevicesButtonsTriggerBackAndRemovalSheetButtons() {
        var backed = 0
        var removedDeviceId: String? = null
        var dismissed = 0

        composeRule.setPassiContent {
            var pendingDevice by remember { mutableStateOf<ManagedDevice?>(null) }

            AccountDevicesScreen(
                uiState = AccountDevicesUiState(
                    account = sampleAccount(),
                    devices = listOf(sampleManagedDevice(isCurrent = false)),
                    isLoading = false,
                ),
                pendingDevice = pendingDevice,
                onBack = { backed++ },
                onRequestRemove = { pendingDevice = it },
                onConfirmRemove = {
                    removedDeviceId = it.deviceId
                    pendingDevice = null
                },
                onDismissRemove = {
                    dismissed++
                    pendingDevice = null
                },
            )
        }

        composeRule.onNodeWithContentDescription("Back").performClick()
        composeRule.onNodeWithText("Remove device").performClick()
        composeRule.onNodeWithText("Cancel").performClick()
        composeRule.onNodeWithText("Remove device").performClick()
        composeRule.onNodeWithText("Remove Device").performClick()

        assert(backed == 1)
        assert(dismissed == 1)
        assert(removedDeviceId == "device-b")
    }

    @Test
    fun accountsButtonsTriggerAddDeleteSyncAndPendingEntryWithoutProviderMenu() {
        var added = 0
        var toggledDelete = 0
        var synced = 0
        var opened: UUID? = null

        composeRule.setPassiContent {
            AccountsScreen(
                uiState = AccountsUiState(
                    providers = listOf(sampleProvider()),
                    accounts = listOf(sampleAccount(isConfirmed = false)),
                ),
                versionLabel = "0.1.0",
                onAddAccount = { added++ },
                onOpenProviders = {},
                onToggleDeleteMode = { toggledDelete++ },
                onSync = { synced++ },
                onOpenAccount = { opened = it.id },
                onRevealDelete = {},
                onDelete = {},
            )
        }

        composeRule.onNodeWithContentDescription("Add").performClick()
        composeRule.onNodeWithContentDescription("Delete").performClick()
        composeRule.onNodeWithTag("accounts-sync-action").performClick()
        composeRule.onNodeWithText("Enter Code").performClick()

        assert(added == 1)
        assert(toggledDelete == 1)
        assert(synced == 1)
        assert(opened != null)
    }

    @Test
    fun accountsDeleteRowButtonsTriggerRevealAndDelete() {
        var revealed: UUID? = null
        var deleted: UUID? = null
        val account = sampleAccount()

        composeRule.setPassiContent {
            AccountsScreen(
                uiState = AccountsUiState(
                    providers = listOf(sampleProvider()),
                    accounts = listOf(account),
                    isDeleteMode = true,
                    revealedDeleteAccountIds = setOf(account.id),
                ),
                versionLabel = "0.1.0",
                onAddAccount = {},
                onOpenProviders = {},
                onToggleDeleteMode = {},
                onSync = {},
                onOpenAccount = {},
                onRevealDelete = { revealed = it },
                onDelete = { deleted = it.id },
            )
        }

        composeRule.onNodeWithContentDescription("Reveal delete").performClick()
        composeRule.onNodeWithText("Delete").performClick()

        assert(revealed == account.id)
        assert(deleted == account.id)
    }

    private fun androidx.compose.ui.test.junit4.ComposeContentTestRule.setPassiContent(content: @androidx.compose.runtime.Composable () -> Unit) {
        setContent {
            MaterialTheme {
                content()
            }
        }
    }

    private fun sampleProvider(): Provider = Provider(
        id = UUID.fromString("11111111-1111-1111-1111-111111111111"),
        name = "Default Provider",
        baseUrl = "https://passi.test",
        apiPaths = ApiPaths.defaultPaths(),
        isDefault = true,
    )

    private fun sampleAccount(
        isConfirmed: Boolean = true,
        pinLength: Int = 0,
        hasFingerprint: Boolean = false,
    ): Account = Account(
        id = UUID.fromString("22222222-2222-2222-2222-222222222222"),
        providerId = sampleProvider().id,
        email = "user@example.com",
        deviceId = "device-a",
        isConfirmed = isConfirmed,
        thumbprint = "thumbprint",
        validFrom = Instant.parse("2026-05-08T10:00:00Z"),
        validTo = Instant.parse("2027-05-08T10:00:00Z"),
        pinLength = pinLength,
        hasFingerprint = hasFingerprint,
    )

    private fun sampleSession(): NotificationSession = NotificationSession(
        sender = "Passi Test",
        confirmationColor = ConfirmationColor.BLUE,
        sessionId = UUID.fromString("33333333-3333-3333-3333-333333333333"),
        expirationTime = Instant.now().plus(5, ChronoUnit.MINUTES),
        randomString = "random-string",
        returnHost = "example.test",
        accountId = sampleAccount().id,
    )

    private fun sampleManagedDevice(isCurrent: Boolean): ManagedDevice = ManagedDevice(
        deviceId = if (isCurrent) "device-a" else "device-b",
        platform = if (isCurrent) "Android" else "iPhone",
        creationTime = Instant.parse("2026-05-08T10:00:00Z"),
        isCurrent = isCurrent,
    )
}