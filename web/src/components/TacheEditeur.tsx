import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
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
    return { mode: 'Intervalle', intervalleJours: f.intervalleJours, rollover: f.rollover, ...fenetre }
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
  const maj = (champ: Partial<Formulaire>) => setF((ancien) => ({ ...ancien, ...champ }))

  const { data: utilisateurs } = useQuery({ queryKey: ['utilisateurs'], queryFn: api.utilisateurs })
  const { data: zones } = useQuery({ queryKey: ['zones'], queryFn: api.zones })
  const { data: equipements } = useQuery({ queryKey: ['equipements'], queryFn: api.equipements })
  const { data: tache } = useQuery({
    queryKey: ['tache', tacheId],
    queryFn: () => api.tache(tacheId!),
    enabled: tacheId !== null,
  })

  useEffect(() => {
    function surTouche(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        onFermer()
      }
    }
    window.addEventListener('keydown', surTouche)
    return () => window.removeEventListener('keydown', surTouche)
  }, [onFermer])

  useEffect(() => {
    if (tache === undefined) {
      return
    }
    const r = tache.recurrence
    setF({
      ...defaut,
      titre: tache.titre,
      description: tache.description ?? '',
      assigneAId: tache.assigneAId ?? '',
      zoneId: tache.zoneId ?? '',
      equipementId: tache.equipementId ?? '',
      strategie: tache.strategie,
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
    })
  }, [tache])

  const invalider = () => {
    queryClient.invalidateQueries({ queryKey: ['occurrences'] })
    queryClient.invalidateQueries({ queryKey: ['tache'] })
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
        <input
          value={f.description}
          onChange={(e) => maj({ description: e.target.value })}
          placeholder="Détails (facultatif)"
          aria-label="Description"
          className={classeChamp}
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
