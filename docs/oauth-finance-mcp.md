# Finance MCP OAuth protocol

Issuer: `https://passi.cloud/openidc`.

## Discovery

These paths return the same issuer and endpoint capabilities:

- `/openidc/.well-known/openid-configuration` (existing OIDC endpoint)
- `/openidc/.well-known/oauth-authorization-server`
- `/.well-known/oauth-authorization-server/openidc` (RFC 8414 issuer-path endpoint)
- `/.well-known/openid-configuration/openidc` (compatibility alias)

The application handles the root aliases directly, and the existing HAProxy discovery rewrite remains supported. Published endpoints are `/openidc/login`, `/openidc/connect/token`, `/openidc/connect/register`, and `/openidc/.well-known/jwks`. Discovery advertises authorization-code and refresh grants, public authentication `none`, existing secret methods, and S256 only.

Finance must publish its own protected-resource metadata pointing to this issuer. Codex and other MCP clients follow that metadata to Passi, register, and open the advertised authorization endpoint. This change does not modify Finance or individual MCP-client configurations.

## Public dynamic registration

POST JSON to `/openidc/connect/register`:

```json
{
  "client_name": "Finance MCP client",
  "redirect_uris": ["http://127.0.0.1:4242/callback"],
  "token_endpoint_auth_method": "none",
  "grant_types": ["authorization_code", "refresh_token"],
  "response_types": ["code"],
  "scope": "openid profile email"
}
```

Success is HTTP 201 with `client_id`, `client_id_issued_at`, the extension `client_id_expires_at`, and accepted metadata. The ID contains 32 cryptographically random bytes encoded as base64url, prefixed `mcp_`. No client secret, registration access token, or management endpoint is issued. Cache control is `no-store`.

Omitted authentication/grant/response fields default to the profile above. Supplied values must match it exactly (grant order is irrelevant). `client_secret`, `jwks`, and `jwks_uri` are rejected as confidential-client settings. Unknown descriptive fields, including `client_uri` and `logo_uri`, are discarded without network access. Client names are unverified text, limited to 200 characters, with control characters rejected.

Limits: 16 KiB request body; 1–10 redirect URIs, each at most 2048 characters; scopes limited to `openid`, `profile`, and `email`. Redirects must be well-formed HTTPS URLs or HTTP loopback URLs (`localhost`, `127.0.0.1`, `[::1]`). Wildcards, fragments (including empty fragments), userinfo, whitespace, backslashes, and non-loopback HTTP are rejected. Dynamic-client redirects match the original registered string exactly, including case, query, and port. Bind the local callback port before registration; the existing native-client ephemeral-port exception does not apply to dynamic clients.

Invalid metadata returns HTTP 400 with `invalid_client_metadata` or `invalid_redirect_uri`. Rate limiting returns 429 and `Retry-After`; full/unavailable storage returns 503 with `temporarily_unavailable`. Limits apply across replicas sharing Redis.

## Authorization and token exchange

Generate a random 43–128 character RFC 7636 verifier. Calculate `BASE64URL(SHA256(ASCII(verifier)))`, without padding. Open the authorization endpoint with:

```text
response_type=code
client_id=<registered ID>
redirect_uri=http://127.0.0.1:4242/callback
scope=openid email
state=<random client correlation value>
code_challenge=<S256 challenge>
code_challenge_method=S256
resource=https://finance.passi.cloud/mcp
```

URL-encode parameter values. The user reviews the application, redirect, resource and permissions, then completes normal Passi phone approval. `nonce` is optional; when supplied, it is included in the ID token. OAuth-only scopes such as `email` are supported for dynamic/resource requests. A dynamic request omitting `scope` defaults to its registered scopes. Finance needs the `email` scope; an explicitly empty scope grants none.

The callback receives `code` and the original `state`. Codes expire after five minutes and are atomically single-use. POST `application/x-www-form-urlencoded` to the token endpoint:

```text
grant_type=authorization_code
client_id=<registered ID>
code=<authorization code>
redirect_uri=http://127.0.0.1:4242/callback
code_verifier=<original verifier>
resource=https://finance.passi.cloud/mcp
```

Public dynamic clients send neither client secrets nor Authorization headers. S256 is mandatory for public clients and all resource requests. Missing or invalid challenges/verifiers and `plain` are rejected. Existing confidential clients without resources retain secret-authenticated exchange without PKCE. Legacy clients requesting PKCE must also use S256.

Responses contain `access_token`, `token_type=Bearer`, `expires_in=3600`, `scope`, and `refresh_token`. `id_token` is returned only when `openid` is granted, and retains `aud=client_id` even when the access token is resource-bound.

An omitted token-endpoint `resource` inherits the authorization grant. An unknown resource or explicit mismatch returns `invalid_target`; a resource cannot be added to a grant that omitted it. A resource cannot be removed by omitting it during exchange. Repeated indicators and explicitly empty indicators are rejected. Client, redirect, PKCE, and resource bindings are checked independently.

## JWT validation by Finance

Send the **same access token** as `Authorization: Bearer <access_token>` to Finance MCP and Finance API. Both validate using Passi JWKS with:

| Field | Required value |
| --- | --- |
| Signature algorithm | RS256 |
| `kid` | Matching public key in Passi JWKS |
| `iss` | `https://passi.cloud/openidc` |
| `aud` | `https://finance.passi.cloud/mcp` |
| `client_id` | Registered client that obtained the grant |
| `sub` | Authenticated Passi subject |
| `email` | Authenticated Passi email, when `email` was granted |
| `scope` | Space-delimited granted scopes |
| `exp`, `iat`, `jti` | Expiry, issue time, unique token identifier |

Require valid signature, issuer, lifetime and audience; reject unsigned or non-RS256 tokens. Authorize by the signed email according to Finance's own policy. Do not require or infer `email_verified`. Do not use the ID token as the API bearer. For a non-resource legacy grant, access-token audience remains its client ID.

## Refresh

POST `grant_type=refresh_token`, `client_id`, and `refresh_token`; `resource` is optional and must match the original when present. Atomically replace the refresh token after each successful response. Rotation retains client, resource, subject, email claims, and scopes. If `scope` is sent, it must equal the originally granted set; expanding or narrowing is rejected for this profile.

Refresh lifetime is at most 30 days and never exceeds dynamic registration expiry. Reusing a rotated token invalidates its token family; store replacements atomically and avoid concurrent refresh attempts. Legacy pre-upgrade refresh tokens remain usable once and acquire family tracking on rotation. JWTs already issued are not remotely revoked and expire after one hour.

After `client_id_expires_at`, discard the expired registration, register again, and have the user authenticate. Redis restart/data loss may likewise require re-registration if Redis persistence is unavailable.

## Configuration and operation

Settings follow the existing `AppSetting` / environment lookup convention:

| Setting | Default | Constraint |
| --- | --- | --- |
| `OAuthAllowedResources` | `https://finance.passi.cloud/mcp` | Space-delimited exact HTTPS URLs; no fragment/userinfo |
| `DynamicClientCapacity` | 10000 | Positive; hard maximum 100000 |
| `DynamicClientLifetimeDays` | 30 | Positive; hard maximum 90 |
| `DynamicClientRegistrationsPerMinute` | 30 | Global fixed window; hard maximum 1000 |
| `TrustedProxyHosts` | `haproxy` | Existing trusted-proxy resolution for client IP |

Per-IP registration rate is five per minute. Only forwarding headers from configured trusted proxies affect the client IP. Redis automatically expires client records and the idle capacity index; subsequent registrations prune expired index members before admission. The bounded Lua write uses the same double-encoded ServiceStack string format as existing Redis writes; ordinary `Get<T>` still reads it correctly.

Application request logs and proxy logs omit query strings. Token logs omit codes, tokens, verifiers, secret values and registered redirects. Registration failures log a generic storage message without exception bodies or submitted metadata. Keep upstream infrastructure from logging request/response bodies, Authorization headers, or complete callback URLs.

## Validation

Run the entire suite against disposable local Redis (never production):

```powershell
rtk docker run -d --rm --name passi-oauth-tests-redis -p 127.0.0.1:16389:6379 redis:7-alpine
$env:PASSI_TEST_REDIS_PORT = '16389'
rtk dotnet test OpenIDCTests/OpenIDCTests.csproj
rtk docker stop passi-oauth-tests-redis
```

Without `PASSI_TEST_REDIS_PORT`, only the real-Redis integration cases are skipped. Tests cover registration validation, concurrent storage bounds and cleanup, rate limiting, complete HTTP phone-login simulation, session/resource tampering, S256, single-use codes, token/JWKS verification, refresh rotation/replay, scope claims, logging, discovery routes, and legacy client compatibility. The PR workflow runs real Redis and the entire OpenIDC suite.

Frontend checks: in `OpenIDC/vue-project`, run `rtk npm install`, `rtk npm run build`, and `rtk npm test`. Browser validation should include hostile HTML in the client name, no automatic login from `login_hint`, and requiring the approval checkbox before login.

No production login, merge, or deployment is part of this change. After an explicitly authorized deployment, follow AGENTS.md: perform one real login at Passi and inspect Cloud Logging for OpenIDC errors.
