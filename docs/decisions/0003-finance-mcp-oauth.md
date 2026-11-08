# ADR-0003: Resource-bound OAuth for Finance MCP

## Status

Proposed — implemented for review; deployment requires a separate merge decision.

## Date

2026-11-08

## Context

MCP clients need to register public clients, authenticate through Passi, and present one access token to Finance MCP and Finance API. Finance must validate the signature and claims locally using Passi's certificate and JWKS. Existing Passi websites and native clients must continue working.

## Decision

Passi remains the authorization server at `https://passi.cloud/openidc`. The existing RS256 signing certificate and JWKS implementation issue and verify resource-bound access tokens. Finance uses the audience `https://finance.passi.cloud/mcp` for both its MCP and API interfaces. There is no second token exchange, shared secret with Finance, introspection dependency, or `email_verified` requirement.

New public registrations use RFC 7591, cryptographically random client IDs, no secret, exact registered redirects, and authorization code with PKCE S256. Confidential-client registration continues through the existing authenticated client-management API. Configured/previously registered clients keep their existing storage and redirect behavior; requests without `resource` keep `aud=client_id`. Existing confidential clients may continue secret-authenticated code exchange without PKCE. Any supplied PKCE challenge must now use S256; `plain` is deliberately retired.

RFC 8707 resource selection is an exact configured allowlist match, bound into the browser start token, login session, authorization code, and refresh token. Missing token/refresh resource inherits the grant. An explicit resource cannot change or add a grant audience. Only one resource is supported per grant; repeated indicators are rejected. This keeps one token suitable for both Finance interfaces without widening it to unrelated APIs.

Dynamic clients live separately in Redis. One atomic Lua operation prunes expired index entries, checks the global capacity, and creates the record with native TTL. The index itself expires when its last record expires. Native Redis expiration provides cleanup even when registration is idle; no background scan or database migration is required. Shared global and IP rate limits, metadata/body bounds, and fixed expiry constrain anonymous registration storage. Open registration remains susceptible to deliberate capacity exhaustion; administrators can tune limits, and storage exhaustion fails closed without evicting active clients.

Registration lifetime is fixed, not renewed by authentication. Refresh expiry is capped to client expiry. Clients must register again and authenticate after expiration. Rotated refresh grants preserve lineage; replay invalidates successors. Previously issued refresh records without lineage remain redeemable once and acquire lineage on rotation. Already issued JWTs remain independently valid until their normal expiration.

The browser displays the unverified application name as text, actual redirect, resource, and permissions and requires an approval action before starting phone login. It does not automatically start dynamic/resource login from `login_hint`. No supplied metadata URL is fetched or rendered as active content. Frame protection and no-referrer headers protect the login page. The existing phone approval and browser-session binding remain the authentication boundary; application metadata is not proof of a client's identity. This does not redesign the existing passwordless phone flow or make it resistant to a user approving an attacker's own login session.

## Consequences

- Finance can validate issuer, audience, expiration, RS256 signature, `kid`, subject, client, email, and granted scopes using JWKS alone.
- `email` appears in access tokens and userinfo only with the `email` scope. Passi's existing subject identifier may itself be an email address; omitting the email claim is not subject pseudonymization.
- Registration accepts exactly the public profile requested for this integration: `none`, both authorization-code and refresh grants, and the `code` response. Requests for confidential authentication or different grant sets fail.
- Unknown resources, changed binding parameters, expired registrations, replayed codes, and reused refresh tokens fail without exposing credential values in application logs. Proxy access logs omit queries, including OAuth callbacks to existing Passi clients.
- Redis availability is required. Registration outages return 503. Finance must independently enforce its own authorization policy using the signed email and scopes.

## Protocol and operations

See [OAuth protocol](../oauth-finance-mcp.md) for endpoint examples, configuration, client expiry, validation, and checks before deployment.

## Sources

- [RFC 7591 — dynamic registration](https://www.rfc-editor.org/rfc/rfc7591.html)
- [RFC 7636 — PKCE](https://www.rfc-editor.org/rfc/rfc7636.html)
- [RFC 8707 — resource indicators](https://www.rfc-editor.org/rfc/rfc8707.html)
- [RFC 8414 §3 — issuer-path discovery](https://www.rfc-editor.org/rfc/rfc8414.html#section-3)
- [RFC 9700 §4.14.2 — refresh-token replay](https://www.rfc-editor.org/rfc/rfc9700.html#section-4.14.2)
- [Redis Lua atomicity](https://redis.io/docs/latest/develop/programmability/eval-intro/)
- [ASP.NET Core 10 middleware branching](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0#branch-the-middleware-pipeline)
- [HAProxy 3.2 log formats](https://docs.haproxy.org/3.2/configuration.html#8.2.4)
