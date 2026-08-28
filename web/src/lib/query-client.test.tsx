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
