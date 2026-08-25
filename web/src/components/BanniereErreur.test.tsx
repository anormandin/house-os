import { act, render, screen } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import BanniereErreur from '@/components/BanniereErreur'
import { effacerErreur, signalerErreur } from '@/lib/erreurs'

afterEach(() => {
  vi.useRealTimers()
  // Le store d'erreurs est un singleton de module : sans purge, une erreur
  // signalée dans un test fuit dans le suivant.
  effacerErreur()
})

test('la bannière disparaît après 6 secondes', () => {
  vi.useFakeTimers()
  render(<BanniereErreur />)

  act(() => signalerErreur('Erreur serveur (500)'))
  expect(screen.getByRole('alert')).toHaveTextContent('Erreur serveur (500)')

  act(() => vi.advanceTimersByTime(6100))
  expect(screen.queryByRole('alert')).not.toBeInTheDocument()
})

test('une seconde erreur ré-arme la minuterie de six secondes', () => {
  vi.useFakeTimers()
  render(<BanniereErreur />)

  act(() => signalerErreur('Première erreur'))
  act(() => vi.advanceTimersByTime(5000))
  act(() => signalerErreur('Deuxième erreur'))
  // 5 s + 2 s : la première minuterie aurait expiré — la seconde erreur doit
  // rester affichée ses six secondes pleines.
  act(() => vi.advanceTimersByTime(2000))
  expect(screen.getByRole('alert')).toHaveTextContent('Deuxième erreur')

  act(() => vi.advanceTimersByTime(4100))
  expect(screen.queryByRole('alert')).not.toBeInTheDocument()
})

test('le bouton fermer efface immédiatement', async () => {
  render(<BanniereErreur />)

  act(() => signalerErreur('À fermer'))
  screen.getByRole('button', { name: "Fermer le message d'erreur" }).click()

  await vi.waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument())
})
