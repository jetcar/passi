import type { HomeCommand, HomeDevice, HomeServer } from './state.ts'
import { canControl, isStale, recent } from './state.ts'

export interface ScheduleInterval {
  id: string; name: string; enabled: boolean; days: number[]; start: string; end: string; on: boolean
}
export interface ScheduleConfiguration {
  version: number; deviceId: string; enabled: boolean; timeZone: string
  outsidePower: boolean | null; intervals: ScheduleInterval[]
}
export interface ScheduleRuntime {
  revision: number; state: string; desiredPower?: boolean | null; actualPower?: boolean | null
  checkedAt?: string | null; nextCheckAt?: string | null; nextBoundary?: string | null
  manualActive: boolean; manualUntil?: string | null; error?: string | null
}
export interface DeviceSchedule {
  deviceId: string; snapshotAt: string; stale: boolean
  configuration: ScheduleConfiguration; runtime: ScheduleRuntime
}
export interface ScheduleDraft {
  configuration: ScheduleConfiguration
  baseline: string
  conflict: boolean
  requiresReload: boolean
  submitted?: { id: string; operation: 'schedule.replace' | 'schedule.resume'; configuration: string }
}

export const scheduleKey = (homeId: string, deviceId: string) => JSON.stringify([homeId, deviceId])
const clone = (value: ScheduleConfiguration): ScheduleConfiguration => JSON.parse(JSON.stringify(value))
export const draftChanged = (draft: ScheduleDraft) => JSON.stringify(draft.configuration) !== draft.baseline
export const canAdvanceRevision = (version: number) => Number.isSafeInteger(version) && version >= 0 && version < Number.MAX_SAFE_INTEGER
export function configurationFingerprint(value: ScheduleConfiguration): string {
  return JSON.stringify({ version: value.version, deviceId: value.deviceId, enabled: value.enabled, timeZone: value.timeZone,
    outsidePower: value.outsidePower, intervals: value.intervals.map(interval => ({ id: interval.id.toLowerCase(),
      name: interval.name, enabled: interval.enabled, days: [...interval.days].sort((a, b) => a - b),
      start: interval.start, end: interval.end, on: interval.on })).sort((a, b) => a.id.localeCompare(b.id)) })
}

export function reloadDraft(snapshot: DeviceSchedule): ScheduleDraft {
  return { configuration: clone(snapshot.configuration), baseline: JSON.stringify(snapshot.configuration), conflict: false, requiresReload: false }
}

// Polling never rebases a local edit. Only an acknowledged save of that exact draft
// can advance its baseline; external changes require an explicit reload.
export function observeSchedule(drafts: Map<string, ScheduleDraft>, homeId: string, snapshot: DeviceSchedule, commands: HomeCommand[]) {
  const key = scheduleKey(homeId, snapshot.deviceId)
  const draft = drafts.get(key)
  if (!draft) { drafts.set(key, reloadDraft(snapshot)); return }
  const submitted = draft.submitted
  const command = submitted && commands.find(item => item.id === submitted.id && item.homeId === homeId && item.deviceId === snapshot.deviceId)
  if (command?.status === 'confirmed' && submitted?.operation === 'schedule.replace') {
    const saved = JSON.parse(submitted.configuration) as ScheduleConfiguration
    saved.version += 1
    if (configurationFingerprint(snapshot.configuration) === configurationFingerprint(saved) &&
      configurationFingerprint(draft.configuration) === configurationFingerprint(JSON.parse(submitted.configuration)) && !draft.requiresReload) {
      drafts.set(key, reloadDraft(snapshot))
      return
    }
  }
  if (snapshot.configuration.version !== draft.configuration.version) draft.conflict = true
  if (command?.status === 'failed' || command?.status === 'unknown' || command?.status === 'expired') draft.requiresReload = true
}

export function scheduleFresh(home: HomeServer, device: HomeDevice, snapshot: DeviceSchedule | undefined, now: number, unavailable: boolean) {
  return !scheduleSnapshotStale(home, device, snapshot, now, unavailable) && canControl(home, device, now, unavailable)
}

export function scheduleSnapshotStale(home: HomeServer, device: HomeDevice, snapshot: DeviceSchedule | undefined, now: number, unavailable: boolean) {
  return !snapshot || snapshot.deviceId !== device.id || snapshot.configuration.deviceId !== device.id ||
    snapshot.stale || !recent(snapshot.snapshotAt, now, 30_000) || isStale(home, device, now, unavailable)
}

function minutes(value: string, end = false): number | null {
  if (end && value === '24:00') return 1440
  if (!/^([01]\d|2[0-3]):[0-5]\d$/.test(value)) return null
  const [hour, minute] = value.split(':').map(Number)
  return hour! * 60 + minute!
}

export function validateSchedule(configuration: ScheduleConfiguration): string[] {
  const errors: string[] = []
  try { new Intl.DateTimeFormat('ru', { timeZone: configuration.timeZone }).format() }
  catch { errors.push('Укажите существующий часовой пояс IANA, например Europe/Tallinn.') }
  if (!/^[A-Za-z][A-Za-z0-9_+/-]{0,99}$/.test(configuration.timeZone) || !Number.isSafeInteger(configuration.version) || configuration.version < 0)
    errors.push('Версия или часовой пояс недопустимы. Загрузите текущий снимок.')
  if (configuration.intervals.length > 64) errors.push('Допустимо не более 64 интервалов.')
  const ids = new Set<string>()
  const spans: { start: number; end: number; on: boolean }[] = []
  configuration.intervals.forEach((interval, index) => {
    const label = `Интервал ${index + 1}`
    if (interval.name.length > 80) errors.push(`${label}: название не более 80 символов.`)
    if (ids.has(interval.id)) errors.push(`${label}: повтор идентификатора.`)
    ids.add(interval.id)
    const start = minutes(interval.start), end = minutes(interval.end, true)
    if (start === null || end === null || start === end) errors.push(`${label}: укажите разное время начала и конца в формате ЧЧ:мм; конец допускает 24:00.`)
    if (!interval.days.length || new Set(interval.days).size !== interval.days.length || interval.days.some(day => !Number.isInteger(day) || day < 1 || day > 7))
      errors.push(`${label}: выберите дни недели.`)
    if (interval.enabled && start !== null && end !== null && start !== end) {
      for (const day of interval.days) {
        const from = (day - 1) * 1440 + start
        const to = (day - 1) * 1440 + end + (end <= start ? 1440 : 0)
        spans.push({ start: from, end: Math.min(to, 10080), on: interval.on })
        if (to > 10080) spans.push({ start: 0, end: to - 10080, on: interval.on })
      }
    }
  })
  if (spans.some((span, index) => spans.slice(index + 1).some(other => span.on !== other.on && span.start < other.end && other.start < span.end)))
    errors.push('Активные интервалы с противоположным действием пересекаются, возможно через полночь.')
  return errors
}

export function commandAction(command: HomeCommand): string {
  return command.operation === 'schedule.replace' ? 'Сохранить расписание' : command.operation === 'schedule.resume' ? 'Возобновить расписание' : command.on ? 'Включить' : 'Выключить'
}

export function scheduleCommandStatus(command: HomeCommand): string | undefined {
  if (command.operation === 'power.set') return undefined
  if (command.status === 'confirmed') return command.operation === 'schedule.replace'
    ? 'Локальный сервер подтвердил сохранение настроек; это не подтверждение переключения питания'
    : 'Локальный сервер подтвердил снятие ручной паузы; это не подтверждение переключения питания'
  if (command.status === 'sent') return 'Отправлена — ожидаем подтверждения локального сервера'
  if (command.status === 'failed') return 'Локальный сервер отклонил изменение; загрузите текущий снимок'
  return undefined
}
