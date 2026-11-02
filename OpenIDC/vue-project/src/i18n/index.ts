import { createI18n } from 'vue-i18n'

import de from './locales/de.json'
import en from './locales/en.json'
import es from './locales/es.json'
import et from './locales/et.json'
import fr from './locales/fr.json'
import ru from './locales/ru.json'

export const supportedLocales = ['en', 'et', 'ru', 'de', 'fr', 'es'] as const
export type SupportedLocale = (typeof supportedLocales)[number]
export const fallbackLocale: SupportedLocale = 'en'
export const localeStorageKey = 'passi.locale'

function normalizeLocale(value?: string | null): SupportedLocale | null {
    const language = value?.trim().toLowerCase().split(/[-_]/)[0]
    return supportedLocales.find((locale) => locale === language) ?? null
}

export function detectLocale(): SupportedLocale {
    try {
        const storedLocale = normalizeLocale(window.localStorage.getItem(localeStorageKey))
        if (storedLocale) return storedLocale
    }
    catch {
        // Storage can be unavailable in privacy-restricted browser contexts.
    }

    const browserLanguages = typeof navigator === 'undefined'
        ? []
        : [...(navigator.languages ?? []), navigator.language]

    for (const language of browserLanguages) {
        const locale = normalizeLocale(language)
        if (locale) return locale
    }

    return fallbackLocale
}

const i18n = createI18n({
    legacy: false,
    globalInjection: true,
    locale: detectLocale(),
    fallbackLocale,
    messages: { de, en, es, et, fr, ru },
})

export function setLocale(value: string): void {
    const locale = normalizeLocale(value) ?? fallbackLocale
    i18n.global.locale.value = locale
    document.documentElement.lang = locale

    try {
        window.localStorage.setItem(localeStorageKey, locale)
    }
    catch {
        // The active locale still applies when persistence is unavailable.
    }
}

export default i18n
