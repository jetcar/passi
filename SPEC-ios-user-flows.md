# Spec: iOS User Flows

## Objective

Provide native SwiftUI flows for terms, email enrollment, confirmation code, optional PIN/Face ID, accounts, providers, foreground refresh, login challenge approval/cancellation, and remote account deletion.

## Tech Stack

SwiftUI `NavigationStack`, observable application state, async actions.

## Commands

Use the build and test commands in `SPEC-ios-packaging.md`.

## Project Structure

`ios/Passi/Features` contains feature views; navigation and state ownership live in `RootView` and `PassiStore`.

## Code Style

```swift
Button("Approve") {
    Task { await store.approve(session, pin: pin) }
}
.disabled(store.isBusy)
```

## Testing Strategy

Unit-test state helpers and exercise enrollment and login approval against `https://passi.cloud` on a sideloaded device.

## Boundaries

- Always: require the matching number or color before signing.
- Ask first: add QR enrollment, certificate rotation, or device-management screens.
- Never: approve a session after a wrong challenge answer.

## Success Criteria

- A user can enroll and later approve/cancel a login.
- Pending sessions appear during foreground polling.
- A user can manage providers and delete an account.

## Open Questions

None.
