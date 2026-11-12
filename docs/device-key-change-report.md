# Device-held authentication key change report

## Outcome

Android/API30+ and backend implementation, independently reviewed. Physical-device release validation remains incomplete; this is not a production deployment. The iOS source is absent, and Apple does not support RSA Secure Enclave/privateKeyUsage under the existing signature contract.

New registrations and migrated accounts use non-exportable hardware-held RSA keys with per-operation strong biometric/device-credential authorization. Active legacy accounts show a persistent account-list security notice and direct Update certificate action until all are migrated. Pending public metadata is durable before submission; exact server certificate acknowledgment and checked local commit precede deletion. Backend retirement blocks copied old keys, supports identical retries, preserves other devices, and prevents unsafe schema rollback. Failed/ambiguous requests retain the old material and exact replacement checkpoint.

See [ADR-0004](decisions/0004-device-held-authentication-keys.md) for threat model, platform policy, key lifecycle, recovery, residual risks and official documentation; see [physical-device matrix](device-key-validation.md) for outstanding release checks.

## Verification

| Check | Result |
| --- | --- |
| Android `:app:testDebugUnitTest` | 113 passed, 0 failed/skipped; includes migration-notice selection and exclusion of device-held accounts. |
| Docker backend E2E | 2 passed: public-only new registration/login; legacy migration exact retry, retired-key rejection and replacement-key login. |
| `dotnet test PassiWebApiTests/PassiWebApiTests.csproj` | 76 passed, including transactional rotation, concurrent retry, retired-key rejection, enrollment retry and guarded rollback. |
| `dotnet test ServicesTests/ServicesTests.csproj` | 31 passed. |
| EF pending model changes | None; migration and model agree. |
| Android main/instrumentation compilation | Passed. |
| Android `:app:assembleRelease` | Passed, including R8 and release vital lint. |
| Android `:app:lintDebug` | Fails on 3 pre-existing errors: notification MissingPermission at PassiNotifications.kt:54,80; ContextCastToActivity at PassiNavGraph.kt:33. 43 warnings. No remaining new generator API errors. |
| Emulator | No-screen-lock generation rejection/entry cleanup passed. Hardware test prerequisite unmet. Subsequent missing-key test could not run because emulator disconnected. |
| Independent security review | Two rounds; actionable findings corrected, scoped re-review found none remaining. |
| `git diff --check` | Passed. |
| `graphify update .` | Completed AST update; existing BiometricPromptLauncher.kt extractor syntax warning (compiler passes). |

## Limits and rollout

- Release is gated on real hardware positive authentication, cancellation, credential fallback, biometric changes and lock-reset tests. JVM tests do not establish those properties.
- All backend instances and schema must be retirement-aware before client rollout. Do not roll back binaries that ignore IsRetired.
- API28/29 and software-only Keystore cannot create new hardened accounts; existing legacy accounts retain access until migration on supported hardware.
- A failed first checkpoint commit may leave an unregistered unused Keystore alias. It cannot authenticate an enrolled account but consumes local key storage.
- Legacy PrivateCert column retained because production contents cannot be verified from source. New private-material writes are rejected.
- No custom Passi PIN fallback is provided for device-held keys. Platform credential and existing email recovery are the selected recovery methods. No independent PIN product decision blocks this Android slice.
- No iOS changes or production deployment were made.

## Exact repository files changed

- `Models/CertificateDb.cs`
- `PassiWebApiTests/Controllers/CertificateRotationTests.cs`
- `Repos/CertificateRepository.cs`
- `Repos/Migrations/20261111195656_retire_migrated_certificate.Designer.cs`
- `Repos/Migrations/20261111195656_retire_migrated_certificate.cs`
- `Repos/Migrations/PassiDbContextModelSnapshot.cs`
- `Repos/PassiDbContext.cs`
- `Repos/SessionsRepository.cs`
- `Repos/UserRepository.cs`
- `Services/CertValidator.cs`
- `Services/CertificatesService.cs`
- `Services/MyRestClient.cs`
- `ServicesTests/CertificatesServiceTests.cs`
- `WebApiDto/Certificate/CertificateDto.cs`
- `android-native/.kotlin/sessions/kotlin-compiler-4368898427773477804.salive`
- `android-native/README.md`
- `android-native/USE_CASES.md`
- `android-native/app/proguard-rules.pro`
- `android-native/app/src/androidTest/java/com/passi/cloud/passi_android/DeviceHeldKeyTest.kt`
- `android-native/app/src/androidTest/java/com/passi/cloud/passi_android/ButtonCoverageTest.kt`
- `android-native/app/src/main/AndroidManifest.xml`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/MainActivity.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/PassiAppContainer.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/auth/BackendAuthSessionService.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/auth/KeystoreAccountSigner.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/auth/Pkcs12AccountSigner.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/biometric/LocalBiometricCertificateService.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/certificate/BackendCertificateRotationService.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/crypto/AndroidKeystoreCertificateGenerator.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/crypto/BouncyCastleCertificateGenerator.kt` (removed from production; retained as a legacy test fixture)
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/local/AccountStorage.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/local/PassiPreferences.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/remote/PassiApiClient.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/data/repository/PersistentAccountsRepository.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/domain/auth/AccountSigner.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/domain/biometric/BiometricCertificateService.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/domain/enrollment/CertificateGenerator.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/domain/enrollment/DeviceKeyMetadata.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/domain/enrollment/GeneratedCertificate.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/domain/model/Account.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/domain/repository/AccountsRepository.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/feature/account/AccountDetailRoute.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/feature/accounts/AccountsViewModel.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/feature/accounts/AccountsRoute.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/feature/auth/SessionChallengeViewModel.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/feature/certificate/UpdateCertificateRoute.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/feature/certificate/UpdateCertificateViewModel.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/feature/enrollment/ConfirmCodeViewModel.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/feature/enrollment/FinishEnrollmentRoute.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/feature/enrollment/FinishEnrollmentViewModel.kt`
- `android-native/app/src/main/java/com/passi/cloud/passi_android/ui/DeviceKeyAuthorization.kt`
- `android-native/app/src/main/res/values-de/strings.xml`
- `android-native/app/src/main/res/values-es/strings.xml`
- `android-native/app/src/main/res/values-et/strings.xml`
- `android-native/app/src/main/res/values-fr/strings.xml`
- `android-native/app/src/main/res/values-ru/strings.xml`
- `android-native/app/src/main/res/values/strings.xml`
- `android-native/app/src/main/res/xml/backup_rules.xml`
- `android-native/app/src/main/res/xml/data_extraction_rules.xml`
- `android-native/app/src/test/java/com/passi/cloud/passi_android/ApprovalAndSyncUseCaseTests.kt`
- `android-native/app/src/test/java/com/passi/cloud/passi_android/BackendServicesIntegrationTests.kt`
- `android-native/app/src/test/java/com/passi/cloud/passi_android/CertificateUseCaseTests.kt`
- `android-native/app/src/test/java/com/passi/cloud/passi_android/DeviceKeyMigrationTests.kt`
- `android-native/app/src/test/java/com/passi/cloud/passi_android/EnrollmentUseCaseTests.kt`
- `android-native/app/src/test/java/com/passi/cloud/passi_android/NotificationUseCaseTests.kt`
- `android-native/app/src/test/java/com/passi/cloud/passi_android/PersistentKeyLifecycleTests.kt`
- `android-native/app/src/test/java/com/passi/cloud/passi_android/TestSupport.kt`
- `android-native/app/src/test/java/com/passi/cloud/passi_android/data/auth/KeystoreAccountSignerTest.kt`
- `android-native/app/src/test/java/com/passi/cloud/passi_android/data/crypto/BouncyCastleCertificateGenerator.kt`
- `android-native/e2e/src/main/kotlin/com/passi/cloud/passi_android/domain/enrollment/GeneratedCertificate.kt`
- `android-native/e2e/src/test/kotlin/com/passi/cloud/passi_android/E2EFlowTests.kt`
- `docs/decisions/0004-device-held-authentication-keys.md`
- `docs/device-key-change-report.md`
- `docs/device-key-validation.md`
- `passiwebapi/Controllers/SignUpController.cs`
