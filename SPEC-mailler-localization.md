# Spec: Mailler Localization

## Objective

Localize the Mailler application shell, login, inbox, message actions, compose flow, imports, and settings. Email content and folder/account names remain unchanged.

## Tech Stack

React 18, React Router, `i18next`, `react-i18next`.

## Commands

- Install: `npm install`
- Test: `npm test`
- Build: `npm run build`

## Project Structure

- `mailler/frontend/src/i18n/` — initialization and six catalogs.
- `mailler/frontend/src/App.jsx` — selector and shell.
- `mailler/frontend/src/components/` — translated feature strings.

## Code Style

```jsx
const { t } = useTranslation()
return <button>{t('compose.send')}</button>
```

## Testing Strategy

Add catalog parity and locale-selection tests, preserve existing mobile tests, then build.

## Boundaries

- Always: localize client-owned labels, status text, and accessibility text.
- Ask first: localize backend errors.
- Never: translate email content, email addresses, folder names, or account names.

## Success Criteria

- All client-owned Mailler strings use translation keys.
- Selector persists through `passi.locale`.
- Dates use the active locale.
- Tests and build pass.

## Open Questions

None.
