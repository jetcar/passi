import test from 'node:test'
import assert from 'node:assert/strict'
import { mountView, reply, settle } from './helpers/view-fixture.mjs'

const timestamp = () => new Date().toISOString()
const schedule = () => ({ deviceId: 'socket', snapshotAt: timestamp(), stale: false,
  configuration: { version: 4, deviceId: 'socket', enabled: false, timeZone: 'Europe/Tallinn', outsidePower: null,
    intervals: [{ id: '11111111-1111-4111-8111-111111111111', name: 'Ночь', enabled: true, days: [7], start: '22:00', end: '06:00', on: false }] },
  runtime: { revision: 9, state: 'manual', manualActive: true, manualUntil: null, actualPower: false, desiredPower: null, checkedAt: timestamp() } })
const home = id => ({ id, name: id, online: true, stale: false, lastSeenAt: timestamp(), snapshotAt: timestamp(), commands: [],
  devices: [{ id: 'socket', name: 'Розетка', online: true, power: false, capabilities: ['power.read', 'power.set'], updatedAt: timestamp() }], schedules: [schedule()] })
const dashboard = homes => ({ serverTime: timestamp(), homes })
const button = (f, name, id = 'a') => f.all(node => node.tag === 'button' && node.textContent === name, f.device(id, 'socket'))[0]
const zone = (f, id = 'a') => f.all(node => node.tag === 'input' && node.props.placeholder === 'Europe/Tallinn', f.device(id, 'socket'))[0]
const submit = (f, id = 'a') => f.all(node => node.tag === 'form', f.device(id, 'socket'))[0].props.onSubmit({ preventDefault() {} })
const editZone = async (f, value, id = 'a') => { const input = zone(f, id); input.value = value; input.dispatch('input'); await settle() }
const writes = f => f.requests.filter(request => request.method === 'POST')
const receipt = request => {
  const body = JSON.parse(request.body), parts = request.url.split('/')
  return { id: body.commandId, homeId: decodeURIComponent(parts[4]), deviceId: decodeURIComponent(parts[6]), operation: request.url.endsWith('/resume') ? 'schedule.resume' : 'schedule.replace',
    on: false, expectedVersion: body.expectedVersion, expectedRuntimeRevision: body.expectedRuntimeRevision, configuration: body.configuration,
    status: 'sent', createdAt: timestamp(), expiresAt: new Date(Date.now() + 30000).toISOString() }
}

test('old agent shows update notice and has no schedule mutation controls', async t => {
  const server = home('a'); delete server.schedules
  const f = await mountView(t, () => reply(dashboard([server])))
  assert.match(f.device('a', 'socket').textContent, /требуется обновление локального агента/)
  assert.equal(button(f, 'Сохранить расписание'), undefined)
  assert.equal(button(f, 'Возобновить расписание'), undefined)
  assert.equal(writes(f).length, 0)
})

test('mounted editor keeps separate drafts through polling and server selection', async t => {
  const f = await mountView(t, () => reply(dashboard([home('a'), home('b')])))
  await editZone(f, 'UTC')
  await f.poll()
  assert.equal(zone(f).value, 'UTC')
  assert.equal(zone(f, 'b').value, 'Europe/Tallinn')
  await f.choose('b'); await editZone(f, 'Europe/Helsinki', 'b')
  await f.choose('a')
  assert.equal(zone(f).value, 'UTC')
  await f.choose('b')
  assert.equal(zone(f, 'b').value, 'Europe/Helsinki')
})

test('save includes CAS and CSRF, preserves null/off, blocks power until confirmed and never updates actual power', async t => {
  const server = home('a')
  const f = await mountView(t, request => {
    if (request.url === '/api/smarthome') return reply(dashboard([server]))
    if (request.url.endsWith('/csrf')) return reply({ token: 'test-csrf' })
    server.commands = [receipt(request)]
    return reply(server.commands[0])
  })
  await editZone(f, 'UTC')
  await submit(f); await settle()
  assert.equal(writes(f).length, 1)
  const sent = writes(f)[0], payload = JSON.parse(sent.body)
  assert.equal(sent.url, '/api/smarthome/homes/a/devices/socket/schedule')
  assert.equal(sent.headers.RequestVerificationToken, 'test-csrf')
  assert.equal(payload.expectedVersion, 4)
  assert.equal(payload.configuration.version, 4)
  assert.equal(payload.configuration.outsidePower, null)
  assert.equal(payload.configuration.intervals[0].on, false)
  assert.equal(f.powerButton('a', 'socket', true).props.disabled, true)
  await submit(f); await settle()
  assert.equal(writes(f).length, 1)
  assert.match(f.device('a', 'socket').textContent, /Версия на сервере: 4/)
  server.schedules[0].configuration = { ...payload.configuration, version: 5 }
  server.commands[0].status = 'confirmed'
  await f.poll()
  assert.match(f.device('a', 'socket').textContent, /подтвердил сохранение настроек/)
  assert.match(f.device('a', 'socket').textContent, /Последнее фактическое питание: Выключено/)
  assert.equal(button(f, 'Сохранить расписание').props.disabled, true, 'Confirmed unchanged draft is no longer dirty')
  assert.equal(f.powerButton('a', 'socket', true).props.disabled, false)
})

test('409 keeps draft and forbids subsequent submission until explicit fresh reload', async t => {
  const server = home('a')
  const f = await mountView(t, request => request.url === '/api/smarthome' ? reply(dashboard([server]))
    : request.url.endsWith('/csrf') ? reply({ token: 'test-csrf' }) : reply({ error: 'version_conflict' }, 409))
  await editZone(f, 'UTC')
  await submit(f); await settle()
  server.schedules[0].configuration.version = 5
  await f.poll()
  assert.equal(zone(f).value, 'UTC')
  assert.equal(button(f, 'Сохранить расписание').props.disabled, true)
  await submit(f); await settle()
  assert.equal(writes(f).length, 1)
  await button(f, 'Загрузить текущие настройки').props.onClick(); await settle()
  assert.equal(zone(f).value, 'Europe/Tallinn')
  await editZone(f, 'UTC')
  assert.equal(button(f, 'Сохранить расписание').props.disabled, false)
})

test('resume includes configuration and manual revisions, no retry, stale blocks both operations', async t => {
  const server = home('a')
  const f = await mountView(t, request => {
    if (request.url === '/api/smarthome') return reply(dashboard([server]))
    if (request.url.endsWith('/csrf')) return reply({ token: 'test-csrf' })
    server.commands = [receipt(request)]; return reply(server.commands[0])
  })
  await button(f, 'Возобновить расписание').props.onClick(); await settle()
  const payload = JSON.parse(writes(f)[0].body)
  assert.equal(payload.expectedVersion, 4)
  assert.equal(payload.expectedRuntimeRevision, 9)
  assert.equal(writes(f)[0].url, '/api/smarthome/homes/a/devices/socket/schedule/resume')
  await f.poll()
  assert.equal(writes(f).length, 1)
  server.commands[0].status = 'unknown'; server.schedules[0].stale = true
  await f.poll()
  assert.equal(button(f, 'Возобновить расписание').props.disabled, true)
  assert.equal(button(f, 'Сохранить расписание').props.disabled, true)
  assert.match(f.device('a', 'socket').textContent, /Снимок устарел/)
})
