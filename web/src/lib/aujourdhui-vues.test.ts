import { expect, test } from 'vitest'
import { bornesSemainesBilan, comptesParSemaine, NB_SEMAINES_BILAN } from '@/lib/aujourdhui-vues'

test('les bornes couvrent 8 semaines lundi → lundi autour d’un mercredi', () => {
  const bornes = bornesSemainesBilan(new Date(2026, 7, 26)) // mercredi 26 août 2026

  expect(bornes.lundis).toHaveLength(NB_SEMAINES_BILAN)
  expect(bornes.lundis[0]).toBe('2026-07-06')
  expect(bornes.lundis[NB_SEMAINES_BILAN - 1]).toBe('2026-08-24')
  expect(bornes.de).toBe(new Date(2026, 6, 6).toISOString())
  expect(bornes.a).toBe(new Date(2026, 7, 31).toISOString())
})

test('un lundi appartient à sa propre semaine, un dimanche à la semaine entamée', () => {
  expect(bornesSemainesBilan(new Date(2026, 7, 24)).lundis.at(-1)).toBe('2026-08-24')
  expect(bornesSemainesBilan(new Date(2026, 7, 23)).lundis.at(-1)).toBe('2026-08-17')
})

test('les instants s’agrègent par semaine locale, hors fenêtre ignorés', () => {
  const lundis = ['2026-08-17', '2026-08-24']
  const instants = [
    new Date(2026, 7, 25, 14, 0).toISOString(), // mardi 25 → semaine du 24
    new Date(2026, 7, 23, 23, 30).toISOString(), // dimanche 23 tard → semaine du 17
    new Date(2026, 7, 24, 0, 0).toISOString(), // lundi 24 minuit → semaine du 24
    new Date(2026, 6, 1, 12, 0).toISOString(), // hors fenêtre
  ]

  expect(comptesParSemaine(instants, lundis)).toEqual([1, 2])
})

test('aucune complétion → une case à zéro par lundi', () => {
  expect(comptesParSemaine([], ['2026-08-17', '2026-08-24'])).toEqual([0, 0])
})
