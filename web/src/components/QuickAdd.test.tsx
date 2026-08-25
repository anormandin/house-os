import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test } from 'vitest'
import QuickAdd from '@/components/QuickAdd'
import { rendre } from '@/test/rendre'

test('le bouton ouvre le modal complet de nouvelle tâche', async () => {
  rendre(<QuickAdd />)

  await userEvent.click(screen.getByRole('button', { name: /Ajouter une tâche/ }))

  expect(await screen.findByRole('heading', { name: 'Nouvelle tâche' })).toBeInTheDocument()
  // Le modal complet, pas le mini-formulaire : la répétition y est réglable.
  expect(screen.getByText('Répétition')).toBeInTheDocument()
})

test('⌘K ouvre le modal complet', async () => {
  rendre(<QuickAdd />)

  await userEvent.keyboard('{Meta>}k{/Meta}')

  expect(await screen.findByRole('heading', { name: 'Nouvelle tâche' })).toBeInTheDocument()
})
