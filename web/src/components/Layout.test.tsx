import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { expect, test } from 'vitest'
import Layout from '@/components/Layout'
import { rendre } from '@/test/rendre'
import { ALAIN, FLUX_ICAL, FLUX_ICAL_TOURNE } from '@/test/serveur-msw'

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
