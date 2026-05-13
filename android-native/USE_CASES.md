# Passi App Use Cases

This document describes the end-user use cases supported by the Passi mobile application, based on the existing MAUI app behavior and the current Android-native rewrite.

## Scope

The application is an Android client for Passi. It lets a user enroll one or more accounts against configured Passi providers, keep those accounts available on-device, and approve backend authentication sessions using a certificate protected by optional PIN and biometric checks.

## Use Cases

### 1. Open The App And See Existing Accounts

The user opens the app and sees all locally stored accounts together with their provider association and confirmation state.

Expected behavior:
- Load saved providers and accounts from local storage.
- Show whether each account is confirmed or still waiting for enrollment completion.
- Show whether each account is active or inactive.
- Make provider management and account enrollment available from the main screen.

### 2. Add A New Provider

The user adds a custom Passi provider endpoint instead of relying only on the default provider.

Expected behavior:
- Open the provider editor.
- Enter a provider name, base URL, and endpoint paths.
- Save the provider for later use in account enrollment and account operations.

### 3. Edit An Existing Provider

The user adjusts an existing provider configuration.

Expected behavior:
- Open provider details.
- Change display name, base URL, or API paths.
- Save the updated provider definition.

### 4. Delete A Provider

The user removes a provider that is no longer needed.

Expected behavior:
- Open provider details.
- Remove the provider from local storage.
- Keep remaining providers available.

### 5. Start Account Enrollment

The user begins registration of a new Passi account against a selected provider.

Expected behavior:
- Accept the enrollment terms.
- Enter an email address.
- Select a provider.
- Call the signup endpoint.
- Store a pending local account record.
- Continue into confirmation-code verification.

### 6. Confirm Enrollment Code

The user finishes the email verification step of enrollment.

Expected behavior:
- Enter the confirmation code received out-of-band.
- Call the signup-check endpoint.
- If valid, continue to certificate creation and final signup confirmation.

### 7. Complete Enrollment With Certificate Creation

The user finalizes account registration on the device.

Expected behavior:
- Optionally choose a PIN for local certificate protection.
- Generate a self-signed client certificate.
- Protect the exported PKCS#12 using the generated salt and optional PIN.
- Send the public certificate to the backend.
- Mark the account as confirmed.

### 8. Resume An Unfinished Enrollment

The user has a pending account that was not fully confirmed or completed earlier.

Expected behavior:
- Keep the pending account in local storage.
- Allow the user to reopen the account and continue the enrollment flow.

### 9. View Account Details

The user inspects the details of a confirmed account.

Expected behavior:
- Show email, provider name, certificate thumbprint, validity range, confirmation state, and biometric status.
- Offer actions such as certificate update, biometric enablement, and remote delete.

### 10. Delete An Account Remotely And Locally

The user removes an enrolled account.

Expected behavior:
- Call the backend delete endpoint with the account identifier and certificate thumbprint.
- Remove the local account record after successful backend deletion.
- Remove any stored biometric certificate material for that account.

### 11. Manually Sync Account State

The user requests a refresh of local account state from the backend.

Expected behavior:
- Call the sync endpoint for the device and configured accounts.
- Mark accounts inactive if they no longer exist on the server.
- Refresh local state without requiring re-enrollment.

### 12. Automatically Refresh While The App Is In Foreground

The app keeps account and pending-session state fresh while it is actively open.

Expected behavior:
- Trigger sync and pending-session checks when the accounts screen is active.
- Repeat checks while the screen stays in the started lifecycle state.
- Avoid requiring background polling when push notifications are available.

### 13. Receive An Authentication Request By Notification

The backend sends a push notification when a login approval is required.

Expected behavior:
- Register the device notification token with configured providers.
- Receive the push notification.
- Open the app when the notification arrives.
- Trigger an immediate session sync so the pending session can be loaded.

### 14. Open A Pending Authentication Session

The user enters the approval flow for a session that is waiting for authorization.

Expected behavior:
- Load the pending session for the correct account.
- Show requester information and return host.
- Show a three-color challenge that includes the expected confirmation color.

### 15. Approve A Session With No PIN

The user has a confirmed account without PIN protection.

Expected behavior:
- Choose the correct confirmation color.
- Sign the backend random string using the stored certificate.
- Send the signed hash and certificate thumbprint to the authorize endpoint.

### 16. Approve A Session With PIN

The user has a confirmed account protected by a PIN.

Expected behavior:
- Choose the correct confirmation color.
- Enter the account PIN.
- Sign the backend random string with the PIN-protected certificate.
- Send the authorization response to the backend.

### 17. Approve A Session With Biometric

The user has biometric approval enabled for an account.

Expected behavior:
- Choose the correct confirmation color.
- Confirm a biometric prompt instead of entering the PIN when allowed.
- Use the locally stored biometric certificate copy to sign the backend random string.
- Send the authorization response to the backend.

### 18. Cancel A Pending Authentication Session

The user decides not to approve the current session.

Expected behavior:
- Call the backend cancel endpoint for the session.
- Clear the local pending-session state.
- Return to the main app screen.

### 19. Enable Biometric Approval For An Account

The user opts into biometric approval on a device that supports enrolled biometrics.

Expected behavior:
- Confirm a biometric prompt.
- If the account uses a PIN, ask for the current PIN to access the existing certificate.
- Re-export certificate material for biometric-backed local use.
- Mark the account as biometric-enabled.

### 20. Rotate A Certificate Without Changing Security Mode

The user updates the certificate associated with an account while keeping the same basic local security mode.

Expected behavior:
- Generate a new self-signed certificate.
- Sign the new public certificate with the currently enrolled certificate.
- Send the certificate-update request to the backend.
- Store the updated certificate and metadata locally.

### 21. Rotate A Certificate And Set A New PIN

The user changes the certificate and changes local PIN protection at the same time.

Expected behavior:
- Validate the old PIN when required.
- Validate the new PIN and confirmation.
- Generate a new certificate protected by the new PIN.
- Update the backend and local certificate state.

### 22. Rotate A Certificate Using Biometric For The Current Certificate

The user has biometric approval enabled and wants to use biometric confirmation instead of the old PIN during certificate rotation.

Expected behavior:
- Confirm a biometric prompt for the currently enrolled certificate.
- Generate the replacement certificate.
- Sign the new public certificate using the biometric-backed certificate copy.
- Update backend and local state.

### 23. Register In Mailler By Username And Confirm In Passi By Scanning A QR Code

The user starts registration from Mailler using only a username. Instead of entering a verification code manually in the Passi app, Mailler shows a QR code that the mobile app can scan. Scanning the QR code should act like successful verification-code confirmation and continue directly into certificate creation and Passi API registration.

Expected behavior:
- The user starts registration in Mailler with a username.
- Mailler creates a short-lived registration payload and renders it as a QR code.
- The Passi mobile app opens a QR scanner flow.
- The app scans the QR code and validates the payload against the backend.
- The scan result is treated as equivalent to successful confirmation-code verification.
- The app generates a client certificate on-device.
- The app registers the public certificate with Passi API for the corresponding account.
- The account becomes confirmed and available for later approval flows.

Required integration points:
- A Mailler backend or frontend flow that can generate the QR payload.
- A stable payload contract shared between Mailler, the Android app, and Passi API.
- An Android QR-scanning flow in the native app.
- A Passi API endpoint or registration bridge that accepts QR-confirmed enrollment.

## Use Cases Not Covered By The Current Native Rewrite

These are application-level concerns that exist in the broader product space or old codebase discussions but are not fully implemented in the current Android-native rewrite yet.

- Full real-device Firebase configuration validation.
- Any iOS, macOS, or Tizen behavior.
- Data migration from MAUI secure storage into the native Android app.
- Any future push-flow refinements beyond opening the app and syncing immediately.
- The Mailler username-plus-QR registration flow described above.

## Summary

At a user-flow level, the application supports four main areas:

- Provider management.
- Account enrollment and account maintenance.
- Authentication-session approval through certificate signing.
- Notification-driven entry back into the app for pending approvals.