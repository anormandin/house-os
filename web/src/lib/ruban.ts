// Ruban des 7 prochains jours — logique pure (voir la décision
// « Ruban Des 7 Prochains Jours » au vault). Horizon : un cycle d'avance,
// plafonné à 30 jours ; la matérialisation à la complétion garantit qu'une
// occurrence en attente est à au plus un cycle de son échéance, donc le
// filtre client se réduit à « échéance ≤ aujourd'hui + 30 ».

import { dateLocaleIso, type EvenementExterne, type Occurrence, type Zone } from '@/lib/api'

export const HORIZON_JOURS = 30
export const JOURS_RUBAN = 7
export const PUCES_PAR_JOUR = 3
export const LIGNES_PLUS_TARD = 6

// Couleurs de pièces : palette fixe du langage chaleureuse, attribution
// déterministe par position (les zones arrivent triées par ordre).
export const PALETTE_ZONES = ['#4d699b', '#624c83', '#cc6d00', '#6f894e', '#597b75', '#77713f']
export const COULEUR_SANS_ZONE = '#b5b3a6'

export function couleurZone(zones: Zone[], zoneId: string | null): string {
  const index = zones.findIndex((z) => z.id === zoneId)
  if (index === -1) {
    return COULEUR_SANS_ZONE
  }
  return PALETTE_ZONES[index % PALETTE_ZONES.length]
}

export type JourRuban = {
  date: string
  taches: Occurrence[]
  evenements: EvenementExterne[]
}

export type GroupesRuban = {
  retard: Occurrence[]
  jours: JourRuban[]
  plusTard: Occurrence[]
}

function ajouterJours(dateIso: string, jours: number): string {
  const [annee, mois, jour] = dateIso.split('-').map(Number)
  return dateLocaleIso(new Date(annee, mois - 1, jour + jours))
}

export function regrouperRuban(
  occurrences: Occurrence[],
  evenements: EvenementExterne[],
  aujourdhui: string,
): GroupesRuban {
  const datees = occurrences.filter(
    (o) => o.statut === 'EnAttente' && o.echeance !== null,
  )
  const retard = datees
    .filter((o) => o.echeance! < aujourdhui)
    .sort((a, b) => a.echeance!.localeCompare(b.echeance!))

  const jours: JourRuban[] = []
  for (let i = 0; i < JOURS_RUBAN; i++) {
    const date = ajouterJours(aujourdhui, i)
    jours.push({
      date,
      taches: datees.filter((o) => o.echeance === date),
      evenements: evenements.filter((e) => e.date === date),
    })
  }

  const apresRuban = ajouterJours(aujourdhui, JOURS_RUBAN - 1)
  const horizon = ajouterJours(aujourdhui, HORIZON_JOURS)
  const plusTard = datees
    .filter((o) => o.echeance! > apresRuban && o.echeance! <= horizon)
    .sort((a, b) => a.echeance!.localeCompare(b.echeance!))

  return { retard, jours, plusTard }
}
