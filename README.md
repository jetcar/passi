# Passi

Passwordless OAuth2 / OpenID Connect two-factor authentication: a .NET backend plus a native Android app.
Instead of a password, the user approves each login on their phone by picking the matching color and signing
the challenge with a device certificate, which can be protected by a PIN or fingerprint.

- Demo video: https://www.youtube.com/watch?v=tRrWp6LWQNU
- Android app: https://play.google.com/store/apps/details?id=com.passi.cloud.passi_android
- Sample web app: https://passi.cloud

## Try it

1. Install the Android app, open it and enter your email.
2. Enter the confirmation code from the email you receive.
3. Optionally protect the account with a PIN. With a PIN set, you can also enable fingerprint approval.
4. Go to https://passi.cloud and log in. You are redirected to the identity provider at
   https://passi.cloud/identity, which only asks for your email.
5. A push notification arrives on your phone (or open the app to see the pending request). It shows the
   confirmation colors and where the login was started.
6. Pick the color shown in the browser and confirm with your PIN or fingerprint if enabled.
7. You are redirected back to https://passi.cloud, which verifies the signature and logs you in. The profile
   page shows the data exchanged between the phone and the web service.

## Repository layout

| Path | Description |
| --- | --- |
| `passiwebapi/` | Core API used by the phone app: signup, devices, certificates, sessions, push notifications |
| `OpenIDC/` | OpenID Connect identity provider (`/identity`), with a Vue front end in `vue-project/` |
| `WebApp/` | Sample relying-party web app served at https://passi.cloud, with a Vue front end in `vue-project/` |
| `SampleApi/` | Sample API protected by Passi tokens |
| `Services/`, `Repos/`, `Models/`, `WebApiDto/`, `RedisClient/`, `ConfigurationManager/`, `NotificationsService/`, `GoogleTracer/` | Shared libraries |
| `*Tests/` | .NET test projects |
| `android-native/` | Native Android app (Kotlin, Jetpack Compose) and its e2e tests. See [android-native/README.md](android-native/README.md) |
| `mailler/` | Mail server backend (`mailler-backend` image) |
| `configs/` | HAProxy, identity and environment configuration |
| `scripts/` | Container entrypoint and Let's Encrypt certificate scripts |

The backend targets .NET 10 (`passi.sln`).

## Running

### Production-style stack (Docker Compose)

`docker-compose.yml` runs PostgreSQL, Redis, HAProxy, certbot, the webapp, openidc, passiwebapi,
mailler-backend, WireGuard and Porkbun DDNS. It expects these directories next to the repository:

| Path | Contents |
| --- | --- |
| `../passi_config/` | `dev.env`, `mailler.env`, `ddclient.env`. See [configs/variables/dev.env](configs/variables/dev.env) for a sample |
| `../passi_cert/` | TLS certificates for HAProxy |
| `../passi_identity_cert/` | Identity signing certificate |
| `../creds/` | Service credentials, such as Firebase |

```bash
docker compose -f docker-compose.yml up --build
```

On Windows, `build_dockers.bat` builds the images locally and `build local docker compose.bat` starts the stack.

### Tests

```bash
dotnet test passi.sln
```

Android e2e tests (need Docker, because they start PostgreSQL, Redis and the API in containers):

```bash
run_e2e_tests.bat
```

## CI/CD

| Workflow | Trigger | What it does |
| --- | --- | --- |
| `build-and-push-with-build-number-version.yml` | Push to `main` (except Android, `.github` and Markdown changes) | Builds and pushes backend images, then deploys to the server with `run.sh` |
| `google-play-release.yml` | Manual | Builds a signed AAB and uploads it to a Google Play track |
| `monthly-dependency-release.yml` | 1st of each month, or manual | Updates NuGet, npm and Gradle dependencies, runs every test suite, then deploys and creates a Play production draft. If anything fails, it opens a PR instead |
| `codeql.yml` | Push/PR to `main`, weekly | CodeQL analysis |

## License

See [LICENSE](LICENSE).
