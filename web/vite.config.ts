import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      // App desktop-web servie sur le LAN : le pré-cache offline n'apporte rien et
      // retarde chaque déploiement d'un rechargement. selfDestroying publie un SW
      // qui désenregistre ceux déjà installés ; le manifest (installable) reste.
      selfDestroying: true,
      registerType: 'autoUpdate',
      manifest: {
        name: 'House OS',
        short_name: 'Maison',
        description: 'Gestion de la maison',
        lang: 'fr',
        theme_color: '#f2ecbc',
        background_color: '#f2ecbc',
        display: 'standalone',
      },
    }),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
      '/ical': 'http://localhost:5000',
    },
  },
})
