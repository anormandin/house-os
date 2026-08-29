import { QueryClientProvider, useMutation, type QueryClient } from '@tanstack/react-query'
import { act, renderHook, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import type { ReactNode } from 'react'
import { afterEach, expect, test } from 'vitest'
import { api } from '@/lib/api'
import { effacerErreur, useErreurCourante } from '@/lib/erreurs'
import { creerQueryClient } from '@/lib/query-client'
import { ALAIN, serveur } from '@/test/serveur-msw'

afterEach(() => effacerErreur())

const avecClient = (client: QueryClient) =>
  function Enveloppe({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>
  }

test('un 401 sur une requête réinitialise `moi` — retour à l’écran de connexion', async () => {
  const client = creerQueryClient()
  client.setQueryData(['moi'], ALAIN)
  serveur.use(http.get('/api/zones', () => new HttpResponse(null, { status: 401 })))

  await client.prefetchQuery({ queryKey: ['zones'], queryFn: api.zones })

  await waitFor(() => expect(client.getQueryData(['moi'])).toBeUndefined())
})

test('le 401 de `moi` elle-même ne repart pas en boucle de refetchs', async () => {
  const client = creerQueryClient()
  let appels = 0
  serveur.use(
    http.get('/api/auth/moi', () => {
      appels += 1
      return new HttpResponse(null, { status: 401 })
    }),
  )

  await client.prefetchQuery({ queryKey: ['moi'], queryFn: api.moi })
  await new Promise((resoudre) => setTimeout(resoudre, 25))

  expect(appels).toBe(1)
})

test('un 401 de mutation déconnecte aussi, sans bannière d’erreur', async () => {
  const client = creerQueryClient()
  client.setQueryData(['moi'], ALAIN)
  serveur.use(http.delete('/api/zones/:id', () => new HttpResponse(null, { status: 401 })))
  const { result } = renderHook(
    () => ({
      suppression: useMutation({ mutationFn: () => api.supprimerZone('z-bureau') }),
      erreur: useErreurCourante(),
    }),
    { wrapper: avecClient(client) },
  )

  act(() => result.current.suppression.mutate())

  await waitFor(() => expect(client.getQueryData(['moi'])).toBeUndefined())
  expect(result.current.erreur).toBeNull()
})

test('les autres erreurs de mutation nourrissent la bannière — sauf celles gérées localement', async () => {
  const client = creerQueryClient()
  serveur.use(
    http.delete('/api/zones/:id', () =>
      HttpResponse.json({ message: 'La pièce résiste.' }, { status: 500 })),
  )
  const { result } = renderHook(
    () => ({
      globale: useMutation({ mutationFn: () => api.supprimerZone('z-bureau') }),
      locale: useMutation({
        mutationFn: () => api.supprimerZone('z-cuisine'),
        meta: { erreurLocale: true },
      }),
      erreur: useErreurCourante(),
    }),
    { wrapper: avecClient(client) },
  )

  act(() => result.current.locale.mutate())
  await waitFor(() => expect(result.current.locale.isError).toBe(true))
  expect(result.current.erreur).toBeNull()

  act(() => result.current.globale.mutate())
  await waitFor(() => expect(result.current.erreur?.message).toBe('La pièce résiste.'))
})

test('une erreur sans statut (panne réseau) devient un message générique', async () => {
  const client = creerQueryClient()
  serveur.use(http.delete('/api/zones/:id', () => HttpResponse.error()))
  const { result } = renderHook(
    () => ({
      suppression: useMutation({ mutationFn: () => api.supprimerZone('z-bureau') }),
      erreur: useErreurCourante(),
    }),
    { wrapper: avecClient(client) },
  )

  act(() => result.current.suppression.mutate())

  await waitFor(() => expect(result.current.erreur?.message).toBe('Une erreur est survenue.'))
})

test('une exception locale du mutationFn nourrit aussi la bannière', async () => {
  // Rien à voir avec le réseau : un bug dans la mutation elle-même ne doit pas
  // laisser l'utilisateur devant un écran qui ne bouge pas.
  const client = creerQueryClient()
  const { result } = renderHook(
    () => ({
      cassee: useMutation({
        mutationFn: async () => {
          throw new Error('boum')
        },
      }),
      erreur: useErreurCourante(),
    }),
    { wrapper: avecClient(client) },
  )

  act(() => result.current.cassee.mutate())

  await waitFor(() => expect(result.current.erreur?.message).toBe('Une erreur est survenue.'))
})

test('une requête en échec reste muette : seules les mutations parlent', async () => {
  // Voulu : un GET qui échoue laisse l'écran sur ses données précédentes plutôt
  // que d'ouvrir une bannière à chaque refetch raté. Si ça change un jour, c'est
  // ce test qui doit être rediscuté en premier.
  const client = creerQueryClient()
  serveur.use(http.get('/api/zones', () =>
    HttpResponse.json({ message: 'Base indisponible.' }, { status: 500 })))
  const { result } = renderHook(() => useErreurCourante(), { wrapper: avecClient(client) })

  await client.prefetchQuery({ queryKey: ['zones'], queryFn: api.zones })

  // Ancrage : sans ceci, le test passerait aussi si la requête avait réussi.
  expect(client.getQueryState(['zones'])?.status).toBe('error')
  expect(result.current).toBeNull()
})
