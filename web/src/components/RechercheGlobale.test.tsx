import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { Route, Routes, useLocation } from 'react-router-dom'
import { expect, test } from 'vitest'
import Layout from '@/components/Layout'
import { rendre } from '@/test/rendre'
import { ALAIN, serveur, TACHE_COMPLETE } from '@/test/serveur-msw'

function Emplacement() {
  const { pathname, search } = useLocation()
  return <p data-testid="emplacement">{pathname + search}</p>
}

function rendreCoquille() {
  serveur.use(
    http.get('/api/taches', () =>
      HttpResponse.json([
        {
          id: TACHE_COMPLETE.id, titre: TACHE_COMPLETE.titre, description: null,
          occurrenceId: 'o-1', echeance: null, assigneA: null, zoneId: 'z-cuisine',
          equipementId: null, strategie: 'Fixe', recurrence: { mode: 'Ponctuelle' },
          nbDocuments: 0, completee: false, echeanceFerme: false,
        },
      ])),
    http.get('/api/equipements', () =>
      HttpResponse.json([
        { id: 'e-jura', nom: 'Machine à café', zoneId: 'z-cuisine', marque: 'Jura',
          modele: 'A1', finGarantie: null, nbDocuments: 0 },
      ])),
  )
  return rendre(
    <Routes>
      <Route element={<Layout moi={ALAIN} />}>
        <Route path="*" element={<Emplacement />} />
      </Route>
    </Routes>,
  )
}

test('« / » ouvre la palette ; un document mène à sa fiche', async () => {
  rendreCoquille()
  await userEvent.keyboard('/')
  await userEvent.type(screen.getByRole('combobox', { name: 'Chercher dans la maison' }), 'inspection')

  expect(await screen.findByRole('option', { name: /Rapport d’inspection/ })).toBeInTheDocument()
  await userEvent.keyboard('{Enter}')

  expect(screen.getByTestId('emplacement')).toHaveTextContent('/documents?id=d-rapport')
  expect(screen.queryByRole('dialog', { name: 'Recherche' })).not.toBeInTheDocument()
})

test('on trouve un équipement par sa marque, et une pièce par son nom', async () => {
  rendreCoquille()
  await userEvent.click(screen.getByRole('button', { name: 'Chercher dans la maison' }))
  const champ = screen.getByRole('combobox', { name: 'Chercher dans la maison' })

  await userEvent.type(champ, 'jura')
  await userEvent.click(await screen.findByRole('option', { name: /Machine à café/ }))
  expect(screen.getByTestId('emplacement')).toHaveTextContent('/equipements?id=e-jura')

  await userEvent.click(screen.getByRole('button', { name: 'Chercher dans la maison' }))
  await userEvent.type(screen.getByRole('combobox', { name: 'Chercher dans la maison' }), 'cuisine')
  // La pièce, mais aussi la tâche et l'équipement qui y sont.
  await screen.findByRole('option', { name: /^Cuisine/ })
  expect(screen.getByRole('option', { name: /Machine à café/ })).toBeInTheDocument()
  await userEvent.click(screen.getByRole('option', { name: /^Cuisine/ }))
  expect(screen.getByTestId('emplacement')).toHaveTextContent('/pieces?zone=z-cuisine')
})

test('une tâche s’ouvre dans son éditeur, sans changer de page', async () => {
  rendreCoquille()
  await userEvent.keyboard('/')
  // Sans accents et dans le désordre.
  await userEvent.type(screen.getByRole('combobox', { name: 'Chercher dans la maison' }), 'adresse saaq')
  await userEvent.click(await screen.findByRole('option', { name: /SAAQ/ }))

  await waitFor(() => expect(screen.getByLabelText('Titre')).toHaveValue(TACHE_COMPLETE.titre))
  expect(screen.getByTestId('emplacement')).toHaveTextContent(/^\/$/)
})

test('« / » tapé dans un champ reste un caractère', async () => {
  rendreCoquille()
  await userEvent.click(screen.getByRole('button', { name: 'Chercher dans la maison' }))
  await userEvent.type(screen.getByRole('combobox', { name: 'Chercher dans la maison' }), 'a/b')
  expect(screen.getByRole('combobox', { name: 'Chercher dans la maison' })).toHaveValue('a/b')
})

test('Échap referme la palette', async () => {
  rendreCoquille()
  await userEvent.keyboard('/')
  expect(screen.getByRole('dialog', { name: 'Recherche' })).toBeInTheDocument()
  await userEvent.keyboard('{Escape}')
  expect(screen.queryByRole('dialog', { name: 'Recherche' })).not.toBeInTheDocument()
})
