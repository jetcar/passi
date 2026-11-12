# Device-held key release validation

The Android/backend slice implements [ADR-0004](decisions/0004-device-held-authentication-keys.md). These hardware checks remain a release gate; JVM fakes and an emulator do not prove hardware enforcement.

## Automated coverage

- `KeystoreAccountSignerTest`: failed/cancelled authorization cannot sign; the returned operation must be identical; authorized signature verifies using SHA-512/RSA.
- `DeviceKeyMigrationTests`: durable checkpoint before network, exact retries after HTTP failure/lost response/failed local commit, invalid legacy PIN, invalidated pending replacement, wrong/missing acknowledgment, redirects, stale screen, retained cleanup aliases and secret-free HTTP logging.
- `PersistentKeyLifecycleTests`: failed commits do not publish state; restart cleanup; failed cleanup checkpoint retries; alias deletion; canonical recovery and stale-snapshot protection.
- `CertificateRotationTests`: real database transactions, replay/concurrency/conflicts, retired authentication rejection, existing-device preservation, signup retry, private-field guard and rollback refusal.
- `E2EFlowTests`: a public-only new registration signs a real login with its retained test key; a legacy registration migrates through the real Docker backend, retries the exact request, rejects the retired key and accepts the replacement.
- `DeviceHeldKeyTest`: missing screen lock fails generation and removes the failed entry; missing checkpoint key fails validation. With physical-device prerequisites, verifies null private-key encoding/format, hardware/authentication policy, metadata mismatch rejection and denied unauthenticated signing.

## Physical-device matrix — not yet executed

Use isolated test accounts and test devices. Do not capture PINs, signatures, private blobs or challenges in logs/screenshots. Test on API 30 TEE and a current StrongBox device (SHA-512 fallback may use TEE).

| Scenario | Required result |
| --- | --- |
| New email registration | Ordinary/encrypted preferences contain public metadata and alias only; private-key encoding is null. |
| Legacy account discovery | A persistent account-list notice and row label appear for active confirmed PKCS#12 accounts only; the action selects the account and opens migration. The notice remains until all such accounts migrate. |
| Strong biometric approval | A fresh CryptoObject operation signs and the actual backend verifies the unchanged challenge contract. |
| Device credential fallback | Credential UI authorizes that same operation; no app PIN field or cached authorization bypass. |
| Cancel/back/failed biometric | No authorize HTTP request and no signature; a new attempt presents a new authorization prompt. |
| Rotate activity/kill app during prompt | Pending operation fails; restart requires fresh authorization. Switching into system credential UI must still work. |
| Invoke signer while device unlocked, without prompt | Keystore rejects signing. Copying app files to another device cannot sign. |
| Add/remove biometric enrollment | Key remains usable with device credential; per-use authorization remains required. This policy intentionally does not invalidate on biometric enrollment changes. |
| Remove/reset secure screen lock | Existing key cannot silently sign. A pending migration is not submitted with an invalidated replacement. Email recovery remains available. |
| Kill before rotation request | Old credential and fixed pending replacement remain; retry is safe. |
| Drop response after server commit | Parent is retired; retry submits the same replacement and receives exact acknowledgment; local metadata then advances. |
| Kill after local metadata commit, before cleanup | New key remains selected; startup removes old biometric copies and all obsolete aliases. |
| Fail cleanup, rotate again, restart | All unfinished old aliases remain journaled and are eventually deleted. |
| Delete account | Current, pending and cleanup aliases and biometric copies are removed. |
| Recover via email on same device | Exact confirmed enrollment retries succeed; old local aliases are cleaned only after replacement is durably selected. |
| API 28/29 or software-only Keystore | New registration/migration fails clearly; an existing legacy account retains access until it can migrate on supported hardware. |

## Rollout

Apply the database migration and retirement-aware code to every backend instance first. Never roll back to binaries that ignore `IsRetired` after migration begins. Schema downgrade refuses retired rows. Keep the existing email recovery channel available. No iOS implementation is included because this repository has no iOS project and the requested RSA/privateKeyUsage combination is unsupported by Apple.
