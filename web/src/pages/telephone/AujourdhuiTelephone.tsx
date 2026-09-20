import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Check, ChevronRight, Clock, MapPin, MoreHorizontal, Plus, Repeat, SkipForward } from 'lucide-react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import Avatar from '@/components/Avatar'
import ErreurChargement from '@/components/ErreurChargement'
import TacheEditeur from '@/components/TacheEditeur'
import FeuilleActions from '@/components/telephone/FeuilleActions'
import { api, dateLocaleIso, type Occurrence } from '@/lib/api'
import { invaliderAutourOccurrences, useCompletionAvecUndo } from '@/lib/completion'
import { bornesJourneeLocale, dateCourte, dateLongue, dodosAvant } from '@/lib/format'
import { DATE_DEMENAGEMENT, phraseDuJour } from '@/lib/humeur'
import { cn } from '@/lib/utils'

function joursDeRetard(echeance: string, aujourdhui: string): number {
  return Math.round(
    (new Date(`${aujourdhui}T00:00:00`).getTime() - new Date(`${echeance}T00:00:00`).getTime()) /
      86_400_000,
  )
}

/** L'écran d'accueil du téléphone : la journée en pile de cartes, une à la fois,
 * dans la zone du pouce (direction « La pile », retenue 2026-09-19). Le bureau
 * garde son tableau de bord — cette vue est un compagnon, pas un remplacement. */
export default function AujourdhuiTelephone() {
  const queryClient = useQueryClient()
  const [editeurTacheId, setEditeurTacheId] = useState<string | null>(null)
  const [feuillePour, setFeuillePour] = useState<Occurrence | null>(null)
  const [journeeDepliee, setJourneeDepliee] = useState(false)

  const {
    data: ouvertes,
    isLoading,
    isError,
    refetch,
  } = useQuery({
    queryKey: ['occurrences', 'aujourdhui'],
    queryFn: () => api.occurrences('aujourdhui'),
  })
  const { data: faites } = useQuery({
    queryKey: ['occurrences', 'faites'],
    queryFn: () => {
      const bornes = bornesJourneeLocale()
      return api.occurrencesFaites(bornes.de, bornes.a)
    },
  })
  const { data: zones } = useQuery({ queryKey: ['zones'], queryFn: api.zones })
  const { data: phraseServeur } = useQuery({
    queryKey: ['phrase-du-jour'],
    queryFn: api.phraseDuJour,
    refetchInterval: 15 * 60 * 1000,
  })

  const { completer } = useCompletionAvecUndo()
  const passer = useMutation({
    mutationFn: (id: string) => api.passer(id),
    onSuccess: () => invaliderAutourOccurrences(queryClient),
  })

  const aujourdhui = dateLocaleIso()
  const dodosDemenagement = dodosAvant(DATE_DEMENAGEMENT)
  const enRetard = (ouvertes ?? []).filter(
    (o) => o.echeance !== null && o.echeance < aujourdhui,
  )
  const phrase =
    phraseServeur ??
    phraseDuJour({
      ouvertes: ouvertes?.length ?? 0,
      enRetard: enRetard.length,
      faites: faites?.length ?? 0,
      dodosDemenagement: dodosDemenagement > 0 ? dodosDemenagement : null,
    })

  // Le plus en retard d'abord : la pile propose toujours la chose la plus vieille
  // avant celles d'aujourd'hui.
  const pile = [...(ouvertes ?? [])].sort((a, b) => {
    const ea = a.echeance ?? '9999-12-31'
    const eb = b.echeance ?? '9999-12-31'
    return ea === eb ? a.titre.localeCompare(b.titre, 'fr-CA') : ea.localeCompare(eb)
  })
  const [dessus, ...dessous] = pile
  const total = (ouvertes?.length ?? 0) + (faites?.length ?? 0)
  const nomZone = (id: string | null) => zones?.find((z) => z.id === id)?.nom ?? null

  if (isError) {
    return (
      <ErreurChargement quoi="les tâches du jour" onReessayer={() => void refetch()} />
    )
  }

  return (
    <div className="flex flex-col gap-3 pt-1">
      <div className="flex items-baseline justify-between gap-3">
        <span className="text-[13px] font-extrabold tracking-[0.08em] text-orange uppercase">
          {dateLongue()}
        </span>
        {enRetard.length > 0 && (
          <span className="text-[13px] font-extrabold text-rouge">
            {enRetard.length} en retard
          </span>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <h1 className="font-titre text-[27px] leading-tight font-bold text-encre">{phrase.titre}</h1>
        <p className="text-[15px] leading-snug text-texte">{phrase.sousTitre}</p>
      </div>

      {total > 0 && (
        <div className="flex items-center gap-3">
          <div className="h-2 flex-1 overflow-hidden rounded-full bg-barre-piste">
            <div
              className="h-full rounded-full bg-orange"
              style={{ width: `${Math.round(((faites?.length ?? 0) / total) * 100)}%` }}
            />
          </div>
          <span className="shrink-0 text-[13px] font-bold text-dore">
            {faites?.length ?? 0} sur {total} aujourd’hui
          </span>
        </div>
      )}

      {isLoading && <div className="py-10 text-center text-dore">Chargement…</div>}

      {isLoading === false && dessus === undefined && (
        <div className="mt-4 rounded-[20px] border border-dashed border-tiret px-5 py-10 text-center text-[15px] text-dore">
          Rien pour aujourd'hui. La maison respire.
        </div>
      )}

      {dessus !== undefined && (
        <div className="relative mt-1">
          <article
            className={cn(
              'relative z-20 flex flex-col gap-3 rounded-[22px] bg-carte p-4 shadow-carte-lg',
              dessus.echeance !== null &&
                dessus.echeance < aujourdhui &&
                'border-l-[5px] border-rouge',
            )}
          >
            <div className="flex flex-wrap items-center gap-2">
              {dessus.echeance !== null && dessus.echeance < aujourdhui ? (
                <span className="flex items-center gap-1.5 rounded-full border border-rouge px-3 py-1 text-[13px] font-extrabold text-rouge">
                  <Clock className="size-3.5" />
                  depuis {joursDeRetard(dessus.echeance, aujourdhui)} jours
                </span>
              ) : (
                dessus.echeance !== null && (
                  <span className="rounded-full bg-creux px-3 py-1 text-[13px] font-bold text-dore">
                    échéance {dateCourte(dessus.echeance)}
                  </span>
                )
              )}
            </div>

            <h2 className="font-titre text-[25px] leading-[1.15] font-bold text-encre">
              {dessus.titre}
            </h2>

            {dessus.description !== null && dessus.description !== '' && (
              <p className="text-[14px] leading-snug text-dore">{dessus.description}</p>
            )}

            <div className="flex flex-wrap items-center gap-2">
              {nomZone(dessus.zoneId) !== null && (
                <span className="flex items-center gap-1.5 rounded-full bg-creux px-3 py-1 text-[13px] font-bold text-dore">
                  <MapPin className="size-3.5" />
                  {nomZone(dessus.zoneId)}
                </span>
              )}
              {dessus.modeRecurrence !== 'Ponctuelle' && (
                <span className="flex items-center gap-1.5 rounded-full bg-creux px-3 py-1 text-[13px] font-bold text-dore">
                  <Repeat className="size-3.5" />
                  récurrente
                </span>
              )}
              {dessus.assigneA !== null && (
                <span className="flex items-center gap-1.5 text-[13px] font-bold text-dore">
                  <Avatar utilisateur={dessus.assigneA} taille={24} />
                  {dessus.assigneA.nomAffichage}
                </span>
              )}
            </div>

            <div className="flex items-stretch gap-2.5">
              <button
                type="button"
                aria-label={`Compléter ${dessus.titre}`}
                onClick={() => completer.mutate({ id: dessus.id, titre: dessus.titre })}
                className="flex min-h-[56px] flex-1 items-center justify-center gap-2 rounded-2xl bg-vert text-[17px] font-extrabold text-carte"
              >
                <Check className="size-5" />
                Compléter
              </button>
              <button
                type="button"
                aria-label={`Reporter ${dessus.titre}`}
                onClick={() => setFeuillePour(dessus)}
                className="flex min-h-[56px] flex-1 items-center justify-center gap-2 rounded-2xl border-2 border-tiret text-[17px] font-extrabold text-encre"
              >
                Reporter
              </button>
            </div>

            <div className="flex items-center justify-between">
              {dessus.modeRecurrence !== 'Ponctuelle' ? (
                <button
                  type="button"
                  aria-label={`Passer ${dessus.titre} cette fois-ci`}
                  onClick={() => passer.mutate(dessus.id)}
                  className="flex min-h-[44px] items-center gap-2 text-[15px] font-bold text-dore"
                >
                  <SkipForward className="size-4" />
                  Passer
                </button>
              ) : (
                <span />
              )}
              <button
                type="button"
                aria-label={`Actions pour ${dessus.titre}`}
                onClick={() => setFeuillePour(dessus)}
                className="flex size-11 items-center justify-center text-dore"
              >
                <MoreHorizontal className="size-5" />
              </button>
            </div>
          </article>

          {/* Les tranches derrière : on voit ce qui s'en vient sans le lire en entier. */}
          {dessous.slice(0, 2).map((o, i) => (
            <div
              key={o.id}
              className="relative flex items-center gap-2 rounded-b-[20px] bg-carte px-4 shadow-carte"
              style={{
                zIndex: 10 - i,
                marginTop: -12,
                marginInline: `${(i + 1) * 8}px`,
                paddingTop: 14,
                paddingBottom: 6,
                opacity: 1 - i * 0.25,
              }}
            >
              {i === 0 && (
                <span className="shrink-0 text-[12px] font-extrabold tracking-[0.08em] text-dore uppercase">
                  Ensuite
                </span>
              )}
              <span className="min-w-0 flex-1 truncate text-[14px] font-bold text-texte">
                {o.titre}
              </span>
            </div>
          ))}
        </div>
      )}

      <button
        type="button"
        onClick={() => setJourneeDepliee((v) => !v)}
        className="mt-2 flex min-h-[48px] items-center justify-center gap-1.5 text-[15px] font-extrabold text-dore"
      >
        {journeeDepliee ? 'Replier la journée' : 'Voir toute la journée'}
        <ChevronRight className={cn('size-4 transition-transform', journeeDepliee && 'rotate-90')} />
      </button>

      {journeeDepliee && (
        <div className="flex flex-col overflow-hidden rounded-[20px] bg-carte shadow-carte">
          {[...(ouvertes ?? []), ...(faites ?? [])].map((o) => {
            const faite = o.statut === 'Completee'
            return (
              <button
                key={o.id}
                type="button"
                onClick={() => setFeuillePour(o)}
                className={cn(
                  'flex min-h-[56px] items-center gap-3 border-b border-creux px-4 text-left last:border-b-0',
                  faite && 'bg-vert-fond',
                )}
              >
                <span
                  className={cn(
                    'flex size-[26px] shrink-0 items-center justify-center rounded-[9px] border-[2.5px]',
                    faite
                      ? 'border-vert bg-vert'
                      : o.echeance !== null && o.echeance < aujourdhui
                        ? 'border-rouge'
                        : 'border-coche',
                  )}
                >
                  {faite && <Check className="size-4 text-carte" />}
                </span>
                <span
                  className={cn(
                    'min-w-0 flex-1 text-[16px] font-bold',
                    faite ? 'text-sourdine line-through' : 'text-encre',
                  )}
                >
                  {o.titre}
                </span>
              </button>
            )
          })}
        </div>
      )}

      <button
        type="button"
        onClick={() => setEditeurTacheId('')}
        className="mt-1 flex min-h-[52px] items-center justify-center gap-2 rounded-2xl border border-dashed border-tiret text-[16px] font-extrabold text-dore"
      >
        <Plus className="size-5" />
        Ajouter une tâche
      </button>

      {feuillePour !== null && (
        <FeuilleActions
          occurrence={feuillePour}
          onFermer={() => setFeuillePour(null)}
          onModifier={(tacheId) => setEditeurTacheId(tacheId)}
        />
      )}
      {editeurTacheId !== null && (
        <TacheEditeur
          tacheId={editeurTacheId === '' ? null : editeurTacheId}
          onFermer={() => setEditeurTacheId(null)}
        />
      )}
    </div>
  )
}
