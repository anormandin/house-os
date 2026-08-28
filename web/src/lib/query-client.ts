import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query'
import { ApiError } from '@/lib/api'
import { signalerErreur } from '@/lib/erreurs'

/**
 * QueryClient de l'app : traitement global des erreurs.
 *
 * - 401 (session expirée) : la requête `moi` est réinitialisée pour ramener à
 *   l'écran de connexion — TanStack v5 conserve sinon les données périmées et
 *   l'app resterait figée, clics silencieux. Le 401 de `moi` elle-même est
 *   ignoré (sinon boucle de refetchs).
 * - Toute autre erreur de mutation remonte dans la bannière globale, sauf si la
 *   mutation la gère localement (`meta: { erreurLocale: true }`).
 */
export function creerQueryClient(): QueryClient {
  const client: QueryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, staleTime: 30_000 } },
    queryCache: new QueryCache({
      onError: (erreur, requete) => {
        if (requete.queryKey[0] !== 'moi') {
          deconnecterSiSessionExpiree(client, erreur)
        }
      },
    }),
    mutationCache: new MutationCache({
      onError: (erreur, _variables, _contexte, mutation) => {
        if (erreur instanceof ApiError && erreur.statut === 401) {
          deconnecterSiSessionExpiree(client, erreur)
          return
        }
        if (mutation.meta?.erreurLocale === true) {
          return
        }
        signalerErreur(erreur instanceof ApiError ? erreur.message : 'Une erreur est survenue.')
      },
    }),
  })
  return client
}

function deconnecterSiSessionExpiree(client: QueryClient, erreur: unknown) {
  const sessionExpiree = erreur instanceof ApiError && erreur.statut === 401
  if (sessionExpiree && client.getQueryData(['moi']) !== undefined) {
    client.resetQueries({ queryKey: ['moi'] })
  }
}
