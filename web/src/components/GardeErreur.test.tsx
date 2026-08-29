import { render, screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import GardeErreur from '@/components/GardeErreur'
import { reinitialiserJournal } from '@/lib/journal'
import { serveur } from '@/test/serveur-msw'

type LotRecu = { evenements?: { categorie: string; message: string }[] }

let lots: LotRecu[] = []

function QuiPlante(): never {
  throw new Error('boum au rendu')
}

beforeEach(() => {
  lots = []
  reinitialiserJournal()
  serveur.use(http.post('/api/journal-client', async ({ request }) => {
    lots.push((await request.json()) as LotRecu)
    return new HttpResponse(null, { status: 202 })
  }))
  // React écrit lui-même l'erreur attrapée sur la console : la museler garde la
  // sortie des tests lisible sans masquer l'assertion.
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  vi.restoreAllMocks()
  reinitialiserJournal()
})

test('un plantage de rendu affiche un écran de secours au lieu d’une page blanche', () => {
  render(
    <GardeErreur>
      <QuiPlante />
    </GardeErreur>,
  )

  expect(screen.getByText("L'écran a planté.")).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Recharger' })).toBeInTheDocument()
})

test('le plantage part tout de suite au serveur — Alain va recharger dans la seconde', async () => {
  render(
    <GardeErreur>
      <QuiPlante />
    </GardeErreur>,
  )

  await vi.waitFor(() => expect(lots).toHaveLength(1))
  expect(lots[0].evenements![0]).toMatchObject({
    categorie: 'Rendu',
    message: 'boum au rendu',
  })
})

test('un arbre sain passe sans être touché', () => {
  render(
    <GardeErreur>
      <p>Tout va bien</p>
    </GardeErreur>,
  )

  expect(screen.getByText('Tout va bien')).toBeInTheDocument()
})
