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

test('un corps qui n’est pas du JSON retombe sur le statut', async () => {
  // Le cas d'un intermédiaire qui répond à la place du serveur : le proxy de dev
  // rend du HTML en 503, une passerelle rend du texte. `reponse.json()` lève, et
  // ce qui doit sortir est un message lisible, pas l'exception de parsing.
  serveur.use(http.get('/api/zones', () =>
    new HttpResponse('<html><body>503 Service Unavailable</body></html>', {
      status: 503,
      headers: { 'Content-Type': 'text/html' },
    })))

  await expect(api.zones()).rejects.toMatchObject({
    statut: 503,
    message: 'Erreur serveur (503)',
  })
})

test('un JSON tronqué retombe aussi sur le statut', async () => {
  serveur.use(http.get('/api/zones', () =>
    new HttpResponse('{"message": "coupé au mil', {
      status: 502,
      headers: { 'Content-Type': 'application/json' },
    })))

  await expect(api.zones()).rejects.toMatchObject({ message: 'Erreur serveur (502)' })
})

test('une panne réseau remonte telle quelle, sans devenir une ApiError', async () => {
  // Serveur injoignable, connexion coupée : il n'y a pas de statut à rapporter.
  // Le traitement global la traduit en message générique (voir query-client).
  serveur.use(http.get('/api/zones', () => HttpResponse.error()))

  await expect(api.zones()).rejects.not.toBeInstanceOf(ApiError)
})

test("l'identifiant de trace du serveur voyage jusqu'à l'ApiError", async () => {
  // C'est ce qu'Alain recopie de la bannière pour retrouver la requête dans Seq :
  // sans lui, un incident reste un récit sans preuve.
  serveur.use(http.get('/api/zones', () =>
    HttpResponse.json({ message: 'Occurrence déjà complétée.' }, {
      status: 409,
      headers: { 'X-Trace-Id': 'e857c25eb3e3fbbbf09cddef581c117b' },
    })))

  await expect(api.zones()).rejects.toMatchObject({
    statut: 409,
    traceId: 'e857c25eb3e3fbbbf09cddef581c117b',
  })
})

test("une réponse sans en-tête de trace laisse simplement le champ vide", async () => {
  // Un intermédiaire (proxy Vite, NPM) peut répondre à la place du serveur : la
  // bannière doit alors afficher le message seul, sans « réf. undefined ».
  serveur.use(http.get('/api/zones', () => new HttpResponse(null, { status: 502 })))

  try {
    await api.zones()
  } catch (e) {
    expect((e as ApiError).traceId).toBeUndefined()
    return
  }
  throw new Error('aurait dû lever')
})
