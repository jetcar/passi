# Spec: iOS Device Security

## Objective

Keep RSA private keys in the iOS Keychain, gate protected approvals with a PIN or Face ID, and avoid APNs-only capabilities that free Sideloadly signing cannot provision.

## Tech Stack

Security Keychain Services, LocalAuthentication, CryptoKit.

## Commands

Use the build and test commands in `SPEC-ios-packaging.md`.

## Project Structure

`ios/Passi/Security` owns key creation, lookup, signing, PIN verification, and local authentication.

## Code Style

```swift
let authenticated = try await context.evaluatePolicy(
    .deviceOwnerAuthenticationWithBiometrics,
    localizedReason: "Approve this login"
)
```

## Testing Strategy

Unit-test deterministic PIN verification and key-tag lifecycle helpers; perform Face ID and signing checks on a physical iPhone.

## Boundaries

- Always: use Keychain items limited to this device and request Face ID only with a clear reason.
- Ask first: add APNs or paid-team entitlements.
- Never: export or persist a plaintext private key outside Keychain.

## Success Criteria

- RSA private keys remain Keychain-backed.
- Incorrect PINs cannot authorize.
- Face ID can gate approval when enabled.
- No APNs entitlement is required.

## Open Questions

None.
