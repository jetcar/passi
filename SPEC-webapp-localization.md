# Spec: Public WebApp Localization

## Objective

Localize navigation, footer, public pages, account pages, OAuth app management, news chrome, reactions, and admin UI. News titles and bodies remain authored content.

## Tech Stack

Vue 3, TypeScript/JavaScript, Vue Router, `vue-i18n`.

## Commands

- Install: `npm install`
- Build/type-check: `npm run build`
- Lint: `npm run lint`

## Project Structure

- `WebApp/vue-project/src/i18n/` — locale setup and six catalogs.
- `WebApp/vue-project/src/App.vue` — language selector and translated shell.
- `WebApp/vue-project/src/views/`, `components/` — translated UI strings.

## Code Style

```vue
<span>{{ $t('navigation.home') }}</span>
```

## Testing Strategy

Add a catalog parity test, then run type-check/build. Manually verify selector persistence and responsive navigation.

## Boundaries

- Always: localize accessibility labels and errors owned by the client.
- Ask first: translate published news content.
- Never: translate server-returned free text.

## Success Criteria

- All client-owned visible strings use translation keys.
- Selector is available in the header and works on mobile and desktop.
- Dates format using the active locale.
- Build and catalog parity test pass.

## Open Questions

None.
