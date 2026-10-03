import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    strictPort: false,
    watch: { usePolling: false },
    proxy: {
      '/api': {
        target: 'http://localhost:5102',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
