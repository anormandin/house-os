import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { expect, test } from 'vitest'
import Lettre from '@/pages/Lettre'
import { rendre } from '@/test/rendre'
import { serveur } from '@/test/serveur-msw'

const lettre = {
  date: '2026-09-22',
  sujet: 'Rien à faire, sauf sortir les bacs',
  paragraphes: ['Je ne vous demande rien aujourd’hui.', 'Douze dodos avant le camion.'],
  source: 'Llm',
  modele: 'claude-opus-5',
  composeeLe: '2026-09-22T06:28:00-04:00',
  envoyeeLe: '2026-09-22T06:31:00-04:00',
  destinataires: ['alain@exemple.tld'],
  ecrite: true,
  envoiActif: true,
  destinatairesPrevus: ['alain@exemple.tld'],
  texte:
    'Mardi 22 septembre 2026\n\nBonjour vous deux.\n\nJe ne vous demande rien aujourd’hui.\n\nDouze dodos avant le camion.\n\nBonne journée.\n\n— la maison\n\nHouse OS · écrite à 6 h 28\n',
  html: '<p>…</p>',
}

test('la lettre envoyée se lit comme le courriel, avec son état', async () => {
  serveur.use(http.get('/api/lettre', () => HttpResponse.json(lettre)))
  rendre(<Lettre />)

  expect(await screen.findByText('Rien à faire, sauf sortir les bacs')).toBeInTheDocument()
  expect(screen.getByText('Bonjour vous deux.')).toBeInTheDocument()
  expect(screen.getByText('— la maison')).toBeInTheDocument()
  expect(screen.getByText(/Envoyée à 6 h 31 à alain@exemple.tld/)).toBeInTheDocument()
})

test('un aperçu jamais écrit le dit, et l’essai part à moi', async () => {
  const essais: unknown[] = []
  serveur.use(
    http.get('/api/lettre', () => HttpResponse.json({ ...lettre, ecrite: false, envoyeeLe: null, source: 'Gabarit' })),
    http.post('/api/lettre/essai', async ({ request }) => {
      essais.push(await request.json())
      return HttpResponse.json({ envoyeA: 'alain@exemple.tld' })
    }),
  )
  rendre(<Lettre />)

  expect(await screen.findByText(/Aperçu en gabarit/)).toBeInTheDocument()
  await userEvent.click(screen.getByRole('button', { name: "M'envoyer un essai" }))
  expect(await screen.findByRole('status')).toHaveTextContent('Essai envoyé à alain@exemple.tld.')
  expect(essais).toHaveLength(1)
})

test('sans serveur de courriel, l’essai est désactivé', async () => {
  serveur.use(http.get('/api/lettre', () => HttpResponse.json({ ...lettre, envoyeeLe: null, envoiActif: false })))
  rendre(<Lettre />)

  expect(await screen.findByText(/aucun serveur de courriel/)).toBeInTheDocument()
  expect(screen.getByRole('button', { name: "M'envoyer un essai" })).toBeDisabled()
})
