# Passi for iPhone

Native SwiftUI client for iOS 16 or later. It supports enrollment, provider and account management, foreground login polling, color or number challenges, approval and cancellation, PIN protection, and Face ID.

## Install with Sideloadly

No Mac is required.

1. Open [GitHub Releases](https://github.com/jetcar/passi/releases) and download the
   `Passi-<version>-unsigned.ipa` asset from the newest `ios-v*` release.
2. Install [Sideloadly](https://sideloadly.io/) on a Windows PC or Mac.
3. Connect the iPhone by USB, unlock it, tap **Trust** if prompted, and select it in Sideloadly.
4. Drag the IPA into Sideloadly, enter the Apple ID used for signing, and start the sideload.
5. On iOS 16 or later, enable **Settings → Privacy & Security → Developer Mode** and restart when prompted.
6. If the app is still marked as untrusted, open **Settings → General → VPN & Device Management**, select the
   Apple ID used to sign it, and trust it.

If no release asset is available, open
[Actions → iOS unsigned IPA](https://github.com/jetcar/passi/actions/workflows/ios-ipa.yml), choose **Run workflow**,
and download the `Passi-unsigned-ipa` artifact after it succeeds.

A free Apple developer account signs the app for seven days. Sideloadly Daemon can refresh it when the iPhone is available over USB or configured Wi-Fi. Reuse the same Apple ID and bundle ID when updating.

The workflow publishes an unsigned IPA and does not require Apple credentials, certificates, provisioning profiles, or device identifiers.

## Build layout

- `project.yml` generates the Xcode project with XcodeGen.
- `Passi/` contains the app, API client, Keychain identity, and SwiftUI screens.
- `PassiTests/` contains model, authentication, and challenge tests.
- `scripts/package-ipa.sh` creates the Sideloadly-compatible IPA.

Push notifications are intentionally excluded from unsigned/free-account builds. Keep Passi open while waiting for a login request; it checks for pending requests every five seconds while active.
