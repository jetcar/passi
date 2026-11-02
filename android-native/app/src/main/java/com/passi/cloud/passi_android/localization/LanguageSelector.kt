package com.passi.cloud.passi_android.localization

import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.res.stringResource
import com.passi.cloud.passi_android.R

@Composable
fun LanguageSelector() {
    var expanded by remember { mutableStateOf(false) }
    val selectedLanguageTag = AppLocale.selectedLanguageTag()
    val languageDescription = stringResource(R.string.language)

    IconButton(onClick = { expanded = true }) {
        Text(
            text = "🌐",
            modifier = Modifier.semantics {
                contentDescription = languageDescription
            },
        )
    }
    DropdownMenu(
        expanded = expanded,
        onDismissRequest = { expanded = false },
    ) {
        AppLocale.options.forEach { option ->
            val label = stringResource(option.labelRes)
            DropdownMenuItem(
                text = {
                    Text(
                        text = if (option.languageTag == selectedLanguageTag) {
                            stringResource(R.string.language_selected, label)
                        } else {
                            label
                        },
                    )
                },
                onClick = {
                    expanded = false
                    AppLocale.select(option.languageTag)
                },
            )
        }
    }
}
