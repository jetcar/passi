import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
    base: '/identity/', // Path relative to project root
  plugins: [
    vue(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      vue: '@vue/compat',

    }
  },
  build: {
    outDir: '../wwwroot'
  },
  server: {
      proxy: {
        '/api': 'http://localhost:5003/identity'
      }
    }
})


