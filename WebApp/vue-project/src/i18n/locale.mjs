export const supportedLocales = ['en', 'et', 'ru', 'de', 'fr', 'es']
export const fallbackLocale = 'en'
export const localeStorageKey = 'passi.locale'

export const localeOptions = [
  { code: 'en', label: 'English' },
  { code: 'et', label: 'Eesti' },
  { code: 'ru', label: 'Русский' },
  { code: 'de', label: 'Deutsch' },
  { code: 'fr', label: 'Français' },
  { code: 'es', label: 'Español' },
]

export function normalizeLocale(value) {
  if (typeof value !== 'string') return null
  const code = value.trim().toLowerCase().split(/[-_]/)[0]
  return supportedLocales.includes(code) ? code : null
}

export function detectLocale(storage, browserLanguages = []) {
  try {
    const stored = normalizeLocale(storage?.getItem(localeStorageKey))
    if (stored) return stored
  } catch {
    // Storage may be unavailable in privacy-restricted browser contexts.
  }

  for (const language of browserLanguages) {
    const locale = normalizeLocale(language)
    if (locale) return locale
  }

  return fallbackLocale
}

export function persistLocale(storage, locale) {
  const normalized = normalizeLocale(locale) || fallbackLocale
  try {
    storage?.setItem(localeStorageKey, normalized)
  } catch {
    // Keep the active in-memory locale when storage is unavailable.
  }
  return normalized
}
