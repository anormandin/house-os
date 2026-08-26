import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, expect, test, vi } from 'vitest'
import Taches from '@/pages/Taches'
import type { TacheResume } from '@/lib/api'
import { ALAIN, serveur, TACHE_COMPLETE } from '@/test/serveur-msw'
import { rendre } from '@/test/rendre'

function resume(partiel: Partial<TacheResume> & { id: string; titre: string }): TacheResume {
  return {
    description: null,
    echeance: null,
    assigneA: null,
    zoneId: null,
    equipementId: null,
    strategie: 'Fixe',
    recurrence: { mode: 'Ponctuelle' },
    nbDocuments: 0,
    completee: false,
    ...partiel,
  }
}

function servirTaches(liste: TacheResume[]) {
  serveur.use(
    http.get('/api/taches', () => HttpResponse.json(liste)),
    http.get('/api/comptes-a-rebours', () => HttpResponse.json([])),
  )
}

function dansNJours(n: number): string {
  const d = new Date()
  d.setDate(d.getDate() + n)
  return d.toLocaleDateString('fr-CA')
}

const HIER = dansNJours(-1)

const CORPUS: TacheResume[] = [
  resume({
    id: 'r-1',
    titre: 'Changer les draps',
    recurrence: { mode: 'Fixe', fixeType: 'JoursSemaine', joursSemaine: [0] },
    strategie: 'Alternance',
    assigneA: ALAIN,
  }),
  resume({
    id: 'r-2',
    titre: 'Passer l’aspirateur',
    recurrence: { mode: 'Intervalle', intervalleJours: 4 },
    echeance: HIER,
  }),
  resume({ id: TACHE_COMPLETE.id, titre: 'Notaire — répartitions', echeance: dansNJours(5) }),
  resume({ id: 'p-faite', titre: 'Déjà réglée', completee: true }),
]

// L'environnement jsdom de Vitest n'expose pas localStorage : stub en mémoire,
// remis à neuf entre les tests (la page tolère aussi son absence).
beforeEach(() => {
  const stockage = new Map<string, string>()
  vi.stubGlobal('localStorage', {
    getItem: (cle: string) => stockage.get(cle) ?? null,
    setItem: (cle: string, valeur: string) => void stockage.set(cle, valeur),
  })
})

test('la vue Liste groupe par rythme, compte les retards et la progression', async () => {
  servirTaches(CORPUS)
  rendre(<Taches />)

  expect(await screen.findByText('Chaque semaine')).toBeInTheDocument()
  expect(screen.getByText('Aux quelques jours')).toBeInTheDocument()
  expect(screen.getByText('1 en retard')).toBeInTheDocument()
  expect(screen.getByText('Alternance')).toBeInTheDocument()
  // La ponctuelle faite ne s'affiche pas mais compte dans la progression.
  expect(screen.queryByText('Déjà réglée')).not.toBeInTheDocument()
  expect(screen.getByText('1 faites sur 2')).toBeInTheDocument()
})

test('le commutateur bascule vers l’Année et le mode survit à une réouverture', async () => {
  servirTaches(CORPUS)
  const { unmount } = rendre(<Taches />)
  await screen.findByText('Chaque semaine')

  await userEvent.click(screen.getByRole('button', { name: 'Année' }))
  // Le ruban, sa bande de ponctuelles et le tempo court remplacent les groupes.
  expect(await screen.findByText('fenêtre en cours')).toBeInTheDocument()
  expect(screen.getByText('Le tempo court')).toBeInTheDocument()
  expect(screen.getByText('Les ponctuelles — semaine par semaine')).toBeInTheDocument()
  expect(screen.getByText('Jalons')).toBeInTheDocument()
  expect(screen.queryByText('Chaque semaine')).not.toBeInTheDocument()

  // Réouverture : la page se souvient du dernier mode.
  unmount()
  servirTaches(CORPUS)
  rendre(<Taches />)
  expect(await screen.findByText('fenêtre en cours')).toBeInTheDocument()
})

test('cliquer une rangée ouvre l’éditeur de la tâche', async () => {
  servirTaches(CORPUS)
  rendre(<Taches />)

  await userEvent.click(await screen.findByText('Notaire — répartitions'))

  // L'éditeur charge le détail (TACHE_COMPLETE) — invariant édition-sans-perte.
  await waitFor(() =>
    expect(screen.getByLabelText('Titre')).toHaveValue(TACHE_COMPLETE.titre))
})
