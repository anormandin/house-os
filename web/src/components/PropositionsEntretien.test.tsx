import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { expect, test, vi } from 'vitest'
import PropositionsEntretien from '@/components/PropositionsEntretien'
import { rendre } from '@/test/rendre'
import { serveur } from '@/test/serveur-msw'

const PROPOSITIONS = {
  pack: 'Chauffage',
  equipementId: 'e-thermopompe',
  equipement: 'Thermopompe',
  raison: null,
  propositions: [
    {
      cle: 'chauffage.filtre', titre: 'Changer ou nettoyer le filtre', description: 'Aux trois mois.',
      recurrence: { mode: 'Intervalle', intervalleJours: 90 }, strategie: 'Alternance',
      dejaPresente: false, tacheExistanteId: null,
    },
    {
      cle: 'chauffage.entretien', titre: 'Entretien annuel par un technicien', description: null,
      recurrence: { mode: 'Fixe', fixeType: 'Annuelle', moisAnnuel: 9, jourAnnuel: 15,
        fenetreDebutMois: 9, fenetreDebutJour: 1, fenetreFinMois: 10, fenetreFinJour: 31 },
      strategie: null, dejaPresente: false, tacheExistanteId: null,
    },
    {
      cle: 'chauffage.plinthes', titre: 'Passer l’aspirateur dans les plinthes', description: null,
      recurrence: { mode: 'Fixe', fixeType: 'Annuelle', moisAnnuel: 10, jourAnnuel: 1 },
      strategie: 'Alternance', dejaPresente: true, tacheExistanteId: 't-plinthes',
    },
  ],
}

test('les propositions partent cochées, le déjà présent est grisé, et l’adoption n’envoie que le reste', async () => {
  let corps: { cles: string[]; equipementId: string | null } | null = null
  serveur.use(
    http.get('/api/entretien/propositions', ({ request }) => {
      expect(new URL(request.url).searchParams.get('equipementId')).toBe('e-thermopompe')
      return HttpResponse.json(PROPOSITIONS)
    }),
    http.post('/api/entretien/adopter', async ({ request }) => {
      corps = (await request.json()) as typeof corps
      return HttpResponse.json(
        { creees: [{ id: 't1', titre: 'Changer ou nettoyer le filtre', echeance: '2026-12-28' }] },
        { status: 201 },
      )
    }),
  )
  const onFermer = vi.fn()
  rendre(<PropositionsEntretien equipementId="e-thermopompe" titre="Entretiens — Thermopompe" onFermer={onFermer} />)

  const filtre = await screen.findByLabelText('Changer ou nettoyer le filtre')
  const technicien = screen.getByLabelText('Entretien annuel par un technicien')
  const plinthes = screen.getByLabelText('Passer l’aspirateur dans les plinthes')
  expect(filtre).toBeChecked()
  expect(technicien).toBeChecked()
  expect(plinthes).toBeChecked()
  expect(plinthes).toBeDisabled()
  expect(screen.getByText('déjà dans vos tâches')).toBeInTheDocument()
  expect(screen.getByText('↻ aux 90 jours')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Créer 2 tâches' })).toBeEnabled()

  await userEvent.click(technicien)
  await userEvent.click(screen.getByRole('button', { name: 'Créer 1 tâche' }))

  await waitFor(() => expect(corps).not.toBeNull())
  expect(corps).toEqual({ cles: ['chauffage.filtre'], equipementId: 'e-thermopompe' })
  await waitFor(() => expect(onFermer).toHaveBeenCalled())
})

test('un équipement pas encore classé lit la raison et ne peut rien créer', async () => {
  serveur.use(
    http.get('/api/entretien/propositions', () =>
      HttpResponse.json({
        pack: 'maison', equipementId: 'e-velo', equipement: 'Vélo',
        raison: 'Classe d’abord cet équipement : les propositions dépendent de sa catégorie.',
        propositions: [],
      })),
  )
  rendre(<PropositionsEntretien equipementId="e-velo" titre="Entretiens — Vélo" onFermer={() => {}} />)

  expect(await screen.findByText(/Classe d’abord cet équipement/)).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Créer 0 tâches' })).toBeDisabled()
})

test('le programme de la maison se demande sans équipement', async () => {
  let url: string | null = null
  serveur.use(
    http.get('/api/entretien/propositions', ({ request }) => {
      url = request.url
      return HttpResponse.json({ pack: 'maison', equipementId: null, equipement: null, raison: null, propositions: [] })
    }),
  )
  rendre(<PropositionsEntretien equipementId={null} titre="Programme de la maison" onFermer={() => {}} />)

  await screen.findByText('Rien à proposer.')
  expect(url).not.toContain('equipementId')
})
