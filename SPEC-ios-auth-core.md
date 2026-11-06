# Spec: iOS Authentication Core

## Objective

Implement the Passi backend protocol used by the Android client: enrollment, foreground session polling, RSA signatures, approval, cancellation, account deletion, and provider configuration.

## Tech Stack

Foundation `URLSession`, Security `SecKey`, Apple `swift-certificates` 1.21.1, and `swift-asn1`.

## Commands

Use the build and test commands in `SPEC-ios-packaging.md`.

## Project Structure

`ios/Passi/Models`, `Networking`, `Security`, and `Storage` contain the core.

## Code Style

```swift
actor PassiAPI {
    func beginSignup(email: String, provider: Provider, deviceID: String) async throws -> PendingEnrollment
}
```

DTO coding keys exactly match the backend's upper-camel-case JSON fields.

## Testing Strategy

Unit-test JSON compatibility, challenge parsing, URL construction, and PIN verification. Device installation remains manual.

## Boundaries

- Always: use HTTPS, RSA-2048, SHA-256 certificate signatures, and SHA-512/PKCS#1 v1.5 login signatures.
- Ask first: change backend endpoints or certificate algorithms.
- Never: log PINs, private keys, signatures, or enrollment codes.

## Success Criteria

- Public certificates are accepted by the existing .NET backend.
- Login signatures verify through the existing `CertHelper` implementation.
- Accounts and providers survive app restarts.

## Open Questions

None.
