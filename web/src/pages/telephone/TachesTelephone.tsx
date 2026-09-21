import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Check, MoreHorizontal, Paperclip, Plus, Search } from 'lucide-react'
import Avatar from '@/components/Avatar'
import ErreurChargement from '@/components/ErreurChargement'
import TacheEditeur from '@/components/TacheEditeur'
import FeuilleActions from '@/components/telephone/FeuilleActions'
import { api, dateLocaleIso, type Occurrence, type TacheResume } from '@/lib/api'
import { useCompletionAvecUndo } from '@/lib/completion'
import { dateCourte, jourCourt } from '@/lib/format'
import {
  chipsRecurrence,
  grouperParRythme,
  tonEcheance,
  type ChipRecurrence,
} from '@/lib/taches-vues'
import { cn } from '@/lib/utils'

// La console des tâches au doigt (D-2026-09-19 Portée De La Vue Téléphone) :
// même source de vérité que le bureau — mêmes clés de requête, mêmes groupes de
// rythme (lib/taches-vues), mêmes mots — mais une rangée retournée. Sur le bureau
// le titre est pris entre des colonnes `shrink-0` et s'écrase à 0 px dès qu'un
// écran est étroit ; ici il occupe sa propre ligne pleine largeur et tout le reste
// (chips de récurrence, lieu, échéance) descend d'un cran en métadonnées.
//
// Pas de commutateur « Liste | Année » : la vue Année reste au bureau — son ruban
// fait ~3700 px de piste, illisible au doigt (même décision).

const COULEURS_CHIPS: Record<ChipRecurrence['classe'], string> = {
  hebdo: 'text-[#4d699b]',
  intervalle: 'text-[#597b75]',
  mois: 'text-[#624c83]',
  annuelle: 'text-dore',
  saison: 'text-orange',
}

/** Recherche insensible aux accents : « corvee » doit trouver « corvée ». */
function normaliser(texte: string): string {
  return texte
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
}

/** FeuilleActions parle Occurrence ; la console, elle, liste des définitions qui
 * portent l'id de leur occurrence en attente. On rhabille la tâche en occurrence
 * plutôt que de refaire une feuille d'actions pour le même geste. */
function enOccurrence(tache: TacheResume, occurrenceId: string): Occurrence {
  return {
    id: occurrenceId,
    tacheId: tache.id,
    titre: tache.titre,
    description: tache.description,
    echeance: tache.echeance,
    statut: 'EnAttente',
    assigneA: tache.assigneA,
    completeePar: null,
    completeeLe: null,
    notes: null,
    zoneId: tache.zoneId,
    equipementId: tache.equipementId,
    modeRecurrence: tache.recurrence.mode,
    echeanceFerme: tache.echeanceFerme,
  }
}

/** L'échéance en un mot, mêmes tons que le bureau. Rien à dire quand il n'y a pas
 * d'échéance : le tiret du bureau ne servait qu'à tenir sa colonne. */
function LibelleEcheance({ echeance, aujourdhui }: { echeance: string | null; aujourdhui: string }) {
  const ton = tonEcheance(echeance, aujourdhui)
  if (ton === 'aucune' || echeance === null) {
    return null
  }
  const texte = echeance === aujourdhui ? 'Aujourd’hui' : `${jourCourt(echeance)} ${dateCourte(echeance)}`
  return (
    <span
      className={cn(
        'tabular-nums',
        ton === 'retard' && 'font-extrabold text-rouge',
        ton === 'proche' && 'font-extrabold text-dore',
        ton === 'normal' && 'text-sourdine',
      )}
    >
      {ton === 'retard' ? `retard · ${texte}` : texte}
    </span>
  )
}

export default function TachesTelephone() {
  const [editeur, setEditeur] = useState<{ tacheId: string | null } | null>(null)
  const [feuillePour, setFeuillePour] = useState<Occurrence | null>(null)
  const [recherche, setRecherche] = useState('')
  const aujourdhui = dateLocaleIso()

  const {
    data: taches,
    isLoading,
    isError,
    refetch,
  } = useQuery({ queryKey: ['taches'], queryFn: api.taches })
  const { data: zones } = useQuery({ queryKey: ['zones'], queryFn: api.zones })
  const { data: equipements } = useQuery({ queryKey: ['equipements'], queryFn: api.equipements })

  const { completer, fraichementCompletee } = useCompletionAvecUndo()

  function lieu(tache: TacheResume): string | null {
    const morceaux = [
      zones?.find((z) => z.id === tache.zoneId)?.nom,
      equipements?.find((e) => e.id === tache.equipementId)?.nom,
    ].filter((m): m is string => m !== undefined)
    return morceaux.length > 0 ? morceaux.join(' · ') : null
  }

  // On filtre avant de grouper : les compteurs de rythme et le « en retard » de
  // chaque en-tête doivent parler de ce qui est à l'écran, pas de tout le foyer.
  const terme = normaliser(recherche.trim())
  const visibles = (taches ?? []).filter(
    (t) => terme === '' || normaliser(t.titre).includes(terme),
  )
  const { groupes } = grouperParRythme(visibles, aujourdhui)

  if (isError) {
    return (
      <div className="pt-1">
        <ErreurChargement quoi="les tâches" onReessayer={() => void refetch()} />
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-3 pt-1">
      <div className="flex items-center justify-between gap-3">
        <h1 className="font-titre text-[27px] leading-tight font-bold text-encre">
          Toutes les tâches
        </h1>
        <button
          type="button"
          onClick={() => setEditeur({ tacheId: null })}
          className="flex min-h-[44px] shrink-0 items-center gap-1.5 rounded-2xl bg-orange px-4 text-[15px] font-extrabold text-carte"
        >
          <Plus className="size-4" />
          Nouvelle
        </button>
      </div>

      <label className="relative flex items-center">
        <span className="sr-only">Rechercher une tâche par son titre</span>
        <Search className="pointer-events-none absolute left-3 size-4 text-dore" />
        {/* 16 px pile : en dessous, iOS zoome sur le champ au focus et ne revient pas. */}
        <input
          type="search"
          value={recherche}
          onChange={(e) => setRecherche(e.target.value)}
          placeholder="Rechercher une tâche…"
          className="min-h-[48px] w-full rounded-2xl bg-carte pr-3 pl-9 text-[16px] text-encre shadow-carte placeholder:text-dore"
        />
      </label>

      {isLoading && <p className="py-8 text-center text-[15px] text-sourdine">Chargement…</p>}

      {isLoading === false && (taches ?? []).length === 0 && (
        <p className="rounded-[20px] border border-dashed border-tiret px-5 py-10 text-center text-[15px] text-dore">
          Aucune tâche encore — « Nouvelle » pour créer la première.
        </p>
      )}

      {isLoading === false && (taches ?? []).length > 0 && groupes.length === 0 && (
        <p className="rounded-[20px] border border-dashed border-tiret px-5 py-10 text-center text-[15px] text-dore">
          Aucune tâche ne correspond à « {recherche.trim()} ».
        </p>
      )}

      {groupes.map((groupe) => (
        <section key={groupe.cle} className="rounded-[20px] bg-carte px-3 py-2.5 shadow-carte">
          <header className="flex items-baseline gap-2 px-1 pb-1.5">
            <h2 className="font-titre text-[17px] font-bold text-encre">{groupe.titre}</h2>
            <span className="rounded-full bg-creux px-2 py-0.5 text-[12px] font-bold text-dore">
              {groupe.taches.length}
            </span>
            {groupe.enRetard > 0 && (
              <span className="ml-auto text-[13px] font-extrabold text-rouge">
                {groupe.enRetard} en retard
              </span>
            )}
          </header>

          {groupe.taches.map((tache) => {
            const enRetard = tache.echeance !== null && tache.echeance < aujourdhui
            const chips = chipsRecurrence(tache.recurrence)
            const endroit = lieu(tache)
            const fraiche =
              tache.occurrenceId !== null && fraichementCompletee === tache.occurrenceId
            return (
              <div
                key={tache.id}
                className={cn(
                  'flex items-start gap-1 border-t border-dashed border-tiret first:border-t-0',
                  // Même lavis vert que les listes d'occurrences : il retombe sur
                  // les 6 s du toast, pour que le geste et sa confirmation se
                  // lisent au même endroit (D-2026-08-28 Toast Carte Posée).
                  fraiche && 'rangee-lavis',
                )}
              >
                {tache.occurrenceId !== null && (
                  <button
                    type="button"
                    aria-label={`Compléter ${tache.titre}`}
                    disabled={completer.isPending}
                    onClick={() =>
                      completer.mutate({ id: tache.occurrenceId!, titre: tache.titre })
                    }
                    className="flex size-11 shrink-0 items-center justify-center"
                  >
                    <span
                      className={cn(
                        'flex size-[26px] items-center justify-center rounded-[9px] border-[2.5px]',
                        fraiche ? 'border-vert bg-vert' : enRetard ? 'border-rouge' : 'border-coche',
                      )}
                    >
                      {fraiche && <Check className="size-4 text-carte" />}
                    </span>
                  </button>
                )}
                {/* Sans occurrence en attente (ponctuelle déjà faite, récurrente hors
                    saison) : pas de case, mais la gouttière reste pour que les
                    titres restent alignés d'une rangée à l'autre. */}
                {tache.occurrenceId === null && <span className="size-11 shrink-0" />}

                <button
                  type="button"
                  onClick={() => setEditeur({ tacheId: tache.id })}
                  className="flex min-h-[44px] min-w-0 flex-1 flex-col justify-center gap-0.5 py-2 text-left"
                >
                  {/* Le titre d'abord, sur toute la largeur, jusqu'à deux lignes :
                      c'est lui qu'on cherche du regard en balayant la liste. */}
                  <span className="line-clamp-2 text-[16px] leading-snug font-bold text-encre">
                    {tache.titre}
                  </span>
                  <span className="flex flex-wrap items-center gap-x-2 gap-y-1 text-[13px] text-sourdine">
                    {chips.map((chip) => (
                      <span
                        key={chip.libelle}
                        className={cn(
                          'inline-flex items-center rounded-full border border-tiret bg-creux px-2 py-px text-[12px] font-bold whitespace-nowrap',
                          COULEURS_CHIPS[chip.classe],
                        )}
                      >
                        {chip.libelle}
                      </span>
                    ))}
                    {endroit !== null && <span>{endroit}</span>}
                    {tache.nbDocuments > 0 && (
                      <span className="inline-flex items-center gap-0.5">
                        <Paperclip className="size-3.5" />
                        {tache.nbDocuments}
                      </span>
                    )}
                    <LibelleEcheance echeance={tache.echeance} aujourdhui={aujourdhui} />
                    {tache.assigneA !== null && <Avatar utilisateur={tache.assigneA} taille={20} />}
                  </span>
                </button>

                {tache.occurrenceId !== null && (
                  <button
                    type="button"
                    aria-label={`Actions pour ${tache.titre}`}
                    onClick={() => setFeuillePour(enOccurrence(tache, tache.occurrenceId!))}
                    className="flex size-11 shrink-0 items-center justify-center self-center text-dore"
                  >
                    <MoreHorizontal className="size-5" />
                  </button>
                )}
              </div>
            )
          })}
        </section>
      ))}

      {feuillePour !== null && (
        <FeuilleActions
          occurrence={feuillePour}
          onFermer={() => setFeuillePour(null)}
          onModifier={(tacheId) => setEditeur({ tacheId })}
        />
      )}
      {editeur !== null && (
        <TacheEditeur tacheId={editeur.tacheId} onFermer={() => setEditeur(null)} />
      )}
    </div>
  )
}
