import { act, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import ConfirmerSuppression from '@/components/ConfirmerSuppression'

// fireEvent (synchrone) plutôt que userEvent : sous fake timers, les délais
// internes de userEvent se bloquent.

afterEach(() => vi.useRealTimers())

function preparer() {
  vi.useFakeTimers()
  const onConfirmer = vi.fn()
  render(<ConfirmerSuppression ariaLabel="Supprimer la tâche" onConfirmer={onConfirmer} />)
  return onConfirmer
}

test('un double-clic rapide ne supprime pas sans que « Vraiment? » soit vu', () => {
  const onConfirmer = preparer()

  // Les deux boutons occupent le même endroit : deux clics en 200 ms supprimeraient
  // une tâche (destructif, non annulable) sans confirmation réelle.
  fireEvent.click(screen.getByRole('button', { name: 'Supprimer la tâche' }))
  fireEvent.click(screen.getByRole('button', { name: 'Supprimer la tâche — confirmer' }))
  expect(onConfirmer).not.toHaveBeenCalled()

  act(() => vi.advanceTimersByTime(400))
  fireEvent.click(screen.getByRole('button', { name: 'Supprimer la tâche — confirmer' }))
  expect(onConfirmer).toHaveBeenCalledOnce()
})

test('la pastille « Vraiment? » se désarme après 4 secondes', () => {
  const onConfirmer = preparer()

  fireEvent.click(screen.getByRole('button', { name: 'Supprimer la tâche' }))
  expect(screen.getByText('Vraiment?')).toBeInTheDocument()

  act(() => vi.advanceTimersByTime(4100))

  // Sinon la pastille resterait armée indéfiniment et un clic distrait supprimerait.
  expect(screen.queryByText('Vraiment?')).not.toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: 'Supprimer la tâche' }))
  expect(onConfirmer).not.toHaveBeenCalled()
})
