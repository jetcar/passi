# Passi

Passwordless OAuth2 / OpenID Connect two-factor authentication: a .NET backend plus native Android and iPhone apps.
Instead of a password, the user approves each login on their phone by picking the matching color and signing
the challenge with a device certificate, which can be protected by a PIN or fingerprint.

- Demo video: https://www.youtube.com/watch?v=Koe_8YcmYkU
- Android app: https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android
- Sample web app: https://passi.cloud

## Try it

1. Install the Android app, open it and enter your email.
2. Enter the confirmation code from the email you receive.
3. Optionally protect the account with a PIN. With a PIN set, you can also enable fingerprint approval.
4. Go to https://passi.cloud and log in. You are redirected to the identity provider at
   https://passi.cloud/openidc/login, which only asks for your email.
5. A push notification arrives on your phone (or open the app to see the pending request). It shows the
   confirmation colors and where the login was started.
6. Pick the color shown in the browser and confirm with your PIN or fingerprint if enabled.
7. You are redirected back to https://passi.cloud, which verifies the signature and logs you in. The profile
   page shows the data exchanged between the phone and the web service.

## Install the mobile app

### Android APK

The [Google Play listing](https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android) is the
recommended installation. To sideload, download `Passi-<version>.apk` from the newest `android-v*` entry on the
[GitHub Releases page](https://github.com/jetcar/passi/releases), open it on the Android device, allow that browser
or file manager to install unknown apps when prompted, review the Play Protect warning, and install it. Only use an
APK published by this repository. See the [Android installation guide](android-native/README.md#install-the-apk)
for update and signature-conflict guidance.

### iPhone IPA

The iPhone app requires iOS 16 or later and is distributed as an unsigned IPA for Sideloadly:

1. Download `Passi-<version>-unsigned.ipa` from the newest `ios-v*` entry on the
   [GitHub Releases page](https://github.com/jetcar/passi/releases).
2. Install [Sideloadly](https://sideloadly.io/) on Windows or macOS, connect and trust the iPhone, drag in the IPA,
   select the device, enter the Apple ID used for signing, and start the sideload.
3. Enable **Settings → Privacy & Security → Developer Mode** on the iPhone and restart if prompted.
4. If required, trust the signing Apple ID under **Settings → General → VPN & Device Management**.

A free Apple developer account signs the app for seven days. Re-sign it with the same Apple ID and bundle ID, or
enable Sideloadly's automatic refresh. Push notifications are unavailable in the unsigned build, so keep Passi open
while approving a login. See the [iPhone installation guide](ios/README.md#install-with-sideloadly) for details.

## Repository layout

| Path | Description |
| --- | --- |
| `passiwebapi/` | Core API used by the phone app: signup, devices, certificates, sessions, push notifications |
| `OpenIDC/` | OpenID Connect identity provider (`/openidc`), with a Vue front end in `vue-project/` |
| `WebApp/` | Sample relying-party web app served at https://passi.cloud, with a Vue front end in `vue-project/` |
| `SampleApi/` | Sample API protected by Passi tokens |
| `Services/`, `Repos/`, `Models/`, `WebApiDto/`, `RedisClient/`, `ConfigurationManager/`, `NotificationsService/`, `GoogleTracer/` | Shared libraries |
| `*Tests/` | .NET test projects |
| `android-native/` | Native Android app (Kotlin, Jetpack Compose) and its e2e tests. See [android-native/README.md](android-native/README.md) |
| `ios/` | Native iPhone app (SwiftUI) and unsigned IPA build. See [ios/README.md](ios/README.md) |
| `mailler/` | Mail server backend (`mailler-backend` image) |
| `configs/` | HAProxy, identity and environment configuration |
| `scripts/` | Container entrypoint and Let's Encrypt certificate scripts |

The backend targets .NET 10 (`passi.sln`).

## Use Passi with your service

Passi is an OpenID Connect provider. Use a standard OpenID Connect library for your framework and load its
configuration from the discovery document instead of hard-coding individual endpoints.

| Setting | Value for the hosted service |
| --- | --- |
| Issuer / authority | `https://passi.cloud/openidc` |
| Discovery document | `https://passi.cloud/openidc/.well-known/openid-configuration` |
| Flow | Authorization Code |
| Scopes | `openid profile email` |
| User key | The `sub` claim; do not use email as a database key |

### Register and test an OAuth application on passi.cloud

1. Install the Android app, create an account, and complete one login at https://passi.cloud as described in
   [Try it](#try-it).
2. While signed in, open **My OAuth apps** in the navigation bar, or go directly to
   https://passi.cloud/OAuthApps.
3. Enter a name and every callback URL your service will use, one per line. A callback must be an exact absolute
   URL with no fragment. HTTPS is required except for loopback development URLs such as
   `http://localhost:3000/callback`.
4. Select the client type:
   - **Website** for a server-side application. It receives a client secret and must authenticate at the token
     endpoint. Copy the secret immediately; Passi stores only its hash and cannot show it again.
   - **App / SPA** for a mobile, desktop, command-line, or browser-only application. It has no secret and must use
     Authorization Code with PKCE (`S256`).
5. Configure the service with the issuer, client ID, client secret when applicable, and the same callback URL.
   Keep the secret in a secret manager or environment variable, never in source control or browser code.
6. Start login through the service's OpenID Connect library. Verify `state` and `nonce`; public clients must also
   generate a fresh PKCE verifier and challenge. Approve the request in the Passi phone app.
7. On the callback, exchange the authorization code at the discovered token endpoint. The callback URL must
   exactly match the registered URL and the URL used to start authorization. Public clients send the PKCE verifier
   and no secret; website clients send their secret.
8. Validate the ID token's signature, issuer, audience, expiry, and nonce using the discovery document and JWKS.
   The access token can be sent as a Bearer token to the discovered UserInfo endpoint.
9. Confirm refresh-token handling, logout, rejected `state`, an incorrect callback URL, and an invalid or missing
   PKCE verifier before production use. Delete the test application or rotate its website secret when finished.

Quick connectivity check:

```bash
curl --fail https://passi.cloud/openidc/.well-known/openid-configuration
```

### Instructions for an AI coding agent

When adding Passi login to another service:

1. Inspect the service's existing authentication middleware, session model, callback routes, and secret-loading
   convention before editing code.
2. Use the framework's maintained OpenID Connect client. Configure it from the issuer/discovery URL above; do not
   implement token parsing or signature verification manually.
3. Ask the operator for the registered client ID, exact callback URL, client type, and—only for a website
   client—the secret. Never register an application silently, print a secret to logs, or commit it.
4. Use Authorization Code flow. Require PKCE with `S256` for apps, SPAs, desktop tools, and CLIs. Website clients
   authenticate at the token endpoint and may also use PKCE.
5. Request only `openid profile email` unless the service has a documented need for another advertised scope.
6. Map the local account to `sub`. Treat email and display-name claims as changeable profile data.
7. Validate issuer, audience, signature, expiry, `state`, and `nonce`; use secure, HTTP-only cookies for server-side
   sessions and do not store tokens in browser local storage.
8. Add tests for login, callback validation, logout, invalid state, token-validation failure, and the client-type
   rules. Keep the existing authentication method available until the Passi flow has been verified end to end.

The human operator must create the application under **My OAuth apps** because registration is tied to their Passi
account and a website secret is displayed only once.

## Running

### Self-hosting with Docker Compose

The checked-in Compose and HAProxy files describe the current `passi.cloud` deployment. They are a deployment
reference, not a domain-neutral one-command installer: paths, certificate names, domains, optional mail, DDNS, and
WireGuard services must be adapted before use on another host.

Requirements:

- Docker Engine with Docker Compose
- A public HTTPS hostname and a reverse proxy that preserves `X-Forwarded-*` headers
- PostgreSQL and Redis with persistent storage
- SMTP or SendGrid for signup codes
- Firebase service credentials for Android push notifications
- .NET 10 SDK when running tests or services outside containers

The core login deployment consists of PostgreSQL, Redis, `passiwebapi`, `openidc`, and a public reverse proxy.
`webapp` provides the sample site and **My OAuth apps** UI. `mailler-backend`, certbot, DDNS, and WireGuard are
optional when equivalent external services are used.

#### Prepare configuration

`docker-compose.yml` runs PostgreSQL, Redis, HAProxy, certbot, the webapp, openidc, passiwebapi,
mailler-backend, WireGuard and Porkbun DDNS. It expects these directories next to the repository:

| Path | Contents |
| --- | --- |
| `../passi_config/` | `dev.env`, plus `mailler.env` and `ddclient.env` only when those services are enabled. Start from [configs/variables/dev.env](configs/variables/dev.env) |
| `../passi_cert/` | Public TLS certificate bundle used by HAProxy |
| `../passi_identity_cert/` | Kestrel `aspnetcore.pfx` and stable OIDC signing files at `certs/certificate.crt` and `certs/privatekey.pem` |
| `../creds/` | Firebase/Google service credentials referenced by the configured environment variables |
| `../redis_data/`, database volume | Persistent Redis and PostgreSQL data on host paths appropriate for the deployment |

Before starting the stack, an operator or AI agent must:

1. Copy the sample environment file outside the repository and replace every example password, client secret,
   project ID, credential path, and URL. The checked-in values are development examples and are not production
   secrets.
2. Set `IdentityUrlBase` to the public origin, `openIdcUrl` to `<origin>/openidc`, and `PassiUrl` to the internal or
   public `/passiapi` URL. Configure database, Redis, email, and Firebase settings.
3. Replace every `passi.cloud` hostname in the deployment-specific Compose, HAProxy, certbot, and WireGuard
   settings. Update HAProxy's certificate filename or use another reverse proxy.
4. Mount a persistent OIDC signing key. Development can fall back to an ephemeral RSA key, but restarting it
   invalidates issued tokens and is not suitable for production.
5. Remove or disable optional services that are supplied externally, and restrict PostgreSQL and Redis to private
   networks. Do not expose their ports publicly.

#### Build and start

Build the shared image first because the service Dockerfiles use it as their build stage:

```bash
docker build -f Dockerfile -t jetcar/common_image:latest .
docker build -f WebApp/Dockerfile -t jetcar/webapp:latest .
docker build -f OpenIDC/Dockerfile -t jetcar/openidc:latest .
docker build -f passiwebapi/Dockerfile -t jetcar/passiwebapi:latest .
docker compose -f docker-compose.yml config
docker compose -f docker-compose.yml up -d
```

On Windows, `build_dockers.bat` builds the core images and `build local docker compose.bat` starts the stack.
Use `docker-compose.override.yml` only after reviewing its development-only host ports, test credentials, and volume
paths.

Verify the deployment before connecting another service:

```bash
docker compose -f docker-compose.yml ps
curl --fail https://auth.example.com/openidc/.well-known/openid-configuration
dotnet test passi.sln
```

The discovery document's `issuer` must exactly equal `https://auth.example.com/openidc`, its endpoints must be
publicly reachable over HTTPS, and the signing key must survive container restarts. Then register each relying
service through **My OAuth apps** on the self-hosted `webapp` and configure it as described in
[Use Passi with your service](#use-passi-with-your-service).

### Tests

```bash
dotnet test passi.sln
```

Android e2e tests (need Docker, because they start PostgreSQL, Redis and the API in containers):

```bash
run_e2e_tests.bat
```

## News and MCP

The sample web app has a news page (`/news`, latest posts on the home page, RSS at `/news/rss.xml`). Anyone can
react to posts anonymously. Posts are managed at `/news/admin` by the users listed in `NewsAdminEmails`.

AI agents can manage posts through the MCP server at `/mcp`, signing in with Passi (OAuth authorization code +
PKCE). Register an *app* client under **My OAuth apps** with redirect URI `http://localhost:8765/callback`, add its
client id to `NewsMcpClientIds`, then:

```bash
claude mcp add --transport http passi-news https://passi.cloud/mcp --client-id <client id> --callback-port 8765
```

## CI/CD

| Workflow | Trigger | What it does |
| --- | --- | --- |
| `build-and-push-with-build-number-version.yml` | Push to `main` (except Android, `.github` and Markdown changes) | Builds and pushes backend images, then deploys to the server with `run.sh` |
| `google-play-release.yml` | Manual | Builds a signed AAB and uploads it to a Google Play track |
| `ios-ipa.yml` | Manual or iOS changes | Tests the iPhone app and uploads an unsigned IPA for Sideloadly |
| `monthly-dependency-release.yml` | 1st of each month, or manual | Updates NuGet, npm and Gradle dependencies, runs every test suite, then deploys and creates a Play production draft. If anything fails, it opens a PR instead |
| `codeql.yml` | Push/PR to `main`, weekly | CodeQL analysis |

## License

See [LICENSE](LICENSE).
