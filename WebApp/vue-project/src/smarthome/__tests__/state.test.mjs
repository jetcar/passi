import test from 'node:test'
import assert from 'node:assert/strict'
import { canControl, commandLabels, isOnline, isStale, recent } from '../state.ts'

const now = Date.parse('2026-11-21T12:00:00Z')
const ago = milliseconds => new Date(now - milliseconds).toISOString()
const device = () => ({ id: 'socket', name: 'Socket', online: true, capabilities: ['power.read', 'power.set'], power: false, updatedAt: ago(1000) })
const home = () => ({ id: 'home', name: 'Home', online: true, lastSeenAt: ago(1000), snapshotAt: ago(1000), stale: false, devices: [device()], commands: [] })

test('fresh registered device can be explicitly controlled without changing reported power', () => {
  const item = device()
  assert.equal(canControl(home(), item, now, false), true)
  assert.equal(item.power, false)
})

test('expired online lease and a failed API request immediately disable control', () => {
  assert.equal(isOnline({ ...home(), lastSeenAt: ago(20_000) }, now, false), false)
  assert.equal(canControl({ ...home(), lastSeenAt: ago(20_000) }, device(), now, false), false)
  assert.equal(isStale(home(), device(), now, true), true)
  assert.equal(canControl(home(), device(), now, true), false)
})

test('old snapshot, old reading, explicit stale, offline and missing device availability disable control', () => {
  assert.equal(canControl({ ...home(), snapshotAt: ago(30_000) }, device(), now, false), false)
  assert.equal(canControl(home(), { ...device(), updatedAt: ago(30_000) }, now, false), false)
  assert.equal(canControl({ ...home(), stale: true }, device(), now, false), false)
  assert.equal(canControl({ ...home(), online: false }, device(), now, false), false)
  assert.equal(canControl(home(), { ...device(), online: false }, now, false), false)
  assert.equal(canControl(home(), { ...device(), online: undefined }, now, false), false)
})

test('read-only devices cannot receive power commands', () => {
  assert.equal(canControl(home(), { ...device(), capabilities: ['power.read'] }, now, false), false)
})

test('power readback capability and known power are required before control', () => {
  assert.equal(canControl(home(), { ...device(), capabilities: ['power.set'] }, now, false), false)
  for (const power of [null, undefined]) assert.equal(canControl(home(), { ...device(), power }, now, false), false)
})

test('queued and sent commands block duplicate UI actions until reconciliation', () => {
  for (const status of ['queued', 'sent']) {
    assert.equal(canControl({ ...home(), commands: [{ deviceId: 'socket', status }] }, device(), now, false), false)
  }
  assert.equal(canControl({ ...home(), commands: [{ deviceId: 'another', status: 'sent' }] }, device(), now, false), true)
})

test('unknown outcome is distinct from confirmation and allows only a new explicit action on fresh state', () => {
  assert.match(commandLabels.unknown, /Результат неизвестен/)
  assert.notEqual(commandLabels.sent, commandLabels.confirmed)
  assert.equal(canControl({ ...home(), commands: [{ deviceId: 'socket', status: 'unknown' }] }, device(), now, false), true)
  assert.equal(canControl({ ...home(), commands: [{ deviceId: 'socket', status: 'unknown' }] }, device(), now, true), false)
})

test('missing, malformed and implausibly future timestamps cannot appear fresh', () => {
  for (const value of [null, undefined, '', 'invalid', ago(-6000)]) assert.equal(recent(value, now, 30_000), false)
})
