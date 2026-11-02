import { createI18n } from 'vue-i18n'
import { messages } from './messages.mjs'
import {
  detectLocale,
  fallbackLocale,
  localeOptions,
  persistLocale,
  supportedLocales,
} from './locale.mjs'

const browserLanguages = typeof navigator === 'undefined'
  ? []
  : [...(navigator.languages || []), navigator.language]
const storage = (() => {
  if (typeof window === 'undefined') return undefined
  try {
    return window.localStorage
  } catch {
    return undefined
  }
})()
const initialLocale = detectLocale(storage, browserLanguages)

export const i18n = createI18n({
  legacy: false,
  locale: initialLocale,
  fallbackLocale,
  messages,
})

export function setLocale(locale: string) {
  const nextLocale = persistLocale(storage, locale)
  i18n.global.locale.value = nextLocale as typeof i18n.global.locale.value
  if (typeof document !== 'undefined') document.documentElement.lang = nextLocale
}

if (typeof document !== 'undefined') document.documentElement.lang = initialLocale

export { fallbackLocale, localeOptions, supportedLocales }
