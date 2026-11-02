# Capability Map: Passi Localization

| Module id | Responsibility | Depends on |
|---|---|---|
| `locale-contract` | Supported locale codes, names, fallback, detection, and persistence behavior | — |
| `webapp-localization` | Localize the public Passi Vue application | `locale-contract` |
| `openidc-localization` | Localize OpenIDC login and account-management Vue screens | `locale-contract` |
| `mailler-localization` | Localize the Mailler React application | `locale-contract` |
| `android-localization` | Localize the Android Compose app and expose an in-app language choice | `locale-contract` |

Build order: `locale-contract` → web modules (independent) → `android-localization` → cross-app verification.
