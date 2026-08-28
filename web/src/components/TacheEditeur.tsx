import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { FileText, X } from 'lucide-react'
import ConfirmerSuppression from '@/components/ConfirmerSuppression'
import { api, type Recurrence, type TacheDonnees } from '@/lib/api'
import { cn } from '@/lib/utils'

const MOIS = [
  'janvier', 'février', 'mars', 'avril', 'mai', 'juin',
  'juillet', 'août', 'septembre', 'octobre', 'novembre', 'décembre',
]
const JOURS_COURTS = ['D', 'L', 'M', 'M', 'J', 'V', 'S']

type Formulaire = {
  titre: string
  description: string
  echeance: string
  assigneAId: string
  zoneId: string
  equipementId: string
  strategie: string
  documentIds: string[]
  mode: 'Ponctuelle' | 'Fixe' | 'Intervalle'
  fixeType: 'JoursSemaine' | 'JourDuMois' | 'Annuelle'
  joursSemaine: number[]
  jourDuMois: number
  moisAnnuel: number
  jourAnnuel: number
  intervalleJours: number
  enSaison: boolean
  fenetreDebutMois: number
  fenetreDebutJour: number
  fenetreFinMois: number
  fenetreFinJour: number
  rollover: boolean
}

const defaut: Formulaire = {
  titre: '',
  description: '',
  echeance: '',
  assigneAId: '',
  zoneId: '',
  equipementId: '',
  strategie: 'Fixe',
  documentIds: [],
  mode: 'Ponctuelle',
  fixeType: 'JoursSemaine',
  joursSemaine: [],
  jourDuMois: 1,
  moisAnnuel: 5,
  jourAnnuel: 1,
  intervalleJours: 7,
  enSaison: false,
  fenetreDebutMois: 5,
  fenetreDebutJour: 1,
  fenetreFinMois: 10,
  fenetreFinJour: 31,
  rollover: true,
}

function versRecurrence(f: Formulaire): Recurrence | undefined {
  if (f.mode === 'Ponctuelle') {
    return undefined
  }
  const fenetre = f.enSaison
    ? {
        fenetreDebutMois: f.fenetreDebutMois,
        fenetreDebutJour: f.fenetreDebutJour,
        fenetreFinMois: f.fenetreFinMois,
        fenetreFinJour: f.fenetreFinJour,
      }
    : {}
  if (f.mode === 'Intervalle') {
    // Rollover réservé au mode fixe : le serveur refuse le flag ailleurs (T9).
    return { mode: 'Intervalle', intervalleJours: f.intervalleJours, ...fenetre }
  }
  return {
    mode: 'Fixe',
    fixeType: f.fixeType,
    joursSemaine: f.fixeType === 'JoursSemaine' ? f.joursSemaine : undefined,
    jourDuMois: f.fixeType === 'JourDuMois' ? f.jourDuMois : undefined,
    moisAnnuel: f.fixeType === 'Annuelle' ? f.moisAnnuel : undefined,
    jourAnnuel: f.fixeType === 'Annuelle' ? f.jourAnnuel : undefined,
    rollover: f.rollover,
    ...fenetre,
  }
}

function Pilule({
  actif,
  onClick,
  children,
}: {
  actif: boolean
  onClick: () => void
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        'rounded-full px-3.5 py-1.5 text-sm font-bold transition-colors',
        actif ? 'bg-orange text-carte' : 'bg-creux text-sourdine hover:text-dore',
      )}
    >
      {children}
    </button>
  )
}

// Pile des éditeurs ouverts (module) : Échap ne ferme que le plus récent, et le
// quick-add (⌘K) n'empile pas un second modal par-dessus une édition en cours.
const pileEditeurs: symbol[] = []
export function editeurDejaOuvert(): boolean {
  return pileEditeurs.length > 0
}

export default function TacheEditeur({
  tacheId,
  zoneInitialeId,
  onFermer,
}: {
  tacheId: string | null
  zoneInitialeId?: string
  onFermer: () => void
}) {
  const queryClient = useQueryClient()
  const [f, setF] = useState<Formulaire>({ ...defaut, zoneId: zoneInitialeId ?? '' })
  const jeton = useRef(Symbol('editeur'))
  const initialiseePour = useRef<string | null>(null)
  const champsSaisis = useRef(new Set<keyof Formulaire>())

  useEffect(() => {
    const present = jeton.current
    pileEditeurs.push(present)
    return () => {
      const i = pileEditeurs.indexOf(present)
      if (i >= 0) {
        pileEditeurs.splice(i, 1)
      }
    }
  }, [])
  const maj = (champ: Partial<Formulaire>) => {
    for (const cle of Object.keys(champ) as (keyof Formulaire)[]) {
      champsSaisis.current.add(cle)
    }
    setF((ancien) => ({ ...ancien, ...champ }))
  }

  const { data: utilisateurs } = useQuery({ queryKey: ['utilisateurs'], queryFn: api.utilisateurs })
  const { data: zones } = useQuery({ queryKey: ['zones'], queryFn: api.zones })
  const { data: equipements } = useQuery({ queryKey: ['equipements'], queryFn: api.equipements })
  const { data: documents } = useQuery({ queryKey: ['documents'], queryFn: () => api.documents() })
  const { data: tache } = useQuery({
    queryKey: ['tache', tacheId],
    queryFn: () => api.tache(tacheId!),
    enabled: tacheId !== null,
  })

  useEffect(() => {
    function surTouche(e: KeyboardEvent) {
      // Seul l'éditeur du dessus ferme : deux écouteurs window fermeraient les
      // deux modaux d'un seul Échap.
      if (e.key === 'Escape' && pileEditeurs[pileEditeurs.length - 1] === jeton.current) {
        onFermer()
      }
    }
    window.addEventListener('keydown', surTouche)
    return () => window.removeEventListener('keydown', surTouche)
  }, [onFermer])

  useEffect(() => {
    // Initialiser le formulaire une seule fois par tâche : un refetch (retour
    // d'onglet, invalidation) ne doit jamais écraser une saisie en cours. Une
    // saisie commencée avant l'arrivée du détail prime champ par champ — le
    // reste de la fiche se charge quand même (un PUT partiel l'effacerait).
    if (tache === undefined || initialiseePour.current === tache.id) {
      return
    }
    initialiseePour.current = tache.id
    const r = tache.recurrence
    setF((saisie) => {
      const chargee: Formulaire = {
        ...defaut,
        titre: tache.titre,
        description: tache.description ?? '',
        echeance: tache.echeance ?? '',
        assigneAId: tache.assigneAId ?? '',
        zoneId: tache.zoneId ?? '',
        equipementId: tache.equipementId ?? '',
        strategie: tache.strategie,
        documentIds: tache.documentIds,
        mode: r.mode,
        fixeType: r.fixeType ?? 'JoursSemaine',
        joursSemaine: r.joursSemaine ?? [],
        jourDuMois: r.jourDuMois ?? 1,
        moisAnnuel: r.moisAnnuel ?? 5,
        jourAnnuel: r.jourAnnuel ?? 1,
        intervalleJours: r.intervalleJours ?? 7,
        enSaison: r.fenetreDebutMois !== null && r.fenetreDebutMois !== undefined,
        fenetreDebutMois: r.fenetreDebutMois ?? 5,
        fenetreDebutJour: r.fenetreDebutJour ?? 1,
        fenetreFinMois: r.fenetreFinMois ?? 10,
        fenetreFinJour: r.fenetreFinJour ?? 31,
        rollover: r.rollover ?? true,
      }
      const conservee = Object.fromEntries(
        [...champsSaisis.current].map((cle) => [cle, saisie[cle]]),
      ) as Partial<Formulaire>
      return { ...chargee, ...conservee }
    })
  }, [tache])

  const invalider = () => {
    // Invalidation croisée taches/occurrences/journal — ['tache'] ne couvre pas
    // la liste ['taches'] ; le budget dérive des tâches liées et des occurrences.
    queryClient.invalidateQueries({ queryKey: ['taches'] })
    queryClient.invalidateQueries({ queryKey: ['tache'] })
    queryClient.invalidateQueries({ queryKey: ['occurrences'] })
    queryClient.invalidateQueries({ queryKey: ['journal'] })
    queryClient.invalidateQueries({ queryKey: ['budget'] })
  }

  const enregistrer = useMutation({
    mutationFn: () => {
      const donnees: TacheDonnees = {
        titre: f.titre,
        description: f.description || undefined,
        echeance: f.echeance || undefined,
        assigneAId: f.assigneAId || undefined,
        zoneId: f.zoneId || undefined,
        equipementId: f.equipementId || undefined,
        strategie: f.strategie,
        recurrence: versRecurrence(f),
        documentIds: f.documentIds,
      }
      return tacheId === null
        ? api.creerTache(donnees).then(() => undefined)
        : api.modifierTache(tacheId, donnees)
    },
    onSuccess: () => {
      invalider()
      onFermer()
    },
  })

  const supprimer = useMutation({
    mutationFn: () => api.supprimerTache(tacheId!),
    onSuccess: () => {
      invalider()
      onFermer()
    },
  })

  const valide =
    f.titre.trim().length > 0 &&
    (f.mode !== 'Fixe' || f.fixeType !== 'JoursSemaine' || f.joursSemaine.length > 0)

  const classeChamp =
    'rounded-xl bg-creux px-3 py-2 text-sm text-texte focus:outline-2 focus:outline-orange/60'
  const classeEtiquette = 'text-sm font-bold text-dore'

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-encre/30 p-4"
      onClick={onFermer}
    >
      <div
        className="flex max-h-[92dvh] w-full max-w-2xl flex-col gap-4 overflow-y-auto rounded-[28px] bg-carte p-7 shadow-carte-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 className="text-2xl font-bold">
          {tacheId === null ? 'Nouvelle tâche' : 'Modifier la tâche'}
        </h2>

        <input
          value={f.titre}
          onChange={(e) => maj({ titre: e.target.value })}
          placeholder="Quoi faire?"
          aria-label="Titre"
          className="rounded-xl bg-creux px-4 py-2.5 text-base font-bold placeholder:font-normal placeholder:text-sourdine focus:outline-2 focus:outline-orange/60"
        />
        <textarea
          value={f.description}
          onChange={(e) => maj({ description: e.target.value })}
          placeholder="Détails (facultatif)"
          aria-label="Description"
          rows={4}
          className={cn(classeChamp, 'resize-y leading-snug')}
        />

        <div className="flex flex-wrap gap-3">
          <label className="flex flex-1 min-w-40 flex-col gap-1">
            <span className={classeEtiquette}>Pièce</span>
            <select
              value={f.zoneId}
              onChange={(e) => maj({ zoneId: e.target.value })}
              className={classeChamp}
            >
              <option value="">Aucune</option>
              {zones?.map((z) => (
                <option key={z.id} value={z.id}>{z.nom}</option>
              ))}
            </select>
          </label>
          <label className="flex flex-1 min-w-40 flex-col gap-1">
            <span className={classeEtiquette}>Équipement</span>
            <select
              value={f.equipementId}
              onChange={(e) => maj({ equipementId: e.target.value })}
              className={classeChamp}
            >
              <option value="">Aucun</option>
              {equipements?.map((eq) => (
                <option key={eq.id} value={eq.id}>{eq.nom}</option>
              ))}
            </select>
          </label>
        </div>

        {/* Documents de référence */}
        <div className="flex flex-col gap-2">
          <span className={classeEtiquette}>Documents de référence</span>
          {f.documentIds.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {f.documentIds.map((documentId) => {
                const document = documents?.find((d) => d.id === documentId)
                const titre = document?.titre ?? 'Document'
                return (
                  <span
                    key={documentId}
                    className="flex max-w-full items-center gap-1.5 rounded-full bg-creux py-1 pl-3 pr-1.5 text-xs font-bold"
                  >
                    <FileText className="size-3.5 shrink-0 text-dore" />
                    <a
                      href={`/api/documents/${documentId}/fichier`}
                      target="_blank"
                      rel="noreferrer"
                      className="min-w-0 truncate text-texte hover:text-orange"
                    >
                      {titre}
                    </a>
                    <button
                      type="button"
                      aria-label={`Délier ${titre}`}
                      onClick={() =>
                        maj({ documentIds: f.documentIds.filter((id) => id !== documentId) })
                      }
                      className="grid size-4 shrink-0 place-items-center rounded-full text-sourdine hover:bg-carte hover:text-rouge"
                    >
                      <X className="size-3.5" />
                    </button>
                  </span>
                )
              })}
            </div>
          )}
          <select
            value=""
            onChange={(e) => {
              if (e.target.value.length > 0) {
                maj({ documentIds: [...f.documentIds, e.target.value] })
              }
            }}
            aria-label="Lier un document"
            className={cn(classeChamp, 'max-w-72 text-sourdine')}
          >
            <option value="">Lier un document…</option>
            {documents
              ?.filter((d) => f.documentIds.includes(d.id) === false)
              .map((d) => (
                <option key={d.id} value={d.id}>
                  {[d.titre, d.dossier].filter(Boolean).join(' · ')}
                </option>
              ))}
          </select>
        </div>

        {/* Répétition */}
        <div className="flex flex-col gap-2">
          <span className={classeEtiquette}>Répétition</span>
          <div className="flex flex-wrap gap-2">
            <Pilule actif={f.mode === 'Ponctuelle'} onClick={() => maj({ mode: 'Ponctuelle' })}>
              Une fois
            </Pilule>
            <Pilule actif={f.mode === 'Fixe'} onClick={() => maj({ mode: 'Fixe' })}>
              Horaire fixe
            </Pilule>
            <Pilule actif={f.mode === 'Intervalle'} onClick={() => maj({ mode: 'Intervalle' })}>
              Après la dernière fois
            </Pilule>
          </div>

          {f.mode === 'Fixe' && (
            <div className="flex flex-col gap-3 rounded-2xl bg-creux/60 p-4">
              <div className="flex flex-wrap gap-2">
                <Pilule
                  actif={f.fixeType === 'JoursSemaine'}
                  onClick={() => maj({ fixeType: 'JoursSemaine' })}
                >
                  Chaque semaine
                </Pilule>
                <Pilule
                  actif={f.fixeType === 'JourDuMois'}
                  onClick={() => maj({ fixeType: 'JourDuMois' })}
                >
                  Chaque mois
                </Pilule>
                <Pilule actif={f.fixeType === 'Annuelle'} onClick={() => maj({ fixeType: 'Annuelle' })}>
                  Chaque année
                </Pilule>
              </div>

              {f.fixeType === 'JoursSemaine' && (
                <div className="flex gap-1.5">
                  {JOURS_COURTS.map((jour, i) => (
                    <button
                      key={i}
                      type="button"
                      aria-label={`Jour ${i}`}
                      onClick={() =>
                        maj({
                          joursSemaine: f.joursSemaine.includes(i)
                            ? f.joursSemaine.filter((j) => j !== i)
                            : [...f.joursSemaine, i],
                        })
                      }
                      className={cn(
                        'size-9 rounded-full text-sm font-bold transition-colors',
                        f.joursSemaine.includes(i)
                          ? 'bg-orange text-carte'
                          : 'bg-carte text-sourdine hover:text-dore',
                      )}
                    >
                      {jour}
                    </button>
                  ))}
                </div>
              )}
              {f.fixeType === 'JourDuMois' && (
                <label className="flex items-center gap-2 text-sm">
                  le
                  <input
                    type="number"
                    min={1}
                    max={31}
                    value={f.jourDuMois}
                    onChange={(e) => maj({ jourDuMois: Number(e.target.value) })}
                    className={cn(classeChamp, 'w-20 bg-carte')}
                  />
                  du mois <span className="text-sourdine">(31 = fin du mois)</span>
                </label>
              )}
              {f.fixeType === 'Annuelle' && (
                <label className="flex items-center gap-2 text-sm">
                  le
                  <input
                    type="number"
                    min={1}
                    max={31}
                    value={f.jourAnnuel}
                    onChange={(e) => maj({ jourAnnuel: Number(e.target.value) })}
                    className={cn(classeChamp, 'w-20 bg-carte')}
                  />
                  <select
                    value={f.moisAnnuel}
                    onChange={(e) => maj({ moisAnnuel: Number(e.target.value) })}
                    className={cn(classeChamp, 'bg-carte')}
                  >
                    {MOIS.map((mois, i) => (
                      <option key={mois} value={i + 1}>{mois}</option>
                    ))}
                  </select>
                </label>
              )}
            </div>
          )}

          {f.mode === 'Intervalle' && (
            <label className="flex items-center gap-2 rounded-2xl bg-creux/60 p-4 text-sm">
              tous les
              <input
                type="number"
                min={1}
                value={f.intervalleJours}
                onChange={(e) => maj({ intervalleJours: Number(e.target.value) })}
                className={cn(classeChamp, 'w-20 bg-carte')}
              />
              jours après la dernière fois
            </label>
          )}

          {f.mode !== 'Ponctuelle' && (
            <>
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={f.enSaison}
                  onChange={(e) => maj({ enSaison: e.target.checked })}
                  className="size-4 accent-orange"
                />
                Seulement en saison
              </label>
              {f.enSaison && (
                <div className="flex flex-wrap items-center gap-2 rounded-2xl bg-creux/60 p-4 text-sm">
                  du
                  <input
                    type="number" min={1} max={31} value={f.fenetreDebutJour}
                    onChange={(e) => maj({ fenetreDebutJour: Number(e.target.value) })}
                    className={cn(classeChamp, 'w-16 bg-carte')} aria-label="Jour début"
                  />
                  <select
                    value={f.fenetreDebutMois}
                    onChange={(e) => maj({ fenetreDebutMois: Number(e.target.value) })}
                    className={cn(classeChamp, 'bg-carte')} aria-label="Mois début"
                  >
                    {MOIS.map((mois, i) => <option key={mois} value={i + 1}>{mois}</option>)}
                  </select>
                  au
                  <input
                    type="number" min={1} max={31} value={f.fenetreFinJour}
                    onChange={(e) => maj({ fenetreFinJour: Number(e.target.value) })}
                    className={cn(classeChamp, 'w-16 bg-carte')} aria-label="Jour fin"
                  />
                  <select
                    value={f.fenetreFinMois}
                    onChange={(e) => maj({ fenetreFinMois: Number(e.target.value) })}
                    className={cn(classeChamp, 'bg-carte')} aria-label="Mois fin"
                  >
                    {MOIS.map((mois, i) => <option key={mois} value={i + 1}>{mois}</option>)}
                  </select>
                </div>
              )}
              {f.mode === 'Fixe' && (
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={f.rollover}
                    onChange={(e) => maj({ rollover: e.target.checked })}
                    className="size-4 accent-orange"
                  />
                  Si on la manque, glisser au prochain passage (sans s’empiler)
                </label>
              )}
            </>
          )}
        </div>

        {/* Qui s'en occupe */}
        <div className="flex flex-col gap-2">
          <span className={classeEtiquette}>Qui s’en occupe?</span>
          {f.mode !== 'Ponctuelle' && (
            <div className="flex flex-wrap gap-2">
              <Pilule actif={f.strategie === 'Fixe'} onClick={() => maj({ strategie: 'Fixe' })}>
                Toujours la même personne
              </Pilule>
              <Pilule
                actif={f.strategie === 'Alternance'}
                onClick={() => maj({ strategie: 'Alternance' })}
              >
                En alternance
              </Pilule>
              <Pilule
                actif={f.strategie === 'MoinsLAFait'}
                onClick={() => maj({ strategie: 'MoinsLAFait' })}
              >
                Qui l’a le moins fait
              </Pilule>
            </div>
          )}
          <select
            value={f.assigneAId}
            onChange={(e) => maj({ assigneAId: e.target.value })}
            className={cn(classeChamp, 'max-w-60')}
            aria-label="Assigner à"
          >
            <option value="">Personne pour l’instant</option>
            {utilisateurs?.map((u) => (
              <option key={u.id} value={u.id}>{u.nomAffichage}</option>
            ))}
          </select>
        </div>

        <label className="flex flex-col gap-1">
          <span className={classeEtiquette}>
            {f.mode === 'Ponctuelle' ? 'Échéance' : 'Première échéance (sinon calculée)'}
          </span>
          <input
            type="date"
            value={f.echeance}
            onChange={(e) => maj({ echeance: e.target.value })}
            className={cn(classeChamp, 'max-w-60')}
          />
        </label>

        <div className="mt-2 flex items-center gap-2">
          {tacheId !== null && (
            <ConfirmerSuppression
              ariaLabel="Supprimer la tâche"
              onConfirmer={() => supprimer.mutate()}
              className="rounded-xl px-4 py-2 text-sm font-bold text-rouge hover:bg-rouge/10 hover:text-rouge"
            >
              Supprimer
            </ConfirmerSuppression>
          )}
          <div className="ml-auto flex gap-2">
            <button
              type="button"
              onClick={onFermer}
              className="rounded-xl px-4 py-2 text-sm font-bold text-sourdine hover:text-texte"
            >
              Annuler
            </button>
            <button
              type="button"
              disabled={valide === false || enregistrer.isPending}
              onClick={() => enregistrer.mutate()}
              className="rounded-xl bg-orange px-5 py-2 text-sm font-bold text-carte transition-opacity disabled:opacity-40"
            >
              Enregistrer
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
