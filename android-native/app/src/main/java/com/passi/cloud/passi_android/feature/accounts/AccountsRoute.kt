package com.passi.cloud.passi_android.feature.accounts

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import android.content.pm.PackageManager
import android.os.Build
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.List
import androidx.compose.material.icons.outlined.AddCircle
import androidx.compose.material.icons.outlined.Delete
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.repeatOnLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive

private const val FOREGROUND_POLL_INTERVAL_MS = 5_000L

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AccountsRoute(
    onAddAccount: () -> Unit,
    onOpenPendingSession: () -> Unit,
    onOpenAccount: () -> Unit,
    onOpenProviders: () -> Unit,
    onResumeEnrollment: () -> Unit,
    externalErrorMessage: String? = null,
    onExternalErrorShown: () -> Unit = {},
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: AccountsViewModel = viewModel(
        factory = AccountsViewModel.factory(
            accountsRepository = application.container.accountsRepository,
            providersRepository = application.container.providersRepository,
            authSessionService = application.container.authSessionService,
            pendingSessionStore = application.container.pendingSessionStore,
            pendingEnrollmentStore = application.container.pendingEnrollmentStore,
            selectedAccountStore = application.container.selectedAccountStore,
            accountManagementService = application.container.accountManagementService,
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val lifecycleOwner = LocalLifecycleOwner.current
    val notificationOpenRequest by application.container.notificationOpenStore.openRequests.collectAsStateWithLifecycle()
    val versionLabel = remember(application) {
        application.packageManager.readVersionName(application.packageName)
    }

    LaunchedEffect(notificationOpenRequest) {
        if (notificationOpenRequest > 0L) {
            viewModel.sync(onOpenPendingSession)
        }
    }

    LaunchedEffect(externalErrorMessage) {
        if (!externalErrorMessage.isNullOrBlank()) {
            viewModel.showMainError(externalErrorMessage)
            onExternalErrorShown()
        }
    }

    LaunchedEffect(lifecycleOwner, viewModel) {
        lifecycleOwner.repeatOnLifecycle(androidx.lifecycle.Lifecycle.State.STARTED) {
            viewModel.foregroundPoll(onOpenPendingSession)
            while (isActive) {
                delay(FOREGROUND_POLL_INTERVAL_MS)
                viewModel.foregroundPoll(onOpenPendingSession)
            }
        }
    }

    AccountsScreen(
        uiState = uiState,
        versionLabel = versionLabel,
        onAddAccount = onAddAccount,
        onOpenProviders = onOpenProviders,
        onToggleDeleteMode = viewModel::toggleDeleteMode,
        onSync = { viewModel.sync(onOpenPendingSession) },
        onOpenAccount = { account -> viewModel.openAccount(account, onOpenAccount, onResumeEnrollment) },
        onRevealDelete = viewModel::toggleDeleteForAccount,
        onDelete = viewModel::deleteAccount,
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
internal fun AccountsScreen(
    uiState: AccountsUiState,
    versionLabel: String,
    onAddAccount: () -> Unit,
    onOpenProviders: () -> Unit,
    onToggleDeleteMode: () -> Unit,
    onSync: () -> Unit,
    onOpenAccount: (com.passi.cloud.passi_android.domain.model.Account) -> Unit,
    onRevealDelete: (java.util.UUID) -> Unit,
    onDelete: (com.passi.cloud.passi_android.domain.model.Account) -> Unit,
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA)),
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(Color(0xFF2196F3))
                .statusBarsPadding()
                .padding(horizontal = 12.dp, vertical = 10.dp),
            horizontalArrangement = Arrangement.End,
        ) {
            IconButton(onClick = onOpenProviders) {
                Icon(
                    imageVector = Icons.AutoMirrored.Outlined.List,
                    contentDescription = "Menu",
                    tint = Color.Black,
                )
            }
        }

        LazyColumn(
            modifier = Modifier.weight(1f),
            contentPadding = PaddingValues(vertical = 8.dp),
            verticalArrangement = Arrangement.spacedBy(4.dp),
        ) {
            items(uiState.accounts, key = { it.id }) { account ->
                val providerName = uiState.providers.firstOrNull { it.id == account.providerId }?.name
                val isDeleteRevealed = account.id in uiState.revealedDeleteAccountIds
                val isDeleting = account.id in uiState.isDeletingAccountIds

                AccountRow(
                    email = account.email,
                    providerName = providerName,
                    inactive = account.inactive,
                    isConfirmed = account.isConfirmed,
                    deleteMode = uiState.isDeleteMode,
                    deleteRevealed = isDeleteRevealed,
                    isDeleting = isDeleting,
                    onOpen = { onOpenAccount(account) },
                    onRevealDelete = { onRevealDelete(account.id) },
                    onDelete = { onDelete(account) },
                )
            }

            if (uiState.syncError != null || uiState.deleteError != null || uiState.isForegroundPolling) {
                item {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(horizontal = 16.dp, vertical = 8.dp),
                        verticalArrangement = Arrangement.spacedBy(6.dp),
                    ) {
                        if (uiState.isForegroundPolling) {
                            Text("Foreground polling active", style = MaterialTheme.typography.bodySmall)
                        }
                        if (uiState.syncError != null) {
                            Text(uiState.syncError.orEmpty(), color = MaterialTheme.colorScheme.error)
                        }
                        if (uiState.deleteError != null) {
                            Text(uiState.deleteError.orEmpty(), color = MaterialTheme.colorScheme.error)
                        }
                    }
                }
            }
        }

        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(Color(0xFF2196F3))
                .padding(start = 8.dp, end = 8.dp, top = 8.dp, bottom = 20.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically,
        ) {
            BottomActionIcon(
                imageVector = Icons.Outlined.AddCircle,
                label = "Add",
                onClick = onAddAccount,
            )
            BottomActionIcon(
                imageVector = Icons.Outlined.Delete,
                label = if (uiState.isDeleteMode) "Done" else "Delete",
                onClick = onToggleDeleteMode,
            )
            BottomActionGlyph(
                glyph = "S",
                label = if (uiState.isSyncing) "Syncing" else "Sync",
                enabled = !uiState.isSyncing,
                onClick = onSync,
            )
        }

        Text(
            text = versionLabel,
            modifier = Modifier
                .fillMaxWidth()
                .padding(end = 12.dp, bottom = 8.dp)
                .background(Color(0xFF2196F3)),
            color = Color.Black,
            textAlign = TextAlign.End,
            style = MaterialTheme.typography.bodySmall,
        )
    }
}

private fun PackageManager.readVersionName(packageName: String): String {
    val packageInfo = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
        getPackageInfo(packageName, PackageManager.PackageInfoFlags.of(0))
    } else {
        @Suppress("DEPRECATION")
        getPackageInfo(packageName, 0)
    }

    return packageInfo.versionName.orEmpty()
}

@Composable
private fun AccountRow(
    email: String,
    providerName: String?,
    inactive: Boolean,
    isConfirmed: Boolean,
    deleteMode: Boolean,
    deleteRevealed: Boolean,
    isDeleting: Boolean,
    onOpen: () -> Unit,
    onRevealDelete: () -> Unit,
    onDelete: () -> Unit,
) {
    Surface(
        modifier = Modifier.fillMaxWidth(),
        color = Color(0xFFFAFAFA),
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .clickable(onClick = onOpen)
                .padding(horizontal = 10.dp, vertical = 8.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            if (deleteMode) {
                IconButton(onClick = onRevealDelete) {
                    Icon(
                        imageVector = Icons.Outlined.Delete,
                        contentDescription = "Reveal delete",
                        tint = Color.Black,
                    )
                }
            } else {
                Spacer(modifier = Modifier.width(12.dp))
            }

            Column(
                modifier = Modifier
                    .weight(1f)
                    .padding(start = 4.dp),
                verticalArrangement = Arrangement.spacedBy(2.dp),
            ) {
                Text(
                    text = email,
                    color = Color.Black.copy(alpha = if (inactive) 0.5f else 1f),
                    style = MaterialTheme.typography.bodyLarge,
                )
                if (inactive) {
                    Text(
                        text = "REMOVED!",
                        color = Color.Black,
                        style = MaterialTheme.typography.labelLarge,
                        fontWeight = FontWeight.Bold,
                    )
                } else if (!isConfirmed) {
                    Text(
                        text = "PENDING CONFIRMATION",
                        color = Color(0xFFE65100),
                        style = MaterialTheme.typography.labelLarge,
                        fontWeight = FontWeight.Bold,
                    )
                } else if (!providerName.isNullOrBlank()) {
                    Text(
                        text = providerName,
                        color = Color.Black.copy(alpha = 0.7f),
                        style = MaterialTheme.typography.bodySmall,
                    )
                }
            }

            if (deleteRevealed) {
                Button(onClick = onDelete, enabled = !isDeleting) {
                    if (isDeleting) {
                        CircularProgressIndicator(modifier = Modifier.size(16.dp), strokeWidth = 2.dp)
                    } else {
                        Text("Delete")
                    }
                }
            } else if (!isConfirmed && !deleteMode) {
                Button(onClick = onOpen) {
                    Text("Enter Code")
                }
            }
        }
    }
}

@Composable
private fun BottomActionIcon(
    imageVector: androidx.compose.ui.graphics.vector.ImageVector,
    label: String,
    enabled: Boolean = true,
    onClick: () -> Unit,
) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(2.dp),
    ) {
        IconButton(onClick = onClick, enabled = enabled) {
            Icon(imageVector = imageVector, contentDescription = label, tint = Color.Black)
        }
        Text(text = label, color = Color.Black, style = MaterialTheme.typography.labelSmall)
    }
}

@Composable
private fun BottomActionGlyph(
    glyph: String,
    label: String,
    enabled: Boolean = true,
    onClick: () -> Unit,
) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(2.dp),
    ) {
        Surface(onClick = onClick, enabled = enabled, color = Color.Transparent) {
            Box(
                modifier = Modifier
                    .size(48.dp)
                    .testTag("accounts-sync-action"),
                contentAlignment = Alignment.Center,
            ) {
                Text(
                    text = glyph,
                    color = Color.Black,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                )
            }
        }
        Text(text = label, color = Color.Black, style = MaterialTheme.typography.labelSmall)
    }
}