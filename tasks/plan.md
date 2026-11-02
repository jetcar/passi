# Implementation Plan: Passi Localization

## Overview

Add English, Estonian, Russian, German, French, and Spanish localization to the three web clients and Android app while preserving user-authored and server-owned content.

## Architecture Decisions

- Each app owns catalogs suited to its framework; only locale codes, fallback, detection, and persistence form the shared contract.
- All same-origin web apps use `passi.locale`, so one selection follows the user across Passi, OpenIDC, and Mailler.
- English catalogs are authoritative; parity tests reject missing or extra keys.
- Android uses platform resources and per-app locales rather than a custom translation map.

## Task List

### Phase 1: Foundation

- [x] Task 1: Implement and test the WebApp locale adapter and catalogs.
- [x] Task 2: Convert the WebApp shell and public pages.
- [x] Task 3: Convert WebApp account, OAuth, news, and admin UI.

### Checkpoint: WebApp

- [x] Catalog tests and production build pass.
- [x] Selector persists and mobile navigation remains correct.

### Phase 2: Remaining Web Clients

- [x] Task 4: Implement OpenIDC locale adapter, catalogs, and selector.
- [x] Task 5: Convert OpenIDC authentication and management views.
- [x] Task 6: Implement Mailler locale adapter, catalogs, and selector.
- [x] Task 7: Convert Mailler feature components.

### Checkpoint: Web Clients

- [x] All three web builds pass.
- [x] Mailler tests pass.
- [x] Locale selection follows the user across same-origin apps.

### Phase 3: Android

- [x] Task 8: Add Android locale resources, configuration, and selector plumbing.
- [x] Task 9: Convert enrollment, account, and provider UI strings.
- [x] Task 10: Convert authentication, certificate, update, notification, and biometric strings.

### Checkpoint: Android

- [x] Resource parity and placeholder checks pass.
- [x] Unit tests, instrumentation compilation, and debug build pass.

### Phase 4: Completion

- [x] Task 11: Run cross-app hardcoded-string audit and final builds.
- [x] Task 12: Update the code graph and prepare atomic commits.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Translation drift | High | English-key parity tests in every app |
| Auth regression | High | Do not change protocol values; build and manually verify login flow |
| Compose placeholder mismatch | High | Resource format validation and compile checks |
| Mobile layout expansion | Medium | Use concise labels and inspect narrow layouts |
| Generated build output noise | Low | Restore generated `wwwroot` output after verification |

## Open Questions

None.
