import { afterEach, expect, test, vi } from 'vitest'
import { cn, copierDansPressePapiers } from '@/lib/utils'

test('cn fusionne les classes Tailwind en conflit et écarte les conditions fausses', () => {
  const masque: boolean = false
  expect(cn('p-2', 'p-4')).toBe('p-4')
  expect(cn('text-sourdine', masque && 'hidden', undefined, 'font-bold'))
    .toBe('text-sourdine font-bold')
})

/* jsdom n'offre ni navigator.clipboard ni execCommand — on simule chaque étage
   du repli pour couvrir le « Copier » servi en HTTP sur le LAN. */

afterEach(() => {
  vi.restoreAllMocks()
  delete (navigator as { clipboard?: unknown }).clipboard
  delete (document as { execCommand?: unknown }).execCommand
})

test('copie via navigator.clipboard quand il existe (contexte sécurisé)', async () => {
  const writeText = vi.fn().mockResolvedValue(undefined)
  Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true })

  await expect(copierDansPressePapiers('https://exemple/ical')).resolves.toBe(true)
  expect(writeText).toHaveBeenCalledWith('https://exemple/ical')
})

test('sans clipboard, repli sur execCommand — sans laisser traîner le champ temporaire', async () => {
  document.execCommand = vi.fn(() => true)

  await expect(copierDansPressePapiers('texte')).resolves.toBe(true)
  expect(document.execCommand).toHaveBeenCalledWith('copy')
  expect(document.body.querySelector('textarea')).toBeNull()
})

test('aucun mécanisme disponible : false, jamais d’exception', async () => {
  await expect(copierDansPressePapiers('texte')).resolves.toBe(false)
  expect(document.body.querySelector('textarea')).toBeNull()
})
