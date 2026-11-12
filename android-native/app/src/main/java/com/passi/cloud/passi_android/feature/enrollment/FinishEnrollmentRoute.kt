package com.passi.cloud.passi_android.feature.enrollment

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.R
import com.passi.cloud.passi_android.ui.NumberPad
import com.passi.cloud.passi_android.localization.asString

@Composable
fun FinishEnrollmentRoute(
    onCancel: () -> Unit,
    onDone: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: FinishEnrollmentViewModel = viewModel(
        factory = FinishEnrollmentViewModel.factory(
            pendingEnrollmentStore = application.container.pendingEnrollmentStore,
            accountsRepository = application.container.accountsRepository,
            providersRepository = application.container.providersRepository,
            enrollmentService = application.container.enrollmentService,
            certificateGenerator = application.container.certificateGenerator,
            deviceIdProvider = { application.container.preferences.getOrCreateDeviceId() },
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    FinishEnrollmentScreen(
        uiState = uiState,
        onPinChanged = viewModel::onPinChanged,
        onPinConfirmationChanged = viewModel::onPinConfirmationChanged,
        onSkip = { viewModel.skip(onDone) },
        onSubmit = { viewModel.submit(onDone) },
    )
}

@Composable
internal fun FinishEnrollmentScreen(
    uiState: FinishEnrollmentUiState,
    onPinChanged: (String) -> Unit,
    onPinConfirmationChanged: (String) -> Unit,
    onSkip: () -> Unit,
    onSubmit: () -> Unit,
) {
    Column(
        modifier = Modifier.fillMaxSize().padding(24.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text(stringResource(R.string.device_key_explanation))
        uiState.responseError?.let { Text(it.asString()) }
        Button(onClick = onSubmit, enabled = !uiState.isSubmitting && !uiState.isMissingContext) {
            Text(stringResource(R.string.device_key_create))
        }
    }
}
