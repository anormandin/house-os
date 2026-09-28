import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter } from 'react-router-dom'

/** Rend un composant sous un QueryClient neuf (pas de retry, pas de cache partagé)
 * et un routeur en mémoire posé sur `url` (les pages lisent des paramètres d'URL,
 * ex. `?id=` venu de la recherche globale). Le client est exposé pour piloter les
 * invalidations/refetchs depuis un test.
 *
 * Le client par défaut est nu : pas de MutationCache, donc aucun traitement global
 * des erreurs. Pour éprouver ce que voit vraiment l'utilisateur quand une mutation
 * échoue (bannière, déconnexion sur 401), passer `creerQueryClient()` — celui de
 * l'app. */
export function rendre(
  ui: ReactElement,
  client: QueryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } }),
  url = '/',
) {
  return {
    ...render(
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[url]}>{ui}</MemoryRouter>
      </QueryClientProvider>,
    ),
    client,
  }
}
