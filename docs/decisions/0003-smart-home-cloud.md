# ADR-0003: Owner-bound cloud exchange for local home agents

## Status

Accepted for implementation; not deployed.

## Date

2026-11-21

## Context

A local .NET server controls a Xiaomi plug. Its LAN operation must survive cloud outages.
Passi WebApp and WebAPI use different cookies; existing OpenIDC subjects are email strings,
while `UserDb.Guid` is the stable account identifier. The legacy REST client logs parameters
and retries writes, which is unsuitable for enrollment secrets and physical-device commands.

## Decision

Resolve the authenticated OpenIDC subject and certificate to an existing confirmed account.
Only `admin@passi.cloud` can initialize a singleton durable GUID pin. Never cascade-delete or
automatically reassign that pin when an account disappears. Check the pin/current account for
every owner action and every authenticated agent exchange.

Use existing PostgreSQL for homes, hashed enrollment/agent credentials and a permanent command
deduplication ledger. Serialize the small single-owner feature with one transaction-scoped
PostgreSQL advisory lock. No network side effects occur inside database transactions. A command
is committed as sent before being returned, so an ambiguous transaction/response may lose delivery
but cannot cause automatic repeat. No Redis read/write format changes are needed.

Agents initiate TLS WebSockets to a SignalR hub, authenticate using an independent expiring
256-bit token and send full snapshots every five seconds. Each exchange rechecks credentials,
owner and active connection. A new connection fences its predecessor. Commands are claimed from
the shared database by the exchange, so multiple WebAPI instances need no paid SignalR backplane.
Only registered device capabilities and `power.set` boolean operations are supported.

The WebApp BFF forwards its saved user access token through fixed routes and a dedicated client
with no redirects, retries or body logging. WebAPI validates OpenIDC issuer, signature, expiry,
the WebApp `ClientId` audience and access-token-specific claims. Browser writes require CSRF.
TLS verification remains enabled for new outgoing connections.

## Consequences

- No home port forwarding, HTTP tunnel, shell access or device keys in Passi.
- Token rotation intentionally requires a new one-use enrollment and invalidates the old token
  immediately. A lost enrollment response requires re-enrollment; plaintext is unrecoverable.
- Delivery and device confirmation are distinct. Disconnect/expiry after sending is `unknown`,
  never retried. Local agents must also persist command IDs before hardware actions.
- Reconnect drops outstanding commands; users explicitly issue new commands after inspecting
  fresh state. This favours avoiding accidental physical actions over guaranteed delivery.
- Stale and offline values remain visible with timestamps. No unintegrated device is fabricated.
- The command ledger grows; any future retention policy must retain deduplication tombstones.
- The advisory lock is appropriate for the deliberately small owner-only installation. Broader
  multi-tenant scale would require a new design, not expanding this owner's authorization.
- Existing login, device services, Redis formats and production configuration remain unchanged.

## References

- [Agent contract](../smart-home-agent-contract.md)
- [Specification](../smart-home-spec.md)
- [SignalR connection authentication is cached](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0#user-and-role-changes-during-the-connection-lifetime)
- [JWT bearer validation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)
