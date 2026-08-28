import { expect, test } from 'vitest'
import type { Occurrence } from '@/lib/api'
import { santeZone } from '@/lib/pieces-vues'

const AUJOURDHUI = '2026-08-28'

function occurrence(echeance: string | null): Occurrence {
  return {
    id: `o-${Math.random()}`,
    tacheId: 't-1',
    titre: 'Passer le balai',
    description: null,
    echeance,
    statut: 'EnAttente',
    assigneA: null,
    completeePar: null,
    completeeLe: null,
    notes: null,
    zoneId: 'z-cuisine',
    equipementId: null,
    modeRecurrence: 'Ponctuelle',
  }
}

test('du retard fait déborder la pièce, peu importe le reste', () => {
  const sante = santeZone(
    [occurrence('2026-08-25'), occurrence(AUJOURDHUI), occurrence('2026-09-15')],
    AUJOURDHUI,
  )

  expect(sante.libelle).toBe('ça déborde un peu')
  expect(sante).toMatchObject({ retard: 1, aujourdhui: 1, aVenir: 1 })
  expect(sante.pct).toBeCloseTo(1 - 0.35 - 0.15)
})

test('la barre plancher à 0,15 même sous une pile de retards', () => {
  const retards = Array.from({ length: 5 }, () => occurrence('2026-08-01'))
  expect(santeZone(retards, AUJOURDHUI).pct).toBe(0.15)
})

test('des choses à faire aujourd’hui, sans retard : libellé au compte, singulier compris', () => {
  expect(santeZone([occurrence(AUJOURDHUI)], AUJOURDHUI).libelle)
    .toBe('1 chose à faire aujourd’hui')
  expect(santeZone([occurrence(AUJOURDHUI), occurrence(AUJOURDHUI)], AUJOURDHUI).libelle)
    .toBe('2 choses à faire aujourd’hui')
})

test('rien d’échu ni de dû : tout est frais, barre au moins à 0,85', () => {
  const sante = santeZone([occurrence('2026-09-15'), occurrence(null)], AUJOURDHUI)

  expect(sante.libelle).toBe('tout est frais')
  expect(sante.pct).toBeGreaterThanOrEqual(0.85)
  expect(sante).toMatchObject({ retard: 0, aujourdhui: 0, aVenir: 2 })
})
