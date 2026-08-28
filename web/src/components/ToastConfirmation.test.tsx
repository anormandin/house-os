import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, expect, test, vi } from 'vitest'
import ToastConfirmation from '@/components/ToastConfirmation'
import { afficherToast, effacerToast } from '@/lib/toast'

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

test('le toast le plus récent remplace le précédent', () => {
  render(<ToastConfirmation />)

  act(() => afficherToast({ message: 'Tâche complétée', actionLibelle: 'Annuler', onAction: () => {} }))
  act(() => afficherToast({ message: 'Complétion annulée', actionLibelle: 'Refaire', onAction: () => {} }))

  expect(screen.getAllByRole('status')).toHaveLength(1)
  expect(screen.getByRole('status')).toHaveTextContent('Complétion annulée')
})
