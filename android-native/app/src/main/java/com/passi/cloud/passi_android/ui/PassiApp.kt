package com.passi.cloud.passi_android.ui

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.material3.lightColorScheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import com.passi.cloud.passi_android.navigation.PassiNavGraph

@Composable
fun PassiApp() {
    MaterialTheme(colorScheme = lightColorScheme()) {
        Surface(modifier = Modifier.fillMaxSize(), color = MaterialTheme.colorScheme.background) {
            // Draw the background edge-to-edge; keep every route clear of system bars, cutouts and IME.
            Box(Modifier.fillMaxSize().safeDrawingPadding()) {
                PassiNavGraph()
            }
        }
    }
}
