import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, FileText, Plus, X } from 'lucide-react'
import ConfirmerSuppression from '@/components/ConfirmerSuppression'
import VignetteDocument, { libelleTypeFichier } from '@/components/VignetteDocument'
import { api, type EquipementDonnees } from '@/lib/api'
import { dateLisible, dollars, heureQuebec } from '@/lib/format'
import { dateLocaleIso } from '@/lib/api'
import { cn } from '@/lib/utils'

type Fiche = {
  nom: string
  zoneId: string
  marque: string
  modele: string
  numeroSerie: string
  dateAchat: string
  finGarantie: string
  notes: string
  specs: [string, string][]
}

const ficheVide: Fiche = {
  nom: '', zoneId: '', marque: '', modele: '', numeroSerie: '',
  dateAchat: '', finGarantie: '', notes: '', specs: [],
}

function versDonnees(f: Fiche): EquipementDonnees {
  return {
    nom: f.nom,
    zoneId: f.zoneId || null,
    marque: f.marque || null,
    modele: f.modele || null,
    numeroSerie: f.numeroSerie || null,
    dateAchat: f.dateAchat || null,
    finGarantie: f.finGarantie || null,
    notes: f.notes || null,
    specs: Object.fromEntries(f.specs.filter(([cle]) => cle.trim().length > 0)),
  }
}

export default function Equipements() {
  const queryClient = useQueryClient()
  const [choisiId, setChoisiId] = useState<string | null>(null)
  const [creation, setCreation] = useState(false)
  const [fiche, setFiche] = useState<Fiche>(ficheVide)
  const champFichier = useRef<HTMLInputElement>(null)
  const maj = (champ: Partial<Fiche>) => setFiche((ancienne) => ({ ...ancienne, ...champ }))

  const { data: zones } = useQuery({ queryKey: ['zones'], queryFn: api.zones })
  const { data: equipements } = useQuery({ queryKey: ['equipements'], queryFn: api.equipements })
  const { data: budget } = useQuery({ queryKey: ['budget'], queryFn: api.budget })
  const enveloppeLiee = budget?.enveloppes.find(
    (e) => e.equipementId === choisiId && e.statut === 'Active',
  )
  const { data: detail } = useQuery({
    queryKey: ['equipement', choisiId],
    queryFn: () => api.equipement(choisiId!),
    enabled: choisiId !== null,
  })

  useEffect(() => {
    if (creation) {
      setFiche(ficheVide)
      return
    }
    if (detail === undefined) {
      return
    }
    setFiche({
      nom: detail.nom,
      zoneId: detail.zoneId ?? '',
      marque: detail.marque ?? '',
      modele: detail.modele ?? '',
      numeroSerie: detail.numeroSerie ?? '',
      dateAchat: detail.dateAchat ?? '',
      finGarantie: detail.finGarantie ?? '',
      notes: detail.notes ?? '',
      specs: Object.entries(detail.specs),
    })
  }, [detail, creation])

  const invalider = () => {
    queryClient.invalidateQueries({ queryKey: ['equipements'] })
    queryClient.invalidateQueries({ queryKey: ['equipement'] })
  }

  const enregistrer = useMutation({
    mutationFn: async () => {
      if (creation) {
        const { id } = await api.creerEquipement(versDonnees(fiche))
        return id
      }
      await api.modifierEquipement(choisiId!, versDonnees(fiche))
      return choisiId!
    },
    onSuccess: (id) => {
      setCreation(false)
      setChoisiId(id)
      invalider()
    },
  })

  const supprimer = useMutation({
    mutationFn: () => api.supprimerEquipement(choisiId!),
    onSuccess: () => {
      setChoisiId(null)
      invalider()
    },
  })

  const televerser = useMutation({
    mutationFn: (fichier: File) => api.televerserDocument(fichier, { equipementId: choisiId! }),
    onSuccess: invalider,
  })

  const supprimerDocument = useMutation({
    mutationFn: (id: string) => api.supprimerDocument(id),
    onSuccess: invalider,
  })

  const parZone = new Map<string, typeof equipements & {}>()
  for (const equipement of equipements ?? []) {
    const cle = equipement.zoneId ?? ''
    parZone.set(cle, [...(parZone.get(cle) ?? []), equipement])
  }
  const nomZone = (zoneId: string) =>
    zones?.find((z) => z.id === zoneId)?.nom ?? 'Sans pièce'

  const classeChamp =
    'rounded-xl bg-creux px-3 py-2 text-sm text-texte focus:outline-2 focus:outline-orange/60'
  const enEdition = creation || choisiId !== null

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-end justify-between">
        <h1 className="text-4xl font-bold">Équipements</h1>
        <button
          type="button"
          onClick={() => {
            setCreation(true)
            setChoisiId(null)
          }}
          className="flex items-center gap-2 rounded-full bg-orange px-4 py-2 text-sm font-bold text-carte"
        >
          <Plus className="size-4" /> Nouvel équipement
        </button>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1fr_1.5fr]">
        {/* Liste par pièce */}
        <div className="flex flex-col gap-4">
          {(equipements ?? []).length === 0 && creation === false && (
            <p className="rounded-[20px] border-2 border-dashed border-tiret px-5 py-8 text-center text-sm font-bold text-tiret-texte">
              Aucun équipement encore — inventorie la maison en t’installant.
            </p>
          )}
          {[...parZone.entries()]
            .sort(([a], [b]) => nomZone(a).localeCompare(nomZone(b)))
            .map(([zoneId, liste]) => (
              <div key={zoneId || 'sans'}>
                <div className="mb-2 text-[13px] font-bold uppercase tracking-[0.05em] text-sourdine">
                  {nomZone(zoneId)}
                </div>
                <div className="flex flex-col gap-2">
                  {liste!.map((equipement) => (
                    <button
                      key={equipement.id}
                      type="button"
                      onClick={() => {
                        setChoisiId(equipement.id)
                        setCreation(false)
                      }}
                      className={cn(
                        'flex items-center gap-3 rounded-[18px] bg-carte px-4 py-3 text-left shadow-carte transition-shadow hover:shadow-carte-lg',
                        choisiId === equipement.id && 'outline-2 outline-orange/50',
                      )}
                    >
                      <div className="min-w-0 flex-1">
                        <div className="truncate text-[15px] font-bold">{equipement.nom}</div>
                        {(equipement.marque || equipement.modele) && (
                          <div className="truncate text-xs text-sourdine">
                            {[equipement.marque, equipement.modele].filter(Boolean).join(' · ')}
                          </div>
                        )}
                      </div>
                      {equipement.nbDocuments > 0 && (
                        <span className="flex items-center gap-1 text-xs text-dore">
                          <FileText className="size-3.5" /> {equipement.nbDocuments}
                        </span>
                      )}
                    </button>
                  ))}
                </div>
              </div>
            ))}
        </div>

        {/* Fiche */}
        <div className="flex min-h-[420px] flex-col gap-4 rounded-3xl bg-carte px-7 py-6 shadow-carte-lg">
          {enEdition === false ? (
            <p className="m-auto max-w-64 text-center text-sm text-sourdine">
              Choisis un équipement, ou ajoutes-en un nouveau — avec son manuel en PDF,
              tu le retrouveras toujours ici.
            </p>
          ) : (
            <>
              <div className="flex items-center gap-2">
                <h2 className="text-2xl font-bold">
                  {creation ? 'Nouvel équipement' : fiche.nom || '…'}
                </h2>
                {creation === false && (
                  <span className="ml-auto flex">
                    <ConfirmerSuppression
                      key={choisiId ?? 'aucun'}
                      ariaLabel="Supprimer l'équipement"
                      onConfirmer={() => supprimer.mutate()}
                    />
                  </span>
                )}
              </div>

              <div className="grid gap-3 sm:grid-cols-2">
                <input
                  value={fiche.nom}
                  onChange={(e) => maj({ nom: e.target.value })}
                  placeholder="Nom (ex. Fournaise)"
                  aria-label="Nom"
                  className={cn(classeChamp, 'font-bold sm:col-span-2')}
                />
                <select
                  value={fiche.zoneId}
                  onChange={(e) => maj({ zoneId: e.target.value })}
                  aria-label="Pièce"
                  className={classeChamp}
                >
                  <option value="">Sans pièce</option>
                  {zones?.map((z) => (
                    <option key={z.id} value={z.id}>{z.nom}</option>
                  ))}
                </select>
                <input value={fiche.marque} onChange={(e) => maj({ marque: e.target.value })}
                  placeholder="Marque" aria-label="Marque" className={classeChamp} />
                <input value={fiche.modele} onChange={(e) => maj({ modele: e.target.value })}
                  placeholder="Modèle" aria-label="Modèle" className={classeChamp} />
                <input value={fiche.numeroSerie} onChange={(e) => maj({ numeroSerie: e.target.value })}
                  placeholder="Numéro de série" aria-label="Numéro de série" className={classeChamp} />
                <label className="flex items-center gap-2 text-xs text-sourdine">
                  Acheté le
                  <input type="date" value={fiche.dateAchat}
                    onChange={(e) => maj({ dateAchat: e.target.value })} className={cn(classeChamp, 'flex-1')} />
                </label>
                <label className="flex items-center gap-2 text-xs text-sourdine">
                  Garantie jusqu’au
                  <input type="date" value={fiche.finGarantie}
                    onChange={(e) => maj({ finGarantie: e.target.value })} className={cn(classeChamp, 'flex-1')} />
                </label>
                <input value={fiche.notes} onChange={(e) => maj({ notes: e.target.value })}
                  placeholder="Notes" aria-label="Notes" className={cn(classeChamp, 'sm:col-span-2')} />
              </div>

              {/* Specs libres */}
              <div className="flex flex-col gap-2">
                <span className="text-sm font-bold text-dore">
                  Specs — taille de filtre, code de peinture, n’importe quoi
                </span>
                {fiche.specs.map(([cle, valeur], i) => (
                  <div key={i} className="flex gap-2">
                    <input
                      value={cle}
                      onChange={(e) =>
                        maj({ specs: fiche.specs.map((s, j) => (j === i ? [e.target.value, s[1]] : s)) })
                      }
                      placeholder="Nom (ex. filtre)"
                      aria-label="Nom de la spec"
                      className={cn(classeChamp, 'w-40')}
                    />
                    <input
                      value={valeur}
                      onChange={(e) =>
                        maj({ specs: fiche.specs.map((s, j) => (j === i ? [s[0], e.target.value] : s)) })
                      }
                      placeholder="Valeur (ex. 16 × 25 × 1)"
                      aria-label="Valeur de la spec"
                      className={cn(classeChamp, 'flex-1')}
                    />
                    <button
                      type="button"
                      aria-label="Retirer la spec"
                      onClick={() => maj({ specs: fiche.specs.filter((_, j) => j !== i) })}
                      className="text-sourdine hover:text-rouge"
                    >
                      <X className="size-4" />
                    </button>
                  </div>
                ))}
                <button
                  type="button"
                  onClick={() => maj({ specs: [...fiche.specs, ['', '']] })}
                  className="flex w-fit items-center gap-1.5 text-sm font-bold text-tiret-texte hover:text-dore"
                >
                  <Plus className="size-3.5" /> Ajouter une spec
                </button>
              </div>

              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setCreation(false)
                    if (creation) setChoisiId(null)
                  }}
                  className="rounded-xl px-4 py-2 text-sm font-bold text-sourdine hover:text-texte"
                >
                  Annuler
                </button>
                <button
                  type="button"
                  disabled={fiche.nom.trim().length === 0 || enregistrer.isPending}
                  onClick={() => enregistrer.mutate()}
                  className="rounded-xl bg-orange px-5 py-2 text-sm font-bold text-carte disabled:opacity-40"
                >
                  Enregistrer
                </button>
              </div>

              {/* Documents liés */}
              {creation === false && detail !== undefined && (
                <>
                  <div className="border-t border-barre-piste pt-4">
                    <div className="mb-2 flex items-center justify-between">
                      <span className="text-sm font-bold text-dore">Documents</span>
                      <input
                        ref={champFichier}
                        type="file"
                        accept="application/pdf,image/*"
                        className="hidden"
                        onChange={(e) => {
                          const fichier = e.target.files?.[0]
                          if (fichier !== undefined) {
                            televerser.mutate(fichier)
                            e.target.value = ''
                          }
                        }}
                      />
                      <button
                        type="button"
                        disabled={televerser.isPending}
                        onClick={() => champFichier.current?.click()}
                        className="flex items-center gap-1.5 rounded-full bg-creux px-3 py-1.5 text-xs font-bold text-dore hover:text-orange disabled:opacity-40"
                      >
                        <Plus className="size-3.5" />
                        {televerser.isPending ? 'Téléversement…' : 'Ajouter un fichier'}
                      </button>
                    </div>
                    {detail.documents.length === 0 ? (
                      <p className="text-xs text-sourdine">Aucun document encore (PDF ou image, max 50 Mo).</p>
                    ) : (
                      <div className="flex flex-col gap-2">
                        {detail.documents.map((document) => (
                          <div
                            key={document.id}
                            className="flex items-center gap-2.5 rounded-xl bg-creux px-3 py-2 text-sm"
                          >
                            <VignetteDocument document={document} classe="size-9 bg-carte" />
                            <span className="min-w-0 flex-1 truncate font-bold">{document.titre}</span>
                            <span className="text-xs text-sourdine">
                              {libelleTypeFichier(document)} ·{' '}
                              {(document.taille / 1024 / 1024).toFixed(1).replace('.', ',')} Mo
                            </span>
                            <a
                              href={`/api/documents/${document.id}/fichier`}
                              aria-label={`Télécharger ${document.nomFichier}`}
                              className="text-sourdine hover:text-orange"
                            >
                              <Download className="size-4" />
                            </a>
                            <ConfirmerSuppression
                              ariaLabel={`Supprimer ${document.titre}`}
                              onConfirmer={() => supprimerDocument.mutate(document.id)}
                            />
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

                  {/* Enveloppe budgétaire liée (fonds de prévoyance) */}
                  {enveloppeLiee !== undefined && (
                    <div className="border-t border-barre-piste pt-4">
                      <div className="mb-2 flex items-baseline justify-between">
                        <span className="text-sm font-bold text-dore">Fonds de prévoyance</span>
                        {enveloppeLiee.provision > 0 && (
                          <span className="text-xs font-extrabold text-dore tabular-nums">
                            +&nbsp;{dollars(enveloppeLiee.provision)}/mois
                          </span>
                        )}
                      </div>
                      <div className="flex items-center gap-3 rounded-xl bg-creux px-3 py-2.5 text-sm">
                        <span className="min-w-0 flex-1 truncate font-bold">{enveloppeLiee.nom}</span>
                        {enveloppeLiee.montantCible !== null && (
                          <span className="h-[7px] w-32 overflow-hidden rounded-full bg-barre-piste">
                            <i
                              className="block h-full rounded-full bg-vert"
                              style={{
                                width: `${Math.min(100, Math.max(0, (enveloppeLiee.solde / enveloppeLiee.montantCible) * 100))}%`,
                              }}
                            />
                          </span>
                        )}
                        <span className="text-xs text-sourdine tabular-nums">
                          <b className={cn('text-[13px]', enveloppeLiee.solde < 0 ? 'text-rouge' : 'text-encre')}>
                            {dollars(enveloppeLiee.solde)}
                          </b>
                          {enveloppeLiee.montantCible !== null && <> / {dollars(enveloppeLiee.montantCible)}</>}
                        </span>
                      </div>
                    </div>
                  )}

                  {/* Historique d'entretien */}
                  {detail.entretiens.length > 0 && (
                    <div className="border-t border-barre-piste pt-4">
                      <div className="mb-2 text-sm font-bold text-dore">Entretien</div>
                      <div className="flex flex-col gap-2 text-sm">
                        {detail.entretiens.map((entretien, i) => (
                          <div key={i} className="flex items-start gap-2.5">
                            <span className="mt-1.5 size-[7px] shrink-0 rounded-full bg-vert" />
                            <div className="min-w-0 flex-1">
                              <span className="block truncate">
                                {entretien.titreTache ?? 'Tâche retirée'}
                              </span>
                              {entretien.notes && (
                                <div className="mt-px whitespace-pre-line text-[13px] text-sourdine">
                                  {entretien.notes}
                                </div>
                              )}
                            </div>
                            <span className="text-xs text-vert">
                              {entretien.utilisateur} ✓{' '}
                              {dateLocaleIso(new Date(entretien.completeeLe)) === dateLocaleIso()
                                ? heureQuebec(entretien.completeeLe)
                                : dateLisible(dateLocaleIso(new Date(entretien.completeeLe)))}
                            </span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  )
}
