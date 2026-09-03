import type { Meteo } from '@/lib/api'
import { jourCourt } from '@/lib/format'
import { iconeMeteo, pastillesMeteo } from '@/lib/meteo-vues'

// Teinte par famille de temps — la forme vient de lib/meteo-vues (partagée avec l'e-ink).
function classeMeteo(codeMeteo: number): string {
  if (codeMeteo <= 2) return 'text-jaune'
  if (codeMeteo <= 57) return 'text-sourdine'
  if (codeMeteo <= 67) return 'text-encre'
  if (codeMeteo <= 77) return 'text-sourdine'
  if (codeMeteo <= 82) return 'text-encre'
  if (codeMeteo <= 86) return 'text-sourdine'
  return 'text-encre'
}

export default function MeteoCarte({ meteo }: { meteo: Meteo | undefined }) {
  // Silencieux tant que rien n'a été ingéré : la carte n'existe pas encore.
  if (!meteo || meteo.jours.length === 0) {
    return null
  }

  const [aujourdhui, ...suivants] = meteo.jours
  // Le moment présent en grand (l'heure courante) ; le jour sert de repli quand
  // la ligne horaire manque.
  const codeCourant = meteo.maintenant?.codeMeteo ?? aujourdhui.codeMeteo
  const IconeJour = iconeMeteo(codeCourant)
  const classe = classeMeteo(codeCourant)
  const temperature = meteo.maintenant?.temperatureC ?? aujourdhui.tempMax
  const chips = pastillesMeteo(meteo.verdicts)

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
          const IconePetit = iconeMeteo(jour.codeMeteo)
          const classePetit = classeMeteo(jour.codeMeteo)
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
              key={chip.regle}
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
