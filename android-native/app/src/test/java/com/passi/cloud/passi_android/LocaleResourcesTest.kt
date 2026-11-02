package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.google.common.truth.Truth.assertWithMessage
import com.passi.cloud.passi_android.localization.AppLocale
import java.io.File
import javax.xml.parsers.DocumentBuilderFactory
import org.junit.Test
import org.w3c.dom.Element

class LocaleResourcesTest {
    private val resourceRoot = sequenceOf(
        File("src/main/res"),
        File("app/src/main/res"),
    ).first(File::isDirectory)

    private val localeFolders = listOf("values", "values-et", "values-ru", "values-de", "values-fr", "values-es")

    @Test
    fun everyLocaleHasTheSameStringKeysAndPlaceholders() {
        val catalogs = localeFolders.associateWith { folder ->
            readStrings(File(resourceRoot, "$folder/strings.xml"))
        }
        val english = catalogs.getValue("values")

        catalogs.forEach { (folder, catalog) ->
            assertWithMessage("keys in $folder").that(catalog.keys).containsExactlyElementsIn(english.keys)
            english.forEach { (key, value) ->
                assertWithMessage("placeholders for $key in $folder")
                    .that(placeholders(catalog.getValue(key)))
                    .containsExactlyElementsIn(placeholders(value))
                    .inOrder()
            }
        }
    }

    @Test
    fun localeConfigDeclaresTheApprovedLanguages() {
        val document = parseXml(File(resourceRoot, "xml/locales_config.xml"))
        val locales = document.getElementsByTagName("locale")
        val tags = (0 until locales.length).map { index ->
            (locales.item(index) as Element).getAttributeNS(ANDROID_NAMESPACE, "name")
        }

        assertThat(tags).containsExactly("en", "et", "ru", "de", "fr", "es").inOrder()
    }

    @Test
    fun selectorOffersSystemDefaultAndEveryApprovedLanguage() {
        assertThat(AppLocale.options.map { it.languageTag })
            .containsExactly(null, "en", "et", "ru", "de", "fr", "es")
            .inOrder()
    }

    @Test
    fun manifestEnablesAppCompatLocalePersistence() {
        val manifest = sequenceOf(
            File("src/main/AndroidManifest.xml"),
            File("app/src/main/AndroidManifest.xml"),
        ).first(File::isFile)
        val document = parseXml(manifest)
        val services = document.getElementsByTagName("service")
        val localeService = (0 until services.length)
            .map { services.item(it) as Element }
            .single { it.getAttributeNS(ANDROID_NAMESPACE, "name") == APP_LOCALES_SERVICE }
        val metadata = localeService.getElementsByTagName("meta-data").item(0) as Element

        assertThat(metadata.getAttributeNS(ANDROID_NAMESPACE, "name")).isEqualTo("autoStoreLocales")
        assertThat(metadata.getAttributeNS(ANDROID_NAMESPACE, "value")).isEqualTo("true")
    }

    private fun readStrings(file: File): Map<String, String> {
        val strings = parseXml(file).getElementsByTagName("string")
        return buildMap {
            for (index in 0 until strings.length) {
                val element = strings.item(index) as Element
                val name = element.getAttribute("name")
                check(put(name, element.textContent) == null) { "Duplicate string key $name in $file" }
            }
        }
    }

    private fun parseXml(file: File) = DocumentBuilderFactory.newInstance().apply {
        isNamespaceAware = true
        setFeature("http://apache.org/xml/features/disallow-doctype-decl", true)
        setFeature("http://xml.org/sax/features/external-general-entities", false)
        setFeature("http://xml.org/sax/features/external-parameter-entities", false)
    }.newDocumentBuilder().parse(file)

    private fun placeholders(value: String): List<String> =
        PLACEHOLDER.findAll(value).map(MatchResult::value).toList()

    private companion object {
        const val ANDROID_NAMESPACE = "http://schemas.android.com/apk/res/android"
        const val APP_LOCALES_SERVICE = "androidx.appcompat.app.AppLocalesMetadataHolderService"
        val PLACEHOLDER = Regex("%\\d+\\$[a-zA-Z]")
    }
}
