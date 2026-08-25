import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'

/** Rend un composant sous un QueryClient neuf (pas de retry, pas de cache partagé).
 * Le client est exposé pour piloter les invalidations/refetchs depuis un test. */
export function rendre(ui: ReactElement) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return { ...render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>), client }
}
