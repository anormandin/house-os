import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test, vi } from 'vitest'
import ChoixCherchable from '@/components/ChoixCherchable'

const OPTIONS = [
  { valeur: 't:1', libelle: 'Déneiger l’entrée', groupe: 'Tâches' },
  { valeur: 't:2', libelle: 'Changer le filtre de la fournaise', groupe: 'Tâches' },
  { valeur: 'e:1', libelle: 'Fournaise centrale', groupe: 'Équipements' },
]

function rendreChoix(valeur = '', onChoisir = vi.fn()) {
  render(
    <ChoixCherchable
      options={OPTIONS}
      valeur={valeur}
      onChoisir={onChoisir}
      libelle="Lien"
      texteVide="Sans lien"
      optionVide
    />,
  )
  return onChoisir
}

test('le bouton montre le choix actuel, ou le texte vide', () => {
  rendreChoix('e:1')
  expect(screen.getByRole('button', { name: 'Lien' })).toHaveTextContent('Fournaise centrale')
})

test('filtrer sans accents puis choisir au clavier', async () => {
  const onChoisir = rendreChoix()
  await userEvent.click(screen.getByRole('button', { name: 'Lien' }))
  await userEvent.type(screen.getByRole('combobox', { name: 'Lien' }), 'fournaise')

  // « Sans lien » disparaît dès qu'on filtre ; les deux fournaises restent.
  expect(screen.getAllByRole('option').map((o) => o.textContent)).toEqual([
    'Changer le filtre de la fournaise',
    'Fournaise centrale',
  ])
  await userEvent.keyboard('{ArrowDown}{Enter}')
  expect(onChoisir).toHaveBeenCalledWith('e:1')
  expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
})

test('« deneiger » trouve « Déneiger »', async () => {
  rendreChoix()
  await userEvent.click(screen.getByRole('button', { name: 'Lien' }))
  await userEvent.type(screen.getByRole('combobox', { name: 'Lien' }), 'deneiger')
  expect(screen.getAllByRole('option')).toHaveLength(1)
})

test('l’option vide ramène à « aucun »', async () => {
  const onChoisir = rendreChoix('t:1')
  await userEvent.click(screen.getByRole('button', { name: 'Lien' }))
  await userEvent.click(screen.getByRole('option', { name: 'Sans lien' }))
  expect(onChoisir).toHaveBeenCalledWith('')
})

test('Échap referme la liste sans remonter au modal parent', async () => {
  const surEchapParent = vi.fn()
  render(
    <div onKeyDown={(e) => e.key === 'Escape' && surEchapParent()}>
      <ChoixCherchable options={OPTIONS} valeur="" onChoisir={() => {}} libelle="Lien" texteVide="Sans lien" />
    </div>,
  )
  await userEvent.click(screen.getByRole('button', { name: 'Lien' }))
  await userEvent.keyboard('{Escape}')
  expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  expect(surEchapParent).not.toHaveBeenCalled()
})
