package com.passi.cloud.passi_android.ui

import androidx.compose.material3.lightColorScheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import com.passi.cloud.passi_android.navigation.PassiNavGraph

@Composable
fun PassiApp() {
    MaterialTheme(colorScheme = lightColorScheme()) {
        Surface(color = MaterialTheme.colorScheme.background) {
            PassiNavGraph()
        }
    }
}