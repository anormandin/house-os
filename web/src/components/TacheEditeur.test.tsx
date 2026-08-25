import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { expect, test, vi } from 'vitest'
import TacheEditeur from '@/components/TacheEditeur'
import { rendre } from '@/test/rendre'
import { serveur, TACHE_COMPLETE, TACHE_RECURRENTE, ZONES } from '@/test/serveur-msw'

test('charge tous les champs du détail, échéance incluse', async () => {
  rendre(<TacheEditeur tacheId={TACHE_COMPLETE.id} onFermer={() => {}} />)

  const titre = await screen.findByLabelText('Titre')
  await waitFor(() => expect(titre).toHaveValue(TACHE_COMPLETE.titre))
  expect(screen.getByLabelText('Description')).toHaveValue('Ligne 1\nLigne 2')
  expect(screen.getByLabelText('Échéance')).toHaveValue('2026-08-30')
  expect(screen.getByLabelText('Pièce')).toHaveValue(TACHE_COMPLETE.zoneId)
  expect(screen.getByLabelText('Assigner à')).toHaveValue(TACHE_COMPLETE.assigneAId)
})

test('enregistrer sans rien modifier renvoie le détail tel quel (rien ne se perd)', async () => {
  let corpsEnvoye: unknown = null
  serveur.use(
    http.put('/api/taches/:id', async ({ request }) => {
      corpsEnvoye = await request.json()
      return new HttpResponse(null, { status: 204 })
    }),
  )
  const onFermer = vi.fn()
  rendre(<TacheEditeur tacheId={TACHE_COMPLETE.id} onFermer={onFermer} />)
  await waitFor(() =>
    expect(screen.getByLabelText('Titre')).toHaveValue(TACHE_COMPLETE.titre),
  )

  await userEvent.click(screen.getByRole('button', { name: 'Enregistrer' }))

  await waitFor(() => expect(onFermer).toHaveBeenCalled())
  expect(corpsEnvoye).toEqual({
    titre: TACHE_COMPLETE.titre,
    description: 'Ligne 1\nLigne 2',
    echeance: '2026-08-30',
    assigneAId: TACHE_COMPLETE.assigneAId,
    zoneId: TACHE_COMPLETE.zoneId,
    strategie: 'Fixe',
  })
})

test('une hebdo en saison se recharge et se réenregistre à l’identique', async () => {
  let corpsEnvoye: unknown = null
  serveur.use(
    http.put('/api/taches/:id', async ({ request }) => {
      corpsEnvoye = await request.json()
      return new HttpResponse(null, { status: 204 })
    }),
  )
  const onFermer = vi.fn()
  rendre(<TacheEditeur tacheId={TACHE_RECURRENTE.id} onFermer={onFermer} />)
  await waitFor(() =>
    expect(screen.getByLabelText('Titre')).toHaveValue(TACHE_RECURRENTE.titre))

  await userEvent.click(screen.getByRole('button', { name: 'Enregistrer' }))

  // Le rejeu du bug d'échéance, version récurrence : une fiche ré-enregistrée sans
  // modification ne doit perdre ni les jours, ni la fenêtre, ni la stratégie.
  await waitFor(() => expect(onFermer).toHaveBeenCalled())
  expect(corpsEnvoye).toEqual({
    titre: TACHE_RECURRENTE.titre,
    description: 'Bac bleu au chemin',
    echeance: '2026-09-02',
    assigneAId: TACHE_RECURRENTE.assigneAId,
    zoneId: TACHE_RECURRENTE.zoneId,
    strategie: 'Alternance',
    recurrence: {
      mode: 'Fixe',
      fixeType: 'JoursSemaine',
      joursSemaine: [1, 3, 5],
      rollover: true,
      fenetreDebutMois: 5,
      fenetreDebutJour: 1,
      fenetreFinMois: 10,
      fenetreFinJour: 31,
    },
  })
})

test('un refetch du détail n’écrase pas la saisie en cours', async () => {
  const { client } = rendre(<TacheEditeur tacheId={TACHE_COMPLETE.id} onFermer={() => {}} />)
  const titre = await screen.findByLabelText('Titre')
  await waitFor(() => expect(titre).toHaveValue(TACHE_COMPLETE.titre))

  await userEvent.clear(titre)
  await userEvent.type(titre, 'Titre en cours de frappe')
  // Le serveur répond désormais autre chose (autre onglet, invalidation globale).
  serveur.use(
    http.get('/api/taches/:id', () =>
      HttpResponse.json({ ...TACHE_COMPLETE, titre: 'Écrasé par le serveur' })),
  )
  await client.refetchQueries({ queryKey: ['tache', TACHE_COMPLETE.id] })

  expect(titre).toHaveValue('Titre en cours de frappe')
})

test('une nouvelle tâche depuis une pièce précoche cette pièce', async () => {
  rendre(<TacheEditeur tacheId={null} zoneInitialeId={ZONES[0].id} onFermer={() => {}} />)

  expect(screen.getByRole('heading', { name: 'Nouvelle tâche' })).toBeInTheDocument()
  await waitFor(() => expect(screen.getByLabelText('Pièce')).toHaveValue(ZONES[0].id))
})

test('Échap ferme le modal', async () => {
  const onFermer = vi.fn()
  rendre(<TacheEditeur tacheId={null} onFermer={onFermer} />)

  await userEvent.keyboard('{Escape}')

  expect(onFermer).toHaveBeenCalled()
})
