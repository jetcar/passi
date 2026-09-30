package com.passi.cloud.passi_android.feature.update

import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import android.net.Uri
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.expandVertically
import androidx.compose.animation.shrinkVertically
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp

@Composable
internal fun AppUpdateBanner(
    visible: Boolean,
    onUpdate: () -> Unit,
    onDismiss: () -> Unit,
) {
    AnimatedVisibility(
        visible = visible,
        enter = expandVertically(expandFrom = Alignment.Bottom),
        exit = shrinkVertically(shrinkTowards = Alignment.Bottom),
    ) {
        Surface(
            modifier = Modifier
                .fillMaxWidth()
                .testTag("app-update-banner"),
            color = Color(0xFF323232),
        ) {
            Row(
                modifier = Modifier.padding(start = 16.dp, end = 8.dp, top = 6.dp, bottom = 6.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text(
                    text = "A new version is available",
                    modifier = Modifier.weight(1f),
                    color = Color.White,
                )
                TextButton(onClick = onDismiss) {
                    Text("Later", color = Color.White)
                }
                Button(onClick = onUpdate) {
                    Text("Update")
                }
            }
        }
    }
}

internal fun openPlayStoreListing(context: Context) {
    val packageName = context.packageName
    val playStoreApp = Intent(Intent.ACTION_VIEW, Uri.parse("market://details?id=$packageName"))
        .setPackage("com.android.vending")
        .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
    val playStoreWeb = Intent(Intent.ACTION_VIEW, Uri.parse("https://play.google.com/store/apps/details?id=$packageName"))
        .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)

    try {
        context.startActivity(playStoreApp)
    } catch (_: ActivityNotFoundException) {
        try {
            context.startActivity(playStoreWeb)
        } catch (_: ActivityNotFoundException) {
            // No Play Store and no browser: nothing to open.
        }
    }
}
