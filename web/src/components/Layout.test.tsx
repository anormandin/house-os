import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, test } from 'vitest'
import BanniereErreur from '@/components/BanniereErreur'
import Layout from '@/components/Layout'
import { effacerErreur } from '@/lib/erreurs'
import { rendre } from '@/test/rendre'
import { ALAIN, FLUX_ICAL, FLUX_ICAL_TOURNE, serveur } from '@/test/serveur-msw'

afterEach(() => effacerErreur())

function ouvrirCalendrier() {
  return userEvent.click(screen.getByRole('button', { name: 'Mon calendrier iCal' }))
}

test('« Mon calendrier » montre les deux URLs : publique (Google) et interne (Tailscale)', async () => {
  rendre(
    <MemoryRouter>
      <Layout moi={ALAIN} />
    </MemoryRouter>,
  )

  await ouvrirCalendrier()

  expect(await screen.findByText(FLUX_ICAL.urlPublique)).toBeInTheDocument()
  expect(screen.getByText('Pour Google Agenda (publique)')).toBeInTheDocument()
  expect(screen.getByText('Sur Tailscale (interne)')).toBeInTheDocument()
  expect(screen.getByText(new URL(FLUX_ICAL.chemin, window.location.origin).href))
    .toBeInTheDocument()
})

test('la rotation demande confirmation puis remplace les URLs affichées', async () => {
  rendre(
    <MemoryRouter>
      <Layout moi={ALAIN} />
    </MemoryRouter>,
  )
  await ouvrirCalendrier()
  await screen.findByText(FLUX_ICAL.urlPublique)

  // Premier clic : rien ne part au serveur, on avertit que les abonnements cassent.
  await userEvent.click(screen.getByRole('button', { name: /Régénérer/ }))
  expect(screen.getByText(FLUX_ICAL.urlPublique)).toBeInTheDocument()

  await userEvent.click(screen.getByRole('button', { name: /Confirmer la rotation/ }))

  expect(await screen.findByText(FLUX_ICAL_TOURNE.urlPublique)).toBeInTheDocument()
  expect(screen.queryByText(FLUX_ICAL.urlPublique)).not.toBeInTheDocument()
})

test('une déconnexion qui échoue est signalée — la session locale reste intacte', async () => {
  serveur.use(http.post('/api/auth/deconnexion', () => new HttpResponse(null, { status: 500 })))
  const { client } = rendre(
    <MemoryRouter>
      <Layout moi={ALAIN} />
      <BanniereErreur />
    </MemoryRouter>,
  )
  client.setQueryData(['moi'], ALAIN)

  await userEvent.click(screen.getByRole('button', { name: 'Se déconnecter' }))

  expect(await screen.findByText('La déconnexion a échoué — réessaie.')).toBeInTheDocument()
  expect(client.getQueryData(['moi'])).toEqual(ALAIN)
})

test('la déconnexion réussie vide le cache — le prochain `moi` ramène à la connexion', async () => {
  serveur.use(http.post('/api/auth/deconnexion', () => new HttpResponse(null, { status: 204 })))
  const { client } = rendre(
    <MemoryRouter>
      <Layout moi={ALAIN} />
    </MemoryRouter>,
  )
  client.setQueryData(['moi'], ALAIN)

  await userEvent.click(screen.getByRole('button', { name: 'Se déconnecter' }))

  await waitFor(() => expect(client.getQueryData(['moi'])).toBeUndefined())
})
