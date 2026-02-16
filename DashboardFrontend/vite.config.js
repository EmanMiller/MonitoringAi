import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    headers: {
      'Content-Security-Policy':
        "default-src 'self'; " +
        "connect-src 'self' http://localhost:* https://localhost:* https://generativelanguage.googleapis.com; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline';"
    }
  }
})
