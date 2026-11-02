# Spec: Android Localization

## Objective

Move Android Compose UI and notification strings to resources, provide six translations, follow the device locale initially, and provide a persisted in-app language selector.

## Tech Stack

Kotlin, Jetpack Compose, Android string resources, AndroidX per-app locale APIs.

## Commands

- Unit tests: `gradlew.bat :app:testDebugUnitTest`
- Build: `gradlew.bat :app:assembleDebug`
- Instrumentation compile: `gradlew.bat :app:compileDebugAndroidTestKotlin`

## Project Structure

- `android-native/app/src/main/res/values*/strings.xml` — source and translated resources.
- `android-native/app/src/main/res/xml/locales_config.xml` — supported locales.
- Compose routes use `stringResource`; services use `getString`.
- Settings/account UI exposes language selection.

## Code Style

```kotlin
Text(text = stringResource(R.string.accounts_title))
```

## Testing Strategy

Add resource-presence tests, compile unit/instrumentation sources, and build debug APK. Verify formatted placeholders in every locale.

## Boundaries

- Always: keep resource identifiers stable and use positional placeholders.
- Ask first: change account/provider/API data models.
- Never: translate account names, provider names, certificate values, email addresses, or server payloads.

## Success Criteria

- All user-visible Android-owned strings are resources.
- Six locale resource sets have matching keys and valid placeholders.
- Device locale is used initially; in-app choice persists.
- Notifications and biometric prompts use localized resources.
- Tests and debug build pass.

## Open Questions

None.
