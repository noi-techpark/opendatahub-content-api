import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    port: 3000,
    proxy: {
      '/api/v1/content/Sensor': {
        target: 'http://localhost:8082', // <--- New Target
        changeOrigin: true,
        secure: false, // Use if the target is http
        // This rewrite:
        // /api/v1/content/Sensor?...
        // becomes
        // /v1/Sensor?...
        rewrite: (path) => path.replace(/^\/api\/v1\/content/, '/v1')
      },
      '/api/v1/content': {
        target: 'https://tourism.opendatahub.com',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/v1\/content/, '/v1')
      },
      '/api/v2/timeseries': {
        target: 'https://mobility.api.opendatahub.com',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/v2\/timeseries/, '')
      },
      '/api/v1/timeseries': {
        target: 'http://localhost:8080',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/v1\/timeseries/, '/api/v1')
      }
    }
  }
})
