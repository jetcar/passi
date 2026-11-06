# ADR-0001: Native iOS client with unsigned IPA delivery

## Status

Accepted

## Date

2026-11-06

## Context

Passi needs an iPhone client installable from Windows with Sideloadly. Free signing cannot provision APNs, and the existing backend requires RSA X.509 certificates and SHA-512/PKCS#1 v1.5 signatures.

## Decision

Build a native iOS 16+ SwiftUI app. Generate RSA-2048 account keys with Security framework APIs, keep them in Keychain, create public certificates with Apple's `swift-certificates`, and poll only while the app is foregrounded. Generate the Xcode project with XcodeGen and package an unsigned IPA on a GitHub-hosted macOS runner.

## Alternatives Considered

### .NET MAUI

Rejected because the tracked MAUI clients are no longer complete buildable projects and the maintained app is the native Android rewrite.

### Web wrapper

Rejected because enrollment and login approval require device-held RSA keys and native biometric gating.

### APNs notifications

Deferred because they require signing capabilities unavailable to the free Sideloadly path.

## Consequences

- Windows users can download and sideload the CI artifact.
- Pending logins appear through foreground polling and manual refresh rather than push.
- Face ID is available without storing biometric data in the app.
- The iOS app has one Apple-maintained Swift package dependency for X.509 encoding.
