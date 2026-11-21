export type CommandStatus = 'queued' | 'sent' | 'confirmed' | 'failed' | 'unknown' | 'expired'

export interface HomeCommand {
  id: string
  homeId: string
  deviceId: string
  operation: 'power.set'
  on: boolean
  createdAt: string
  expiresAt: string
  status: CommandStatus
  completedAt?: string | null
  errorCode?: string | null
}

export interface HomeDevice {
  id: string
  name: string
  model?: string | null
  online?: boolean | null
  capabilities: string[]
  power?: boolean | null
  temperatureCelsius?: number | null
  updatedAt: string
}

export interface HomeServer {
  id: string
  name: string
  online: boolean
  lastSeenAt?: string | null
  snapshotAt?: string | null
  stale: boolean
  devices: HomeDevice[]
  commands: HomeCommand[]
}

export interface HomeDashboard { serverTime: string; homes: HomeServer[] }
export interface HomeEnrollment { homeId: string; enrollmentSecret: string; expiresAt: string }

export function recent(timestamp: string | null | undefined, now: number, maxAge: number): boolean {
  if (!timestamp) return false
  const age = now - Date.parse(timestamp)
  return Number.isFinite(age) && age >= -5000 && age < maxAge
}

export function isOnline(home: HomeServer, now: number, unavailable: boolean): boolean {
  return !unavailable && home.online && recent(home.lastSeenAt, now, 20_000)
}

export function isStale(home: HomeServer, device: HomeDevice, now: number, unavailable: boolean): boolean {
  return !isOnline(home, now, unavailable) || home.stale || device.online !== true ||
    !recent(home.snapshotAt, now, 30_000) || !recent(device.updatedAt, now, 30_000)
}

export function canControl(home: HomeServer, device: HomeDevice, now: number, unavailable: boolean): boolean {
  return device.capabilities.includes('power.set') && device.capabilities.includes('power.read') &&
    typeof device.power === 'boolean' && !isStale(home, device, now, unavailable) &&
    !home.commands.some(command => command.deviceId === device.id &&
      (command.status === 'queued' || command.status === 'sent'))
}

export const commandLabels: Record<CommandStatus, string> = {
  queued: 'Ожидает отправки',
  sent: 'Отправлена — ожидаем подтверждения устройства',
  confirmed: 'Устройство подтвердило результат',
  failed: 'Устройство сообщило об ошибке',
  unknown: 'Результат неизвестен — автоматического повтора не будет',
  expired: 'Срок истёк — команда не выполнена',
}
