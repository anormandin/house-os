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

/**
 * Le même découpage des codes WMO, en mots. La dateline du journal mural dit le
 * temps qu'il fait au lieu de le dessiner : à trois mètres, une icône de 52 px en
 * 1-bit se lit moins vite qu'un mot.
 */
export function libelleMeteo(codeMeteo: number): string {
  if (codeMeteo === 0) return 'Dégagé'
  if (codeMeteo === 1) return 'Éclaircies'
  if (codeMeteo === 2) return 'Passages nuageux'
  if (codeMeteo === 3) return 'Nuageux'
  if (codeMeteo <= 48) return 'Brouillard'
  if (codeMeteo <= 57) return 'Bruine'
  if (codeMeteo <= 67) return 'Pluie'
  if (codeMeteo <= 77) return 'Neige'
  if (codeMeteo <= 82) return 'Averses'
  if (codeMeteo <= 86) return 'Averses de neige'
  return 'Orages'
}

export type MeteoEnMots = {
  codeMeteo: number
  temperatureC: number
  tempMax: number
  probabilitePrecipitation: number
}

/**
 * La météo de la dateline : « Nuageux · −1 °C · max 4 · 60 % de pluie ». Le maximum
 * ne sort que s'il reste à venir — en fin d'après-midi, « max 4 » quand il fait 4
 * est du bruit. La pluie ne sort qu'au-dessus de 30 %, comme partout ailleurs.
 */
export function meteoEnMots(meteo: MeteoEnMots): string {
  const maintenant = Math.round(meteo.temperatureC)
  const max = Math.round(meteo.tempMax)
  const bouts = [libelleMeteo(meteo.codeMeteo), `${signe(maintenant)} °C`]
  if (max > maintenant) {
    // Le maximum passe par `signe` lui aussi : une journée de janvier à −18 avec un
    // max de −9 écrivait « −18 °C · max -9 », le vrai moins et le trait d'union côte
    // à côte sur la même ligne. Au Québec ce n'est pas un cas limite, c'est l'hiver.
    bouts.push(`max ${signe(max)}`)
  }
  if (meteo.probabilitePrecipitation >= 30) {
    bouts.push(`${meteo.probabilitePrecipitation} % de pluie`)
  }
  return bouts.join(' · ')
}

/**
 * Le vrai signe moins (U+2212), pas le trait d'union. Sur la dateline, les bouts
 * sont déjà séparés par des « · » : un « -1 » au trait d'union se lit comme une
 * coupure de mot à trois mètres, en 1-bit, à 28 px.
 */
function signe(degres: number): string {
  return degres < 0 ? `−${Math.abs(degres)}` : String(degres)
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
