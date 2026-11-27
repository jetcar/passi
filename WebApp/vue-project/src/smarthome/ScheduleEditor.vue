<script setup lang="ts">
import { computed } from 'vue'
import { canAdvanceRevision, draftChanged, validateSchedule, scheduleCommandStatus } from './schedules'
import type { DeviceSchedule, ScheduleDraft } from './schedules'
import type { HomeCommand } from './state'
import { commandLabels } from './state'

const props = defineProps<{
  snapshot?: DeviceSchedule; draft?: ScheduleDraft; blocked: boolean; stale: boolean
  pending: boolean; command?: HomeCommand
}>()
const emit = defineEmits<{ save: []; resume: []; reload: [] }>()
const days = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс']
const validation = computed(() => props.draft ? validateSchedule(props.draft.configuration) : [])
const conflict = computed(() => props.draft?.conflict || props.draft?.requiresReload)
const date = (value?: string | null) => value ? new Date(value).toLocaleString('ru-RU') : 'нет данных'
const power = (value?: boolean | null) => value === true ? 'Включено' : value === false ? 'Выключено' : 'Неизвестно'
function addInterval() {
  if (!props.draft || props.draft.configuration.intervals.length >= 64) return
  props.draft.configuration.intervals.push({ id: crypto.randomUUID(), name: 'Новый интервал', enabled: true,
    days: [1, 2, 3, 4, 5], start: '06:00', end: '09:00', on: true })
}
function reload() {
  if (props.draft && draftChanged(props.draft) && !window.confirm('Заменить несохранённый черновик текущими настройками локального сервера?')) return
  emit('reload')
}
</script>

<template>
<section class="schedule-editor border-top mt-4 pt-3" aria-label="Расписание устройства">
  <h4 class="h6 mb-2"><i class="bi bi-calendar-week me-1" aria-hidden="true"></i>Недельное расписание</h4>
  <p v-if="!snapshot || !draft" class="small text-secondary mb-0">Для расписания требуется обновление локального агента и свежий снимок поддерживаемого устройства.</p>
  <template v-else>
    <div class="small mb-3">
      <span class="badge me-1" :class="stale ? 'text-bg-warning' : 'text-bg-light'">{{ stale ? 'Снимок устарел' : 'Снимок актуален' }}</span>
      <span>Версия на сервере: {{ snapshot.configuration.version }} · {{ snapshot.configuration.enabled ? 'Расписание включено' : 'Расписание выключено' }}</span>
      <div class="text-secondary mt-1">Получено: {{ date(snapshot.snapshotAt) }}</div>
      <div class="text-secondary">Состояние исполнителя: {{ snapshot.runtime.state }}</div>
      <div>Последнее фактическое питание: {{ power(snapshot.runtime.actualPower) }}</div>
      <div>Требуемое расписанием: {{ snapshot.runtime.desiredPower == null ? 'Не управлять' : power(snapshot.runtime.desiredPower) }}</div>
      <div class="text-secondary">Проверено дома: {{ date(snapshot.runtime.checkedAt) }}</div>
      <div class="text-secondary">Следующая проверка: {{ date(snapshot.runtime.nextCheckAt) }}</div>
      <div class="text-secondary">Следующая смена состояния: {{ date(snapshot.runtime.nextBoundary) }}</div>
      <div v-if="snapshot.runtime.error" class="text-danger">Ошибка локального исполнителя: {{ snapshot.runtime.error }}</div>
    </div>
    <div v-if="snapshot.runtime.manualActive" class="alert alert-info small py-2">
      <p class="mb-2">Ручной режим {{ snapshot.runtime.manualUntil ? `до ${date(snapshot.runtime.manualUntil)}` : 'без установленного срока' }}. Ревизия: {{ snapshot.runtime.revision }}.</p>
      <button type="button" class="btn btn-sm btn-outline-primary" :disabled="blocked || conflict || !canAdvanceRevision(snapshot.runtime.revision)" @click="emit('resume')">Возобновить расписание</button>
    </div>
    <p v-if="command" class="small mb-3" :class="command.status === 'confirmed' ? 'text-success' : 'text-secondary'" role="status">{{ scheduleCommandStatus(command) ?? commandLabels[command.status] }}</p>
    <details>
      <summary class="fw-semibold mb-3">Редактировать расписание <span v-if="draftChanged(draft)" class="badge text-bg-warning">Черновик</span></summary>
      <p class="small text-secondary">Настройки выполняет домашний сервер. Сохранение не означает переключения питания. Черновик хранится только в этой вкладке.</p>
      <p v-if="conflict" class="alert alert-warning small" role="alert">Снимок изменился или результат предыдущей операции требует проверки. Черновик сохранён на версии {{ draft.configuration.version }}; текущая версия {{ snapshot.configuration.version }}. Перед новой отправкой явно загрузите текущие настройки.</p>
      <form @submit.prevent="emit('save')">
        <fieldset :disabled="pending">
          <legend class="visually-hidden">Настройки расписания</legend>
          <label class="form-check mb-3"><input v-model="draft.configuration.enabled" type="checkbox" class="form-check-input" /><span class="form-check-label">Расписание включено</span></label>
          <label class="form-label d-block">Часовой пояс IANA
            <input v-model="draft.configuration.timeZone" class="form-control form-control-sm mt-1" maxlength="100" placeholder="Europe/Tallinn" required />
          </label>
          <label class="form-label d-block">Вне интервалов
            <select v-model="draft.configuration.outsidePower" class="form-select form-select-sm mt-1">
              <option :value="false">Выключать</option><option :value="true">Включать</option><option :value="null">Не управлять</option>
            </select>
          </label>
          <p class="small text-secondary">Конец не включается. Если конец раньше начала, интервал заканчивается на следующий день. 00:00–24:00 — весь день.</p>
          <div v-for="(interval, index) in draft.configuration.intervals" :key="interval.id" class="border rounded p-2 p-md-3 mb-3" :aria-label="`Интервал ${index + 1}`">
            <div class="d-flex align-items-center justify-content-between gap-2 mb-2">
              <label class="form-check mb-0"><input v-model="interval.enabled" type="checkbox" class="form-check-input" /><span class="form-check-label">Интервал {{ index + 1 }} активен</span></label>
              <button type="button" class="btn btn-sm btn-outline-secondary" :aria-label="`Убрать интервал ${index + 1} из черновика`" @click="draft.configuration.intervals.splice(index, 1)">Убрать</button>
            </div>
            <label class="form-label d-block small">Название интервала {{ index + 1 }}<input v-model="interval.name" class="form-control form-control-sm mt-1" maxlength="80" /></label>
            <fieldset class="mb-2"><legend class="small mb-1">Дни недели интервала {{ index + 1 }}</legend><div class="d-flex flex-wrap gap-2">
              <label v-for="(day, dayIndex) in days" :key="day" class="form-check form-check-inline me-0"><input v-model="interval.days" class="form-check-input" type="checkbox" :value="dayIndex + 1" /><span class="form-check-label">{{ day }}</span></label>
            </div></fieldset>
            <div class="row g-2">
              <label class="col-6 form-label small">Начало {{ index + 1 }}<input v-model="interval.start" type="time" step="60" required class="form-control form-control-sm mt-1" /></label>
              <label class="col-6 form-label small">Конец {{ index + 1 }}<input v-model="interval.end" type="text" inputmode="numeric" pattern="([01][0-9]|2[0-3]):[0-5][0-9]|24:00" placeholder="24:00" required class="form-control form-control-sm mt-1" /></label>
            </div>
            <label class="form-label d-block small mb-0">Питание в интервале {{ index + 1 }}<select v-model="interval.on" class="form-select form-select-sm mt-1"><option :value="true">Включать</option><option :value="false">Выключать</option></select></label>
          </div>
          <p v-if="!draft.configuration.intervals.length" class="small text-secondary">Интервалов нет. При включённом расписании применяется поведение «Вне интервалов».</p>
          <button type="button" class="btn btn-sm btn-outline-secondary mb-3" :disabled="draft.configuration.intervals.length >= 64" @click="addInterval">Добавить интервал</button>
        </fieldset>
        <ul v-if="validation.length" class="small text-danger ps-3"><li v-for="message in validation" :key="message">{{ message }}</li></ul>
        <div class="d-flex flex-wrap gap-2">
          <button type="submit" class="btn btn-sm btn-primary" :disabled="blocked || conflict || validation.length > 0 || !draftChanged(draft) || !canAdvanceRevision(draft.configuration.version)">Сохранить расписание</button>
          <button type="button" class="btn btn-sm btn-outline-secondary" :disabled="blocked" @click="reload">Загрузить текущие настройки</button>
        </div>
      </form>
    </details>
  </template>
</section>
</template>

<style scoped>
summary { cursor: pointer; }
.schedule-editor { min-width: 0; }
</style>
