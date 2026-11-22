import test from 'node:test'
import assert from 'node:assert/strict'
import { mountView, reply, settle } from './helpers/view-fixture.mjs'

const firstId = 'server/first'
const secondId = 'server/second'
const sharedId = 'same/device'
const timestamp = () => new Date().toISOString()
const device = (id = sharedId, power = false) => ({
  id, name: `Device ${id}`, model: 'synthetic.plug', online: true,
  capabilities: ['power.read', 'power.set'], power, updatedAt: timestamp(),
})
const home = (id, extra = {}) => ({
  id, name: id === firstId ? 'Первый сервер' : 'Второй сервер', online: true,
  lastSeenAt: timestamp(), snapshotAt: timestamp(), stale: false,
  devices: [device(sharedId, id === secondId)], commands: [], ...extra,
})
const dashboard = homes => ({ serverTime: timestamp(), homes })
const receipt = (request, status = 'queued') => {
  const body = JSON.parse(request.body)
  const parts = request.url.split('/')
  return { id: body.commandId, homeId: decodeURIComponent(parts[4]), deviceId: decodeURIComponent(parts[6]),
    operation: 'power.set', on: body.on, createdAt: timestamp(), expiresAt: new Date(Date.now() + 30_000).toISOString(), status }
}
const powerRequests = f => f.requests.filter(request => request.method === 'POST')

test('all servers are default, counts include every device and identical IDs remain nested', async t => {
  const f = await mountView(t, () => reply(dashboard([home(firstId), home(secondId, { devices: [device(), device('other')] })])))
  const options = f.byId('server-filter').options
  assert.equal(options[0].value, '')
  assert.equal(options[0].selected, true)
  assert.match(options[0].textContent, /Все серверы · 2 · Устройств: 3/)
  assert.match(options[1].textContent, /Устройств: 1/)
  assert.match(options[2].textContent, /Устройств: 2/)
  assert.ok(options[1].textContent.includes(`…${firstId.slice(-8)}`))
  assert.ok(!options[1].textContent.includes(firstId))
  assert.ok(f.home(firstId).textContent.includes(`Сервер: ${firstId}`))
  assert.equal(f.all(node => node.props['data-home-id']).length, 2)
  assert.equal(f.all(node => node.props['data-device-id']).length, 3)
  assert.equal(f.powerButton(firstId, sharedId, true).props.disabled, false)
  assert.equal(f.powerButton(secondId, sharedId, true).props.disabled, false)
  assert.equal(powerRequests(f).length, 0)
})

test('filter is stable across polling, reordering, renamed servers, disappearance and recovery', async t => {
  let homes = [home(firstId), home(secondId)]
  const f = await mountView(t, () => reply(dashboard(homes)))
  await f.choose(secondId)
  assert.equal(f.home(firstId), undefined)
  assert.ok(f.home(secondId))
  homes = [home(secondId, { name: 'Переименованный сервер', devices: [device(), device('new')] }), home(firstId)]
  await f.poll()
  assert.equal(f.home(firstId), undefined)
  assert.match(f.home(secondId).textContent, /Переименованный сервер/)
  assert.equal(f.byId('server-filter').options.find(option => option.selected).value, secondId)
  assert.match(f.byId('server-counts').textContent, /Устройств: 3/)
  homes = [home(firstId)]
  await f.poll()
  assert.equal(f.all(node => node.props['data-home-id']).length, 0)
  const missing = f.byId('server-filter').options.find(option => option.selected)
  assert.equal(missing.value, secondId)
  assert.equal(missing.props.disabled, true)
  homes = [home(firstId), home(secondId)]
  await f.poll()
  assert.equal(f.home(firstId), undefined)
  assert.ok(f.home(secondId))
  await f.choose('')
  assert.equal(f.all(node => node.props['data-home-id']).length, 2)
  assert.equal(powerRequests(f).length, 0)
})

test('in-flight and queued commands isolate matching device IDs by home and preserve target routing', async t => {
  let finishFirst
  const f = await mountView(t, request => {
    if (request.url === '/api/smarthome') return reply(dashboard([home(firstId), home(secondId)]))
    if (request.url.endsWith('/csrf')) return reply({ token: 'synthetic-csrf' })
    if (request.url.includes(encodeURIComponent(firstId))) return new Promise(resolve => { finishFirst = () => resolve(reply(receipt(request))) })
    return reply(receipt(request, 'confirmed'))
  })
  const pending = f.powerButton(firstId, sharedId, true).props.onClick()
  await settle()
  assert.equal(f.powerButton(firstId, sharedId, false).props.disabled, true)
  assert.equal(f.powerButton(secondId, sharedId, false).props.disabled, false)
  await f.powerButton(firstId, sharedId, true).props.onClick()
  assert.equal(powerRequests(f).length, 1, 'Duplicate handler invocation must not submit again')
  await f.choose(secondId)
  await f.powerButton(secondId, sharedId, false).props.onClick()
  await settle()
  const sent = powerRequests(f)
  assert.equal(sent.length, 2)
  assert.equal(sent[0].url, `/api/smarthome/homes/${encodeURIComponent(firstId)}/devices/${encodeURIComponent(sharedId)}/power`)
  assert.equal(sent[1].url, `/api/smarthome/homes/${encodeURIComponent(secondId)}/devices/${encodeURIComponent(sharedId)}/power`)
  assert.equal(JSON.parse(sent[0].body).on, true)
  assert.equal(JSON.parse(sent[1].body).on, false)
  assert.notEqual(JSON.parse(sent[0].body).commandId, JSON.parse(sent[1].body).commandId)
  assert.ok(sent.every(request => request.headers.RequestVerificationToken === 'synthetic-csrf'))
  finishFirst(); await pending; await settle()
  await f.choose('')
  assert.equal(f.powerButton(firstId, sharedId, true).props.disabled, true, 'Queued local receipt survives an older dashboard')
  assert.equal(f.powerButton(secondId, sharedId, true).props.disabled, false)
  assert.match(f.home(firstId).textContent, /Ожидает отправки/)
  assert.doesNotMatch(f.home(secondId).textContent, /Ожидает отправки/)
  assert.match(f.device(firstId, sharedId).textContent, /Выключено/)
  assert.match(f.device(secondId, sharedId).textContent, /Включено/)
})

test('offline server and its matching device do not disable the online server', async t => {
  const f = await mountView(t, request => request.url === '/api/smarthome'
    ? reply(dashboard([home(firstId, { online: false, stale: true }), home(secondId)]))
    : request.url.endsWith('/csrf') ? reply({ token: 'synthetic-csrf' }) : reply(receipt(request, 'confirmed')))
  assert.equal(f.powerButton(firstId, sharedId, true).props.disabled, true)
  assert.equal(f.powerButton(secondId, sharedId, true).props.disabled, false)
  await f.powerButton(firstId, sharedId, true).props.onClick()
  assert.equal(powerRequests(f).length, 0)
  await f.powerButton(secondId, sharedId, true).props.onClick()
  assert.equal(powerRequests(f).length, 1)
  assert.match(powerRequests(f)[0].url, /server%2Fsecond/)
})

test('server-specific command failure blocks only that device until the next dashboard', async t => {
  const f = await mountView(t, request => request.url === '/api/smarthome'
    ? reply(dashboard([home(firstId), home(secondId)]))
    : request.url.endsWith('/csrf') ? reply({ token: 'synthetic-csrf' }) : reply({ error: 'offline' }, 409))
  await f.powerButton(firstId, sharedId, true).props.onClick()
  await settle()
  assert.equal(f.powerButton(firstId, sharedId, true).props.disabled, true)
  assert.equal(f.powerButton(secondId, sharedId, true).props.disabled, false)
  assert.match(f.device(firstId, sharedId).textContent, /Команда отклонена/)
  assert.doesNotMatch(f.device(secondId, sharedId).textContent, /Команда отклонена/)
  await f.poll()
  assert.equal(f.powerButton(firstId, sharedId, true).props.disabled, false)
})

test('dashboard connection failure still disables every server without changing the filter', async t => {
  let offline = false
  const f = await mountView(t, () => {
    if (offline) throw new Error('Synthetic dashboard failure')
    return reply(dashboard([home(firstId), home(secondId)]))
  })
  await f.choose(secondId)
  offline = true
  await f.poll()
  assert.equal(f.byId('server-filter').options.find(option => option.selected).value, secondId)
  assert.equal(f.powerButton(secondId, sharedId, true).props.disabled, true)
  await f.choose('')
  assert.equal(f.powerButton(firstId, sharedId, true).props.disabled, true)
  assert.equal(f.powerButton(secondId, sharedId, true).props.disabled, true)
  assert.equal(powerRequests(f).length, 0)
})
