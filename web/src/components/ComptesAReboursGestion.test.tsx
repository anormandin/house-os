import { act, fireEvent, screen } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import ComptesAReboursGestion from '@/components/ComptesAReboursGestion'
import { api, type CompteARebours } from '@/lib/api'
import { rendre } from '@/test/rendre'

// fireEvent + fake timers : même contrainte que ConfirmerSuppression.test.tsx.

const COMPTE: CompteARebours = {
  id: 'c-demenagement',
  titre: 'Déménagement',
  dateCible: '2099-10-06',
  icone: 'Camion',
}

afterEach(() => {
  vi.restoreAllMocks()
  vi.useRealTimers()
})

test('la suppression est en deux temps : le premier clic arme, le second confirme', async () => {
  vi.useFakeTimers()
  const suppression = vi.spyOn(api, 'supprimerCompteARebours').mockResolvedValue(undefined)
  rendre(<ComptesAReboursGestion comptes={[COMPTE]} onFermer={() => {}} />)

  fireEvent.click(screen.getByRole('button', { name: 'Supprimer Déménagement' }))
  expect(suppression).not.toHaveBeenCalled()
  expect(screen.getByText('Vraiment?')).toBeInTheDocument()

  act(() => vi.advanceTimersByTime(400))
  fireEvent.click(screen.getByRole('button', { name: 'Supprimer Déménagement — confirmer' }))
  // mutate() lance la mutationFn en microtâche — la laisser partir.
  await act(async () => {})
  expect(suppression).toHaveBeenCalledWith('c-demenagement')
})

test('la pastille non confirmée se désarme toute seule, sans supprimer', () => {
  vi.useFakeTimers()
  const suppression = vi.spyOn(api, 'supprimerCompteARebours').mockResolvedValue(undefined)
  rendre(<ComptesAReboursGestion comptes={[COMPTE]} onFermer={() => {}} />)

  fireEvent.click(screen.getByRole('button', { name: 'Supprimer Déménagement' }))
  act(() => vi.advanceTimersByTime(4100))

  expect(screen.queryByText('Vraiment?')).not.toBeInTheDocument()
  expect(suppression).not.toHaveBeenCalled()
})
