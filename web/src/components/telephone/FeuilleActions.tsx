import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { CalendarClock, Check, Pencil, SkipForward, Trash2 } from 'lucide-react'
import { api, dateLocaleIso, type Occurrence } from '@/lib/api'
import { invaliderAutourOccurrences, useCompletionAvecUndo } from '@/lib/completion'
import { cn } from '@/lib/utils'

function ajouterJours(depuis: string, jours: number): string {
  const date = new Date(`${depuis}T00:00:00`)
  date.setDate(date.getDate() + jours)
  return dateLocaleIso(date)
}

/** La feuille du bas qui remplace, au doigt, les actions que le bureau ne révèle
 * qu'au survol (Reporter / Passer / Supprimer étaient en opacity-0 :hover — donc
 * invisibles et inatteignables sur un écran tactile). */
export default function FeuilleActions({
  occurrence,
  onFermer,
  onModifier,
}: {
  occurrence: Occurrence
  onFermer: () => void
  onModifier: (tacheId: string) => void
}) {
  const queryClient = useQueryClient()
  const invalider = () => invaliderAutourOccurrences(queryClient)
  const [reportOuvert, setReportOuvert] = useState(false)
  const [confirmerSuppression, setConfirmerSuppression] = useState(false)

  const { completer } = useCompletionAvecUndo()
  const passer = useMutation({
    mutationFn: (id: string) => api.passer(id),
    onSuccess: () => {
      invalider()
      onFermer()
    },
  })
  const reporter = useMutation({
    mutationFn: ({ id, echeance }: { id: string; echeance: string }) => api.reporter(id, echeance),
    onSuccess: () => {
      invalider()
      onFermer()
    },
  })
  const supprimer = useMutation({
    mutationFn: (tacheId: string) => api.supprimerTache(tacheId),
    onSuccess: () => {
      invalider()
      onFermer()
    },
  })

  const aujourdhui = dateLocaleIso()
  const recurrente = occurrence.modeRecurrence !== 'Ponctuelle'
  const classeRangee =
    'flex min-h-[56px] w-full items-center gap-3 px-5 text-left text-[17px] font-bold text-texte'

  return (
    <div className="fixed inset-0 z-50 flex flex-col justify-end" role="dialog" aria-modal="true">
      <button
        type="button"
        aria-label="Fermer"
        onClick={onFermer}
        className="absolute inset-0 cursor-default bg-encre/35"
      />
      <div className="relative flex max-h-[85dvh] flex-col overflow-y-auto rounded-t-[28px] bg-carte pb-[calc(env(safe-area-inset-bottom)+12px)] shadow-carte-lg">
        <div className="flex justify-center pt-2.5 pb-1">
          <span className="h-1 w-10 rounded-full bg-tiret" />
        </div>
        <h2 className="px-5 pt-1 pb-3 font-titre text-[19px] font-bold text-encre">
          {occurrence.titre}
        </h2>

        <button
          type="button"
          onClick={() => {
            completer.mutate({ id: occurrence.id, titre: occurrence.titre })
            onFermer()
          }}
          className={classeRangee}
        >
          <Check className="size-5 shrink-0 text-vert" />
          Compléter
        </button>

        {recurrente && (
          <button
            type="button"
            aria-label={`Passer ${occurrence.titre} cette fois-ci`}
            onClick={() => passer.mutate(occurrence.id)}
            className={classeRangee}
          >
            <SkipForward className="size-5 shrink-0 text-dore" />
            Passer cette fois-ci
          </button>
        )}

        <button
          type="button"
          aria-label={`Reporter ${occurrence.titre}`}
          aria-expanded={reportOuvert}
          onClick={() => setReportOuvert((v) => !v)}
          className={classeRangee}
        >
          <CalendarClock className="size-5 shrink-0 text-dore" />
          Reporter…
        </button>
        {reportOuvert && (
          <div className="flex flex-col gap-2 px-5 pt-1 pb-3">
            <div className="flex flex-wrap gap-2">
              {[
                { libelle: 'Demain', jours: 1 },
                { libelle: 'Dans 2 jours', jours: 2 },
                { libelle: 'Semaine prochaine', jours: 7 },
              ].map(({ libelle, jours }) => (
                <button
                  key={libelle}
                  type="button"
                  onClick={() =>
                    reporter.mutate({
                      id: occurrence.id,
                      echeance: ajouterJours(aujourdhui, jours),
                    })
                  }
                  className="min-h-[44px] rounded-full bg-creux px-4 text-[15px] font-bold text-texte"
                >
                  {libelle}
                </button>
              ))}
            </div>
            <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
              Une date précise
              <input
                type="date"
                aria-label="Reporter à une date précise"
                min={aujourdhui}
                onChange={(e) =>
                  e.target.value !== '' &&
                  reporter.mutate({ id: occurrence.id, echeance: e.target.value })
                }
                className="min-h-[48px] rounded-xl bg-creux px-3 text-[16px] text-texte"
              />
            </label>
          </div>
        )}

        <button
          type="button"
          onClick={() => {
            onModifier(occurrence.tacheId)
            onFermer()
          }}
          className={classeRangee}
        >
          <Pencil className="size-5 shrink-0 text-dore" />
          Modifier la tâche
        </button>

        <div className="mt-1 border-t border-creux pt-1">
          <button
            type="button"
            aria-label={
              confirmerSuppression
                ? `Supprimer la tâche ${occurrence.titre} — confirmer`
                : `Supprimer la tâche ${occurrence.titre}`
            }
            onClick={() =>
              confirmerSuppression
                ? supprimer.mutate(occurrence.tacheId)
                : setConfirmerSuppression(true)
            }
            className={cn(classeRangee, 'text-rouge')}
          >
            <Trash2 className="size-5 shrink-0" />
            {confirmerSuppression ? 'Vraiment? Supprimer' : 'Supprimer la tâche'}
          </button>
        </div>
      </div>
    </div>
  )
}
