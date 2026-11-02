import assert from 'node:assert/strict'
import test from 'node:test'
import { createI18n } from 'vue-i18n'
import { messages } from '../src/i18n/messages.mjs'
import {
  detectLocale,
  fallbackLocale,
  localeStorageKey,
  normalizeLocale,
  supportedLocales,
} from '../src/i18n/locale.mjs'

function flatten(value, prefix = '', result = new Map()) {
  for (const [key, child] of Object.entries(value)) {
    const path = prefix ? `${prefix}.${key}` : key
    if (child && typeof child === 'object' && !Array.isArray(child)) flatten(child, path, result)
    else result.set(path, child)
  }
  return result
}

test('every locale catalog has exactly the English keys and non-empty values', () => {
  const english = flatten(messages.en)
  assert.ok(english.size > 0)

  for (const locale of supportedLocales) {
    const catalog = flatten(messages[locale])
    assert.deepEqual([...catalog.keys()].sort(), [...english.keys()].sort(), `${locale} keys differ from English`)
    for (const [key, value] of catalog) {
      assert.ok(typeof value === 'string' && value.trim(), `${locale}.${key} is empty`)
    }
  }
})

test('stored locale wins over browser locale', () => {
  const storage = { getItem: key => key === localeStorageKey ? 'de' : null }
  assert.equal(detectLocale(storage, ['et-EE']), 'de')
})

test('browser locale is normalized and unknown locales fall back to English', () => {
  const storage = { getItem: () => null }
  assert.equal(detectLocale(storage, ['fr-CA']), 'fr')
  assert.equal(detectLocale(storage, ['ja-JP']), fallbackLocale)
  assert.equal(normalizeLocale('ES_es'), 'es')
})

test('vue-i18n uses English when the active locale is unsupported', () => {
  const i18n = createI18n({ legacy: false, locale: 'ja', fallbackLocale, messages })
  assert.equal(i18n.global.t('navigation.home'), 'Home')
})
