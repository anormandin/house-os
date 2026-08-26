import { useMemo, useState } from 'react'
import { CalendarPlus } from 'lucide-react'
import Avatar from '@/components/Avatar'
import { ICONES_FLUX } from '@/components/FluxExternesGestion'
import type { EvenementExterne, Occurrence, Zone } from '@/lib/api'
import { dateLongue, jourCourt } from '@/lib/format'
import {
  couleurZone,
  LIGNES_PLUS_TARD,
  PUCES_PAR_JOUR,
  regrouperRuban,
} from '@/lib/ruban'
import { cn } from '@/lib/utils'

/* Le ruban des 7 prochains jours : En retard · aujourd'hui · 6 jours · Plus tard.
   Composant canonique des tâches à venir (décision « Ruban Des 7 Prochains
   Jours ») — sur Aujourd'hui en pleine largeur, sur Pièces avec le focus par
   pièce (le reste s'estompe). */

function enDate(dateIso: string): Date {
  return new Date(`${dateIso}T00:00:00`)
}

function jourMoisCourt(dateIso: string): string {
  return enDate(dateIso).toLocaleDateString('fr-CA', { day: 'numeric', month: 'short' })
}

function joursDeRetard(echeance: string, aujourdhui: string): number {
  return Math.round((enDate(aujourdhui).getTime() - enDate(echeance).getTime()) / 86_400_000)
}

function estWeekend(dateIso: string): boolean {
  const jour = enDate(dateIso).getDay()
  return jour === 0 || jour === 6
}

function PuceTache({
  occurrence,
  couleur,
  sousTitre,
  estompee,
  onOuvrir,
}: {
  occurrence: Occurrence
  couleur: string
  sousTitre?: string
  estompee: boolean
  onOuvrir: (tacheId: string) => void
}) {
  return (
    <button
      type="button"
      onClick={() => onOuvrir(occurrence.tacheId)}
      className={cn(
        'flex w-full gap-1.5 rounded-[10px] bg-creux px-2 py-1.5 text-left text-[11px] font-bold leading-[1.3] text-texte',
        'transition-shadow hover:shadow-carte-lg hover:outline hover:outline-1 hover:outline-orange/50',
        'group-data-[fond=carte]:bg-carte group-data-[fond=carte]:shadow-carte',
        estompee && 'opacity-40',
      )}
    >
      <span className="mt-1 size-1.5 shrink-0 rounded-full" style={{ background: couleur }} />
      <span className="min-w-0 flex-1">
        {occurrence.titre}
        {sousTitre && <span className="block text-[10px] font-bold text-rouge">{sousTitre}</span>}
      </span>
      {occurrence.assigneA && <Avatar utilisateur={occurrence.assigneA} taille={17} className="self-center" />}
    </button>
  )
}

function PuceEvenement({ evenement, estompee }: { evenement: EvenementExterne; estompee: boolean }) {
  const { Icone } = ICONES_FLUX[evenement.type]
  return (
    <div
      className={cn(
        'flex w-full gap-1.5 rounded-[10px] border-[1.5px] border-dashed border-tiret px-2 py-1.5 text-[11px] font-semibold italic leading-[1.3] text-dore',
        estompee && 'opacity-40',
      )}
    >
      <Icone className="mt-0.5 size-3.5 shrink-0 text-sourdine" />
      <span className="min-w-0 flex-1">{evenement.titre}</span>
    </div>
  )
}

export default function Ruban({
  occurrences,
  evenements,
  zones,
  aujourdhui,
  focusZone,
  onOuvrirTache,
  onGererFlux,
}: {
  occurrences: Occurrence[]
  evenements: EvenementExterne[]
  zones: Zone[]
  aujourdhui: string
  focusZone?: Zone | null
  onOuvrirTache: (tacheId: string) => void
  onGererFlux?: () => void
}) {
  const groupes = useMemo(
    () => regrouperRuban(occurrences, evenements, aujourdhui),
    [occurrences, evenements, aujourdhui],
  )
  const [joursDeplies, setJoursDeplies] = useState<ReadonlySet<string>>(new Set())

  const deplierJour = (date: string) =>
    setJoursDeplies((avant) => new Set([...avant, date]))
  const estompee = (zoneId: string | null) =>
    focusZone != null && zoneId !== focusZone.id
  const dernierJour = groupes.jours[groupes.jours.length - 1].date

  return (
    <section className="rounded-[20px] bg-carte px-5 py-4 shadow-carte">
      <div className="flex items-center gap-3">
        <h3 className="text-lg font-bold">Les 7 prochains jours</h3>
        {focusZone && (
          <span className="flex items-center gap-1.5 rounded-full border border-tiret bg-creux px-3 py-0.5 text-[11.5px] font-bold text-dore">
            <span
              className="size-[7px] rounded-full"
              style={{ background: couleurZone(zones, focusZone.id) }}
            />
            {focusZone.nom} choisie — le ruban se concentre
          </span>
        )}
        <span className="ml-auto text-xs tabular-nums text-sourdine">
          du {dateLongue(enDate(aujourdhui))} au {dateLongue(enDate(dernierJour))}
        </span>
        {onGererFlux && (
          <button
            type="button"
            aria-label="Gérer les calendriers externes"
            onClick={onGererFlux}
            className="rounded-lg p-1 text-sourdine transition-colors hover:text-dore"
          >
            <CalendarPlus className="size-4" />
          </button>
        )}
      </div>

      <div className="mt-3.5 grid grid-cols-[1.15fr_repeat(7,minmax(0,1fr))_1.25fr] gap-2">
        {/* En retard */}
        <div className="min-h-[118px] rounded-xl bg-orange/[0.07] p-2">
          <div className="mb-2 text-center text-[10.5px] font-bold uppercase tracking-[0.05em] text-orange">
            En retard
          </div>
          <div className="flex flex-col gap-1.5">
            {groupes.retard.length === 0 && (
              <span className="pt-4 text-center font-bold text-tiret">·</span>
            )}
            {groupes.retard.map((o) => (
              <PuceTache
                key={o.id}
                occurrence={o}
                couleur={couleurZone(zones, o.zoneId)}
                sousTitre={`depuis ${joursDeRetard(o.echeance!, aujourdhui)} jour${joursDeRetard(o.echeance!, aujourdhui) > 1 ? 's' : ''}`}
                estompee={estompee(o.zoneId)}
                onOuvrir={onOuvrirTache}
              />
            ))}
          </div>
        </div>

        {/* Aujourd'hui + 6 jours */}
        {groupes.jours.map((jour, i) => {
          const entrees = jour.taches.length + jour.evenements.length
          const deplie = joursDeplies.has(jour.date)
          const tachesVisibles =
            deplie ? jour.taches : jour.taches.slice(0, Math.max(0, PUCES_PAR_JOUR - jour.evenements.length))
          const masquees = jour.taches.length - tachesVisibles.length
          return (
            <div
              key={jour.date}
              data-fond={i === 0 ? 'carte' : undefined}
              className={cn(
                'group min-h-[118px] rounded-xl p-2',
                i === 0 && 'bg-[#f6efd3] outline outline-[1.5px] outline-jaune',
                i > 0 && estWeekend(jour.date) && 'bg-tiret/20',
              )}
            >
              <div
                className={cn(
                  'mb-2 text-center text-[10.5px] font-bold uppercase leading-[1.35] tracking-[0.05em] tabular-nums',
                  i === 0 ? 'text-dore' : 'text-sourdine',
                )}
              >
                {i === 0 ? (
                  <>
                    aujourd’hui
                    <br />
                  </>
                ) : null}
                {jourCourt(jour.date)} {enDate(jour.date).getDate()}
              </div>
              <div className="flex flex-col gap-1.5">
                {entrees === 0 && <span className="pt-4 text-center font-bold text-tiret">·</span>}
                {tachesVisibles.map((o) => (
                  <PuceTache
                    key={o.id}
                    occurrence={o}
                    couleur={couleurZone(zones, o.zoneId)}
                    estompee={estompee(o.zoneId)}
                    onOuvrir={onOuvrirTache}
                  />
                ))}
                {jour.evenements.map((e) => (
                  <PuceEvenement
                    key={`${e.titre}-${e.date}`}
                    evenement={e}
                    estompee={focusZone != null}
                  />
                ))}
                {masquees > 0 && (
                  <button
                    type="button"
                    onClick={() => deplierJour(jour.date)}
                    className="text-center text-[10.5px] font-bold text-dore hover:text-orange"
                  >
                    + {masquees} autre{masquees > 1 ? 's' : ''}
                  </button>
                )}
              </div>
            </div>
          )
        })}

        {/* Plus tard */}
        <div className="min-h-[118px] rounded-xl bg-creux p-2">
          <div className="mb-2 text-center text-[10.5px] font-bold uppercase tracking-[0.05em] text-sourdine">
            Plus tard →
          </div>
          {groupes.plusTard.length === 0 && (
            <span className="block pt-4 text-center font-bold text-tiret">·</span>
          )}
          <div className="flex flex-col gap-1">
            {groupes.plusTard.slice(0, LIGNES_PLUS_TARD).map((o) => (
              <button
                key={o.id}
                type="button"
                onClick={() => onOuvrirTache(o.tacheId)}
                className={cn(
                  'flex items-center gap-1.5 text-left text-[10.5px] font-bold tabular-nums text-texte hover:text-orange',
                  estompee(o.zoneId) && 'opacity-40',
                )}
              >
                <span
                  className="size-1.5 shrink-0 rounded-full"
                  style={{ background: couleurZone(zones, o.zoneId) }}
                />
                <span className="min-w-0 truncate">
                  {jourMoisCourt(o.echeance!)} · {o.titre}
                </span>
              </button>
            ))}
          </div>
          {groupes.plusTard.length > LIGNES_PLUS_TARD && (
            <div className="mt-1.5 text-[10px] text-sourdine">
              {groupes.plusTard.length - LIGNES_PLUS_TARD} de plus
            </div>
          )}
          {groupes.plusTard.length > 0 && (
            <div className="mt-2 text-[10px] text-sourdine">
              {groupes.plusTard.length} tâche{groupes.plusTard.length > 1 ? 's' : ''} d’ici un mois
            </div>
          )}
        </div>
      </div>

      {zones.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-3.5 border-t border-barre-piste pt-2.5 text-[11.5px] text-sourdine">
          {zones.map((zone) => (
            <span key={zone.id} className="flex items-center gap-1.5">
              <span
                className="size-[7px] rounded-full"
                style={{ background: couleurZone(zones, zone.id) }}
              />
              {zone.nom}
            </span>
          ))}
        </div>
      )}
    </section>
  )
}
