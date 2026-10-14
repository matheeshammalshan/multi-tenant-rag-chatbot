import react, { reactCompilerPreset } from '@vitejs/plugin-react'
import babel from '@rolldown/plugin-babel'
import { defineConfig, loadEnv } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())
  const apiTarget = env.VITE_API_URL || 'http://localhost:5270'

  const apiRoutes = ['/Auth', '/ChatBot', '/ChatBots', '/ChatbotAccess', '/Api', '/Dashboard',
    '/Document', '/Documents', '/SemanticSearch', '/Organization', '/Organizations', '/Users', '/ChatHistory', '/Models', '/LlmConfig']

  const proxy: Record<string, object> = {}
  apiRoutes.forEach(route => {
    proxy[route] = { target: apiTarget, changeOrigin: true, secure: false, ws: true }
  })

  return {
    plugins: [
      react(),
      babel({ presets: [reactCompilerPreset()] })
    ],
    server: {
      port: 5173,
      proxy,
    }
  }
})
