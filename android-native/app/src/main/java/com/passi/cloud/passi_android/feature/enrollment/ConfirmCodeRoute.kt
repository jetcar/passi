package com.passi.cloud.passi_android.feature.enrollment

import androidx.compose.foundation.background
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
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.ui.NumberPad

@Composable
fun ConfirmCodeRoute(
    onCancel: () -> Unit,
    onComplete: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: ConfirmCodeViewModel = viewModel(
        factory = ConfirmCodeViewModel.factory(
            pendingEnrollmentStore = application.container.pendingEnrollmentStore,
            enrollmentService = application.container.enrollmentService,
            accountsRepository = application.container.accountsRepository,
            providersRepository = application.container.providersRepository,
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    ConfirmCodeScreen(
        uiState = uiState,
        onCodeChanged = viewModel::onCodeChanged,
        onSubmit = { viewModel.submit(onComplete) },
        onCancel = onCancel,
    )
}

@Composable
internal fun ConfirmCodeScreen(
    uiState: ConfirmCodeUiState,
    onCodeChanged: (String) -> Unit,
    onSubmit: () -> Unit,
    onCancel: () -> Unit,
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA)),
        contentAlignment = Alignment.Center,
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            if (uiState.responseError != null) {
                Text(
                    text = uiState.responseError.orEmpty(),
                    color = Color.Black,
                    textAlign = TextAlign.Center,
                )
            }

            Text(uiState.email, color = Color.Black, textAlign = TextAlign.Center)

            Text("Confirmation Code", color = Color.Black)

            Text(
                text = uiState.code,
                color = Color.Black,
                fontWeight = FontWeight.Bold,
                fontSize = 20.sp,
                textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxWidth(),
            )

            NumberPad(onKey = { key ->
                when (key) {
                    "del" -> onCodeChanged(uiState.code.dropLast(1))
                    "confirm" -> if (!uiState.isSubmitting && !uiState.isMissingContext) onSubmit()
                    else -> onCodeChanged(uiState.code + key)
                }
            })

            Spacer(modifier = Modifier.height(4.dp))

            Button(
                onClick = onCancel,
                shape = RoundedCornerShape(100.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color.White,
                    contentColor = Color.Black,
                ),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text("Cancel")
            }
        }
    }
}