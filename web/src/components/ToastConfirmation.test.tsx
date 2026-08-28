import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, expect, test, vi } from 'vitest'
import ToastConfirmation from '@/components/ToastConfirmation'
import { afficherToast, effacerToast, FENETRE_FUSION_MS, SLOT_GESTE_LOCAL } from '@/lib/toast'

afterEach(() => {
  effacerToast()
  vi.useRealTimers()
})

test('le toast affiche le message et déclenche son action inverse', async () => {
  const onAction = vi.fn()
  render(<ToastConfirmation />)

  act(() => afficherToast({ message: 'Tâche complétée', actionLibelle: 'Annuler', onAction }))

  const toast = screen.getByRole('status')
  expect(toast).toHaveTextContent('Tâche complétée')
  await userEvent.click(screen.getByRole('button', { name: 'Annuler' }))

  expect(onAction).toHaveBeenCalledOnce()
  expect(screen.queryByRole('status')).not.toBeInTheDocument()
})

test('le toast disparaît de lui-même après 6 s', () => {
  vi.useFakeTimers()
  render(<ToastConfirmation />)

  act(() => afficherToast({ message: 'Tâche complétée', actionLibelle: 'Annuler', onAction: () => {} }))
  expect(screen.getByRole('status')).toBeInTheDocument()

  act(() => vi.advanceTimersByTime(6000))
  expect(screen.queryByRole('status')).not.toBeInTheDocument()
})

test('un geste local chasse le précédent : une seule action inverse valide à la fois', () => {
  render(<ToastConfirmation />)

  const local = { actionLibelle: 'Annuler', onAction: () => {}, slot: SLOT_GESTE_LOCAL }
  act(() => afficherToast({ ...local, message: 'Tâche complétée' }))
  act(() => afficherToast({ ...local, message: 'Complétion annulée' }))

  expect(screen.getAllByRole('status')).toHaveLength(1)
  expect(screen.getByRole('status')).toHaveTextContent('Complétion annulée')
})

test('une annonce distante coexiste avec mon geste local', () => {
  render(<ToastConfirmation />)

  act(() => afficherToast({
    message: 'Tâche complétée', actionLibelle: 'Annuler', onAction: () => {}, slot: SLOT_GESTE_LOCAL,
  }))
  act(() => afficherToast({ message: 'Ariane a complété « Litière »' }))

  expect(screen.getAllByRole('status')).toHaveLength(2)
})

test('une annonce sans action inverse n’affiche aucun bouton d’action', () => {
  render(<ToastConfirmation />)

  act(() => afficherToast({ message: 'Ariane a complété « Litière »' }))

  expect(screen.getByRole('status')).toHaveTextContent('Ariane a complété « Litière »')
  expect(screen.queryByRole('button', { name: 'Annuler' })).not.toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Fermer la confirmation' })).toBeInTheDocument()
})

test('les annonces de même clé fusionnent en un seul toast compté', () => {
  render(<ToastConfirmation />)

  for (let i = 0; i < 3; i++) {
    act(() => afficherToast({
      message: 'Ariane a complété une tâche',
      cleFusion: 'occurrence.completee|ariane|web',
      recomposer: (nombre) => `Ariane a complété ${nombre} tâches`,
    }))
  }

  expect(screen.getAllByRole('status')).toHaveLength(1)
  expect(screen.getByRole('status')).toHaveTextContent('Ariane a complété 3 tâches')
})

test('deux acteurs différents restent deux toasts', () => {
  render(<ToastConfirmation />)

  act(() => afficherToast({ message: 'Ariane a complété…', cleFusion: 'occurrence.completee|ariane|web' }))
  act(() => afficherToast({ message: 'Claude a complété…', cleFusion: 'occurrence.completee|alain|mcp' }))

  expect(screen.getAllByRole('status')).toHaveLength(2)
})

test('trois annonces au plus : la plus ancienne sort', () => {
  render(<ToastConfirmation />)

  for (let i = 0; i < 4; i++) {
    act(() => afficherToast({ message: `Annonce ${i}` }))
  }

  const affiches = screen.getAllByRole('status')
  expect(affiches).toHaveLength(3)
  expect(affiches[0]).toHaveTextContent('Annonce 1')
})

test('passé la fenêtre de fusion, un geste identique ouvre un nouveau toast', () => {
  vi.useFakeTimers()
  render(<ToastConfirmation />)

  act(() => afficherToast({ message: 'Ariane a complété…', cleFusion: 'occurrence.completee|ariane|web' }))
  act(() => vi.advanceTimersByTime(FENETRE_FUSION_MS + 1))
  act(() => afficherToast({ message: 'Ariane a complété…', cleFusion: 'occurrence.completee|ariane|web' }))

  expect(screen.getAllByRole('status')).toHaveLength(2)
})
