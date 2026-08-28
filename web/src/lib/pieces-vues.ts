import type { Occurrence } from '@/lib/api'

export type Sante = {
  retard: number
  aujourdhui: number
  aVenir: number
  pct: number
  couleur: string
  libelle: string
}

/** Fraîcheur d'une pièce d'après ses occurrences ouvertes — jamais une punition. */
export function santeZone(occurrences: Occurrence[], aujourdhui: string): Sante {
  const retard = occurrences.filter((o) => o.echeance !== null && o.echeance < aujourdhui).length
  const ceJour = occurrences.filter((o) => o.echeance === aujourdhui).length
  const aVenir = occurrences.length - retard - ceJour
  const pct = Math.max(0.15, 1 - retard * 0.35 - ceJour * 0.15)
  if (retard > 0) {
    return {
      retard, aujourdhui: ceJour, aVenir, pct,
      couleur: '#d98d6e',
      libelle: 'ça déborde un peu',
    }
  }
  if (ceJour > 0) {
    return {
      retard, aujourdhui: ceJour, aVenir, pct,
      couleur: '#d9bd6e',
      libelle: ceJour === 1 ? '1 chose à faire aujourd’hui' : `${ceJour} choses à faire aujourd’hui`,
    }
  }
  return { retard, aujourdhui: ceJour, aVenir, pct: Math.max(pct, 0.85), couleur: '#9db07e', libelle: 'tout est frais' }
}
