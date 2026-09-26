plugins {
    id("com.android.application") version "9.0.0" apply false
    id("com.google.gms.google-services") version "4.4.2" apply false
    id("org.jetbrains.kotlin.android") version "2.2.10" apply false
    id("org.jetbrains.kotlin.jvm") version "2.2.10" apply false
    id("org.jetbrains.kotlin.plugin.compose") version "2.2.10" apply false
    // Reports newer dependency versions; used by the monthly dependency-update workflow.
    id("com.github.ben-manes.versions") version "0.53.0"
}

tasks.named<com.github.benmanes.gradle.versions.updates.DependencyUpdatesTask>("dependencyUpdates") {
    outputFormatter = "json"
    outputDir = "build/dependencyUpdates"
    reportfileName = "report"
    revision = "milestone"
    // Offer the newest *stable* version; allow pre-releases only for artifacts already on one.
    // Keep in sync with is_stable() in scripts/deps/gradle_updates.py.
    rejectVersionIf { isNonStable(candidate.version) && !isNonStable(currentVersion) }
}

fun isNonStable(version: String): Boolean {
    val qualifier = version.substringAfter('-', "")
    return qualifier.isNotEmpty() &&
        Regex("""(alpha|beta|rc|cr|m\d|milestone|preview|snapshot|dev|eap)""", RegexOption.IGNORE_CASE).containsMatchIn(qualifier)
}
