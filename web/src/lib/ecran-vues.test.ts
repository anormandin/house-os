import { afterEach, expect, test, vi } from 'vitest'
import {
  dimensionsEcran,
  initiales,
  libelleDodos,
  pileAnnoncee,
  quandCeJour,
} from '@/lib/ecran-vues'

afterEach(() => vi.useRealTimers())

test('initiales prend deux lettres en majuscules (Alain et Ariane se distinguent), vide sans nom', () => {
  expect(initiales('alain')).toBe('AL')
  expect(initiales(' Ariane ')).toBe('AR')
  expect(initiales('éloi')).toBe('ÉL')
  expect(initiales('A')).toBe('A')
  expect(initiales(null)).toBe('')
  expect(initiales('  ')).toBe('')
})

test("quandCeJour parle comme à la maison : aujourd'hui, demain, le jour, dans N jours", () => {
  const aujourdhui = '2026-09-03' // jeudi
  expect(quandCeJour('2026-09-03', aujourdhui)).toBe("aujourd'hui")
  expect(quandCeJour('2026-09-01', aujourdhui)).toBe("aujourd'hui")
  expect(quandCeJour('2026-09-04', aujourdhui)).toBe('demain')
  expect(quandCeJour('2026-09-07', aujourdhui)).toBe('lun')
  expect(quandCeJour('2026-09-10', aujourdhui)).toBe('dans 7 jours')
})

test('libelleDodos compte les nuits', () => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date(2026, 8, 3, 14, 30))
  expect(libelleDodos('2026-10-06')).toBe('33 dodos')
  expect(libelleDodos('2026-09-04')).toBe('1 dodo')
  expect(libelleDodos('2026-09-03')).toBe("c'est aujourd'hui")
})

test("dimensionsEcran retombe sur l'E1003 en paysage quand la requête est absente ou farfelue", () => {
  expect(dimensionsEcran(new URLSearchParams(''))).toEqual({ largeur: 1872, hauteur: 1404 })
  expect(dimensionsEcran(new URLSearchParams('largeur=800&hauteur=480'))).toEqual({
    largeur: 800,
    hauteur: 480,
  })
  expect(dimensionsEcran(new URLSearchParams('largeur=abc&hauteur=99999'))).toEqual({
    largeur: 1872,
    hauteur: 1404,
  })
})

test('pileAnnoncee est bornée à 0–100 et absente sans paramètre', () => {
  expect(pileAnnoncee(new URLSearchParams(''))).toBeNull()
  expect(pileAnnoncee(new URLSearchParams('pile=87.4'))).toBe(87)
  expect(pileAnnoncee(new URLSearchParams('pile=140'))).toBe(100)
  expect(pileAnnoncee(new URLSearchParams('pile=oui'))).toBeNull()
})
