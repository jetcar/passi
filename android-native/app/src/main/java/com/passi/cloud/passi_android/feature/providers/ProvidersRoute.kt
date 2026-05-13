package com.passi.cloud.passi_android.feature.providers

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
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
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.domain.model.Provider

private val Blue = Color(0xFF2196F3)

@Composable
fun ProvidersRoute(
    onBack: () -> Unit,
    onOpenProvider: () -> Unit,
    onAddProvider: () -> Unit,
) {
    val application = LocalContext.current.applicationContext as PassiApplication
    val viewModel: ProvidersViewModel = viewModel(
        factory = ProvidersViewModel.factory(
            providersRepository = application.container.providersRepository,
            selectedProviderStore = application.container.selectedProviderStore,
        )
    )
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    var isDeleteMode by remember { mutableStateOf(false) }
    var revealedDeleteIds by remember { mutableStateOf(emptySet<String>()) }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFFAFAFA)),
    ) {
        // Blue header with back arrow
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(Blue)
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

        if (uiState.responseError != null) {
            Text(
                text = uiState.responseError.orEmpty(),
                color = Color.Black,
                modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp),
            )
        }

        LazyColumn(modifier = Modifier.weight(1f)) {
            items(uiState.providers, key = { it.id }) { provider ->
                ProviderRow(
                    provider = provider,
                    isDeleteMode = isDeleteMode,
                    showDeleteButton = provider.id.toString() in revealedDeleteIds,
                    onTap = { viewModel.openProvider(provider, onOpenProvider) },
                    onDeleteReveal = {
                        revealedDeleteIds = if (provider.id.toString() in revealedDeleteIds) {
                            revealedDeleteIds - provider.id.toString()
                        } else {
                            revealedDeleteIds + provider.id.toString()
                        }
                    },
                    onDelete = {
                        revealedDeleteIds = revealedDeleteIds - provider.id.toString()
                        viewModel.deleteProvider(provider)
                    },
                )
            }
        }

        // Blue bottom action bar
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(Blue)
                .padding(bottom = 20.dp),
            horizontalArrangement = Arrangement.SpaceEvenly,
        ) {
            IconButton(onClick = { viewModel.addProvider(onAddProvider) }) {
                Icon(
                    imageVector = Icons.Filled.Add,
                    contentDescription = "Add provider",
                    tint = Color.White,
                    modifier = Modifier.size(28.dp),
                )
            }
            IconButton(onClick = { isDeleteMode = !isDeleteMode; if (!isDeleteMode) revealedDeleteIds = emptySet() }) {
                Icon(
                    imageVector = Icons.Filled.Delete,
                    contentDescription = "Delete mode",
                    tint = Color.White,
                    modifier = Modifier.size(28.dp),
                )
            }
        }
    }
}

@Composable
private fun ProviderRow(
    provider: Provider,
    isDeleteMode: Boolean,
    showDeleteButton: Boolean,
    onTap: () -> Unit,
    onDeleteReveal: () -> Unit,
    onDelete: () -> Unit,
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onTap)
            .padding(horizontal = 16.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        if (isDeleteMode) {
            IconButton(onClick = onDeleteReveal, modifier = Modifier.size(36.dp)) {
                Icon(
                    imageVector = Icons.Filled.Delete,
                    contentDescription = "Reveal delete",
                    tint = Color.Black,
                )
            }
        }
        Text(
            text = provider.name,
            color = Color.Black,
            modifier = Modifier.weight(1f),
        )
        if (showDeleteButton) {
            Button(
                onClick = onDelete,
                colors = ButtonDefaults.buttonColors(containerColor = Color.Red, contentColor = Color.White),
                shape = RoundedCornerShape(4.dp),
                modifier = Modifier.size(width = 80.dp, height = 36.dp),
            ) {
                Text("Delete")
            }
        }
    }
}

