package com.passi.cloud.passi_android.feature.account

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.domain.model.ManagedDevice
import com.passi.cloud.passi_android.ui.IPhoneConfirmationSheet
import java.time.ZoneId
import java.time.format.DateTimeFormatter

@Composable
fun AccountDevicesRoute(
    onBack: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: AccountDevicesViewModel = viewModel(
        factory = AccountDevicesViewModel.factory(
            selectedAccountStore = application.container.selectedAccountStore,
            accountsRepository = application.container.accountsRepository,
            accountManagementService = application.container.accountManagementService,
            currentDeviceIdProvider = { application.container.preferences.getOrCreateDeviceId() },
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    var pendingDevice by remember { mutableStateOf<ManagedDevice?>(null) }

    AccountDevicesScreen(
        uiState = uiState,
        pendingDevice = pendingDevice,
        onBack = onBack,
        onRequestRemove = { pendingDevice = it },
        onConfirmRemove = {
            pendingDevice = null
            viewModel.deleteDevice(it.deviceId)
        },
        onDismissRemove = { pendingDevice = null },
    )
}

@Composable
internal fun AccountDevicesScreen(
    uiState: AccountDevicesUiState,
    pendingDevice: ManagedDevice?,
    onBack: () -> Unit,
    onRequestRemove: (ManagedDevice) -> Unit,
    onConfirmRemove: (ManagedDevice) -> Unit,
    onDismissRemove: () -> Unit,
) {

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA)),
    ) {
        // Blue header with back arrow and account email
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(Color(0xFF2196F3))
                .statusBarsPadding()
                .padding(top = 4.dp, bottom = 8.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            IconButton(onClick = onBack) {
                Icon(
                    imageVector = Icons.AutoMirrored.Filled.KeyboardArrowLeft,
                    contentDescription = "Back",
                    tint = Color.White,
                    modifier = Modifier.size(28.dp),
                )
            }
            Text(
                text = uiState.account?.email ?: "",
                color = Color.White,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.padding(start = 4.dp),
            )
        }

        Text(
            text = "Remove old devices one by one. This device stays protected from deletion.",
            color = Color.Black,
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 8.dp),
        )

        if (uiState.isLoading) {
            CircularProgressIndicator(
                modifier = Modifier
                    .align(Alignment.CenterHorizontally)
                    .padding(16.dp),
            )
        }

        if (uiState.responseError != null) {
            Text(
                text = uiState.responseError.orEmpty(),
                color = Color.Red,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 4.dp),
            )
        }

        LazyColumn(
            modifier = Modifier.weight(1f),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            items(uiState.devices, key = { it.deviceId }) { device ->
                DeviceRow(
                    device = device,
                    isRemoving = uiState.isRemoving && pendingDevice?.deviceId == device.deviceId,
                    onRemove = { onRequestRemove(device) },
                )
            }
        }
    }

    pendingDevice?.let { device ->
        IPhoneConfirmationSheet(
            title = "Remove device?",
            message = "${device.displayName} will stop receiving login approvals for this account.",
            confirmLabel = "Remove Device",
            onConfirm = { onConfirmRemove(device) },
            onDismiss = onDismissRemove,
        )
    }
}

@Composable
private fun DeviceRow(
    device: ManagedDevice,
    isRemoving: Boolean,
    onRemove: () -> Unit,
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp)
            .background(Color.White, RoundedCornerShape(8.dp))
            .padding(12.dp),
        verticalArrangement = Arrangement.spacedBy(4.dp),
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                Text(device.displayName, color = Color.Black, fontWeight = FontWeight.SemiBold)
                Text("ID ${device.shortIdentifier}", color = Color.Black, fontSize = 12.sp)
                Text(
                    text = "Registered ${device.creationTime.atZone(ZoneId.systemDefault()).format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm"))}",
                    color = Color.Black,
                    fontSize = 12.sp,
                )
            }
            if (device.isCurrent) {
                Text("Current", color = Color(0xFF2196F3), fontWeight = FontWeight.Medium)
            }
        }
        if (!device.isCurrent) {
            Button(
                onClick = onRemove,
                enabled = !isRemoving,
                shape = RoundedCornerShape(100.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color.White,
                    contentColor = Color.Black,
                ),
                modifier = Modifier.fillMaxWidth(),
            ) {
                if (isRemoving) {
                    CircularProgressIndicator(modifier = Modifier.size(16.dp), strokeWidth = 2.dp)
                } else {
                    Text("Remove device")
                }
            }
        }
    }
}