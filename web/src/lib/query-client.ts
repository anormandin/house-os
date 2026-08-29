import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query'
import { ApiError } from '@/lib/api'
import { signalerErreur } from '@/lib/erreurs'
import { journaliser } from '@/lib/journal'

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
        // Les GET restent silencieux à l'écran, mais plus dans le journal : c'est
        // souvent une lecture ratée qui précède le geste qui échoue.
        journaliser('warn', 'Requete', decrire(erreur), {
          cle: JSON.stringify(requete.queryKey),
          traceId: erreur instanceof ApiError ? erreur.traceId : undefined,
        })
        if (requete.queryKey[0] !== 'moi') {
          deconnecterSiSessionExpiree(client, erreur)
        }
      },
    }),
    mutationCache: new MutationCache({
      onError: (erreur, variables, _contexte, mutation) => {
        journaliser('error', 'Mutation', decrire(erreur), {
          cle: JSON.stringify(mutation.options.mutationKey ?? mutation.meta ?? null),
          variables: variables === undefined ? undefined : JSON.stringify(variables),
          traceId: erreur instanceof ApiError ? erreur.traceId : undefined,
        })
        if (erreur instanceof ApiError && erreur.statut === 401) {
          deconnecterSiSessionExpiree(client, erreur)
          return
        }
        if (mutation.meta?.erreurLocale === true) {
          return
        }
        signalerErreur(
          erreur instanceof ApiError ? erreur.message : 'Une erreur est survenue.',
          erreur instanceof ApiError ? erreur.traceId : undefined,
        )
      },
    }),
  })
  return client
}

function decrire(erreur: unknown): string {
  if (erreur instanceof ApiError) {
    return `${erreur.statut} — ${erreur.message}`
  }
  return erreur instanceof Error ? erreur.message : String(erreur)
}

function deconnecterSiSessionExpiree(client: QueryClient, erreur: unknown) {
  const sessionExpiree = erreur instanceof ApiError && erreur.statut === 401
  if (sessionExpiree && client.getQueryData(['moi']) !== undefined) {
    client.resetQueries({ queryKey: ['moi'] })
  }
}
