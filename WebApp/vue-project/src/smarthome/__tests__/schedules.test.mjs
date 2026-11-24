import test from 'node:test'
import assert from 'node:assert/strict'
import { canAdvanceRevision, commandAction, configurationFingerprint, draftChanged, observeSchedule, reloadDraft, scheduleCommandStatus, scheduleFresh, scheduleKey, scheduleSnapshotStale, validateSchedule } from '../schedules.ts'

const now = Date.parse('2026-11-24T10:00:00Z')
const timestamp = new Date(now - 1000).toISOString()
const interval = (changes = {}) => ({ id: '11111111-1111-4111-8111-111111111111', name: '', enabled: true, days: [1], start: '06:00', end: '09:00', on: false, ...changes })
const snapshot = (version = 2) => ({ deviceId: 'socket', snapshotAt: timestamp, stale: false,
  configuration: { version, deviceId: 'socket', enabled: false, timeZone: 'Europe/Tallinn', outsidePower: null, intervals: [interval()] },
  runtime: { revision: 5, state: 'manual', actualPower: false, desiredPower: null, manualActive: true, manualUntil: null } })
const device = { id: 'socket', online: true, power: false, capabilities: ['power.read', 'power.set'], updatedAt: timestamp }
const home = { id: 'a', online: true, stale: false, lastSeenAt: timestamp, snapshotAt: timestamp, devices: [device], commands: [] }

test('polling preserves draft edits, false intervals and null outside behavior', () => {
  const drafts = new Map(), original = snapshot()
  observeSchedule(drafts, 'a', original, [])
  const draft = drafts.get(scheduleKey('a', 'socket'))
  draft.configuration.intervals[0].name = 'Мой черновик'
  observeSchedule(drafts, 'a', snapshot(), [])
  assert.equal(draft.configuration.intervals[0].name, 'Мой черновик')
  assert.equal(draft.configuration.outsidePower, null)
  assert.equal(draft.configuration.intervals[0].on, false)
  assert.equal(original.configuration.intervals[0].name, '')
  assert.equal(draftChanged(draft), true)
})

test('same device IDs in distinct homes and delimiter IDs have isolated drafts', () => {
  const drafts = new Map()
  observeSchedule(drafts, 'a', snapshot(), [])
  observeSchedule(drafts, 'b', snapshot(8), [])
  drafts.get(scheduleKey('a', 'socket')).configuration.enabled = true
  assert.equal(drafts.get(scheduleKey('b', 'socket')).configuration.enabled, false)
  assert.notEqual(scheduleKey('a/b', 'c'), scheduleKey('a', 'b/c'))
})

test('external version conflict retains edits until explicit reload', () => {
  const drafts = new Map(), key = scheduleKey('a', 'socket')
  observeSchedule(drafts, 'a', snapshot(), [])
  drafts.get(key).configuration.timeZone = 'UTC'
  observeSchedule(drafts, 'a', snapshot(3), [])
  assert.equal(drafts.get(key).conflict, true)
  assert.equal(drafts.get(key).configuration.version, 2)
  assert.equal(drafts.get(key).configuration.timeZone, 'UTC')
  drafts.set(key, reloadDraft(snapshot(3)))
  assert.equal(drafts.get(key).configuration.version, 3)
  assert.equal(drafts.get(key).conflict, false)
})

test('queued save does not advance baseline; only exact confirmed settings do', () => {
  const drafts = new Map(), key = scheduleKey('a', 'socket')
  observeSchedule(drafts, 'a', snapshot(), [])
  const draft = drafts.get(key)
  draft.configuration.enabled = true
  draft.submitted = { id: 'command', operation: 'schedule.replace', configuration: JSON.stringify(draft.configuration) }
  const command = { id: 'command', homeId: 'a', deviceId: 'socket', operation: 'schedule.replace', status: 'sent' }
  observeSchedule(drafts, 'a', snapshot(), [command])
  assert.equal(draftChanged(drafts.get(key)), true)
  const saved = snapshot(3); saved.configuration.enabled = true
  observeSchedule(drafts, 'a', saved, [{ ...command, status: 'confirmed' }])
  assert.equal(drafts.get(key).configuration.version, 3)
  assert.equal(draftChanged(drafts.get(key)), false)
})

test('confirmed save cannot overwrite edits made while waiting', () => {
  const drafts = new Map(), key = scheduleKey('a', 'socket')
  observeSchedule(drafts, 'a', snapshot(), [])
  const draft = drafts.get(key)
  draft.submitted = { id: 'command', operation: 'schedule.replace', configuration: JSON.stringify(draft.configuration) }
  draft.configuration.timeZone = 'UTC'
  observeSchedule(drafts, 'a', snapshot(3), [{ id: 'command', homeId: 'a', deviceId: 'socket', status: 'confirmed' }])
  assert.equal(drafts.get(key).configuration.timeZone, 'UTC')
  assert.equal(drafts.get(key).conflict, true)
})

test('unknown result requires reload and cannot silently become acknowledged', () => {
  const drafts = new Map(), key = scheduleKey('a', 'socket')
  observeSchedule(drafts, 'a', snapshot(), [])
  const draft = drafts.get(key)
  draft.submitted = { id: 'command', operation: 'schedule.replace', configuration: JSON.stringify(draft.configuration) }
  const command = { id: 'command', homeId: 'a', deviceId: 'socket', status: 'unknown' }
  observeSchedule(drafts, 'a', snapshot(), [command])
  assert.equal(draft.requiresReload, true)
  observeSchedule(drafts, 'a', snapshot(3), [{ ...command, status: 'confirmed' }])
  assert.equal(drafts.get(key).configuration.version, 2)
})

test('offline, stale schedule, wrong device and active power/schedule command block schedule control', () => {
  assert.equal(scheduleFresh(home, device, snapshot(), now, false), true)
  assert.equal(scheduleFresh(home, device, snapshot(), now, true), false)
  assert.equal(scheduleFresh({ ...home, online: false }, device, snapshot(), now, false), false)
  assert.equal(scheduleFresh(home, device, { ...snapshot(), stale: true }, now, false), false)
  assert.equal(scheduleFresh(home, device, { ...snapshot(), snapshotAt: new Date(now - 30_000).toISOString() }, now, false), false)
  assert.equal(scheduleFresh(home, device, { ...snapshot(), deviceId: 'other' }, now, false), false)
  assert.equal(scheduleFresh(home, device, undefined, now, false), false)
  for (const operation of ['power.set', 'schedule.replace', 'schedule.resume'])
    assert.equal(scheduleFresh({ ...home, commands: [{ deviceId: 'socket', operation, status: 'sent' }] }, device, snapshot(), now, false), false)
  assert.equal(scheduleSnapshotStale(home, device, { ...snapshot(), snapshotAt: 'invalid' }, now, false), true)
})

test('week wrap conflict is rejected; same power overlaps and touching boundaries are permitted', () => {
  const config = snapshot().configuration
  config.intervals = [interval({ days: [7], start: '22:00', end: '06:00', on: true }), interval({ id: 'other', days: [1], start: '05:00', end: '07:00', on: false })]
  assert.match(validateSchedule(config).join(' '), /пересекаются/)
  config.intervals[1].start = '06:00'
  assert.deepEqual(validateSchedule(config), [])
  config.intervals[1].start = '05:00'; config.intervals[1].on = true
  assert.deepEqual(validateSchedule(config), [])
})

test('full days, off intervals, disabled overlaps and null outside survive validation unchanged', () => {
  const config = snapshot().configuration
  config.intervals = [interval({ start: '00:00', end: '24:00' }), interval({ id: 'other', enabled: false, on: true })]
  const before = JSON.stringify(config)
  assert.deepEqual(validateSchedule(config), [])
  assert.equal(JSON.stringify(config), before)
})

test('invalid zone, equal times, 24:00 start, empty or duplicate days and too many intervals are rejected', () => {
  for (const patch of [{ timeZone: 'No/Such_Zone' }, { timeZone: '+03:00' }, { intervals: [interval({ start: '09:00', end: '09:00' })] },
    { intervals: [interval({ start: '24:00' })] }, { intervals: [interval({ days: [] })] }, { intervals: [interval({ days: [1, 1] })] },
    { intervals: Array.from({ length: 65 }, (_, id) => interval({ id: String(id) })) }])
    assert.ok(validateSchedule({ ...snapshot().configuration, ...patch }).length)
})

test('schedule acknowledgements are distinct from physical power confirmation', () => {
  assert.equal(commandAction({ operation: 'schedule.replace', on: false }), 'Сохранить расписание')
  assert.match(scheduleCommandStatus({ operation: 'schedule.replace', status: 'confirmed' }), /сохранение настроек/)
  assert.match(scheduleCommandStatus({ operation: 'schedule.resume', status: 'confirmed' }), /снятие ручной паузы/)
  assert.equal(scheduleCommandStatus({ operation: 'power.set', status: 'confirmed' }), undefined)
})

test('canonical configuration ignores JSON key, interval and day order but preserves power semantics', () => {
  const original = snapshot().configuration
  original.intervals.push(interval({ id: '00000000-0000-4000-8000-000000000001', days: [3, 2, 1], on: true }))
  const reordered = { intervals: [...original.intervals].reverse().map(item => ({ ...item, days: [...item.days].reverse() })), ...original }
  reordered.intervals = [...original.intervals].reverse().map(item => ({ ...item, days: [...item.days].reverse() }))
  assert.equal(configurationFingerprint(original), configurationFingerprint(reordered))
  reordered.outsidePower = false
  assert.notEqual(configurationFingerprint(original), configurationFingerprint(reordered))
})

test('safe integer boundary can be read but cannot be incremented', () => {
  assert.deepEqual(validateSchedule({ ...snapshot().configuration, version: Number.MAX_SAFE_INTEGER }), [])
  assert.equal(canAdvanceRevision(Number.MAX_SAFE_INTEGER - 1), true)
  for (const revision of [Number.MAX_SAFE_INTEGER, Number.MAX_SAFE_INTEGER + 1, -1, NaN]) assert.equal(canAdvanceRevision(revision), false)
})
