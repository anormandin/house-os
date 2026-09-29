import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check } from 'lucide-react'
import ErreurChargement from '@/components/ErreurChargement'
import { api } from '@/lib/api'
import { chipsRecurrence } from '@/lib/taches-vues'
import { afficherToast } from '@/lib/toast'
import { cn } from '@/lib/utils'

const LIBELLES_STRATEGIE: Record<string, string> = {
  Alternance: 'chacun son tour',
  MoinsLAFait: 'à qui l’a le moins fait',
}

/**
 * Le panneau des packs d'entretien (D-2026-09-28 Packs D'entretien En Fichier De
 * Données) : pour un équipement classé, ou pour le programme de la maison. Ce qui est
 * déjà dans les tâches est coché et grisé ; le reste part coché et se décoche.
 */
export default function PropositionsEntretien({
  equipementId,
  titre,
  onFermer,
}: {
  equipementId: string | null
  titre: string
  onFermer: () => void
}) {
  const queryClient = useQueryClient()
  const [decochees, setDecochees] = useState<Set<string>>(() => new Set())

  const {
    data: propositions,
    isError,
    refetch,
  } = useQuery({
    queryKey: ['entretien', 'propositions', equipementId ?? 'maison'],
    queryFn: () => api.propositionsEntretien(equipementId),
    staleTime: 0,
  })

  const candidates = (propositions?.propositions ?? []).filter((p) => p.dejaPresente === false)
  const retenues = candidates.filter((p) => decochees.has(p.cle) === false)

  const adopter = useMutation({
    mutationFn: () => api.adopterEntretiens(retenues.map((p) => p.cle), equipementId),
    onSuccess: ({ creees }) => {
      queryClient.invalidateQueries({ queryKey: ['taches'] })
      queryClient.invalidateQueries({ queryKey: ['occurrences'] })
      queryClient.invalidateQueries({ queryKey: ['equipement'] })
      queryClient.invalidateQueries({ queryKey: ['entretien'] })
      afficherToast({
        message: creees.length === 1 ? 'Tâche créée' : `${creees.length} tâches créées`,
        sousTitre: creees.length === 1 ? creees[0].titre : titre,
        ton: 'succes',
        slot: 'entretien',
      })
      onFermer()
    },
  })

  const basculer = (cle: string) =>
    setDecochees((anciennes) => {
      const nouvelles = new Set(anciennes)
      if (nouvelles.has(cle)) {
        nouvelles.delete(cle)
      } else {
        nouvelles.add(cle)
      }
      return nouvelles
    })

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-encre/30 p-4"
      onClick={onFermer}
    >
      <div
        role="dialog"
        aria-label={titre}
        className="flex max-h-[92dvh] w-full max-w-xl flex-col gap-4 overflow-y-auto rounded-[28px] bg-carte p-7 shadow-carte-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <div>
          <h2 className="text-2xl font-bold">{titre}</h2>
          <p className="mt-1 text-sm text-sourdine">
            {equipementId === null
              ? 'Ce que toute maison de par ici demande, au fil des saisons. Décoche ce qui ne s’applique pas.'
              : 'Les entretiens habituels pour ce genre d’équipement. Décoche ce qui ne s’applique pas.'}
          </p>
        </div>

        {isError && <ErreurChargement quoi="les propositions" onReessayer={() => refetch()} />}

        {propositions !== undefined && propositions.propositions.length === 0 && (
          <p className="rounded-2xl bg-creux/60 px-4 py-3 text-sm text-sourdine">
            {propositions.raison ?? 'Rien à proposer.'}
          </p>
        )}

        {propositions !== undefined && propositions.propositions.length > 0 && (
          <ul className="flex flex-col gap-2">
            {propositions.propositions.map((p) => {
              const cochee = p.dejaPresente || decochees.has(p.cle) === false
              return (
                <li key={p.cle}>
                  <label
                    className={cn(
                      'flex cursor-pointer items-start gap-3 rounded-2xl bg-creux/60 px-4 py-3',
                      p.dejaPresente && 'cursor-default opacity-60',
                    )}
                  >
                    <input
                      type="checkbox"
                      checked={cochee}
                      disabled={p.dejaPresente}
                      onChange={() => basculer(p.cle)}
                      aria-label={p.titre}
                      className="mt-1 size-4 accent-orange"
                    />
                    <span className="min-w-0 flex-1">
                      <span className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
                        <span className="font-bold">{p.titre}</span>
                        {chipsRecurrence(p.recurrence).map((chip) => (
                          <span key={chip.libelle} className="text-xs text-dore">
                            {chip.libelle}
                          </span>
                        ))}
                        {p.strategie !== null && LIBELLES_STRATEGIE[p.strategie] !== undefined && (
                          <span className="text-xs text-sourdine">· {LIBELLES_STRATEGIE[p.strategie]}</span>
                        )}
                      </span>
                      {p.description !== null && (
                        <span className="mt-0.5 block text-[13px] leading-snug text-sourdine">
                          {p.description}
                        </span>
                      )}
                      {p.dejaPresente && (
                        <span className="mt-1 flex items-center gap-1 text-xs font-bold text-vert">
                          <Check className="size-3.5" /> déjà dans vos tâches
                        </span>
                      )}
                    </span>
                  </label>
                </li>
              )
            })}
          </ul>
        )}

        <div className="flex items-center justify-end gap-2 pt-1">
          <button
            type="button"
            onClick={onFermer}
            className="rounded-xl px-4 py-2 text-sm font-bold text-sourdine hover:text-texte"
          >
            Fermer
          </button>
          <button
            type="button"
            disabled={retenues.length === 0 || adopter.isPending}
            onClick={() => adopter.mutate()}
            className="rounded-xl bg-orange px-5 py-2 text-sm font-bold text-carte disabled:opacity-40"
          >
            {retenues.length === 1 ? 'Créer 1 tâche' : `Créer ${retenues.length} tâches`}
          </button>
        </div>
      </div>
    </div>
  )
}
