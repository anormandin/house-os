import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Pencil, Trash2 } from 'lucide-react'
import { ICONES_COMPTE } from '@/components/Illustrations'
import { api, type CompteARebours, type IconeCompte } from '@/lib/api'
import { dateCourte, dodosAvant } from '@/lib/format'
import { cn } from '@/lib/utils'

const ICONE_DEFAUT: IconeCompte = 'Soleil'

export default function ComptesAReboursGestion({
  comptes,
  onFermer,
}: {
  comptes: CompteARebours[]
  onFermer: () => void
}) {
  const queryClient = useQueryClient()
  const [enEditionId, setEnEditionId] = useState<string | null>(null)
  const [titre, setTitre] = useState('')
  const [dateCible, setDateCible] = useState('')
  const [icone, setIcone] = useState<IconeCompte>(ICONE_DEFAUT)

  const invalider = () => queryClient.invalidateQueries({ queryKey: ['comptes-a-rebours'] })

  const viderFormulaire = () => {
    setEnEditionId(null)
    setTitre('')
    setDateCible('')
    setIcone(ICONE_DEFAUT)
  }

  const enregistrer = useMutation({
    mutationFn: () => {
      const donnees = { titre, dateCible, icone }
      return enEditionId === null
        ? api.creerCompteARebours(donnees).then(() => undefined)
        : api.modifierCompteARebours(enEditionId, donnees)
    },
    onSuccess: () => {
      invalider()
      viderFormulaire()
    },
  })

  const supprimer = useMutation({
    mutationFn: (id: string) => api.supprimerCompteARebours(id),
    onSuccess: (_donnees, id) => {
      invalider()
      if (id === enEditionId) {
        viderFormulaire()
      }
    },
  })

  const modifier = (compte: CompteARebours) => {
    setEnEditionId(compte.id)
    setTitre(compte.titre)
    setDateCible(compte.dateCible)
    setIcone(compte.icone)
  }

  const valide = titre.trim().length > 0 && dateCible.length > 0

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-encre/30 p-4"
      onClick={onFermer}
    >
      <div
        className="flex max-h-[92dvh] w-full max-w-lg flex-col gap-4 overflow-y-auto rounded-[28px] bg-carte p-7 shadow-carte-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 className="text-2xl font-bold">Comptes à rebours</h2>

        <div className="flex flex-col gap-3 rounded-2xl bg-creux/60 p-4">
          <span className="text-sm font-bold text-dore">
            {enEditionId === null ? 'Nouveau compte à rebours' : 'Modifier le compte à rebours'}
          </span>
          <input
            value={titre}
            onChange={(e) => setTitre(e.target.value)}
            placeholder="On attend quoi?"
            aria-label="Titre"
            className="rounded-xl bg-carte px-4 py-2.5 text-base font-bold placeholder:font-normal placeholder:text-sourdine focus:outline-2 focus:outline-orange/60"
          />
          <div className="flex flex-wrap items-center gap-2">
            <input
              type="date"
              value={dateCible}
              onChange={(e) => setDateCible(e.target.value)}
              aria-label="Date cible"
              className="rounded-xl bg-carte px-3 py-2 text-sm text-dore focus:outline-2 focus:outline-orange/60"
            />
            <div className="flex flex-wrap gap-1.5">
              {(Object.keys(ICONES_COMPTE) as IconeCompte[]).map((nom) => {
                const { Icone, libelle } = ICONES_COMPTE[nom]
                return (
                  <button
                    key={nom}
                    type="button"
                    title={libelle}
                    aria-label={`Icône ${libelle}`}
                    aria-pressed={icone === nom}
                    onClick={() => setIcone(nom)}
                    className={cn(
                      'flex size-11 items-center justify-center rounded-xl transition-colors',
                      icone === nom
                        ? 'bg-carte outline-2 outline-orange'
                        : 'bg-carte/50 hover:bg-carte',
                    )}
                  >
                    <Icone />
                  </button>
                )
              })}
            </div>
          </div>
          <div className="flex justify-end gap-2">
            {enEditionId !== null && (
              <button
                type="button"
                onClick={viderFormulaire}
                className="rounded-xl px-4 py-2 text-sm font-bold text-sourdine hover:text-texte"
              >
                Annuler
              </button>
            )}
            <button
              type="button"
              disabled={valide === false || enregistrer.isPending}
              onClick={() => enregistrer.mutate()}
              className="rounded-xl bg-orange px-5 py-2 text-sm font-bold text-carte transition-opacity disabled:opacity-40"
            >
              {enEditionId === null ? 'Ajouter' : 'Enregistrer'}
            </button>
          </div>
        </div>

        {comptes.length > 0 && (
          <ul className="flex flex-col gap-2">
            {comptes.map((compte) => {
              const { Icone } = ICONES_COMPTE[compte.icone]
              const passe = dodosAvant(compte.dateCible) < 0
              return (
                <li
                  key={compte.id}
                  className={cn(
                    'flex items-center gap-3 rounded-2xl bg-creux px-4 py-2.5',
                    passe && 'opacity-60',
                  )}
                >
                  <Icone />
                  <div className="min-w-0 flex-1">
                    <div className="truncate text-[15px] font-bold">{compte.titre}</div>
                    <div className="text-xs text-sourdine">
                      {dateCourte(compte.dateCible)}
                      {passe && ' — passé'}
                    </div>
                  </div>
                  <button
                    type="button"
                    aria-label={`Modifier ${compte.titre}`}
                    onClick={() => modifier(compte)}
                    className="rounded-lg p-1.5 text-sourdine hover:text-dore"
                  >
                    <Pencil className="size-4" />
                  </button>
                  <button
                    type="button"
                    aria-label={`Supprimer ${compte.titre}`}
                    onClick={() => supprimer.mutate(compte.id)}
                    className="rounded-lg p-1.5 text-sourdine hover:text-rouge"
                  >
                    <Trash2 className="size-4" />
                  </button>
                </li>
              )
            })}
          </ul>
        )}

        <button
          type="button"
          onClick={onFermer}
          className="ml-auto rounded-xl px-4 py-2 text-sm font-bold text-sourdine hover:text-texte"
        >
          Fermer
        </button>
      </div>
    </div>
  )
}
