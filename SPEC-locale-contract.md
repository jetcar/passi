# Spec: Locale Contract

## Objective

Define one language contract for all Passi user interfaces. Supported locales are English (`en`), Estonian (`et`), Russian (`ru`), German (`de`), French (`fr`), and Spanish (`es`). English is the fallback.

## Tech Stack

BCP 47 language tags, browser language APIs, web storage, and Android locale resources.

## Commands

- Search hardcoded strings: `rtk rg -n "Text\\(\"|>[^<{]*[A-Za-z][^<{]*<" WebApp/vue-project/src OpenIDC/vue-project/src mailler/frontend/src android-native/app/src/main`

## Project Structure

- Each web project owns its translation catalogs and locale adapter.
- Android owns translated `res/values-<locale>/strings.xml` resources.
- All web projects use local storage key `passi.locale`.

## Code Style

```ts
export const supportedLocales = ['en', 'et', 'ru', 'de', 'fr', 'es'] as const
export const fallbackLocale = 'en'
```

## Testing Strategy

Validate every catalog against English keys, verify fallback behavior, and build each consumer.

## Boundaries

- Always: preserve stable locale codes and English fallback.
- Ask first: add languages or localize server-owned/user-authored content.
- Never: machine-translate news posts, email bodies, usernames, provider names, or API payloads.

## Success Criteria

- Every app exposes the six approved languages.
- Browser/device language is used when no preference exists.
- A user selection persists and takes precedence.
- Unknown or incomplete locales fall back to English.

## Open Questions

None.
