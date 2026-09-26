import react from '@vitejs/plugin-react'
import path from 'path'
import { fileURLToPath } from 'url'
import { defineConfig, loadEnv } from 'vite'

// One .env at the project root supplies both the API and this mobile web app.
const envDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, envDir, '')
  return {
    plugins: [react()],
    envDir,
    server: {
      host: env.VITE_DEV_HOST || '0.0.0.0',
      port: Number(env.VITE_DEV_PORT || 5173),
      strictPort: true,
    },
  }
})
