import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// The browser only talks to Vite; '/api' is forwarded to the C# API
// (http profile of backend/CharliesSneakersPOS.Api/Properties/launchSettings.json).
// No CORS is needed because the requests are same-origin.
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5274',
        changeOrigin: true,
      },
    },
  },
})
