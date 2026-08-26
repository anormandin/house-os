import { expect, test } from 'vitest'
import type { EvenementExterne, Occurrence, Zone } from '@/lib/api'
import {
  COULEUR_SANS_ZONE,
  couleurZone,
  HORIZON_JOURS,
  PALETTE_ZONES,
  regrouperRuban,
} from '@/lib/ruban'

const AUJOURDHUI = '2026-08-25'

function occurrence(champs: Partial<Occurrence>): Occurrence {
  return {
    id: 'o-1',
    tacheId: 't-1',
    titre: 'Balayer',
    description: null,
    echeance: AUJOURDHUI,
    statut: 'EnAttente',
    assigneA: null,
    completeePar: null,
    completeeLe: null,
    notes: null,
    zoneId: null,
    equipementId: null,
    modeRecurrence: 'Ponctuelle',
    ...champs,
  }
}

function evenement(champs: Partial<EvenementExterne>): EvenementExterne {
  return { titre: 'Collecte recyclage', type: 'Collecte', date: AUJOURDHUI, heure: null, ...champs }
}

test('le retard sort des jours et se trie du plus ancien au plus récent', () => {
  const groupes = regrouperRuban(
    [
      occurrence({ id: 'a', echeance: '2026-08-23' }),
      occurrence({ id: 'b', echeance: '2026-08-20' }),
      occurrence({ id: 'c', echeance: AUJOURDHUI }),
    ],
    [],
    AUJOURDHUI,
  )
  expect(groupes.retard.map((o) => o.id)).toEqual(['b', 'a'])
  expect(groupes.jours[0].taches.map((o) => o.id)).toEqual(['c'])
})

test('les 7 colonnes couvrent aujourd’hui + 6 jours, chacune à sa date', () => {
  const groupes = regrouperRuban(
    [occurrence({ id: 'dim', echeance: '2026-08-30' }), occurrence({ id: 'lun', echeance: '2026-08-31' })],
    [],
    AUJOURDHUI,
  )
  expect(groupes.jours.map((j) => j.date)).toEqual([
    '2026-08-25', '2026-08-26', '2026-08-27', '2026-08-28',
    '2026-08-29', '2026-08-30', '2026-08-31',
  ])
  expect(groupes.jours[5].taches.map((o) => o.id)).toEqual(['dim'])
  expect(groupes.jours[6].taches.map((o) => o.id)).toEqual(['lun'])
  expect(groupes.plusTard).toEqual([])
})

test('« Plus tard » couvre les jours 7 à 30, trié, et rien au-delà de l’horizon', () => {
  const groupes = regrouperRuban(
    [
      occurrence({ id: 'spa', echeance: '2026-09-18' }),
      occurrence({ id: 'cafetiere', echeance: '2026-09-03' }),
      occurrence({ id: 'hors-horizon', echeance: '2026-09-25' }),
      occurrence({ id: 'pile-horizon', echeance: '2026-09-24' }),
    ],
    [],
    AUJOURDHUI,
  )
  expect(groupes.plusTard.map((o) => o.id)).toEqual(['cafetiere', 'spa', 'pile-horizon'])
})

test('le passage de mois et d’année ne casse pas l’arithmétique de dates', () => {
  const groupes = regrouperRuban(
    [occurrence({ id: 'janv', echeance: '2027-01-02' })],
    [],
    '2026-12-30',
  )
  expect(groupes.jours.map((j) => j.date)).toContain('2027-01-02')
  expect(groupes.jours.find((j) => j.date === '2027-01-02')!.taches.map((o) => o.id)).toEqual(['janv'])
})

test('sans échéance, complétée ou passée : jamais dans le ruban', () => {
  const groupes = regrouperRuban(
    [
      occurrence({ id: 'sans-date', echeance: null }),
      occurrence({ id: 'faite', statut: 'Completee' }),
      occurrence({ id: 'passee', statut: 'Passee' }),
    ],
    [],
    AUJOURDHUI,
  )
  expect(groupes.retard).toEqual([])
  expect(groupes.jours.every((j) => j.taches.length === 0)).toBe(true)
  expect(groupes.plusTard).toEqual([])
})

test('les événements externes tombent dans leur jour, jamais dans retard ou plus tard', () => {
  const groupes = regrouperRuban(
    [],
    [evenement({ titre: 'Collecte recyclage', date: '2026-08-26' })],
    AUJOURDHUI,
  )
  expect(groupes.jours[1].evenements.map((e) => e.titre)).toEqual(['Collecte recyclage'])
  expect(groupes.jours[0].evenements).toEqual([])
})

test('l’horizon est bien de 30 jours', () => {
  expect(HORIZON_JOURS).toBe(30)
})

const ZONES: Zone[] = [
  { id: 'z-1', nom: 'Bureau', type: 'Interieur', ordre: 1 },
  { id: 'z-2', nom: 'Cuisine', type: 'Interieur', ordre: 2 },
]

test('couleur de zone : déterministe par position, repli neutre sans zone', () => {
  expect(couleurZone(ZONES, 'z-1')).toBe(PALETTE_ZONES[0])
  expect(couleurZone(ZONES, 'z-2')).toBe(PALETTE_ZONES[1])
  expect(couleurZone(ZONES, null)).toBe(COULEUR_SANS_ZONE)
  expect(couleurZone(ZONES, 'z-inconnue')).toBe(COULEUR_SANS_ZONE)
})
