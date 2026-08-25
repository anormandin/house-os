import { defineConfig } from '@playwright/test'

// Fumée E2E contre la pile dev réelle : Postgres (5433) + API (5000) doivent
// tourner (skill demarrer). Vite est démarré/réutilisé automatiquement.
export default defineConfig({
  testDir: 'e2e',
  use: {
    baseURL: process.env.E2E_URL ?? 'http://localhost:5173',
    locale: 'fr-CA',
  },
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: true,
  },
})
