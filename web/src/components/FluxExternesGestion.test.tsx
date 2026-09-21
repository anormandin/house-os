import { act, fireEvent, screen } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import FluxExternesGestion from '@/components/FluxExternesGestion'
import { api, type FluxExterne } from '@/lib/api'
import { rendre } from '@/test/rendre'

const ABONNEMENT: FluxExterne = {
  id: 'f-collectes',
  nom: 'Collectes',
  url: 'https://exemple.test/collectes.ics',
  type: 'Collecte',
  source: 'Ics',
  actif: true,
  dernierRafraichissementLe: new Date().toISOString(),
  derniereErreur: null,
  nbEvenements: 8,
}

const jours = (n: number) => new Date(Date.now() - n * 24 * 60 * 60 * 1000).toISOString()

const POUSSE: FluxExterne = {
  id: 'f-ville',
  nom: 'Ville',
  url: null,
  type: 'Municipal',
  source: 'Poussee',
  actif: true,
  dernierRafraichissementLe: jours(1),
  derniereErreur: null,
  nbEvenements: 3,
}

function rendreAvec(flux: FluxExterne[]) {
  vi.spyOn(api, 'fluxExternes').mockResolvedValue(flux)
  rendre(<FluxExternesGestion onFermer={() => {}} />)
}

afterEach(() => vi.restoreAllMocks())

test("un calendrier poussé n'a pas de champ URL, et son nom suffit à l'ajouter", async () => {
  const creation = vi.spyOn(api, 'creerFluxExterne').mockResolvedValue(POUSSE)
  rendreAvec([])

  expect(screen.getByLabelText('URL du flux iCal')).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: 'Poussé' }))

  // Plus d'URL à saisir : c'est le dehors qui alimente ce flux.
  expect(screen.queryByLabelText('URL du flux iCal')).not.toBeInTheDocument()
  fireEvent.change(screen.getByLabelText('Nom'), { target: { value: 'Ville' } })
  fireEvent.click(screen.getByRole('button', { name: 'Ajouter' }))
  await act(async () => {})

  expect(creation).toHaveBeenCalledWith({
    nom: 'Ville',
    url: null,
    type: 'Collecte',
    source: 'Poussee',
  })
})

test('un flux poussé se juge sur sa dernière réception, pas sur son compte', async () => {
  rendreAvec([POUSSE])

  expect(await screen.findByText(/Reçu hier/)).toBeInTheDocument()
})

/**
 * Au-delà de sept jours, le fonds de tiroir cesse de publier ce flux : la gestion doit
 * le dire, sinon la panne d'un gratteur reste invisible jusqu'à ce qu'on la cherche.
 */
test('un flux poussé périmé se signale', async () => {
  rendreAvec([{ ...POUSSE, dernierRafraichissementLe: jours(9) }])

  const ligne = await screen.findByText(/Reçu il y a 9 jours/)
  expect(ligne.className).toContain('text-rouge')
})

test("un flux qui n'a jamais rien reçu le dit", async () => {
  rendreAvec([{ ...POUSSE, dernierRafraichissementLe: null, nbEvenements: 0 }])

  expect(await screen.findByText(/Rien reçu pour le moment/)).toBeInTheDocument()
})

test('un abonnement iCal reste jugé sur ses événements à venir', async () => {
  rendreAvec([ABONNEMENT])

  expect(await screen.findByText('8 événements à venir')).toBeInTheDocument()
})

test("la source ne se choisit qu'à la création", async () => {
  rendreAvec([POUSSE])

  expect(screen.getByRole('button', { name: 'Poussé' })).toBeInTheDocument()
  fireEvent.click(await screen.findByRole('button', { name: 'Modifier Ville' }))

  // Changer de source, c'est changer de maître : on en crée un autre.
  expect(screen.queryByRole('button', { name: 'Poussé' })).not.toBeInTheDocument()
  expect(screen.getByText(/POST \/api\/flux-externes\/f-ville\/evenements/)).toBeInTheDocument()
})
