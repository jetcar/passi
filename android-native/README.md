# Passi Native Android

This folder contains the first implementation slice of the MAUI-to-native Android rewrite for MauiApp2.

See [USE_CASES.md](USE_CASES.md) for the full end-user use-case description.

## Included in this slice

- Gradle-based Android application scaffold
- Jetpack Compose app shell with a root account list screen
- Kotlin domain models that mirror the current MAUI account, provider, notification, and endpoint contracts
- Repository interfaces for accounts and providers
- Persistent account and provider storage backed by Android SharedPreferences
- Device ID generation compatible with the MAUI app behavior
- Kotlin DTOs for signup, auth approval, sync, and pending-session polling
- Compose enrollment screens for terms, add account, confirmation code, and PIN setup
- Real HTTP-based signup and confirmation-code client using the existing backend endpoints
- Self-signed certificate generation and final signup confirmation from the PIN setup step
- Manual sync plus lifecycle-aware foreground polling, pending-session polling, color challenge UI, and PIN-based authorization signing
- Account detail screen, verified remote account deletion, and provider CRUD screens
- Certificate rotation flow for PIN and non-PIN accounts, including backend update signing with the existing certificate
- Local biometric enablement from account detail, biometric-backed session approval, and biometric-backed certificate rotation for biometric-enabled accounts
- EncryptedSharedPreferences-backed storage for biometric certificate material, with migration from the earlier plaintext preference keys
- Firebase messaging receiver, provider token registration, and notification-driven app opening that triggers immediate session sync when a push request arrives

## Not implemented yet

- Retrofit or Ktor client wiring

## Immediate next step

Validate the push-notification path on a real device with Firebase configuration and confirm that incoming auth requests open the app and land in the approval flow.