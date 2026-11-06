# ADR-0002: Capability-based unlink for missing mobile accounts

## Status

Accepted

## Date

2026-11-06

## Context

Reinstalling the Android app can preserve or recreate an FCM token while removing all local accounts. The server still has the device-account link, so login pushes reach the reinstalled app, which cannot approve them.

## Decision

Each login push carries a random unlink capability bound to exactly one server-side account-device link. The server stores only a SHA-256-derived Redis key with a 15-minute expiry and consumes it atomically before removing the link. If the database transaction fails, the grant is restored for retry. The endpoint accepts no account identifier or certificate from the caller.

Android checks the notification account against local storage. When the account is absent and the notification targets the current device ID, it shows a non-login notification with a **Remove device** action. The action uses durable background work and sends the capability only when the server's configured public API URL exactly matches a configured provider; it never opens the missing account flow.

## Consequences

- A reinstalled app can remove its stale server link without possessing the deleted account certificate.
- A capability cannot select another account or device, cannot be replayed, and expires quickly.
- Removing the final device link intentionally disables login approval until the account is enrolled again.
- Orphaned device rows are retained rather than deleted in the unlink transaction, avoiding destructive foreign-key cascades and enrollment races.
- Redis is required for issuing and consuming unlink capabilities.
- Older notifications without an unlink capability no longer open an unusable login flow, but cannot remove the stale link.
