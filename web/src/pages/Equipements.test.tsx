import { act, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { expect, test } from 'vitest'
import Equipements from '@/pages/Equipements'
import { rendre } from '@/test/rendre'
import { serveur } from '@/test/serveur-msw'

const RESUME = {
  id: 'e-fournaise', nom: 'Fournaise', zoneId: null, marque: 'Carrier',
  modele: null, finGarantie: null, nbDocuments: 0,
}
const DETAIL = {
  id: 'e-fournaise', nom: 'Fournaise', zoneId: null, marque: 'Carrier', modele: 'X9',
  numeroSerie: 'SN-12', dateAchat: null, finGarantie: null, notes: 'Filtre 16 × 25',
  specs: {}, documents: [], entretiens: [],
}

function servirFournaise() {
  let lectures = 0
  serveur.use(
    http.get('/api/equipements', () => HttpResponse.json([RESUME])),
    http.get('/api/equipements/e-fournaise', () => {
      lectures += 1
      return HttpResponse.json(DETAIL)
    }),
  )
  return () => lectures
}

test('un refetch du détail (invalidation par un document) n’écrase pas les édits en cours', async () => {
  const lectures = servirFournaise()
  const { client } = rendre(<Equipements />)

  await userEvent.click(await screen.findByText('Fournaise'))
  const notes = await screen.findByLabelText('Notes')
  await waitFor(() => expect(notes).toHaveValue('Filtre 16 × 25'))

  await userEvent.clear(notes)
  await userEvent.type(notes, 'Nouveau filtre commandé')

  await act(() => client.invalidateQueries({ queryKey: ['equipement'] }))
  await waitFor(() => expect(lectures()).toBeGreaterThanOrEqual(2))

  expect(screen.getByLabelText('Notes')).toHaveValue('Nouveau filtre commandé')
})

test('« Annuler » sur un équipement existant restaure la fiche du serveur', async () => {
  servirFournaise()
  rendre(<Equipements />)

  await userEvent.click(await screen.findByText('Fournaise'))
  const nom = await screen.findByLabelText('Nom')
  await waitFor(() => expect(nom).toHaveValue('Fournaise'))

  await userEvent.clear(nom)
  await userEvent.type(nom, 'Thermopompe')
  await userEvent.click(screen.getByRole('button', { name: 'Annuler' }))

  expect(screen.getByLabelText('Nom')).toHaveValue('Fournaise')
  expect(screen.getByLabelText('Notes')).toHaveValue('Filtre 16 × 25')
})

test('les notes acceptent plusieurs lignes (issue #59)', async () => {
  servirFournaise()
  rendre(<Equipements />)

  await userEvent.click(await screen.findByText('Fournaise'))
  const notes = await screen.findByLabelText('Notes')
  await waitFor(() => expect(notes).toHaveValue('Filtre 16 × 25'))

  expect(notes.tagName).toBe('TEXTAREA')
  await userEvent.clear(notes)
  await userEvent.type(notes, 'Filtre 16 × 25{enter}Courroie A-32')
  expect(notes).toHaveValue('Filtre 16 × 25\nCourroie A-32')
})
