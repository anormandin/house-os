import { afterEach, expect, test, vi } from 'vitest'
import { dateLisible, dodosAvant, jourCourt } from '@/lib/format'

afterEach(() => vi.useRealTimers())

test("dateLisible ajoute l'année seulement hors de l'année courante", () => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date(2026, 11, 31)) // 31 décembre 2026

  expect(dateLisible('2026-01-02')).toBe('2 janvier')
  expect(dateLisible('2027-01-02')).toBe('2 janvier 2027')
  expect(dateLisible('2025-09-22')).toBe('22 septembre 2025')
})

test("dodosAvant vaut 1 par-dessus le passage à l'heure avancée", () => {
  // Au Québec, la nuit du 2026-03-08 ne dure que 23 h : l'arrondi doit tenir.
  vi.useFakeTimers()
  vi.setSystemTime(new Date(2026, 2, 7, 21, 0, 0)) // 7 mars, 21 h

  expect(dodosAvant('2026-03-08')).toBe(1)
  expect(dodosAvant('2026-03-09')).toBe(2)
})

test('dodosAvant est négatif pour une date passée, jamais NaN pour une date valide', () => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date(2026, 9, 10))

  expect(dodosAvant('2026-10-06')).toBe(-4)
})

test('jourCourt rend le jour sans point final', () => {
  expect(jourCourt('2026-08-24')).not.toContain('.')
})
