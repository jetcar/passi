# Passi iPhone Tasks

## Task 1: Packaging foundation

- [ ] XcodeGen creates the Passi app and test targets.
- [ ] Workflow builds and uploads an unsigned IPA.
- Verify: generate project, run simulator tests, inspect IPA.
- Dependencies: none.

## Task 2: Backend-compatible core

- [ ] Models encode the existing upper-camel-case contract.
- [ ] Enrollment, polling, approval, cancellation, and deletion endpoints are implemented.
- Verify: focused model and API tests.
- Dependencies: Task 1.

## Task 3: Device security

- [ ] RSA-2048 certificates and SHA-512 login signatures match the backend.
- [ ] Private keys stay in Keychain; PIN and Face ID gates work.
- Verify: security unit tests and physical-device check.
- Dependencies: Task 2.

## Task 4: Enrollment and providers

- [ ] Enrollment completes from terms through certificate registration.
- [ ] Providers can be added, edited, and deleted.
- Verify: simulator UI build and device enrollment.
- Dependencies: Tasks 2-3.

## Task 5: Login approval

- [ ] Foreground polling presents pending sessions.
- [ ] Correct challenges approve; incorrect challenges never sign; cancellation works.
- Verify: tests and real Passi login.
- Dependencies: Tasks 2-4.

## Task 6: Account management and delivery

- [ ] Account details and remote deletion work.
- [ ] README documents artifact download and Sideloadly install.
- Verify: complete CI workflow and device install.
- Dependencies: Tasks 1-5.
