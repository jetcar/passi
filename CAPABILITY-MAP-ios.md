# Capability Map: Passi for iPhone

| Module id | Responsibility | Depends on |
| --- | --- | --- |
| `ios-packaging` | XcodeGen project, unsigned IPA packaging, CI artifact | — |
| `ios-auth-core` | API contracts, persistence, RSA certificate and signing | `ios-packaging` |
| `ios-user-flows` | Enrollment, accounts, providers, login approval | `ios-auth-core` |
| `ios-device-security` | Keychain storage, PIN checks, Face ID gating | `ios-auth-core` |

Build order: `ios-packaging` → `ios-auth-core` → `ios-user-flows`, `ios-device-security`.
