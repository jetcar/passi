# Spec: OpenIDC Localization

## Objective

Localize login, phone approval, client management, account deletion, and OpenIDC navigation without changing protocol parameters or server error contracts.

## Tech Stack

Vue 3 compatibility mode, TypeScript, Vue Router, `vue-i18n`.

## Commands

- Install: `npm install`
- Build/type-check: `npm run build`

## Project Structure

- `OpenIDC/vue-project/src/i18n/` — locale setup and six catalogs.
- `OpenIDC/vue-project/src/App.vue` — selector and shell strings.
- `OpenIDC/vue-project/src/views/` — authentication and management strings.

## Code Style

```ts
const { t } = useI18n()
const waitingLabel = computed(() => t('login.waitingForApproval'))
```

## Testing Strategy

Add catalog parity coverage and run the production build. Verify login query parameters and redirects remain unchanged.

## Boundaries

- Always: preserve form names, OIDC values, redirects, and authorization behavior.
- Ask first: change backend-generated errors.
- Never: translate OAuth/OIDC identifiers or user/device values.

## Success Criteria

- All client-owned OpenIDC strings use translation keys.
- Selector and persisted web preference work before authentication.
- Login and phone approval flows retain existing behavior.
- Build and catalog parity test pass.

## Open Questions

None.
