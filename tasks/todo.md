# Passi iPhone Tasks

## Task 1: Packaging foundation

- [x] XcodeGen creates the Passi app and test targets.
- [x] Workflow builds and uploads an unsigned IPA.
- Verify: generate project, run simulator tests, inspect IPA.
- Dependencies: none.

## Task 2: Backend-compatible core

- [x] Models encode the existing upper-camel-case contract.
- [x] Enrollment, polling, approval, cancellation, and deletion endpoints are implemented.
- Verify: focused model and API tests.
- Dependencies: Task 1.

## Task 3: Device security

- [x] RSA-2048 certificates and SHA-512 login signatures match the backend.
- [x] Private keys stay in Keychain; PIN and Face ID gates work.
- Verify: security unit tests and physical-device check.
- Dependencies: Task 2.

## Task 4: Enrollment and providers

- [x] Enrollment completes from terms through certificate registration.
- [x] Providers can be added, edited, and deleted.
- Verify: simulator UI build and device enrollment.
- Dependencies: Tasks 2-3.

## Task 5: Login approval

- [x] Foreground polling presents pending sessions.
- [x] Correct challenges approve; incorrect challenges never sign; cancellation works.
- Verify: tests and real Passi login.
- Dependencies: Tasks 2-4.

## Task 6: Account management and delivery

- [x] Account details and remote deletion work.
- [x] README documents artifact download and Sideloadly install.
- Verify: complete CI workflow and device install.
- Dependencies: Tasks 1-5.
