# Passi Android

Native Android client for Passi (Kotlin, Jetpack Compose), published on Google Play as
[`com.passi.cloud.passi_android`](https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android).

See [USE_CASES.md](USE_CASES.md) for the end-user use cases.

## Features

- Account enrollment: terms, email, confirmation code, optional PIN, self-signed certificate generation
- Multiple accounts, each tied to a configurable provider (backend endpoint); provider CRUD
- Session approval: foreground and pending-session polling, color challenge, signing with PIN or biometrics
- Firebase push notifications that open the app and trigger an immediate session sync
- Account detail, linked devices, verified remote account deletion
- Certificate rotation for PIN, non-PIN and biometric accounts
- Biometric certificate material stored in EncryptedSharedPreferences
- In-app banner when a newer version is available on Google Play

## Project structure

| Module / package | Description |
| --- | --- |
| `app/` | The Android application |
| `app/.../feature/` | Compose screens and ViewModels: `accounts`, `account`, `enrollment`, `auth`, `certificate`, `providers`, `update` |
| `app/.../domain/` | Models, repository and service interfaces |
| `app/.../data/` | Storage, crypto, HTTP client (`data/remote`), biometrics, notifications, update check |
| `e2e/` | JVM end-to-end tests that run the real backend in Docker (PostgreSQL, Redis, passiwebapi) |

Requirements: JDK 17, Android SDK (compileSdk 37, minSdk 28, targetSdk 36). The build needs
`app/google-services.json` (git-ignored) for Firebase.

## Build and test

```bash
./gradlew :app:assembleDebug
```

```bash
./gradlew :app:testDebugUnitTest
```

```bash
./gradlew :e2e:test
```

`../run_e2e_tests.bat` runs the e2e suite with a JaCoCo coverage report. `run_visible_full_flow.bat` drives the
full flow on a connected device through `adb`, against a local backend in Docker.

Release builds use R8 minification and resource shrinking (`app/proguard-rules.pro`).

## Release

Releases go through the manual GitHub Actions workflow `.github/workflows/google-play-release.yml`. Pick the
track (`internal`/`alpha`/`beta`/`production`), the release status and the display version. It builds a signed
AAB and uploads it to Google Play. `versionCode` is derived from the workflow run number. The monthly
dependency-update workflow also creates a production draft automatically.
