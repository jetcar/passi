package com.passi.cloud.passi_android.localization

import androidx.annotation.StringRes
import androidx.appcompat.app.AppCompatDelegate
import androidx.core.os.LocaleListCompat
import com.passi.cloud.passi_android.R

data class AppLocaleOption(
    val languageTag: String?,
    @StringRes val labelRes: Int,
)

object AppLocale {
    val options = listOf(
        AppLocaleOption(languageTag = null, labelRes = R.string.language_system_default),
        AppLocaleOption(languageTag = "en", labelRes = R.string.language_english),
        AppLocaleOption(languageTag = "et", labelRes = R.string.language_estonian),
        AppLocaleOption(languageTag = "ru", labelRes = R.string.language_russian),
        AppLocaleOption(languageTag = "de", labelRes = R.string.language_german),
        AppLocaleOption(languageTag = "fr", labelRes = R.string.language_french),
        AppLocaleOption(languageTag = "es", labelRes = R.string.language_spanish),
    )

    fun selectedLanguageTag(): String? =
        AppCompatDelegate.getApplicationLocales().get(0)?.language

    fun select(languageTag: String?) {
        val locales = languageTag
            ?.let(LocaleListCompat::forLanguageTags)
            ?: LocaleListCompat.getEmptyLocaleList()
        AppCompatDelegate.setApplicationLocales(locales)
    }
}
