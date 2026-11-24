<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { canControl, commandLabels, isOnline, isStale } from '../smarthome/state'
import type { HomeCommand, HomeDashboard, HomeDevice, HomeEnrollment, HomeServer } from '../smarthome/state'
import ScheduleEditor from '../smarthome/ScheduleEditor.vue'
import { canAdvanceRevision, commandAction, observeSchedule, reloadDraft, scheduleCommandStatus, scheduleFresh, scheduleSnapshotStale, validateSchedule } from '../smarthome/schedules'
import type { ScheduleDraft } from '../smarthome/schedules'

const dashboard = ref<HomeDashboard | null>(null)
const access = ref<'loading' | 'allowed' | 'denied' | 'login'>('loading')
const unavailable = ref(true)
const error = ref('')
const notice = ref('')
const name = ref('Дом')
const enrollment = ref<HomeEnrollment | null>(null)
const busy = ref(false)
const refreshing = ref(false)
const selectedHomeId = ref('')
const pendingPower = ref(new Set<string>())
const commandErrors = ref(new Map<string, string>())
const commandUnavailable = ref(new Set<string>())
const scheduleDrafts = ref(new Map<string, ScheduleDraft>())
// Keep receipts until a dashboard includes them; an older in-flight poll must not
// re-enable a device immediately after its command has been accepted.
const receipts = ref<HomeCommand[]>([])
const now = ref(Date.now())
let serverOffset = 0
let stopped = false
let pollTimer: ReturnType<typeof setTimeout> | undefined
let clockTimer: ReturnType<typeof setInterval> | undefined
const requests = new Set<AbortController>()
const homes = computed(() => dashboard.value?.homes ?? [])
// Vue keeps this explicit selection while polling replaces the dashboard.
// https://vuejs.org/guide/essentials/forms.html#select
const visibleHomes = computed(() => selectedHomeId.value
  ? homes.value.filter(home => home.id === selectedHomeId.value) : homes.value)
const deviceCount = computed(() => homes.value.reduce((count, home) => count + home.devices.length, 0))
const selectedHomeMissing = computed(() => !!selectedHomeId.value && !homes.value.some(home => home.id === selectedHomeId.value))
const deviceKey = (home: HomeServer, device: HomeDevice) => JSON.stringify([home.id, device.id])

class RequestError extends Error {
  constructor(public status: number) { super('Request failed') }
}

async function request<T>(path = '', body?: object): Promise<T> {
  const controller = new AbortController()
  requests.add(controller)
  const timeout = setTimeout(() => controller.abort(), 10_000)
  try {
    const headers: Record<string, string> = { Accept: 'application/json' }
    if (body !== undefined) {
      const csrf = await request<{ token: string }>('/csrf')
      headers['Content-Type'] = 'application/json'
      headers.RequestVerificationToken = csrf.token
    }
    const response = await fetch(`/api/smarthome${path}`, {
      method: body === undefined ? 'GET' : 'POST', credentials: 'same-origin', cache: 'no-store',
      headers, body: body === undefined ? undefined : JSON.stringify(body), signal: controller.signal,
    })
    if (!response.ok) throw new RequestError(response.status)
    return response.status === 204 ? undefined as T : await response.json() as T
  } finally {
    clearTimeout(timeout)
    requests.delete(controller)
  }
}

function reportFailure(reason: unknown) {
  if (stopped) return
  unavailable.value = true
  if (reason instanceof RequestError && (reason.status === 401 || reason.status === 403)) {
    access.value = reason.status === 401 ? 'login' : 'denied'
    dashboard.value = null
    enrollment.value = null
    receipts.value = []
    scheduleDrafts.value.clear()
    return
  }
  error.value = reason instanceof RequestError && reason.status === 409
    ? 'Операция отклонена: состояние изменилось или команда уже выполняется. Обновите данные.'
    : 'Не удалось связаться с Passi. Показаны последние известные данные; управление отключено.'
}

async function refresh() {
  if (refreshing.value || stopped || access.value === 'denied' || access.value === 'login') return
  refreshing.value = true
  try {
    const data = await request<HomeDashboard>()
    if (stopped) return
    serverOffset = Date.parse(data.serverTime) - Date.now()
    now.value = Date.now() + serverOffset
    dashboard.value = data
    for (const home of data.homes) for (const snapshot of home.schedules ?? [])
      observeSchedule(scheduleDrafts.value, home.id, snapshot, home.commands)
    receipts.value = receipts.value.filter(local => !data.homes.some(home =>
      home.id === local.homeId && home.commands.some(command => command.id === local.id)))
    commandUnavailable.value.clear()
    unavailable.value = false
    access.value = 'allowed'
    error.value = ''
  } catch (reason) { reportFailure(reason) }
  finally { refreshing.value = false }
}

async function poll() {
  await refresh()
  if (!stopped && access.value !== 'denied' && access.value !== 'login') pollTimer = setTimeout(poll, 5000)
}

async function manage(action: 'create' | 'enrollment' | 'revoke', home?: HomeServer) {
  if (busy.value) return
  if (action !== 'create' && !window.confirm(action === 'revoke'
    ? 'Отозвать доступ домашнего сервера? Для подключения потребуется новая регистрация.'
    : 'Создать новый код регистрации? Старый токен и соединение сервера будут отозваны.')) return
  busy.value = true
  enrollment.value = null
  error.value = ''
  notice.value = ''
  try {
    const path = action === 'create' ? '/homes' : `/homes/${encodeURIComponent(home!.id)}/${action}`
    const result = await request<HomeEnrollment | undefined>(path, action === 'create' ? { name: name.value.trim() } : {})
    if (stopped) return
    if (action !== 'revoke' && result) enrollment.value = result
    notice.value = action === 'revoke' ? 'Доступ сервера отозван.' : 'Код регистрации создан. Сохраните его на домашнем сервере до истечения срока.'
    await refresh()
  } catch (reason) { reportFailure(reason) }
  finally { busy.value = false }
}

async function power(home: HomeServer, device: HomeDevice, on: boolean) {
  if (busy.value || !controlAvailable(home, device)) return
  const key = deviceKey(home, device)
  const command: HomeCommand = {
    id: crypto.randomUUID(), homeId: home.id, deviceId: device.id, operation: 'power.set', on,
    status: 'unknown', createdAt: new Date(now.value).toISOString(), expiresAt: new Date(now.value + 30_000).toISOString(),
  }
  pendingPower.value.add(key)
  commandErrors.value.delete(key)
  try {
    const result = await request<HomeCommand>(`/homes/${encodeURIComponent(home.id)}/devices/${encodeURIComponent(device.id)}/power`,
      { commandId: command.id, on })
    if (stopped) return
    receipts.value.push(result)
    home.commands = [result, ...home.commands.filter(item => item.id !== result.id)]
    await refresh()
  } catch (reason) {
    if (!stopped && !(reason instanceof RequestError && reason.status < 500)) receipts.value.push(command)
    if (!stopped) {
      if (reason instanceof RequestError && (reason.status === 401 || reason.status === 403)) reportFailure(reason)
      else {
        commandUnavailable.value.add(key)
        commandErrors.value.set(key, reason instanceof RequestError && reason.status === 409
          ? 'Команда отклонена: устройство недоступно или уже выполняет команду. Обновите данные перед повтором.'
          : 'Ответ на команду не получен. Результат неизвестен; автоматического повтора нет. Обновите данные перед новой командой.')
      }
    }
  } finally { pendingPower.value.delete(key) }
}

function date(value?: string | null): string {
  return value && Number.isFinite(Date.parse(value)) ? new Date(value).toLocaleString('ru-RU') : 'ещё не получено'
}

const scheduleSnapshot = (home: HomeServer, device: HomeDevice) => home.schedules?.find(item => item.deviceId === device.id)
function scheduleAvailable(home: HomeServer, device: HomeDevice) {
  return !busy.value && controlAvailable(home, device) && scheduleFresh(home, device, scheduleSnapshot(home, device), now.value, unavailable.value)
}
function reloadSchedule(home: HomeServer, device: HomeDevice) {
  const snapshot = scheduleSnapshot(home, device)
  if (!snapshot || !scheduleAvailable(home, device)) return
  scheduleDrafts.value.set(deviceKey(home, device), reloadDraft(snapshot))
  commandErrors.value.delete(deviceKey(home, device))
}
async function sendSchedule(home: HomeServer, device: HomeDevice, operation: 'schedule.replace' | 'schedule.resume') {
  const key = deviceKey(home, device), snapshot = scheduleSnapshot(home, device), draft = scheduleDrafts.value.get(key)
  if (!snapshot || !draft || !scheduleAvailable(home, device) || draft.conflict || draft.requiresReload) return
  if (draft.configuration.version !== snapshot.configuration.version) { draft.conflict = true; return }
  if (operation === 'schedule.replace' && (validateSchedule(draft.configuration).length || !canAdvanceRevision(draft.configuration.version))) return
  if (operation === 'schedule.resume' && (!snapshot.runtime.manualActive || !canAdvanceRevision(snapshot.runtime.revision))) return
  const commandId = crypto.randomUUID()
  const configuration = JSON.parse(JSON.stringify(draft.configuration))
  const body = operation === 'schedule.replace'
    ? { commandId, expectedVersion: configuration.version, configuration }
    : { commandId, expectedVersion: snapshot.configuration.version, expectedRuntimeRevision: snapshot.runtime.revision }
  const local: HomeCommand = { id: commandId, homeId: home.id, deviceId: device.id, operation, on: false,
    expectedVersion: body.expectedVersion, expectedRuntimeRevision: body.expectedRuntimeRevision, configuration: operation === 'schedule.replace' ? configuration : undefined,
    status: 'unknown', createdAt: new Date(now.value).toISOString(), expiresAt: new Date(now.value + 30_000).toISOString() }
  draft.submitted = { id: commandId, operation, configuration: JSON.stringify(configuration) }
  pendingPower.value.add(key)
  commandErrors.value.delete(key)
  try {
    const result = await request<HomeCommand>(`/homes/${encodeURIComponent(home.id)}/devices/${encodeURIComponent(device.id)}/schedule${operation === 'schedule.resume' ? '/resume' : ''}`, body)
    if (stopped) return
    receipts.value.push(result)
    home.commands = [result, ...home.commands.filter(item => item.id !== result.id)]
    await refresh()
  } catch (reason) {
    if (stopped) return
    if (reason instanceof RequestError && (reason.status === 401 || reason.status === 403)) { reportFailure(reason); return }
    const conflict = reason instanceof RequestError && reason.status === 409
    const rejected = reason instanceof RequestError && reason.status < 500
    if (!rejected) receipts.value.push(local)
    draft.requiresReload = true
    draft.conflict = conflict
    commandUnavailable.value.add(key)
    commandErrors.value.set(key, conflict
      ? 'Конфликт версии или выполняющаяся команда. Черновик сохранён; загрузите текущие настройки перед новой отправкой.'
      : rejected ? 'Настройки отклонены. Черновик сохранён; проверьте его и загрузите текущие настройки.'
        : 'Результат операции неизвестен. Автоматического повтора нет; дождитесь свежего снимка и загрузите текущие настройки.')
  } finally { pendingPower.value.delete(key) }
}

function commands(home: HomeServer) {
  return [...home.commands, ...receipts.value.filter(command => command.homeId === home.id && !home.commands.some(item => item.id === command.id))]
    .sort((left, right) => Date.parse(right.createdAt) - Date.parse(left.createdAt)).slice(0, 10)
}

function controlAvailable(home: HomeServer, device: HomeDevice) {
  return canControl(home, device, now.value, unavailable.value) &&
    !pendingPower.value.has(deviceKey(home, device)) && !commandUnavailable.value.has(deviceKey(home, device)) &&
    !receipts.value.some(command => command.homeId === home.id && command.deviceId === device.id &&
      (command.status === 'queued' || command.status === 'sent'))
}

onMounted(() => {
  void poll()
  clockTimer = setInterval(() => {
    now.value = Date.now() + serverOffset
    if (enrollment.value && Date.parse(enrollment.value.expiresAt) <= now.value) enrollment.value = null
  }, 1000)
})
onBeforeUnmount(() => {
  stopped = true
  clearTimeout(pollTimer)
  clearInterval(clockTimer)
  requests.forEach(controller => controller.abort())
  enrollment.value = null
})
</script>

<template>
  <div class="container py-4 py-md-5 smart-home" lang="ru">
    <div class="d-flex flex-wrap align-items-start justify-content-between gap-3 mb-4">
      <div>
        <div class="text-uppercase text-secondary small fw-semibold mb-2">Passi · Дом</div>
        <h1 class="h2"><i class="bi bi-house-heart me-2" aria-hidden="true"></i>Умный дом</h1>
        <p class="text-secondary mb-0">Управление через домашний сервер. Локальная работа не зависит от облака.</p>
      </div>
      <button v-if="access !== 'denied' && access !== 'login'" type="button" class="btn btn-outline-secondary" :disabled="refreshing" @click="refresh">
        <i class="bi bi-arrow-clockwise me-1" aria-hidden="true"></i>{{ refreshing ? 'Обновление…' : 'Обновить' }}
      </button>
    </div>

    <div v-if="access === 'login'" class="alert alert-warning" role="alert">
      Сессия завершена. <a href="/Auth/Login" class="alert-link">Войти в Passi</a>
    </div>
    <div v-else-if="access === 'denied'" class="alert alert-warning" role="alert">У этой учётной записи нет доступа к умному дому.</div>
    <div v-else-if="access === 'loading' && !error" class="py-5 text-center text-secondary" role="status">Проверяем доступ и связь с домом…</div>
    <div v-if="error" class="alert alert-warning" role="alert">{{ error }}</div>
    <div v-if="notice && access === 'allowed'" class="alert alert-info" role="status">{{ notice }}</div>

    <template v-if="access === 'allowed'">
      <section v-if="enrollment" class="card border-warning shadow-sm mb-4" aria-labelledby="pairing-title">
        <div class="card-body p-4">
          <h2 id="pairing-title" class="h5">Одноразовая регистрация домашнего сервера</h2>
          <p class="text-secondary">Код показывается только сейчас. Передайте его агенту на домашнем сервере; не отправляйте в чат и не добавляйте в адрес подключения.</p>
          <label for="home-id" class="form-label">Идентификатор дома</label>
          <input id="home-id" class="form-control mb-3 font-monospace" readonly :value="enrollment.homeId" />
          <label for="enrollment-secret" class="form-label">Код регистрации</label>
          <input id="enrollment-secret" class="form-control font-monospace" readonly autocomplete="off" spellcheck="false" :value="enrollment.enrollmentSecret" />
          <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3">
            <small>Действует до {{ date(enrollment.expiresAt) }}</small>
            <button class="btn btn-outline-dark btn-sm" type="button" @click="enrollment = null">Скрыть код</button>
          </div>
        </div>
      </section>

      <div v-if="homes.length === 0" class="card border-0 shadow-sm mb-4">
        <div class="card-body p-4"><h2 class="h5">Домашний сервер ещё не зарегистрирован</h2><p class="mb-0 text-secondary">Добавьте сервер и зарегистрируйте локального агента. Устройства появятся после его подключения.</p></div>
      </div>

      <section class="card border-0 shadow-sm mb-4" aria-label="Выбор домашнего сервера">
        <div class="card-body p-3 p-md-4">
          <label for="server-filter" class="form-label fw-semibold">Домашний сервер</label>
          <select id="server-filter" v-model="selectedHomeId" class="form-select" aria-describedby="server-counts">
            <option value="">Все серверы · {{ homes.length }} · Устройств: {{ deviceCount }}</option>
            <option v-if="selectedHomeMissing" :value="selectedHomeId" disabled>Выбранный сервер недоступен · {{ selectedHomeId }}</option>
            <option v-for="home in homes" :key="home.id" :value="home.id">{{ home.name }} · {{ home.id.length > 8 ? `…${home.id.slice(-8)}` : home.id }} · Устройств: {{ home.devices.length }}</option>
          </select>
          <p id="server-counts" class="small text-secondary mt-2 mb-0" role="status">Серверов: {{ homes.length }} · Устройств: {{ deviceCount }} · Показано серверов: {{ visibleHomes.length }}</p>
          <p v-if="selectedHomeMissing" class="small text-secondary mt-2 mb-0">Сервер отсутствует в текущем списке. Обновите данные или выберите другой сервер.</p>
        </div>
      </section>

      <section v-for="home in visibleHomes" :key="home.id" class="card border-0 shadow-sm mb-4" :aria-label="home.name" :data-home-id="home.id">
        <div class="card-header bg-white p-4 border-bottom">
          <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
            <div>
              <div class="d-flex align-items-center flex-wrap gap-2 mb-1">
                <h2 class="h4 mb-0">{{ home.name }}</h2>
                <span class="badge rounded-pill" :class="isOnline(home, now, unavailable) ? 'text-bg-success' : 'text-bg-secondary'">{{ unavailable ? 'Связь неизвестна' : isOnline(home, now, unavailable) ? 'Онлайн' : 'Офлайн' }}</span>
                <span class="badge text-bg-light">Устройств: {{ home.devices.length }}</span>
              </div>
              <p class="small text-secondary mb-0">Последняя связь: {{ date(home.lastSeenAt) }}</p>
              <p class="small text-secondary mb-0">Сервер: {{ home.id }}</p>
              <p class="small text-secondary mb-0">Снимок устройств: {{ date(home.snapshotAt) }}</p>
            </div>
            <div class="d-flex flex-wrap gap-2">
              <button type="button" class="btn btn-sm btn-outline-secondary" :disabled="busy || unavailable" @click="manage('enrollment', home)">Новый код регистрации</button>
              <button type="button" class="btn btn-sm btn-outline-danger" :disabled="busy || unavailable" @click="manage('revoke', home)">Отозвать доступ</button>
            </div>
          </div>
        </div>
        <div class="card-body p-4">
          <p v-if="!isOnline(home, now, unavailable)" class="alert alert-secondary">Домашний сервер недоступен или связь не подтверждена. Сохранённые показания могут быть устаревшими.</p>
          <p v-if="home.devices.length === 0" class="text-secondary mb-0">Агент пока не передал зарегистрированные устройства.</p>
          <div class="row g-3">
            <div v-for="device in home.devices" :key="device.id" class="col-12 col-lg-6">
              <article class="device-card border rounded-3 p-3 h-100" :data-device-id="device.id" :aria-busy="pendingPower.has(deviceKey(home, device))" :class="{ 'device-stale': isStale(home, device, now, unavailable) }">
                <div class="d-flex align-items-start justify-content-between gap-2 mb-3">
                  <div><h3 class="h5 mb-1">{{ device.name }}</h3><span v-if="device.model" class="small text-secondary">{{ device.model }}</span></div>
                  <span class="badge" :class="isStale(home, device, now, unavailable) ? 'text-bg-warning' : 'text-bg-light'">{{ isStale(home, device, now, unavailable) ? 'Данные устарели' : 'Данные актуальны' }}</span>
                </div>
                <dl class="mb-3">
                  <template v-if="device.capabilities.includes('power.read')">
                    <dt class="small text-secondary fw-normal">{{ isStale(home, device, now, unavailable) ? 'Последнее известное состояние' : 'Состояние устройства' }}</dt>
                    <dd class="fs-4 fw-semibold">{{ device.power === true ? 'Включено' : device.power === false ? 'Выключено' : 'Неизвестно' }}</dd>
                  </template>
                  <template v-if="device.capabilities.includes('temperature.read')">
                    <dt class="small text-secondary fw-normal">Внутренняя температура устройства</dt>
                    <dd class="fs-5">{{ device.temperatureCelsius == null ? 'Нет показаний' : `${device.temperatureCelsius.toLocaleString('ru-RU')} °C` }}</dd>
                  </template>
                  <dt class="small text-secondary fw-normal">Время показаний</dt><dd class="small mb-0">{{ date(device.updatedAt) }}</dd>
                </dl>
                <div v-if="device.capabilities.includes('power.set')" class="d-flex gap-2" :aria-label="`Управление: ${device.name}`">
                  <button type="button" class="btn btn-primary flex-fill" :disabled="busy || !controlAvailable(home, device)" @click="power(home, device, true)">Включить</button>
                  <button type="button" class="btn btn-outline-dark flex-fill" :disabled="busy || !controlAvailable(home, device)" @click="power(home, device, false)">Выключить</button>
                </div>
                <p v-else class="small text-secondary mb-0">Только наблюдение</p>
                <p v-if="pendingPower.has(deviceKey(home, device))" class="small text-secondary mt-2 mb-0" role="status">Отправляем команду этому устройству…</p>
                <p v-if="commandErrors.has(deviceKey(home, device))" class="small text-danger mt-2 mb-0" role="alert">{{ commandErrors.get(deviceKey(home, device)) }}</p>
                <ScheduleEditor :snapshot="scheduleSnapshot(home, device)" :draft="scheduleDrafts.get(deviceKey(home, device))"
                  :blocked="!scheduleAvailable(home, device)" :stale="scheduleSnapshotStale(home, device, scheduleSnapshot(home, device), now, unavailable)"
                  :pending="pendingPower.has(deviceKey(home, device))" :command="commands(home).find(command => command.deviceId === device.id && command.operation !== 'power.set')"
                  @save="sendSchedule(home, device, 'schedule.replace')" @resume="sendSchedule(home, device, 'schedule.resume')" @reload="reloadSchedule(home, device)" />
              </article>
            </div>
          </div>

          <div v-if="commands(home).length" class="mt-4">
            <h3 class="h6">Последние команды</h3>
            <p class="small text-secondary">Отправка не означает выполнение. При неизвестном результате автоматического повтора нет; новая команда требует вашего действия.</p>
            <ol class="list-group list-group-flush">
              <li v-for="command in commands(home)" :key="command.id" class="list-group-item px-0 py-3">
                <div class="d-flex flex-wrap justify-content-between gap-2"><strong>{{ home.devices.find(device => device.id === command.deviceId)?.name ?? command.deviceId }} · {{ commandAction(command) }}</strong><small class="text-secondary">{{ date(command.createdAt) }}</small></div>
                <div :class="{ 'text-success': command.status === 'confirmed', 'text-danger': command.status === 'failed', 'text-warning-emphasis': command.status === 'unknown' }">{{ scheduleCommandStatus(command) ?? commandLabels[command.status] }}</div>
                <div class="small text-secondary">Срок команды: {{ date(command.expiresAt) }}<span v-if="command.completedAt"> · Результат: {{ date(command.completedAt) }}</span></div>
              </li>
            </ol>
          </div>
        </div>
      </section>

      <details class="card border-0 shadow-sm" :open="homes.length === 0">
        <summary class="card-header bg-white p-3 fw-semibold">Добавить домашний сервер</summary>
        <form class="card-body p-4" @submit.prevent="manage('create')">
          <label for="home-name" class="form-label">Название сервера</label>
          <div class="d-flex flex-wrap gap-2"><input id="home-name" v-model="name" class="form-control home-name" maxlength="80" required autocomplete="off" /><button type="submit" class="btn btn-dark" :disabled="busy || unavailable || !name.trim()">Создать код регистрации</button></div>
          <p class="small text-secondary mt-3 mb-0">Код действует ограниченное время и используется один раз. Секреты устройств остаются дома.</p>
        </form>
      </details>
    </template>
  </div>
</template>

<style scoped>
.smart-home { max-width: 1120px; }
.device-card { background: #fff; }
.device-stale { background: #f8f9fa; border-style: dashed !important; }
.home-name { max-width: 24rem; }
summary { cursor: pointer; }
dd { margin-bottom: 0.8rem; }
</style>
