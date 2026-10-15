import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  server: {
    // The API runs on its own port in development; proxying keeps the browser on one origin, so the API needs no CORS.
    proxy: { '/api': 'http://localhost:5055' },
  },
})
