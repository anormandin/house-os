import { expect, test } from 'vitest'
import { phraseDuJour, type FaitsDuJour } from '@/lib/humeur'

const faits: FaitsDuJour = { ouvertes: 3, enRetard: 0, faites: 1, dodosDemenagement: null }

test("la phrase change d'un jour à l'autre, même au changement d'heure", () => {
  // Les 7 et 8 mars 2026 (nuit de 23 h au Québec) : avec un floor, la graine se
  // répétait et la même phrase sortait deux jours de suite.
  const graines = [
    new Date(2026, 2, 6),
    new Date(2026, 2, 7),
    new Date(2026, 2, 8),
    new Date(2026, 2, 9),
  ].map((date) => JSON.stringify(phraseDuJour(faits, date)))

  for (let i = 1; i < graines.length; i++) {
    expect(graines[i]).not.toBe(graines[i - 1])
  }
})

test('la phrase est stable pour une même date', () => {
  const matin = phraseDuJour(faits, new Date(2026, 8, 15, 8, 0))
  const soir = phraseDuJour(faits, new Date(2026, 8, 15, 22, 0))

  expect(matin).toEqual(soir)
})

test('60 dodos déclenche encore le ton déménagement, 61 non', () => {
  const date = new Date(2026, 7, 25)
  const a60 = phraseDuJour({ ...faits, ouvertes: 0, faites: 2, dodosDemenagement: 60 }, date)
  const a61 = phraseDuJour({ ...faits, ouvertes: 0, faites: 2, dodosDemenagement: 61 }, date)
  const jourJ = phraseDuJour({ ...faits, ouvertes: 0, faites: 2, dodosDemenagement: 0 }, date)

  const tonsDemenagement = ['On y est presque.', 'Bientôt chez nous.', 'Le compte à rebours est parti.']
  expect(tonsDemenagement).toContain(a60.titre)
  expect(tonsDemenagement).not.toContain(a61.titre)
  // Dodos = 0 : le jour J n'est plus « proche », il est là — ton calme.
  expect(tonsDemenagement).not.toContain(jourJ.titre)
})
