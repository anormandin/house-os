import { http, HttpResponse } from 'msw'
import { expect, test } from 'vitest'
import { api, ApiError } from '@/lib/api'
import { serveur } from '@/test/serveur-msw'

async function messagePour(corps: unknown, statut = 500): Promise<string> {
  serveur.use(http.get('/api/zones', () =>
    corps === undefined
      ? new HttpResponse(null, { status: statut })
      : HttpResponse.json(corps, { status: statut })))
  try {
    await api.zones()
  } catch (e) {
    return (e as ApiError).message
  }
  throw new Error('aurait dû lever')
}

test('un ProblemDetails complet privilégie message, puis errors, puis title', async () => {
  expect(await messagePour({ message: 'Occurrence déjà complétée.' }, 409))
    .toBe('Occurrence déjà complétée.')
  expect(await messagePour({ errors: { titre: ['Le titre est requis.'] }, title: 'Bad Request' }, 400))
    .toBe('Le titre est requis.')
  expect(await messagePour({ title: 'Bad Request' }, 400)).toBe('Bad Request')
})

test('les ProblemDetails dégénérés retombent sur le statut', async () => {
  expect(await messagePour({ errors: {} }, 400)).toBe('Erreur serveur (400)')
  expect(await messagePour({ errors: { titre: [] } }, 400)).toBe('Erreur serveur (400)')
  expect(await messagePour(undefined)).toBe('Erreur serveur (500)') // corps vide
  expect(await messagePour(null)).toBe('Erreur serveur (500)')     // corps « null »
  expect(await messagePour(123)).toBe('Erreur serveur (500)')      // corps non-objet
})

test("un message non textuel n'est jamais propagé tel quel", async () => {
  // Un objet imbriqué finirait rendu comme enfant React dans la bannière → écran blanc.
  expect(await messagePour({ message: { niche: true } })).toBe('Erreur serveur (500)')
  expect(await messagePour({ message: 42 })).toBe('Erreur serveur (500)')
})

test('un 401 devient une ApiError explicite', async () => {
  serveur.use(http.get('/api/zones', () => new HttpResponse(null, { status: 401 })))

  await expect(api.zones()).rejects.toMatchObject({ statut: 401 })
})
