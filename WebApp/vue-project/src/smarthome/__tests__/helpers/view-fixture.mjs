import assert from 'node:assert/strict'
import fs from 'node:fs'
import vm from 'node:vm'
import { webcrypto } from 'node:crypto'
import { parse, compileScript } from '@vue/compiler-sfc'
import ts from 'typescript'
import * as vue from 'vue'
import * as state from '../../state.ts'
import * as schedules from '../../schedules.ts'

const filename = new URL('../../../views/SmartHomeView.vue', import.meta.url)
const { descriptor } = parse(fs.readFileSync(filename, 'utf8'))
const compiled = compileScript(descriptor, {
  id: 'smart-home-test', inlineTemplate: true,
  templateOptions: { compilerOptions: { hoistStatic: false } },
})
const script = ts.transpileModule(compiled.content, {
  compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS },
}).outputText
const editorFile = new URL('../../ScheduleEditor.vue', import.meta.url)
const editorDescriptor = parse(fs.readFileSync(editorFile, 'utf8')).descriptor
const editorScript = ts.transpileModule(compileScript(editorDescriptor, {
  id: 'schedule-editor-test', inlineTemplate: true,
  templateOptions: { compilerOptions: { hoistStatic: false } },
}).content, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS } }).outputText

export const settle = async () => { await new Promise(resolve => setImmediate(resolve)); await vue.nextTick() }
export const reply = (data, status = 200) => ({ ok: status >= 200 && status < 300, status, json: async () => structuredClone(data) })

// Exercise the real Vue setup, template, reactivity and model directives using
// Vue's renderer API. Only host elements, timers and HTTP are synthetic.
// https://vuejs.org/api/custom-renderer.html
// v-model checks the root constructor when replacing a text input's value.
globalThis.Document ??= class SyntheticDocument {}
globalThis.ShadowRoot ??= class SyntheticShadowRoot {}
class HostNode {
  constructor(tag, text = '') {
    this.tag = tag; this.tagName = tag.toUpperCase(); this.text = text
    this.children = []; this.parent = null; this.props = {}; this.events = new Map()
    this.value = ''; this.selected = false; this.selectedIndex = -1
  }
  get textContent() { return this.text + this.children.map(child => child.textContent).join('') }
  getRootNode() { return this.parent ? this.parent.getRootNode() : this }
  get options() { return this.children.filter(child => child.tag === 'option') }
  get selectedIndex() { return this.options.findIndex(option => option.selected) }
  set selectedIndex(index) { this.options.forEach((option, position) => { option.selected = position === index }) }
  addEventListener(name, listener) {
    if (!this.events.has(name)) this.events.set(name, [])
    this.events.get(name).push(listener)
  }
  removeEventListener(name, listener) { this.events.set(name, (this.events.get(name) || []).filter(fn => fn !== listener)) }
  dispatch(name) { for (const fn of this.events.get(name) || []) fn({ target: this }) }
  setAttribute(name, value) { this.props[name] = value }
  removeAttribute(name) { delete this.props[name] }
}

export async function mountView(t, respond) {
  const requests = []
  const timers = new Map()
  let nextTimer = 0
  const exports = {}
  const editorExports = {}
  const context = vm.createContext({
    exports,
    require(name) {
      if (name === 'vue') return vue
      if (name === '../smarthome/state') return state
      if (name === './state') return state
      if (name === '../smarthome/schedules' || name === './schedules') return schedules
      if (name === '../smarthome/ScheduleEditor.vue') return editorExports
      throw new Error(`Unexpected import: ${name}`)
    },
    AbortController, crypto: webcrypto,
    window: { confirm() { return true } },
    setTimeout: (fn, delay) => { const id = ++nextTimer; timers.set(id, { fn, delay }); return id },
    clearTimeout: id => timers.delete(id),
    setInterval: (fn, delay) => { const id = ++nextTimer; timers.set(id, { fn, delay, repeat: true }); return id },
    clearInterval: id => timers.delete(id),
    fetch: async (url, options) => {
      const request = { url, ...options }
      requests.push(request)
      assert.ok(url === '/api/smarthome' || url === '/api/smarthome/csrf' ||
        /^\/api\/smarthome\/homes\/[^/]+\/devices\/[^/]+\/(power|schedule(?:\/resume)?)$/.test(url), `Unexpected endpoint: ${url}`)
      return respond(request)
    },
  })
  context.exports = editorExports
  vm.runInContext(`(function () { ${editorScript}\n})()`, context, { filename: 'ScheduleEditor.compiled.cjs' })
  context.exports = exports
  vm.runInContext(script, context, { filename: 'SmartHomeView.compiled.cjs' })
  const detach = node => {
    if (node.parent) node.parent.children.splice(node.parent.children.indexOf(node), 1)
    node.parent = null
  }
  const renderer = vue.createRenderer({
    createElement: tag => new HostNode(tag),
    createText: text => new HostNode('#text', text),
    createComment: () => new HostNode('#comment'),
    setText: (node, text) => { node.text = text },
    setElementText: (node, text) => { node.text = text; node.children = [] },
    patchProp: (node, key, previous, next) => {
      node.props[key] = key === 'disabled' ? next === '' || Boolean(next) : next
      if (key === 'value') { node.value = next; node._value = next }
      if (key === 'multiple') node.multiple = next
    },
    insert(node, parent, anchor = null) {
      detach(node); node.parent = parent
      const index = anchor ? parent.children.indexOf(anchor) : -1
      if (index < 0) parent.children.push(node)
      else parent.children.splice(index, 0, node)
    },
    remove: detach,
    parentNode: node => node.parent,
    nextSibling: node => node.parent?.children[node.parent.children.indexOf(node) + 1] || null,
    setScopeId() {},
  })
  const root = new HostNode('root')
  const app = renderer.createApp(exports.default)
  app.config.errorHandler = reason => { throw reason }
  app.mount(root)
  t.after(() => app.unmount())
  const all = (predicate, node = root) => {
    const found = predicate(node) ? [node] : []
    return found.concat(node.children.flatMap(child => all(predicate, child)))
  }
  const byId = id => all(node => node.props.id === id)[0]
  const home = id => all(node => node.props['data-home-id'] === id)[0]
  const device = (homeId, deviceId) => {
    const parent = home(homeId)
    assert.ok(parent, `Server ${homeId} must be visible`)
    const found = all(node => node.props['data-device-id'] === deviceId, parent)[0]
    assert.ok(found, `Device ${deviceId} must be nested under ${homeId}`)
    return found
  }
  const powerButton = (homeId, deviceId, on) => all(node => node.tag === 'button' &&
    node.textContent === (on ? 'Включить' : 'Выключить'), device(homeId, deviceId))[0]
  await settle()
  return {
    root, requests, all, byId, home, device, powerButton,
    async choose(id) {
      const select = byId('server-filter')
      assert.ok(select, 'Server filter must be rendered')
      assert.ok(select.options.some(option => option.value === id), `Filter option ${id} must exist`)
      for (const option of select.options) option.selected = option.value === id
      select.dispatch('change')
      await settle()
    },
    async poll() {
      const entry = [...timers].find(([, timer]) => timer.delay === 5000)
      assert.ok(entry, 'Dashboard must schedule polling')
      timers.delete(entry[0]); await entry[1].fn(); await settle()
    },
  }
}
