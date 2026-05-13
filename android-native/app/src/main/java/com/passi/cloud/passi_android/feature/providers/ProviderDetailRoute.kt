package com.passi.cloud.passi_android.feature.providers

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication

private val ProviderDetailBlue = Color(0xFF2196F3)

@Composable
fun ProviderDetailRoute(
    onBack: () -> Unit,
    onEdit: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: ProviderDetailViewModel = viewModel(
        factory = ProviderDetailViewModel.factory(
            selectedProviderStore = application.container.selectedProviderStore,
            providersRepository = application.container.providersRepository,
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA)),
    ) {
        // Blue header with back arrow
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(ProviderDetailBlue)
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
        }

        Column(
            modifier = Modifier
                .weight(1f)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 20.dp, vertical = 8.dp),
        ) {
            uiState.provider?.let { provider ->
                ProviderLabelValue("Identity provider:", provider.name)
                ProviderLabelValue("IsDefault", provider.isDefault.toString())
                ProviderLabelValue("Base Url:", provider.baseUrl)
                ProviderLabelValue("SignupPath:", provider.apiPaths.signup)
                ProviderLabelValue("Authorize:", provider.apiPaths.authorize)
                ProviderLabelValue("CancelCheck:", provider.apiPaths.cancelCheck)
                ProviderLabelValue("CheckForStartedSessions:", provider.apiPaths.checkForStartedSessions)
                ProviderLabelValue("DeleteAccount:", provider.apiPaths.deleteAccount)
                ProviderLabelValue("SignupCheck:", provider.apiPaths.signupCheck)
                ProviderLabelValue("SignupConfirmation:", provider.apiPaths.signupConfirmation)
            } ?: Text("Provider not found", color = Color.Black)
        }

        uiState.provider?.let {
            Button(
                onClick = onEdit,
                shape = RoundedCornerShape(100.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color.White,
                    contentColor = Color.Black,
                ),
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp),
            ) {
                Text("Edit")
            }
        }
    }
}

@Composable
private fun ProviderLabelValue(label: String, value: String) {
    Text(
        text = label,
        color = Color.Black,
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = 5.dp),
    )
    Text(
        text = value,
        color = Color.Black,
        fontWeight = FontWeight.Medium,
        modifier = Modifier
            .fillMaxWidth()
            .padding(bottom = 5.dp),
    )
}

