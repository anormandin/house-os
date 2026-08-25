import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test, vi } from 'vitest'
import QuickAdd from '@/components/QuickAdd'
import TacheEditeur from '@/components/TacheEditeur'
import { rendre } from '@/test/rendre'
import { TACHE_COMPLETE } from '@/test/serveur-msw'

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

test("⌘K pendant une modification n'ouvre pas un second éditeur", async () => {
  const onFermer = vi.fn()
  rendre(
    <>
      <QuickAdd />
      <TacheEditeur tacheId={TACHE_COMPLETE.id} onFermer={onFermer} />
    </>,
  )
  await waitFor(() =>
    expect(screen.getByLabelText('Titre')).toHaveValue(TACHE_COMPLETE.titre))

  await userEvent.keyboard('{Meta>}k{/Meta}')

  // Un second modal s'empilerait invisible sous le premier, avec deux champs Titre.
  expect(screen.queryByRole('heading', { name: 'Nouvelle tâche' })).not.toBeInTheDocument()
  expect(screen.getAllByLabelText('Titre')).toHaveLength(1)

  // Et Échap ne ferme que l'éditeur du dessus, une seule fois.
  await userEvent.keyboard('{Escape}')
  expect(onFermer).toHaveBeenCalledOnce()
})
