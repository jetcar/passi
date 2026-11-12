# ADR-0004: Device-held authentication keys

## Status

Proposed — Android implementation independently reviewed; physical-device release validation remains outstanding. No iOS implementation is included: this checkout contains no iOS project.

## Date

2026-11-11

## Threat model

| Attacker | Protection and limits |
| --- | --- |
| Copies application files/backups | New Android account metadata contains no private key. Legacy PKCS#12 plus salt permits offline guesses of the short Passi PIN until migration. Previously stolen copies cannot be erased remotely. |
| Runs malicious code in the application process | Cannot export a hardware key; each signing operation needs platform authorization. Can substitute a challenge before authorization, manipulate UI, or request signatures. This is not transaction display integrity or protection from an approved malicious operation. |
| Roots/jailbreaks device | OS/UI trust may be lost. Hardware enforcement raises the extraction barrier, but platform vulnerabilities, coerced approvals, denial of service, and malicious operations remain possible. No absolute resistance is claimed. |
| Reads backend database | Public certificates do not reveal device private keys. Account metadata is exposed. |
| Compromises server application without device/HSM access | Cannot extract device keys, but can bypass server verification, enroll attacker certificates, alter challenges/recovery, and issue application tokens. Device-held keys do not make a malicious relying server trustworthy. |

## Current lifecycle, verified against this checkout

- Android `BouncyCastleCertificateGenerator` creates exportable RSA-2048 and stores PKCS#12 under salt + optional short PIN in `allAccounts` ordinary preferences.
- `Pkcs12AccountSigner` loads the entire key for SHA512withRSA signatures. `LocalBiometricCertificateService` duplicates it under a random password, with blob and password together in encrypted preferences; a separate UI prompt grants no cryptographic authorization.
- Enrollment sends the public certificate only. Rotation signs the base64 replacement certificate with the old key. Backend verifies RSA SHA-512 / PKCS#1; parent certificates are retained for chain validation.
- Existing account persistence uses asynchronous writes. Deletion removes metadata and biometric copies. Backup is disabled in the manifest; explicit extraction exclusions are needed for device transfer as well.
- No Swift, Xcode, or iOS Keychain implementation is present here; historical iOS behavior from the task cannot be verified or changed in this checkout.

## Decision

Keep the public-certificate challenge protocol. This is device-held-key hardening, not Smart-ID-style threshold signing. No threshold/PAKE protocol is introduced.

On Android 11/API 30 or later, generate a separate RSA-2048 Android Keystore key for every certificate generation. Permit only SIGN, SHA-512 and RSA PKCS#1 signature padding. Prefer StrongBox and fall back to a hardware TEE when the requested suite is unavailable; reject software-only storage. StrongBox's mandatory subset need not include SHA-512. Store alias, public X.509 certificate, thumbprint, hardware level and lifecycle metadata only. Keystore's generated certificate may have an invalid self-signature for an authentication-bound key; the backend treats it as a registered public-key container and does not trust a self-signature as identity evidence.

Require authentication for every signing operation with BIOMETRIC_STRONG OR DEVICE_CREDENTIAL, timeout zero. Initialize a Signature, present that operation in BiometricPrompt.CryptoObject, and complete only the same returned operation. Cancellation, failure, background detachment, or missing operation fails closed. A standalone biometric dialog cannot grant a signing capability. New registration and migration use the same policy. API 28/29 retain legacy access only; creating or migrating a key fails explicitly because the chosen credential/CryptoObject combination requires API 30.

A short custom Passi PIN cannot independently unlock this key. Platform device PIN/password/pattern is the fallback. The old Passi PIN is accepted only to unlock the legacy PKCS#12 during migration. New registration and rotation no longer offer a custom PIN. If independent custom PIN support becomes a product requirement, this decision must be revisited: (a) accept exportable-key offline guessing risk with a memory-hard KDF, which cannot enforce online rate limits; or (b) design a separate server-assisted PAKE/threshold protocol and security review. Do not conceal an exportable key behind local encryption as a substitute.

With DEVICE_CREDENTIAL included, biometric enrollment changes do not invalidate the key; the device credential remains the recovery authorization. Removing/resetting secure lock screen can invalidate it. Lost/invalidated keys require the existing email enrollment/recovery flow; there is no private-key backup or custom PIN bypass. Existing manual signup/code confirmation remains available.

## Migration and failure semantics

Persist the exact pending replacement's public metadata before making a rotation request. Keep the old account PKCS#12 and biometric copies through every ambiguous network result. Unlock the old PKCS#12 with the user's Passi PIN; sign the fixed replacement certificate. Retry that exact replacement after interruption, never generate a different successor for the same pending operation. The backend recognizes an identical already-accepted rotation as success and rejects conflicting successors. The new optional `RetireParent=true` flag changes only the rotation authorization payload to `Passi:RetireParent:v2\n{parentThumbprint}\n{deviceId}\n{publicCert}`. The backend accepts retirement only for a device already linked to the account. SHA-512/RSA PKCS#1 and the login challenge contract are unchanged. This domain separation binds retirement and the target device to approval; unsigned retirement flags, substituted devices, and historical enrollment signatures cannot revoke a parent. Retirement and child enrollment commit together. Retired keys cannot authenticate or authorize device management, but their public records remain available for exact retry validation. A parent used to enroll another device may acquire a new retirement child without disabling that earlier child's key. Parent relationships therefore become one-to-many; the transaction serializes retirement and rejects a second different replacement.

Before each submission, reopen the exact pending key, verify its hardware/authentication policy, public certificate, thumbprint and dates, and initialize signing to detect missing/permanently invalidated keys. Mark the checkpoint as submitted with a durable commit immediately before the request. A failed key that is explicitly unsubmitted may be discarded and regenerated; a submitted checkpoint, or an older checkpoint without this marker, is preserved and requires email recovery because the server may already have accepted it. A successful HTTP status alone is insufficient: require the response's public certificate to match the pending certificate exactly. Redirects are not followed. Authenticity of the configured provider/TLS connection is a precondition; a compromised provider can still lie or bypass verification.

Migration is user-initiated through **Account → Update certificate**, with the legacy Passi PIN entered once in the UI. New and migrated accounts use the same device authorization policy. Legacy accounts keep their previous security model until migration; the details screen labels this explicitly. A checkpoint write interrupted before durability may leave an unused, unregistered Keystore alias. It exposes no private bytes or enrolled credential, but can consume Keystore storage; generation failures themselves delete their aliases.

Only confirmed server acceptance permits an atomic, checked local account metadata write that selects the new alias and clears legacy PKCS#12/salt. Retain a cleanup checkpoint until encrypted and plaintext biometric copies and the obsolete Keystore alias (for later rotations) have been removed. Resume cleanup after restart. No signatures, PINs, passwords or plaintext challenges are journaled or logged. Deletion includes current/pending/cleanup key aliases and both biometric-copy locations. Repeated rotations and canonical account replacement preserve all unfinished cleanup aliases. Checked storage commits publish in-memory state only after success; stale sync/UI snapshots cannot restore an old key or discard pending rotation state.

New enrollment also checkpoints only public key metadata before submission. After process death, reopen the unfinished account; once an email code has been accepted it is retained only until exact certificate/device finalization completes, so a consumed code can resume safely. The server acknowledges only the exact already-confirmed code/certificate/device combination. Email recovery remains an independent enrollment trust path. Existing account/device-management authorization conventions are not redesigned by this change.

## Deployment and rollback

Deploy the retirement schema and retirement-aware code to **all** backend instances before distributing this client. An old backend rejects the new domain-separated rotation signature, so the client retains the old credential. A schema rollback refuses while any certificate is retired; rolling back application binaries would ignore retirement and must not be done after migration starts. Keep a compatible forward fix available. The nullable legacy `PrivateCert` column dates to the initial backend schema and has no current writes. Production data cannot be verified from source, so retain it, reject all new private-material writes (sync/async), and audit existing data separately before any future column deletion.

## iOS constraint

Apple documents that `privateKeyUsage` applies to Secure Enclave keys and fails when used to generate a key outside the enclave. Secure Enclave supports P-256, not RSA. Consequently RSA + Secure Enclave/privateKeyUsage cannot be promised under the unchanged RSA contract. A future iOS change needs actual source and a separate decision between Keychain RSA access controls (different exportability guarantee) and protocol support for enclave P-256. Use an operation-bound LAContext where supported; a prior Face ID dialog alone is insufficient.

## Sources

- [Android Keystore](https://developer.android.com/privacy-and-security/keystore)
- [KeyGenParameterSpec.Builder](https://developer.android.com/reference/android/security/keystore/KeyGenParameterSpec.Builder)
- [BiometricPrompt authentication/CryptoObject](https://developer.android.com/reference/androidx/biometric/BiometricPrompt)
- [Android backup rules](https://developer.android.com/identity/data/autobackup)
- [Apple privateKeyUsage](https://developer.apple.com/documentation/security/secaccesscontrolcreateflags/privatekeyusage)
- [Apple Secure Enclave restrictions](https://developer.apple.com/documentation/security/protecting-keys-with-the-secure-enclave)

## Validation

Automated tests cover migration replay, interrupted local commits, cleanup ordering, no enrollment private material, cancellation, and legacy recovery. The physical-device release matrix in [device key validation](../device-key-validation.md) must check hardware KeyInfo, null private-key encoding, unauthorized signing, authenticated signing and cancellation, credential fallback, biometric enrollment changes, and lock-screen invalidation. Emulator/software-only Keystore cannot prove hardware protection; physical-device results must be reported separately from JVM tests.
