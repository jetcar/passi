# Implementation Plan: Passi for iPhone

## Overview

Add a native iOS client and the same unsigned-IPA delivery path used by VidroX, while preserving the existing Passi backend contract.

## Architecture Decisions

- Generate a reviewable Xcode project from `ios/project.yml`.
- Use an RSA-2048 `SecKey` per account and serialize only the public X.509 certificate.
- Persist non-secret state as JSON; keep private keys in Keychain.
- Poll only while foregrounded because APNs is unavailable to free Sideloadly signing.

## Task List

### Phase 1: Foundation

- [ ] Add XcodeGen project, icon catalog, package script, and IPA workflow.
- [ ] Add models, persistence, API client, and protocol tests.

### Checkpoint: Foundation

- [ ] Project generation succeeds and simulator tests start.

### Phase 2: Core Features

- [ ] Add RSA certificate generation, Keychain signing, PIN, and Face ID.
- [ ] Add enrollment and provider-management flows.
- [ ] Add foreground polling and login approval/cancellation.
- [ ] Add account detail and remote deletion.

### Checkpoint: Core Features

- [ ] Simulator tests and unsigned device build pass.

### Phase 3: Delivery

- [ ] Verify IPA contents and document Sideloadly installation.
- [ ] Update the project graph.

### Checkpoint: Complete

- [ ] CI publishes a verified `Passi-unsigned.ipa` artifact.

## Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| RSA/X.509 mismatch | High | Match Android algorithms and backend verifier exactly; unit-test DER and signatures. |
| No local iOS toolchain | Medium | Make macOS CI run tests, device build, and IPA inspection. |
| Free signing lacks APNs | Medium | Foreground polling with manual refresh. |

## Open Questions

None.
