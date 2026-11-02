# Localization Tasks

## Task 1: WebApp locale foundation

- [x] Add `vue-i18n`, locale detection/persistence, six catalogs, and catalog parity tests.
- Verify: `npm run build` and focused catalog test.
- Dependencies: `locale-contract`.
- Files: WebApp package files and `src/i18n/`.

## Task 2: WebApp public shell and pages

- [x] Translate header, footer, selector, home, products, about, contacts, and privacy UI.
- Verify: WebApp build and mobile shell check.
- Dependencies: Task 1.
- Files: `App.vue` and public views, split into commits of at most five files.

## Task 3: WebApp account and news UI

- [x] Translate user info, OAuth apps, news chrome/admin, reactions, and locale-aware dates.
- Verify: WebApp tests/build.
- Dependencies: Task 1.
- Files: account/news views, component, and date helper, split into small commits.

## Task 4: OpenIDC locale foundation

- [x] Add `vue-i18n`, six catalogs, selector, persistence, and parity test.
- Verify: OpenIDC build.
- Dependencies: `locale-contract`.
- Files: OpenIDC package files, `src/i18n/`, and `App.vue`.

## Task 5: OpenIDC views

- [x] Translate login/approval, account deletion, home, and client-management views.
- Verify: OpenIDC build and auth parameter audit.
- Dependencies: Task 4.
- Files: OpenIDC views, split into commits of at most five files.

## Task 6: Mailler locale foundation

- [x] Add i18next libraries, six catalogs, selector, persistence, and tests.
- Verify: `npm test` and `npm run build`.
- Dependencies: `locale-contract`.
- Files: Mailler package files, `src/i18n/`, and `App.jsx`.

## Task 7: Mailler components

- [x] Translate login, inbox, list/detail, compose, import, progress, and settings UI.
- Verify: Mailler tests/build and locale-aware dates.
- Dependencies: Task 6.
- Files: Mailler components, split into commits of at most five files.

## Task 8: Android locale foundation

- [x] Add per-app locale support, locale config, six resource sets, selector, and resource tests.
- Verify: Android unit tests and debug build.
- Dependencies: `locale-contract`.
- Files: Gradle/manifest/config, locale utility, base resources, tests.

## Task 9: Android core screens

- [x] Resource enrollment, account, device, and provider screens.
- Verify: Android unit tests/build after each screen group.
- Dependencies: Task 8.
- Files: Compose routes and resource catalogs, split into small commits.

## Task 10: Android auth and system UI

- [x] Resource auth, certificate, update, notification, and biometric strings.
- Verify: unit tests, instrumentation compile, debug build.
- Dependencies: Task 8.
- Files: feature routes/services and resource catalogs, split into small commits.

## Task 11: Cross-app audit

- [x] Audit hardcoded user-visible strings and verify fallback/persistence.
- Verify: all web builds/tests and Android tests/build.
- Dependencies: Tasks 2, 3, 5, 7, 9, 10.

## Task 12: Graph and delivery

- [x] Run `graphify update .`, verify clean diffs, and prepare atomic commits.
- Dependencies: Task 11.
