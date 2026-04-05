import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Proxy each KubeCart service path to its local port during development.
    // Matches the path-based routing the K8s ingress uses in production.
    proxy: {
      '/api/auth': {
        target: 'http://localhost:5001',  // identity-service
        changeOrigin: true,
      },
      '/api/catalog': {
        target: 'http://localhost:5002',  // catalog-service
        changeOrigin: true,
      },
      '/api/orders': {
        target: 'http://localhost:5003',  // order-service
        changeOrigin: true,
      },
    },
  },
})
