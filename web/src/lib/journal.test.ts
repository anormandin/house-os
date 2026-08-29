import { http, HttpResponse } from 'msw'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { journaliser, reinitialiserJournal, vider } from '@/lib/journal'
import { serveur } from '@/test/serveur-msw'

type LotRecu = {
  sessionId?: string
  origine?: string
  evenements?: { niveau: string; message: string; categorie: string; proprietes?: unknown }[]
}

let lots: LotRecu[] = []

beforeEach(() => {
  lots = []
  reinitialiserJournal()
  serveur.use(http.post('/api/journal-client', async ({ request }) => {
    lots.push((await request.json()) as LotRecu)
    return new HttpResponse(null, { status: 202 })
  }))
})

afterEach(() => {
  vi.useRealTimers()
  reinitialiserJournal()
})

test('un lot part avec la session et l’origine, seule clé de regroupement dans Seq', async () => {
  journaliser('info', 'Geste', 'Complétion demandée', { occurrenceId: 'o-1' })

  await vider()

  const lot = lots[0]
  expect(lot.evenements).toHaveLength(1)
  expect(lot.evenements![0]).toMatchObject({
    niveau: 'info',
    categorie: 'Geste',
    message: 'Complétion demandée',
    proprietes: { occurrenceId: 'o-1' },
  })
  expect(lot.sessionId).toBeTruthy()
  expect(lot.origine).toBe(location.origin)
})

test('le traceId sort du sac de propriétés : c’est lui qui relie au log serveur', async () => {
  journaliser('warn', 'Api', 'POST /api/x → 503', { traceId: 'abc123', dureeMs: 12 })

  await vider()

  const evenement = lots[0].evenements![0] as { traceId?: string; proprietes?: Record<string, string> }
  expect(evenement.traceId).toBe('abc123')
  // Sinon il arriverait aussi en propriété libre, préfixée « Client » côté serveur,
  // et la corrélation se ferait sur la mauvaise clé.
  expect(evenement.proprietes).not.toHaveProperty('traceId')
})

test('le tampon se vide tout seul au seuil, sans attendre la minuterie', async () => {
  vi.useFakeTimers()
  for (let i = 0; i < 20; i++) {
    journaliser('debug', 'Navigation', `écran ${i}`)
  }
  vi.useRealTimers()
  // La vidange déclenchée par le seuil est asynchrone : laisser passer un tour.
  await vi.waitFor(() => expect(lots).toHaveLength(1))

  expect(lots[0].evenements).toHaveLength(20)
})

test('le tampon est circulaire : une rafale ne fait pas enfler la page', async () => {
  vi.useFakeTimers()
  // 250 évènements pour un tampon de 200 et un lot de 50 : sans la borne, la page
  // garderait tout en mémoire pendant une boucle d'erreurs.
  for (let i = 0; i < 250; i++) {
    journaliser('error', 'Rendu', `plantage ${i}`)
  }
  vi.useRealTimers()
  await vi.waitFor(() => expect(lots.length).toBeGreaterThan(0))

  const total = lots.reduce((somme, lot) => somme + (lot.evenements?.length ?? 0), 0)
  expect(total).toBeLessThanOrEqual(250)
  expect(lots[0].evenements!.length).toBeLessThanOrEqual(50)
})

test('un envoi qui échoue ne se journalise pas lui-même', async () => {
  // La boucle à éviter : l'échec produit un évènement, qui déclenche une vidange,
  // qui échoue… Le lot est perdu, en silence, et c'est voulu.
  serveur.use(http.post('/api/journal-client', () => HttpResponse.error()))
  journaliser('error', 'Api', 'panne réseau')

  await expect(vider()).resolves.toBeUndefined()
  expect(lots).toHaveLength(0)
})

test('vider sans rien en tampon n’envoie aucune requête', async () => {
  await vider()

  expect(lots).toHaveLength(0)
})
