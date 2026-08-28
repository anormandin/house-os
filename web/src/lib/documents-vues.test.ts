import { afterEach, expect, test, vi } from 'vitest'
import type { Document } from '@/lib/api'
import {
  comparer,
  joursAvant,
  problemeTailleFichier,
  SANS_DOSSIER,
  TAILLE_MAX_FICHIER,
  type Tri,
} from '@/lib/documents-vues'

afterEach(() => vi.useRealTimers())

test('joursAvant compte depuis aujourd’hui local — négatif si passé, mois franchis', () => {
  vi.useFakeTimers({ now: new Date(2026, 7, 28) }) // 28 août 2026

  expect(joursAvant('2026-08-28')).toBe(0)
  expect(joursAvant('2026-08-30')).toBe(2)
  expect(joursAvant('2026-08-25')).toBe(-3)
  expect(joursAvant('2026-09-01')).toBe(4)
})

function document(partiel: Partial<Document> & { id: string }): Document {
  return {
    titre: partiel.id,
    categorie: 'Autre',
    equipementId: null,
    nomEquipement: null,
    zoneId: null,
    nomZone: null,
    dossier: null,
    notes: null,
    dateDocument: null,
    echeance: null,
    nomFichier: `${partiel.id}.pdf`,
    typeMime: 'application/pdf',
    taille: 1024,
    creeLe: '2026-08-01T12:00:00Z',
    ...partiel,
  }
}

const trier = (docs: Document[], tri: Tri) =>
  [...docs].sort((a, b) => comparer(a, b, tri)).map((d) => d.id)

test('le tri par titre respecte la locale française et le sens', () => {
  const docs = [
    document({ id: 'e', titre: 'Épuration' }),
    document({ id: 'a', titre: 'acte de vente' }),
    document({ id: 'z', titre: 'Zonage' }),
  ]

  expect(trier(docs, { colonne: 'titre', desc: false })).toEqual(['a', 'e', 'z'])
  expect(trier(docs, { colonne: 'titre', desc: true })).toEqual(['z', 'e', 'a'])
})

test('les dates nulles restent en fin, dans les deux sens de tri', () => {
  const docs = [
    document({ id: 'sans-date' }),
    document({ id: 'juin', dateDocument: '2026-06-01' }),
    document({ id: 'janvier', dateDocument: '2026-01-01' }),
  ]

  expect(trier(docs, { colonne: 'dateDocument', desc: false }))
    .toEqual(['janvier', 'juin', 'sans-date'])
  expect(trier(docs, { colonne: 'dateDocument', desc: true }))
    .toEqual(['juin', 'janvier', 'sans-date'])
})

test('deux dates nulles se départagent par le plus récemment ajouté', () => {
  const docs = [
    document({ id: 'ancien', creeLe: '2026-01-01T00:00:00Z' }),
    document({ id: 'recent', creeLe: '2026-08-01T00:00:00Z' }),
  ]

  expect(trier(docs, { colonne: 'echeance', desc: true })).toEqual(['recent', 'ancien'])
})

test('la sentinelle « sans dossier » ne contient aucun octet nul (fichier lisible par git/grep)', () => {
  expect(SANS_DOSSIER.includes('\u0000')).toBe(false)
  expect(SANS_DOSSIER.length).toBeGreaterThan(0)
})

test('un fichier à la limite passe, au-dessus le message nomme le fichier et la limite', () => {
  expect(problemeTailleFichier({ name: 'manuel.pdf', size: TAILLE_MAX_FICHIER })).toBeNull()

  const probleme = problemeTailleFichier({ name: 'video.mov', size: 62 * 1024 * 1024 })
  expect(probleme).toContain('video.mov')
  expect(probleme).toContain('62 Mo')
  expect(probleme).toContain('50 Mo')
})
