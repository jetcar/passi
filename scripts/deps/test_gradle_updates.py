import json
import os
import tempfile
import unittest

from gradle_updates import apply_updates, is_stable, main


def report(*entries):
    """Minimal ben-manes gradle-versions-plugin JSON report."""
    return {
        "outdated": {
            "dependencies": [
                {"group": g, "name": n, "version": cur, "available": {"release": new, "milestone": None, "integration": None}}
                for g, n, cur, new in entries
            ]
        }
    }


APP = '''dependencies {
    val composeBom = platform("androidx.compose:compose-bom:2024.06.00")
    implementation("androidx.core:core-ktx:1.13.1")
    implementation("androidx.security:security-crypto:1.1.0-alpha06")
    testImplementation("junit:junit:4.13.2")
}
'''

ROOT = '''plugins {
    id("com.android.application") version "9.0.0" apply false
    id("org.jetbrains.kotlin.android") version "2.2.10" apply false
}
'''


class IsStableTests(unittest.TestCase):
    def test_release_versions_are_stable(self):
        for v in ["1.13.1", "2024.06.00", "4.13.2", "1.0.0-jre", "33.4.0-android"]:
            self.assertTrue(is_stable(v), v)

    def test_prerelease_versions_are_not_stable(self):
        for v in ["1.1.0-alpha06", "2.0.0-beta01", "1.0.0-rc1", "3.0.0-RC2", "1.2.0-M1", "4.0.0-SNAPSHOT", "1.0.0-dev3", "2.1.0-eap"]:
            self.assertFalse(is_stable(v), v)


class ApplyUpdatesTests(unittest.TestCase):
    def test_bumps_dependency_coordinates_including_major_versions(self):
        files = {"app/build.gradle.kts": APP}
        out, applied = apply_updates(report(
            ("androidx.core", "core-ktx", "1.13.1", "2.0.0"),
            ("androidx.compose", "compose-bom", "2024.06.00", "2025.09.01"),
        ), files)

        text = out["app/build.gradle.kts"]
        self.assertIn('"androidx.core:core-ktx:2.0.0"', text)
        self.assertIn('"androidx.compose:compose-bom:2025.09.01"', text)
        self.assertEqual(len(applied), 2)

    def test_bumps_plugin_versions(self):
        files = {"build.gradle.kts": ROOT}
        out, applied = apply_updates(report(
            ("org.jetbrains.kotlin.android", "org.jetbrains.kotlin.android.gradle.plugin", "2.2.10", "2.3.0"),
        ), files)

        self.assertIn('id("org.jetbrains.kotlin.android") version "2.3.0"', out["build.gradle.kts"])
        self.assertIn('id("com.android.application") version "9.0.0"', out["build.gradle.kts"])
        self.assertEqual(applied, ["org.jetbrains.kotlin.android: 2.2.10 -> 2.3.0"])

    def test_skips_unstable_candidate_when_current_is_stable(self):
        files = {"app/build.gradle.kts": APP}
        out, applied = apply_updates(report(("junit", "junit", "4.13.2", "5.0.0-M1")), files)

        self.assertEqual(out["app/build.gradle.kts"], APP)
        self.assertEqual(applied, [])

    def test_allows_newer_prerelease_when_already_on_prerelease(self):
        files = {"app/build.gradle.kts": APP}
        out, _ = apply_updates(report(("androidx.security", "security-crypto", "1.1.0-alpha06", "1.1.0-beta01")), files)

        self.assertIn('"androidx.security:security-crypto:1.1.0-beta01"', out["app/build.gradle.kts"])

    def test_ignores_dependencies_not_declared_inline(self):
        files = {"app/build.gradle.kts": APP}
        out, applied = apply_updates(report(("com.squareup.okio", "okio", "3.6.0", "3.9.0")), files)

        self.assertEqual(out["app/build.gradle.kts"], APP)
        self.assertEqual(applied, [])

    def test_does_not_touch_other_artifacts_with_same_version(self):
        text = 'implementation("a:x:1.0.0")\nimplementation("a:y:1.0.0")\n'
        out, _ = apply_updates(report(("a", "x", "1.0.0", "1.1.0")), {"f": text})

        self.assertEqual(out["f"], 'implementation("a:x:1.1.0")\nimplementation("a:y:1.0.0")\n')

    def test_uses_milestone_when_release_missing(self):
        rep = {"outdated": {"dependencies": [
            {"group": "androidx.security", "name": "security-crypto", "version": "1.1.0-alpha06",
             "available": {"release": None, "milestone": "1.1.0-alpha07", "integration": None}}]}}
        out, _ = apply_updates(rep, {"app/build.gradle.kts": APP})

        self.assertIn('"androidx.security:security-crypto:1.1.0-alpha07"', out["app/build.gradle.kts"])


class MainTests(unittest.TestCase):
    def test_preserves_crlf_line_endings_and_only_changes_versions(self):
        with tempfile.TemporaryDirectory() as tmp:
            gradle = os.path.join(tmp, "build.gradle.kts")
            with open(gradle, "w", encoding="utf-8", newline="") as f:
                f.write('dependencies {\r\n    implementation("a:x:1.0.0")\r\n}\r\n')
            rep = os.path.join(tmp, "report.json")
            with open(rep, "w", encoding="utf-8") as f:
                json.dump(report(("a", "x", "1.0.0", "2.0.0")), f)

            self.assertEqual(main(["gradle_updates.py", rep, gradle]), 0)

            with open(gradle, "rb") as f:
                self.assertEqual(f.read(), b'dependencies {\r\n    implementation("a:x:2.0.0")\r\n}\r\n')


if __name__ == "__main__":
    unittest.main()
