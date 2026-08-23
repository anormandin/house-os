import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Check, Trash2 } from 'lucide-react'
import Avatar from '@/components/Avatar'
import { api, dateLocaleIso, type Occurrence } from '@/lib/api'
import { dateCourte, heureQuebec, jourCourt } from '@/lib/format'
import { cn } from '@/lib/utils'

function libelleRetard(echeance: string, aujourdhui: string): string {
  const jours = Math.round(
    (new Date(`${aujourdhui}T00:00:00`).getTime() - new Date(`${echeance}T00:00:00`).getTime()) /
      86_400_000,
  )
  return jours === 1 ? 'depuis hier — on s’en occupe?' : `depuis ${jours} jours — on s’en occupe?`
}

function libelleFaite(occurrence: Occurrence): string {
  const qui = occurrence.completeePar ? `bravo ${occurrence.completeePar.nomAffichage}` : 'fait'
  if (occurrence.completeeLe === null) {
    return qui
  }
  const faiteLe = dateLocaleIso(new Date(occurrence.completeeLe))
  const quand =
    faiteLe === dateLocaleIso()
      ? heureQuebec(occurrence.completeeLe)
      : dateCourte(faiteLe)
  return `${qui} ✓ ${quand}`
}

export default function OccurrenceListe({
  occurrences,
  vide,
}: {
  occurrences: Occurrence[]
  vide: string
}) {
  const queryClient = useQueryClient()
  const invalider = () => queryClient.invalidateQueries({ queryKey: ['occurrences'] })

  const completer = useMutation({
    mutationFn: (id: string) => api.completer(id),
    onSuccess: invalider,
  })
  const supprimer = useMutation({
    mutationFn: (tacheId: string) => api.supprimerTache(tacheId),
    onSuccess: invalider,
  })

  if (occurrences.length === 0) {
    return (
      <p className="rounded-[20px] border-2 border-dashed border-tiret px-5 py-8 text-center text-sm font-bold text-tiret-texte">
        {vide}
      </p>
    )
  }

  const aujourdhui = dateLocaleIso()

  return (
    <ul className="flex flex-col gap-[11px]">
      {occurrences.map((o) => {
        const completee = o.statut === 'Completee'
        const enRetard = !completee && o.echeance !== null && o.echeance < aujourdhui
        const aVenir = !completee && o.echeance !== null && o.echeance > aujourdhui

        if (completee) {
          return (
            <li
              key={o.id}
              className="flex items-center gap-4 rounded-[20px] bg-vert-fond px-5 py-3.5"
            >
              <span className="flex size-[26px] shrink-0 items-center justify-center rounded-[9px] bg-vert">
                <Check className="size-3.5 text-carte" strokeWidth={3} />
              </span>
              <span className="flex-1 text-base font-bold text-sourdine line-through">
                {o.titre}
              </span>
              <span className="text-[13px] font-bold text-vert">{libelleFaite(o)}</span>
            </li>
          )
        }

        return (
          <li
            key={o.id}
            className={cn(
              'group flex items-center gap-4 rounded-[20px] bg-carte px-5 py-3.5 shadow-carte',
              enRetard && 'border-l-[6px] border-rouge',
            )}
          >
            <button
              type="button"
              aria-label={`Compléter ${o.titre}`}
              disabled={completer.isPending}
              onClick={() => completer.mutate(o.id)}
              className={cn(
                'size-[26px] shrink-0 rounded-[9px] border-[2.5px] transition-colors hover:border-vert hover:bg-vert-fond',
                enRetard ? 'border-rouge' : 'border-coche',
              )}
            />
            <div className="min-w-0 flex-1">
              <div className="text-base font-bold">{o.titre}</div>
              {enRetard && o.echeance && (
                <div className="mt-px text-[13px] text-rouge">
                  {libelleRetard(o.echeance, aujourdhui)}
                </div>
              )}
              {o.description && !enRetard && (
                <div className="mt-px text-[13px] text-sourdine">{o.description}</div>
              )}
            </div>
            {aVenir && o.echeance && (
              <span className="rounded-full bg-creux px-3 py-1 text-xs font-bold text-dore">
                {jourCourt(o.echeance)} {dateCourte(o.echeance)}
              </span>
            )}
            {o.assigneA && <Avatar utilisateur={o.assigneA} />}
            <button
              type="button"
              aria-label={`Supprimer ${o.titre}`}
              onClick={() => supprimer.mutate(o.tacheId)}
              className="text-sourdine opacity-0 transition-opacity hover:text-rouge focus-visible:opacity-100 group-hover:opacity-100"
            >
              <Trash2 className="size-4" />
            </button>
          </li>
        )
      })}
    </ul>
  )
}
