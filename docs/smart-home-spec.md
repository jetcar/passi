# Smart home cloud — implementation specification

## Scope and dependencies

| Module | Responsibility | Depends on |
|---|---|---|
| home-access | Pin existing admin account GUID, authorize owner and per-home credentials | Existing Passi users/OpenIDC |
| home-exchange | Durable device snapshots, expiring commands, at-most-once delivery | home-access, existing PostgreSQL |
| home-ui | Existing-login BFF and Vue management page | home-exchange |

Build order: access/storage → exchange/API → UI → emulator and independent security review.
Initial scope was cloud-only. The 2026-11-22 extension coordinates the local agent's
multi-device catalog with the existing v1 contract. No production cloud deployment,
paid service or real device mutations are part of verification.

## Decisions and boundaries

- .NET 10 / EF Core 10 / PostgreSQL already used by WebAPI; Vue 3 existing frontend.
- Resolve the existing `Users.EmailHash == admin@passi.cloud` account and persist its `Guid`
  once. Never grant ownership by client email/userId or an admin-role check. Pin survives
  account deletion; replacing the email account must not transfer home ownership.
- Existing WebApp cookie and its saved OpenIDC access token remain unchanged. Narrow BFF
  endpoints forward this access token using a dedicated HttpClient without retries or
  body/header logging. WebAPI validates signature, issuer, lifetime, WebApp audience and
  access-token claims, resolves validated subject through Users, then compares pinned GUID.
- Agent uses its own random 256-bit per-home bearer credential. Store only SHA-256 hashes.
  Owner creates a short-lived one-use enrollment secret; anonymous HTTPS POST redeems it.
  Tokens expire; owner can revoke immediately and issue a fresh pairing secret. Rotation
  invalidates old token and connections; plaintext tokens are returned once, never logged.
- SignalR WebSocket connection originates at home. Agent invokes `Exchange` every 5 seconds
  with complete registered-device snapshot and command results; response claims pending
  commands durably before returning them. This avoids cross-instance backplane delivery.
  Every invocation revalidates token and current connection fencing generation.
- No arbitrary URLs, network targets, scripts or device keys. V1 capabilities only
  `power.read`, `temperature.read`, `power.set`; only explicit `power.set` boolean action.
- Commands have caller-generated GUID, server expiry (30s), one active command per device,
  durable deduplication and statuses queued/sent/confirmed/failed/unknown/expired.
  Sent commands are never delivered again. Expiry/disconnect after delivery is unknown.
  A successful result requires matching post-command device readback, not delivery alone.
- Online lease 20s; snapshots older than 30s are stale. Reconnect fences prior connection,
  invalidates outstanding commands, requires a fresh snapshot before accepting control.
  UI also marks cached values stale after API failure. Never fabricate Tuya/Midea devices.

## Structure and verification

Multi-server extension: device identity is `(homeId, deviceId)`, with independent
credentials and snapshots per server. The dashboard can filter by server without
flattening device identity. Identical device IDs on two homes must retain independent
pending commands, readings, result validation, connection fencing and revocation.
Local agents publish only supported devices from their own LAN catalog; new device
drivers and cloud schedule editing remain separate capabilities. V1 wire types and
the existing database schema remain compatible.

Models + Repos: additive EF models/migration, existing DB context. WebApiDto/SmartHome:
wire types. passiwebapi/SmartHome: service, auth, hub and HTTP controllers. WebApp:
fixed BFF and Vue page. PassiWebApiTests: PostgreSQL-backed domain and HTTP/SignalR emulator
tests. WebAppTests: BFF authorization/CSRF tests where applicable.

Style follows existing C# namespaces, explicit usings and DTO properties, and Vue script setup.
No unrelated login/service refactor. Existing dirty files must be preserved.

Commands: `rtk dotnet test PassiWebApiTests --filter FullyQualifiedName~SmartHome`,
`rtk dotnet test WebAppTests`, `rtk dotnet build passiwebapi`, `rtk dotnet build WebApp`,
`rtk npm run type-check` in WebApp/vue-project. Build frontend into an isolated output directory
to preserve unrelated existing wwwroot files. Run `rtk graphify update .` after code changes.

Acceptance: owner only over HTTP and persistent connections; reject wrong-home agent,
invalid/revoked/expired credentials, query tokens and unsupported actions; one-use enrollment;
rotation; disconnection; expiry; duplicate/conflicting commands; stale/old snapshots; concurrent
delivery and restart durability. All command tests use emulators, no physical device API.

## Framework sources

- https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/ef/core/saving/concurrency

SignalR caches authentication for the connection lifetime, so credentials must additionally
be checked on each exchange. Database concurrency guards delivery before any network send.
