# Spec: iOS Packaging

## Objective

Build Passi as an iOS 16+ application and publish an unsigned IPA that Sideloadly can sign and install.

## Tech Stack

Swift 6, SwiftUI, Xcode 16.4, XcodeGen 2.46.0, GitHub Actions macOS runner.

## Commands

```sh
xcodegen generate --spec ios/project.yml
xcodebuild -project ios/Passi.xcodeproj -scheme Passi -sdk iphonesimulator -destination 'platform=iOS Simulator,name=iPhone 16 Pro,OS=latest' test
xcodebuild -project ios/Passi.xcodeproj -scheme Passi -sdk iphoneos -destination 'generic/platform=iOS' CODE_SIGNING_ALLOWED=NO build
bash ios/scripts/package-ipa.sh build/ios-device/Build/Products/Release-iphoneos/Passi.app build/Passi-unsigned.ipa
```

## Project Structure

`ios/Passi/` contains app source and resources; `ios/PassiTests/` contains unit tests; `ios/scripts/` packages the IPA.

## Code Style

```swift
@main
struct PassiApp: App {
    var body: some Scene { WindowGroup { RootView() } }
}
```

Four-space indentation, explicit access control for infrastructure, and no force unwraps in production code.

## Testing Strategy

Run simulator unit tests, build for a generic iPhone, inspect the IPA for its executable, Info.plist, and icon catalog.

## Boundaries

- Always: publish an unsigned artifact without signing secrets.
- Ask first: change the iOS minimum or distribution method.
- Never: commit Apple credentials, certificates, profiles, device IDs, or signing cookies.

## Success Criteria

- GitHub Actions publishes `Passi-unsigned.ipa`.
- The IPA contains `Payload/Passi.app/Passi` and `Assets.car`.
- Sideloadly can supply signing and install it on iOS 16+.

## Open Questions

None.
