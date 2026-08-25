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
import type { Meteo, VerdictMeteo } from '@/lib/api'
import { jourCourt } from '@/lib/format'

// Codes temps WMO (Open-Meteo) → icône et teinte.
function icone(codeMeteo: number): { Icone: LucideIcon; classe: string } {
  if (codeMeteo === 0) return { Icone: Sun, classe: 'text-jaune' }
  if (codeMeteo <= 2) return { Icone: CloudSun, classe: 'text-jaune' }
  if (codeMeteo === 3) return { Icone: Cloud, classe: 'text-sourdine' }
  if (codeMeteo <= 48) return { Icone: CloudFog, classe: 'text-sourdine' }
  if (codeMeteo <= 57) return { Icone: CloudDrizzle, classe: 'text-sourdine' }
  if (codeMeteo <= 67) return { Icone: CloudRain, classe: 'text-encre' }
  if (codeMeteo <= 77) return { Icone: CloudSnow, classe: 'text-sourdine' }
  if (codeMeteo <= 82) return { Icone: CloudRain, classe: 'text-encre' }
  if (codeMeteo <= 86) return { Icone: CloudSnow, classe: 'text-sourdine' }
  return { Icone: CloudLightning, classe: 'text-encre' }
}

const LIBELLES_BON: Record<string, string> = {
  Tondre: 'Bonne journée pour tondre',
  Aérer: 'Bon moment pour aérer',
  'Être dehors': 'Belle journée pour être dehors',
}

function pastilles(verdicts: VerdictMeteo[]) {
  const chips: { texte: string; raison: string; enDedans: boolean }[] = []
  for (const v of verdicts) {
    if (v.etat === 'Bon' && LIBELLES_BON[v.regle]) {
      chips.push({ texte: LIBELLES_BON[v.regle], raison: v.raison, enDedans: false })
    }
    if (v.etat === 'Defavorable' && v.regle === 'Être dehors') {
      chips.push({ texte: 'Une journée pour rester en dedans', raison: v.raison, enDedans: true })
    }
  }
  return chips
}

export default function MeteoCarte({ meteo }: { meteo: Meteo | undefined }) {
  // Silencieux tant que rien n'a été ingéré : la carte n'existe pas encore.
  if (!meteo || meteo.jours.length === 0) {
    return null
  }

  const [aujourdhui, ...suivants] = meteo.jours
  // Le moment présent en grand (l'heure courante) ; le jour sert de repli quand
  // la ligne horaire manque.
  const { Icone: IconeJour, classe } = icone(meteo.maintenant?.codeMeteo ?? aujourdhui.codeMeteo)
  const temperature = meteo.maintenant?.temperatureC ?? aujourdhui.tempMax
  const chips = pastilles(meteo.verdicts)

  return (
    <section className="rounded-3xl bg-carte px-6 py-5 shadow-carte">
      <h2 className="mb-3 text-lg font-bold">Dehors</h2>

      <div className="flex items-center gap-4">
        <IconeJour className={`size-10 ${classe}`} strokeWidth={1.8} />
        <div className="flex items-baseline gap-2">
          <span className="font-titre text-3xl font-bold">{Math.round(temperature)}°</span>
          <span className="text-sm text-sourdine">
            {Math.round(aujourdhui.tempMin)}° à {Math.round(aujourdhui.tempMax)}°
          </span>
        </div>
        {aujourdhui.probabilitePrecipitation >= 30 && (
          <span className="ml-auto rounded-full bg-creux px-2.5 py-0.5 text-xs font-bold text-dore">
            {aujourdhui.probabilitePrecipitation} % de pluie
          </span>
        )}
      </div>

      <div className="mt-4 flex justify-between">
        {suivants.slice(0, 5).map((jour) => {
          const { Icone: IconePetit, classe: classePetit } = icone(jour.codeMeteo)
          return (
            <div key={jour.date} className="flex flex-col items-center gap-1">
              <span className="text-xs text-sourdine">{jourCourt(jour.date)}</span>
              <IconePetit className={`size-5 ${classePetit}`} strokeWidth={1.8} />
              <span className="text-xs font-bold">{Math.round(jour.tempMax)}°</span>
            </div>
          )
        })}
      </div>

      {chips.length > 0 && (
        <div className="mt-4 flex flex-wrap gap-2">
          {chips.map((chip) => (
            <span
              key={chip.texte}
              title={chip.raison}
              className={
                chip.enDedans
                  ? 'rounded-full bg-creux px-3 py-1 text-xs font-bold text-dore'
                  : 'rounded-full bg-vert-fond px-3 py-1 text-xs font-bold text-vert'
              }
            >
              {chip.texte}
            </span>
          ))}
        </div>
      )}
    </section>
  )
}
