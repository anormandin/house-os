import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { CalendarClock, Check, MessageSquare, MessageSquarePlus, Repeat, SkipForward } from 'lucide-react'
import Avatar from '@/components/Avatar'
import ConfirmerSuppression from '@/components/ConfirmerSuppression'
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

function ajouterJours(aujourdhui: string, jours: number): string {
  const date = new Date(`${aujourdhui}T00:00:00`)
  date.setDate(date.getDate() + jours)
  return dateLocaleIso(date)
}

export default function OccurrenceListe({
  occurrences,
  vide,
  onModifier,
}: {
  occurrences: Occurrence[]
  vide: string
  onModifier?: (tacheId: string) => void
}) {
  const queryClient = useQueryClient()
  const invalider = () => {
    queryClient.invalidateQueries({ queryKey: ['occurrences'] })
    // Compléter/annuler touche le journal, donc le bilan hebdo.
    queryClient.invalidateQueries({ queryKey: ['journal'] })
    // …et la console des définitions (échéance/assigné en attente, progression).
    queryClient.invalidateQueries({ queryKey: ['taches'] })
  }

  // Éditeurs inline, un seul ouvert à la fois (id d'occurrence concerné).
  const [reportOuvert, setReportOuvert] = useState<string | null>(null)
  const [noteEnEdition, setNoteEnEdition] = useState<{ id: string; texte: string } | null>(null)

  const completer = useMutation({
    mutationFn: (id: string) => api.completer(id),
    onSuccess: invalider,
  })
  const annuler = useMutation({
    mutationFn: (id: string) => api.annulerCompletion(id),
    onSuccess: invalider,
  })
  const passer = useMutation({
    mutationFn: (id: string) => api.passer(id),
    onSuccess: invalider,
  })
  const reporter = useMutation({
    mutationFn: ({ id, echeance }: { id: string; echeance: string }) => api.reporter(id, echeance),
    onSuccess: () => {
      setReportOuvert(null)
      invalider()
    },
  })
  const modifierNotes = useMutation({
    mutationFn: ({ id, notes }: { id: string; notes: string | null }) =>
      api.modifierNotes(id, notes),
    onSuccess: () => {
      setNoteEnEdition(null)
      invalider()
    },
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

  const sauverNote = () => {
    // Enter (submit) puis blur (démontage de l'input) passent tous deux ici :
    // sans la garde isPending, la note partirait deux fois.
    if (noteEnEdition === null || modifierNotes.isPending) {
      return
    }
    const texte = noteEnEdition.texte.trim()
    modifierNotes.mutate({ id: noteEnEdition.id, notes: texte === '' ? null : texte })
  }

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
              className="group flex items-center gap-4 rounded-[20px] bg-vert-fond px-5 py-3.5"
            >
              <span className="flex size-[26px] shrink-0 items-center justify-center rounded-[9px] bg-vert">
                <Check className="size-3.5 text-carte" strokeWidth={3} />
              </span>
              <div className="min-w-0 flex-1">
                <span className="text-base font-bold text-sourdine line-through">{o.titre}</span>
                {noteEnEdition?.id === o.id ? (
                  <form
                    onSubmit={(e) => {
                      e.preventDefault()
                      sauverNote()
                    }}
                  >
                    <input
                      autoFocus
                      value={noteEnEdition.texte}
                      onChange={(e) => setNoteEnEdition({ id: o.id, texte: e.target.value })}
                      onBlur={sauverNote}
                      placeholder="Une note? (coût, remarque…)"
                      className="mt-1 w-full rounded-xl bg-carte px-3 py-1.5 text-[13px] text-encre focus:outline-2 focus:outline-orange/60"
                    />
                  </form>
                ) : (
                  o.notes && (
                    <div className="mt-px whitespace-pre-line text-[13px] text-sourdine">
                      {o.notes}
                    </div>
                  )
                )}
              </div>
              <button
                type="button"
                aria-label={o.notes ? `Modifier la note de ${o.titre}` : `Ajouter une note à ${o.titre}`}
                onClick={() => setNoteEnEdition({ id: o.id, texte: o.notes ?? '' })}
                className="text-sourdine opacity-0 transition-opacity hover:text-dore focus-visible:opacity-100 group-hover:opacity-100"
              >
                {o.notes ? <MessageSquare className="size-4" /> : <MessageSquarePlus className="size-4" />}
              </button>
              <button
                type="button"
                aria-label={`Annuler la complétion de ${o.titre}`}
                disabled={annuler.isPending}
                onClick={() => annuler.mutate(o.id)}
                className="text-[13px] font-bold text-sourdine opacity-0 transition-opacity hover:text-rouge focus-visible:opacity-100 group-hover:opacity-100"
              >
                Annuler
              </button>
              <span className="text-[13px] font-bold text-vert">{libelleFaite(o)}</span>
            </li>
          )
        }

        return (
          <li
            key={o.id}
            className={cn(
              'group flex items-center gap-4 rounded-[20px] bg-carte px-5 py-3.5 shadow-carte',
              enRetard && 'border-l-[6px] border-rouge pl-[14px]',
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
            <div
              className={cn('min-w-0 flex-1', onModifier && 'cursor-pointer')}
              onClick={onModifier ? () => onModifier(o.tacheId) : undefined}
            >
              <div className="flex items-center gap-1.5 text-base font-bold">
                {o.titre}
                {o.modeRecurrence !== 'Ponctuelle' && (
                  <Repeat className="size-3.5 shrink-0 text-dore" aria-label="Tâche récurrente" />
                )}
              </div>
              {enRetard && o.echeance && (
                <div className="mt-px text-[13px] text-rouge">
                  {libelleRetard(o.echeance, aujourdhui)}
                </div>
              )}
              {o.description && (
                <div className="mt-0.5 whitespace-pre-line text-[13px] leading-snug text-dore">
                  {o.description}
                </div>
              )}
            </div>
            {/* Colonnes de largeur fixe pour que dates, avatars et actions
                s'alignent verticalement d'une rangée à l'autre. */}
            <span className="flex w-[9rem] shrink-0 justify-end">
              {aVenir && o.echeance && (
                <span className="rounded-full bg-creux px-3 py-1 text-xs font-bold text-dore">
                  {jourCourt(o.echeance)} {dateCourte(o.echeance)}
                </span>
              )}
            </span>
            <span className="flex w-8 shrink-0 justify-center">
              {o.assigneA && <Avatar utilisateur={o.assigneA} />}
            </span>
            {o.modeRecurrence === 'Ponctuelle' ? (
              <span aria-hidden className="size-4 shrink-0" />
            ) : (
              <button
                type="button"
                aria-label={`Passer ${o.titre} cette fois-ci`}
                disabled={passer.isPending}
                onClick={() => passer.mutate(o.id)}
                className="text-sourdine opacity-0 transition-opacity hover:text-dore focus-visible:opacity-100 group-hover:opacity-100"
              >
                <SkipForward className="size-4" />
              </button>
            )}
            <span className="relative flex">
              <button
                type="button"
                aria-label={`Reporter ${o.titre}`}
                onClick={() => setReportOuvert(reportOuvert === o.id ? null : o.id)}
                className="text-sourdine opacity-0 transition-opacity hover:text-dore focus-visible:opacity-100 group-hover:opacity-100"
              >
                <CalendarClock className="size-4" />
              </button>
              {reportOuvert === o.id && (
                <>
                  <div className="fixed inset-0 z-10" onClick={() => setReportOuvert(null)} />
                  <div className="absolute right-0 top-full z-20 mt-2 flex w-44 flex-col gap-1 rounded-[16px] bg-carte p-2 shadow-carte">
                    {(
                      [
                        ['Demain', ajouterJours(aujourdhui, 1)],
                        ['Dans 2 jours', ajouterJours(aujourdhui, 2)],
                        ['Semaine prochaine', ajouterJours(aujourdhui, 7)],
                      ] as const
                    ).map(([libelle, echeance]) => (
                      <button
                        key={libelle}
                        type="button"
                        disabled={reporter.isPending}
                        onClick={() => reporter.mutate({ id: o.id, echeance })}
                        className="rounded-xl px-3 py-1.5 text-left text-[13px] font-bold text-encre hover:bg-creux"
                      >
                        {libelle}
                      </button>
                    ))}
                    <input
                      type="date"
                      aria-label="Reporter à une date précise"
                      min={aujourdhui}
                      disabled={reporter.isPending}
                      onChange={(e) => {
                        if (e.target.value) {
                          reporter.mutate({ id: o.id, echeance: e.target.value })
                        }
                      }}
                      className="rounded-xl bg-creux px-3 py-1.5 text-[13px] text-encre"
                    />
                  </div>
                </>
              )}
            </span>
            <ConfirmerSuppression
              ariaLabel={`Supprimer la tâche ${o.titre}`}
              onConfirmer={() => supprimer.mutate(o.tacheId)}
              className="opacity-0 transition-opacity focus-visible:opacity-100 group-hover:opacity-100"
            />
          </li>
        )
      })}
    </ul>
  )
}
