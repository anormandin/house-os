import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, expect, test, vi } from 'vitest'
import Budget from '@/pages/Budget'
import type { EnveloppeBudget, ResumeBudget, TransactionBudget } from '@/lib/api'
import { serveur } from '@/test/serveur-msw'
import { rendre } from '@/test/rendre'

function enveloppe(
  partiel: Partial<EnveloppeBudget> & { id: string; nom: string; type: EnveloppeBudget['type'] },
): EnveloppeBudget {
  return {
    montantCible: null,
    dateCible: null,
    dateEffective: null,
    tacheId: null,
    titreTache: null,
    equipementId: null,
    nomEquipement: null,
    echeancier: null,
    statut: 'Active',
    solde: 0,
    provision: 0,
    enRetard: false,
    echeancierARenouveler: false,
    ...partiel,
  }
}

const ENVELOPPES: EnveloppeBudget[] = [
  enveloppe({
    id: 'env-taxes',
    nom: 'Taxes municipales',
    type: 'Taxes',
    montantCible: 3720,
    dateEffective: '2026-09-01',
    echeancier: [
      { date: '2026-09-01', montant: 1240 },
      { date: '2027-03-01', montant: 1240 },
    ],
    solde: 1950,
    provision: 310,
  }),
  enveloppe({
    id: 'env-toiture',
    nom: 'Repeindre la toiture métallique',
    type: 'Equipement',
    montantCible: 4500,
    tacheId: 't-toiture',
    titreTache: 'Repeindre la toiture (métallique)',
    dateEffective: '2033-06-01',
    solde: 2260,
    provision: 28,
  }),
  enveloppe({ id: 'env-reserve', nom: 'Réserve — imprévus', type: 'Reserve', solde: 2800 }),
]

const RESUME: ResumeBudget = {
  compte: {
    id: 'c-1',
    nom: 'Fonds de prévoyance',
    institution: 'Desjardins',
    soldeInitial: 12_000,
    dateAncrage: '2026-01-01',
    tacheVirementId: 't-vir',
    titreTacheVirement: 'Virer au fonds de prévoyance',
  },
  soldeCourant: 12_480,
  totalEnveloppes: 7010,
  nonAffecte: 5470,
  virementSuggere: 338,
  occurrenceVirementId: 'o-vir',
  enveloppes: ENVELOPPES,
  sorties: [
    { date: '2026-09-01', nom: 'Taxes municipales', montant: 1240, enveloppeId: 'env-taxes' },
    { date: '2033-06-01', nom: 'Repeindre la toiture métallique', montant: 4500, enveloppeId: 'env-toiture' },
  ],
  nbTransactionsNouvelles: 2,
}

const TRANSACTIONS: TransactionBudget[] = [
  {
    id: 'tx-retrait',
    date: '2026-08-24',
    montant: -84.12,
    description: 'CANADIAN TIRE #213',
    statut: 'Nouvelle',
    suggestionEnveloppeId: 'env-reserve',
    suggestionNom: 'Réserve — imprévus',
    suggererVentilation: false,
  },
  {
    id: 'tx-depot',
    date: '2026-08-26',
    montant: 338,
    description: 'VIR INTERAC DEPOT',
    statut: 'Nouvelle',
    suggestionEnveloppeId: null,
    suggestionNom: null,
    suggererVentilation: true,
  },
]

function servirBudget(resume: ResumeBudget = RESUME, transactions: TransactionBudget[] = TRANSACTIONS) {
  serveur.use(
    http.get('/api/budget', () => HttpResponse.json(resume)),
    http.get('/api/budget/transactions', () => HttpResponse.json(transactions)),
  )
}

beforeEach(() => {
  const stockage = new Map<string, string>()
  vi.stubGlobal('localStorage', {
    getItem: (cle: string) => stockage.get(cle) ?? null,
    setItem: (cle: string, valeur: string) => void stockage.set(cle, valeur),
  })
})

test('l’aperçu montre le chemin de l’argent, le rythme et les enveloppes', async () => {
  servirBudget()
  rendre(<Budget />)

  expect(await screen.findByText('Le rythme mensuel')).toBeInTheDocument()
  // L'invariant tient : solde = enveloppes + non affecté.
  expect(screen.getByText('solde = enveloppes + non affecté')).toBeInTheDocument()
  // Les enveloppes portent leurs liens et leurs états.
  expect(screen.getByText('Tâche « Repeindre la toiture (métallique) »')).toBeInTheDocument()
  expect(screen.getByText('pas de cible ni de provision — le coussin')).toBeInTheDocument()
  // L'inbox propose sans jamais lier automatiquement.
  expect(screen.getByText('répartir selon les provisions')).toBeInTheDocument()
  expect(screen.getByText('Réserve — imprévus', { selector: 'b' })).toBeInTheDocument()
})

test('la sur-allocation affiche le bandeau permanent', async () => {
  servirBudget({ ...RESUME, nonAffecte: -320, totalEnveloppes: 12_800 })
  rendre(<Budget />)

  const alerte = await screen.findByRole('alert')
  expect(alerte).toHaveTextContent('Sur-allocation')
  expect(alerte).toHaveTextContent('320')
})

test('le commutateur bascule vers le Flux et le mode survit à une réouverture', async () => {
  servirBudget()
  const { unmount } = rendre(<Budget />)
  await screen.findByText('Le rythme mensuel')

  await userEvent.click(screen.getByRole('button', { name: 'Flux' }))
  expect(await screen.findByText('Le chemin de l’argent')).toBeInTheDocument()
  expect(screen.getByText('Les enveloppes')).toBeInTheDocument()
  expect(screen.queryByText('Le rythme mensuel')).not.toBeInTheDocument()

  unmount()
  servirBudget()
  rendre(<Budget />)
  expect(await screen.findByText('Le chemin de l’argent')).toBeInTheDocument()
})

test('lier un retrait envoie une ventilation mono-enveloppe (suggestion présélectionnée)', async () => {
  servirBudget()
  let corps: unknown = null
  serveur.use(
    http.post('/api/budget/transactions/tx-retrait/lier', async ({ request }) => {
      corps = await request.json()
      return new HttpResponse(null, { status: 204 })
    }),
  )
  rendre(<Budget />)

  const rangee = (await screen.findByText('CANADIAN TIRE #213')).closest('div')!
  await userEvent.click(within(rangee).getByRole('button', { name: 'Lier' }))
  expect(await screen.findByText('Lier le retrait')).toBeInTheDocument()
  // La suggestion (Réserve) est présélectionnée.
  expect(screen.getByLabelText('Enveloppe du retrait')).toHaveValue('env-reserve')

  const modal = screen.getByText('Lier le retrait').closest('div')!
  await userEvent.click(within(modal).getByRole('button', { name: 'Lier' }))

  await waitFor(() =>
    expect(corps).toEqual({
      ventilation: [{ enveloppeId: 'env-reserve', montant: 84.12 }],
      entreeJournalId: null,
    }))
})

test('ventiler un dépôt préremplit les provisions et peut compléter le virement', async () => {
  servirBudget()
  let corps: unknown = null
  let virementComplete = false
  serveur.use(
    http.post('/api/budget/transactions/tx-depot/lier', async ({ request }) => {
      corps = await request.json()
      return new HttpResponse(null, { status: 204 })
    }),
    http.post('/api/occurrences/o-vir/completer', () => {
      virementComplete = true
      return new HttpResponse(null, { status: 204 })
    }),
  )
  rendre(<Budget />)

  const rangee = (await screen.findByText('VIR INTERAC DEPOT')).closest('div')!
  await userEvent.click(within(rangee).getByRole('button', { name: 'Lier' }))
  expect(await screen.findByText('Ventiler le dépôt')).toBeInTheDocument()

  // Pré-rempli par les provisions suggérées : 310 + 28 = 338, reste 0.
  expect(screen.getByLabelText('Part pour Taxes municipales')).toHaveValue(310)
  expect(screen.getByLabelText('Part pour Repeindre la toiture métallique')).toHaveValue(28)

  const modal = screen.getByText('Ventiler le dépôt').closest('div')!
  await userEvent.click(within(modal).getByRole('button', { name: 'Lier' }))

  await waitFor(() =>
    expect(corps).toEqual({
      ventilation: [
        { enveloppeId: 'env-taxes', montant: 310 },
        { enveloppeId: 'env-toiture', montant: 28 },
      ],
      entreeJournalId: null,
    }))
  await waitFor(() => expect(virementComplete).toBe(true))
})

test('sans compte ancré, la page offre le formulaire d’ancrage', async () => {
  rendre(<Budget />)

  expect(await screen.findByText('Ancrer le fonds de prévoyance')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Ancrer le compte' })).toBeDisabled()
})
