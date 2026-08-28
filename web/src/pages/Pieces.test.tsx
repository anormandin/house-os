import { fireEvent, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { expect, test } from 'vitest'
import Pieces from '@/pages/Pieces'
import { rendre } from '@/test/rendre'
import { serveur } from '@/test/serveur-msw'

function servir() {
  const puts: unknown[] = []
  serveur.use(
    http.get('/api/occurrences', () => HttpResponse.json([])),
    http.get('/api/evenements-externes', () => HttpResponse.json([])),
    http.put('/api/zones/:id', async ({ request }) => {
      puts.push(await request.json())
      return new HttpResponse(null, { status: 204 })
    }),
  )
  return puts
}

async function ouvrirRenommage() {
  await userEvent.click(await screen.findByRole('button', { name: /Bureau/ }))
  await userEvent.click(screen.getByRole('button', { name: 'Renommer la pièce' }))
  return screen.getByLabelText('Nouveau nom de la pièce')
}

test('un nom vide ne part jamais au serveur — le renommage se referme simplement', async () => {
  const puts = servir()
  rendre(<Pieces />)
  const champ = await ouvrirRenommage()

  await userEvent.clear(champ)
  await userEvent.keyboard('{Enter}')

  expect(puts).toHaveLength(0)
  expect(screen.queryByLabelText('Nouveau nom de la pièce')).not.toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Bureau' })).toBeInTheDocument()
})

test('Entrée soumet un seul PUT, même quand le blur suit', async () => {
  const puts = servir()
  rendre(<Pieces />)
  const champ = await ouvrirRenommage()

  await userEvent.clear(champ)
  await userEvent.type(champ, 'Atelier')
  await userEvent.keyboard('{Enter}')
  fireEvent.blur(champ)

  await waitFor(() => expect(puts).toHaveLength(1))
  expect(puts[0]).toMatchObject({ nom: 'Atelier' })
  await waitFor(() =>
    expect(screen.queryByLabelText('Nouveau nom de la pièce')).not.toBeInTheDocument())
  expect(puts).toHaveLength(1)
})

test('Échap abandonne le renommage sans rien envoyer', async () => {
  const puts = servir()
  rendre(<Pieces />)
  const champ = await ouvrirRenommage()

  await userEvent.clear(champ)
  await userEvent.type(champ, 'Grenier')
  await userEvent.keyboard('{Escape}')

  expect(puts).toHaveLength(0)
  expect(screen.queryByLabelText('Nouveau nom de la pièce')).not.toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Bureau' })).toBeInTheDocument()
})
