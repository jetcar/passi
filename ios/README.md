# Passi for iPhone

Native SwiftUI client for iOS 16 or later. It supports enrollment, provider and account management, foreground login polling, color or number challenges, approval and cancellation, PIN protection, and Face ID.

## Install with Sideloadly

No Mac is required.

1. Open [Actions → iOS unsigned IPA](https://github.com/jetcar/passi/actions/workflows/ios-ipa.yml) and choose **Run workflow**.
2. Download the `Passi-unsigned-ipa` artifact after the workflow succeeds.
3. Extract the matching IPA, such as `Passi-0.1.1-unsigned.ipa`, from the downloaded ZIP.
4. Open the IPA in [Sideloadly](https://sideloadly.io/), connect the iPhone, enter the Apple ID used for signing, and start the sideload.
5. If iOS asks, enable Developer Mode and trust the developer profile in device settings.

A free Apple developer account signs the app for seven days. Sideloadly Daemon can refresh it when the iPhone is available over USB or configured Wi-Fi. Reuse the same Apple ID and bundle ID when updating.

The workflow publishes an unsigned IPA and does not require Apple credentials, certificates, provisioning profiles, or device identifiers.

## Build layout

- `project.yml` generates the Xcode project with XcodeGen.
- `Passi/` contains the app, API client, Keychain identity, and SwiftUI screens.
- `PassiTests/` contains model, authentication, and challenge tests.
- `scripts/package-ipa.sh` creates the Sideloadly-compatible IPA.

Push notifications are intentionally excluded from unsigned/free-account builds. Keep Passi open while waiting for a login request; it checks for pending requests every five seconds while active.
