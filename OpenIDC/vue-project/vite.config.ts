import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
    base: '/openidc/', // Path relative to project root

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
        outDir: '../wwwroot',
        minify: false, // Disable minification
        rollupOptions: { treeshake: false },
    },
    server: {
        proxy: {
            '/api': 'http://localhost:5005/openidc'
        }
    }
})