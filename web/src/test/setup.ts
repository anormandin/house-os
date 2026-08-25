import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { serveur } from './serveur-msw'

// L'app fait des fetch relatifs (`/api/...`) ; le fetch de Node exige une URL
// absolue — on préfixe avec l'origine jsdom pour que MSW puisse intercepter.
const fetchNode = globalThis.fetch
globalThis.fetch = (entree, init) =>
  fetchNode(
    typeof entree === 'string' && entree.startsWith('/')
      ? `${location.origin}${entree}`
      : entree,
    init,
  )

beforeAll(() => serveur.listen({ onUnhandledRequest: 'error' }))
afterEach(() => {
  serveur.resetHandlers()
  cleanup()
})
afterAll(() => serveur.close())
