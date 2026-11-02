import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import { dirname, resolve } from 'node:path'
import test from 'node:test'
import { fileURLToPath } from 'node:url'

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const localeDirectory = resolve(projectRoot, 'src/i18n/locales')
const localeCodes = ['en', 'et', 'ru', 'de', 'fr', 'es']

function flatten(value, prefix = '') {
    return Object.entries(value).flatMap(([key, child]) => {
        const path = prefix ? `${prefix}.${key}` : key
        return child && typeof child === 'object' && !Array.isArray(child)
            ? flatten(child, path)
            : [[path, child]]
    })
}

async function readCatalog(locale) {
    return JSON.parse(await readFile(resolve(localeDirectory, `${locale}.json`), 'utf8'))
}

test('all supported locale catalogs match the English catalog', async () => {
    const englishEntries = flatten(await readCatalog('en'))
    const englishKeys = englishEntries.map(([key]) => key).sort()

    for (const locale of localeCodes) {
        const entries = flatten(await readCatalog(locale))
        assert.deepEqual(entries.map(([key]) => key).sort(), englishKeys, `${locale} catalog keys differ from English`)
        for (const [key, value] of entries) {
            assert.equal(typeof value, 'string', `${locale}.${key} must be a string`)
            assert.notEqual(value.trim(), '', `${locale}.${key} must not be empty`)
        }
    }
})
