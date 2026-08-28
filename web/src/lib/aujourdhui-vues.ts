import { dateLocaleIso } from '@/lib/api'

/* Bilan du ménage : total complété par semaine, sans égard à qui a cliqué —
   l'attribution individuelle est indicative, le foyer compte ensemble. */

export const NB_SEMAINES_BILAN = 8

/** Fenêtre du bilan : les 8 dernières semaines (lundi → lundi), bornes en instants. */
export function bornesSemainesBilan(date = new Date()): { de: string; a: string; lundis: string[] } {
  const jour = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  const lundiCourant = new Date(
    jour.getFullYear(),
    jour.getMonth(),
    jour.getDate() - ((jour.getDay() + 6) % 7),
  )
  const lundis: string[] = []
  for (let i = NB_SEMAINES_BILAN - 1; i >= 0; i--) {
    lundis.push(
      dateLocaleIso(
        new Date(
          lundiCourant.getFullYear(),
          lundiCourant.getMonth(),
          lundiCourant.getDate() - 7 * i,
        ),
      ),
    )
  }
  const debut = new Date(`${lundis[0]}T00:00:00`)
  const fin = new Date(
    lundiCourant.getFullYear(),
    lundiCourant.getMonth(),
    lundiCourant.getDate() + 7,
  )
  return { de: debut.toISOString(), a: fin.toISOString(), lundis }
}

/** Agrège des instants de complétion par semaine locale — une case par lundi. */
export function comptesParSemaine(instants: string[], lundis: string[]): number[] {
  const indexParLundi = new Map(lundis.map((lundi, i) => [lundi, i]))
  const comptes = lundis.map(() => 0)
  for (const instant of instants) {
    const local = new Date(instant)
    const lundi = dateLocaleIso(
      new Date(
        local.getFullYear(),
        local.getMonth(),
        local.getDate() - ((local.getDay() + 6) % 7),
      ),
    )
    const i = indexParLundi.get(lundi)
    if (i !== undefined) {
      comptes[i] += 1
    }
  }
  return comptes
}
