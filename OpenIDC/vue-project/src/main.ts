import './assets/site.css'
import { createApp } from 'vue'
import App from './App.vue'
import i18n from './i18n'
import router from './router'

const app = createApp(App)
app.use(i18n)
app.use(router)

document.documentElement.lang = i18n.global.locale.value
app.mount('#app')
