import {
  Cloud,
  CloudDrizzle,
  CloudFog,
  CloudLightning,
  CloudRain,
  CloudSnow,
  CloudSun,
  Sun,
  type LucideIcon,
} from 'lucide-react'
import type { VerdictMeteo } from '@/lib/api'

// Codes temps WMO (Open-Meteo) → icône. Partagé par la carte « Dehors » et la vue
// e-ink : un seul endroit décide qu'un 61 est de la pluie.
export function iconeMeteo(codeMeteo: number): LucideIcon {
  if (codeMeteo === 0) return Sun
  if (codeMeteo <= 2) return CloudSun
  if (codeMeteo === 3) return Cloud
  if (codeMeteo <= 48) return CloudFog
  if (codeMeteo <= 57) return CloudDrizzle
  if (codeMeteo <= 67) return CloudRain
  if (codeMeteo <= 77) return CloudSnow
  if (codeMeteo <= 82) return CloudRain
  if (codeMeteo <= 86) return CloudSnow
  return CloudLightning
}

export const LIBELLES_BON: Record<string, string> = {
  Tondre: 'Bonne journée pour tondre',
  Aérer: 'Bon moment pour aérer',
  'Être dehors': 'Belle journée pour être dehors',
}

export type PastilleMeteo = { regle: string; texte: string; raison: string; enDedans: boolean }

/** Les verdicts qui méritent un mot : un « Bon » connu, ou « Être dehors » défavorable. */
export function pastillesMeteo(verdicts: VerdictMeteo[]): PastilleMeteo[] {
  const chips: PastilleMeteo[] = []
  for (const v of verdicts) {
    if (v.etat === 'Bon' && LIBELLES_BON[v.regle]) {
      chips.push({ regle: v.regle, texte: LIBELLES_BON[v.regle], raison: v.raison, enDedans: false })
    }
    if (v.etat === 'Defavorable' && v.regle === 'Être dehors') {
      chips.push({
        regle: v.regle,
        texte: 'Une journée pour rester en dedans',
        raison: v.raison,
        enDedans: true,
      })
    }
  }
  return chips
}
