import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CalendarDays, GraduationCap, Pencil, Recycle, Trash2, type LucideIcon } from 'lucide-react'
import { api, ApiError, type FluxExterne, type TypeFluxExterne } from '@/lib/api'
import { cn } from '@/lib/utils'

export const ICONES_FLUX: Record<TypeFluxExterne, { Icone: LucideIcon; libelle: string }> = {
  Collecte: { Icone: Recycle, libelle: 'Collecte' },
  Ecole: { Icone: GraduationCap, libelle: 'École' },
  Autre: { Icone: CalendarDays, libelle: 'Autre' },
}

export default function FluxExternesGestion({ onFermer }: { onFermer: () => void }) {
  const queryClient = useQueryClient()
  const { data: flux } = useQuery({ queryKey: ['flux-externes'], queryFn: api.fluxExternes })
  const [enEditionId, setEnEditionId] = useState<string | null>(null)
  const [nom, setNom] = useState('')
  const [url, setUrl] = useState('')
  const [type, setType] = useState<TypeFluxExterne>('Collecte')
  const [erreur, setErreur] = useState<string | null>(null)
  const [confirmationId, setConfirmationId] = useState<string | null>(null)

  const invalider = () => {
    queryClient.invalidateQueries({ queryKey: ['flux-externes'] })
    queryClient.invalidateQueries({ queryKey: ['evenements-externes'] })
  }

  const viderFormulaire = () => {
    setEnEditionId(null)
    setNom('')
    setUrl('')
    setType('Collecte')
    setErreur(null)
  }

  const enregistrer = useMutation({
    mutationFn: () => {
      const donnees = { nom, url, type }
      return enEditionId === null
        ? api.creerFluxExterne(donnees).then(() => undefined)
        : api.modifierFluxExterne(enEditionId, donnees)
    },
    onSuccess: () => {
      invalider()
      viderFormulaire()
    },
    onError: (e) => setErreur(e instanceof ApiError ? e.message : 'Erreur inattendue.'),
    // Affichée dans le modal — le MutationCache global ne doit pas la doubler
    // dans la bannière.
    meta: { erreurLocale: true },
  })

  const supprimer = useMutation({
    mutationFn: (id: string) => api.supprimerFluxExterne(id),
    onSuccess: (_donnees, id) => {
      invalider()
      setConfirmationId(null)
      if (id === enEditionId) {
        viderFormulaire()
      }
    },
  })

  const modifier = (f: FluxExterne) => {
    setEnEditionId(f.id)
    setNom(f.nom)
    setUrl(f.url)
    setType(f.type)
    setErreur(null)
  }

  const valide = nom.trim().length > 0 && url.trim().startsWith('http')

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-encre/30 p-4"
      onClick={onFermer}
    >
      <div
        className="flex max-h-[92dvh] w-full max-w-lg flex-col gap-4 overflow-y-auto rounded-[28px] bg-carte p-7 shadow-carte-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 className="text-2xl font-bold">Calendriers externes</h2>
        <p className="-mt-2 text-sm text-sourdine">
          Collectes, école… tout calendrier qui offre un lien iCal (.ics).
        </p>

        <div className="flex flex-col gap-3 rounded-2xl bg-creux/60 p-4">
          <span className="text-sm font-bold text-dore">
            {enEditionId === null ? 'Nouveau calendrier' : 'Modifier le calendrier'}
          </span>
          <input
            value={nom}
            onChange={(e) => setNom(e.target.value)}
            placeholder="Nom (ex. Collectes de la ville)"
            aria-label="Nom"
            className="rounded-xl bg-carte px-4 py-2.5 text-base font-bold placeholder:font-normal placeholder:text-sourdine focus:outline-2 focus:outline-orange/60"
          />
          <input
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            placeholder="https://…/calendrier.ics"
            aria-label="URL du flux iCal"
            className="rounded-xl bg-carte px-4 py-2.5 text-sm placeholder:text-sourdine focus:outline-2 focus:outline-orange/60"
          />
          <div className="flex gap-1.5">
            {(Object.keys(ICONES_FLUX) as TypeFluxExterne[]).map((t) => {
              const { Icone, libelle } = ICONES_FLUX[t]
              return (
                <button
                  key={t}
                  type="button"
                  aria-pressed={type === t}
                  onClick={() => setType(t)}
                  className={cn(
                    'flex items-center gap-1.5 rounded-xl px-3 py-2 text-sm font-bold transition-colors',
                    type === t ? 'bg-carte outline-2 outline-orange' : 'bg-carte/50 hover:bg-carte',
                  )}
                >
                  <Icone className="size-4" />
                  {libelle}
                </button>
              )
            })}
          </div>
          {erreur !== null && <p className="text-sm font-bold text-rouge">{erreur}</p>}
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
              {enregistrer.isPending
                ? 'Vérification…'
                : enEditionId === null
                  ? 'Ajouter'
                  : 'Enregistrer'}
            </button>
          </div>
        </div>

        {(flux ?? []).length > 0 && (
          <ul className="flex flex-col gap-2">
            {(flux ?? []).map((f) => {
              const { Icone } = ICONES_FLUX[f.type]
              return (
                <li key={f.id} className="flex items-center gap-3 rounded-2xl bg-creux px-4 py-2.5">
                  <Icone className="size-5 shrink-0 text-dore" />
                  <div className="min-w-0 flex-1">
                    <div className="truncate text-[15px] font-bold">{f.nom}</div>
                    <div className="truncate text-xs text-sourdine">
                      {f.derniereErreur !== null ? (
                        <span className="font-bold text-rouge">{f.derniereErreur}</span>
                      ) : (
                        `${f.nbEvenements} événement${f.nbEvenements === 1 ? '' : 's'} à venir`
                      )}
                    </div>
                  </div>
                  <button
                    type="button"
                    aria-label={`Modifier ${f.nom}`}
                    onClick={() => modifier(f)}
                    className="rounded-lg p-1.5 text-sourdine hover:text-dore"
                  >
                    <Pencil className="size-4" />
                  </button>
                  {confirmationId === f.id ? (
                    <button
                      type="button"
                      onClick={() => supprimer.mutate(f.id)}
                      className="rounded-lg px-2 py-1 text-xs font-bold text-rouge"
                    >
                      Confirmer ?
                    </button>
                  ) : (
                    <button
                      type="button"
                      aria-label={`Supprimer ${f.nom}`}
                      onClick={() => setConfirmationId(f.id)}
                      className="rounded-lg p-1.5 text-sourdine hover:text-rouge"
                    >
                      <Trash2 className="size-4" />
                    </button>
                  )}
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
