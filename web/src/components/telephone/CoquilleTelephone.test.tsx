import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { expect, test } from 'vitest'
import CoquilleTelephone from '@/components/telephone/CoquilleTelephone'
import { dateLocaleIso } from '@/lib/api'
import { rendre } from '@/test/rendre'
import { ALAIN, serveur } from '@/test/serveur-msw'

function rendreCoquille() {
  return rendre(
    <MemoryRouter initialEntries={['/']}>
      <Routes>
        <Route element={<CoquilleTelephone moi={ALAIN} />}>
          <Route path="/" element={<p>écran d’accueil</p>} />
          <Route path="/taches" element={<p>console des tâches</p>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

test('le menu des sections est fermé au départ et s’ouvre au bouton', async () => {
  rendreCoquille()

  // La direction « La pile » n'a pas de barre d'onglets : rien ne navigue tant
  // que le menu n'est pas ouvert.
  expect(screen.queryByRole('navigation', { name: 'Sections' })).not.toBeInTheDocument()

  await userEvent.click(screen.getByRole('button', { name: 'Ouvrir le menu des sections' }))

  const menu = screen.getByRole('navigation', { name: 'Sections' })
  expect(menu).toBeInTheDocument()
  for (const libelle of ["Aujourd'hui", 'Tâches', 'Pièces', 'Équipements', 'Documents', 'Budget']) {
    expect(screen.getByRole('link', { name: new RegExp(libelle) })).toBeInTheDocument()
  }
})

test('choisir une destination referme le menu', async () => {
  rendreCoquille()
  await userEvent.click(screen.getByRole('button', { name: 'Ouvrir le menu des sections' }))

  await userEvent.click(screen.getByRole('link', { name: /Tâches/ }))

  expect(await screen.findByText('console des tâches')).toBeInTheDocument()
  expect(screen.queryByRole('navigation', { name: 'Sections' })).not.toBeInTheDocument()
})

test('le menu porte la pastille des tâches en retard — ce qu’une barre d’onglets ne peut pas faire', async () => {
  const hier = new Date()
  hier.setDate(hier.getDate() - 1)
  serveur.use(
    http.get('/api/occurrences', ({ request }) => {
      const filtre = new URL(request.url).searchParams.get('filtre')
      if (filtre !== 'en-attente') {
        return HttpResponse.json([])
      }
      return HttpResponse.json([
        {
          id: 'o1',
          tacheId: 't1',
          titre: 'Nettoyer les gouttières',
          description: null,
          echeance: dateLocaleIso(hier),
          statut: 'EnAttente',
          assigneA: null,
          completeePar: null,
          completeeLe: null,
          notes: null,
          zoneId: null,
          equipementId: null,
          modeRecurrence: 'Ponctuelle',
        },
      ])
    }),
  )
  rendreCoquille()

  await userEvent.click(screen.getByRole('button', { name: 'Ouvrir le menu des sections' }))

  expect(await screen.findByRole('link', { name: /Tâches\s*1/ })).toBeInTheDocument()
})
