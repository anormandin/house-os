import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, Download, FileText, Plus, Search, Trash2, X } from 'lucide-react'
import ConfirmerSuppression from '@/components/ConfirmerSuppression'
import ErreurChargement from '@/components/ErreurChargement'
import VignetteDocument, { libelleTypeFichier } from '@/components/VignetteDocument'
import { api, type EquipementDetail, type EquipementDonnees } from '@/lib/api'
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

function versFiche(detail: EquipementDetail): Fiche {
  return {
    nom: detail.nom,
    zoneId: detail.zoneId ?? '',
    marque: detail.marque ?? '',
    modele: detail.modele ?? '',
    numeroSerie: detail.numeroSerie ?? '',
    dateAchat: detail.dateAchat ?? '',
    finGarantie: detail.finGarantie ?? '',
    notes: detail.notes ?? '',
    specs: Object.entries(detail.specs),
  }
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

/** Les équipements au téléphone : liste ↔ fiche dans le même écran. Le bureau
 * garde la liste à gauche et la fiche à droite ; ici l'état `equipementChoisi`
 * (ou la création) bascule d'une vue à l'autre, sans nouvelle route. */
export default function EquipementsTelephone() {
  const queryClient = useQueryClient()
  const [equipementChoisi, setEquipementChoisi] = useState<string | null>(null)
  const [creation, setCreation] = useState(false)
  const [recherche, setRecherche] = useState('')
  const [fiche, setFiche] = useState<Fiche>(ficheVide)
  // Id de l'équipement que la fiche reflète — null pour la fiche vide (création).
  const [ficheDe, setFicheDe] = useState<string | null>(null)
  const maj = (champ: Partial<Fiche>) => setFiche((ancienne) => ({ ...ancienne, ...champ }))

  const { data: zones } = useQuery({ queryKey: ['zones'], queryFn: api.zones })
  const {
    data: equipements,
    isError: equipementsEnErreur,
    refetch: rechargerEquipements,
  } = useQuery({ queryKey: ['equipements'], queryFn: api.equipements })
  const { data: detail } = useQuery({
    queryKey: ['equipement', equipementChoisi],
    queryFn: () => api.equipement(equipementChoisi!),
    enabled: equipementChoisi !== null,
  })

  // Ne recharge la fiche que quand l'équipement affiché change : un refetch du
  // même détail ne doit jamais écraser des édits en cours.
  useEffect(() => {
    if (creation) {
      if (ficheDe !== null) {
        setFiche(ficheVide)
        setFicheDe(null)
      }
      return
    }
    if (detail === undefined || detail.id === ficheDe) {
      return
    }
    setFiche(versFiche(detail))
    setFicheDe(detail.id)
  }, [detail, creation, ficheDe])

  const invalider = () => {
    queryClient.invalidateQueries({ queryKey: ['equipements'] })
    queryClient.invalidateQueries({ queryKey: ['equipement'] })
    queryClient.invalidateQueries({ queryKey: ['documents'] })
  }

  const enregistrer = useMutation({
    mutationFn: async () => {
      if (creation) {
        const { id } = await api.creerEquipement(versDonnees(fiche))
        return id
      }
      await api.modifierEquipement(equipementChoisi!, versDonnees(fiche))
      return equipementChoisi!
    },
    onSuccess: (id) => {
      setCreation(false)
      setEquipementChoisi(id)
      invalider()
    },
  })

  const supprimer = useMutation({
    mutationFn: () => api.supprimerEquipement(equipementChoisi!),
    onSuccess: () => {
      setEquipementChoisi(null)
      invalider()
    },
  })

  const nomZone = (zoneId: string) => zones?.find((z) => z.id === zoneId)?.nom ?? 'Sans pièce'
  const classeChamp =
    'min-h-[48px] rounded-xl bg-creux px-3 text-[16px] text-texte focus:outline-2 focus:outline-orange/60'
  const classeEtiquette = 'flex flex-col gap-1 text-[13px] font-bold text-dore'

  const retourListe = () => {
    setCreation(false)
    setEquipementChoisi(null)
  }

  if (creation || equipementChoisi !== null) {
    return (
      <div className="flex flex-col gap-3 pt-1">
        <div className="flex items-center gap-1">
          <button
            type="button"
            aria-label="Retour aux équipements"
            onClick={retourListe}
            className="-ml-2 flex size-11 shrink-0 items-center justify-center text-dore"
          >
            <ChevronLeft className="size-6" />
          </button>
          <h1 className="min-w-0 flex-1 truncate font-titre text-[25px] leading-tight font-bold text-encre">
            {creation ? 'Nouvel équipement' : fiche.nom || '…'}
          </h1>
        </div>

        <div className="flex flex-col gap-3 rounded-[20px] bg-carte px-4 py-4 shadow-carte">
          <label className={classeEtiquette}>
            Nom
            <input
              value={fiche.nom}
              onChange={(e) => maj({ nom: e.target.value })}
              placeholder="Nom (ex. Fournaise)"
              className={cn(classeChamp, 'font-bold')}
            />
          </label>
          <label className={classeEtiquette}>
            Pièce
            <select
              value={fiche.zoneId}
              onChange={(e) => maj({ zoneId: e.target.value })}
              className={classeChamp}
            >
              <option value="">Sans pièce</option>
              {zones?.map((z) => (
                <option key={z.id} value={z.id}>{z.nom}</option>
              ))}
            </select>
          </label>
          <label className={classeEtiquette}>
            Marque
            <input
              value={fiche.marque}
              onChange={(e) => maj({ marque: e.target.value })}
              placeholder="Marque"
              className={classeChamp}
            />
          </label>
          <label className={classeEtiquette}>
            Modèle
            <input
              value={fiche.modele}
              onChange={(e) => maj({ modele: e.target.value })}
              placeholder="Modèle"
              className={classeChamp}
            />
          </label>
          <label className={classeEtiquette}>
            Numéro de série
            <input
              value={fiche.numeroSerie}
              onChange={(e) => maj({ numeroSerie: e.target.value })}
              placeholder="Numéro de série"
              className={classeChamp}
            />
          </label>
          <label className={classeEtiquette}>
            Acheté le
            <input
              type="date"
              value={fiche.dateAchat}
              onChange={(e) => maj({ dateAchat: e.target.value })}
              className={classeChamp}
            />
          </label>
          <label className={classeEtiquette}>
            Garantie jusqu’au
            <input
              type="date"
              value={fiche.finGarantie}
              onChange={(e) => maj({ finGarantie: e.target.value })}
              className={classeChamp}
            />
          </label>
          <label className={classeEtiquette}>
            Notes
            <textarea
              value={fiche.notes}
              onChange={(e) => maj({ notes: e.target.value })}
              placeholder="Notes"
              rows={4}
              className={cn(classeChamp, 'resize-y py-2 leading-snug')}
            />
          </label>
        </div>

        {/* Specs libres */}
        <div className="flex flex-col gap-2.5 rounded-[20px] bg-carte px-4 py-4 shadow-carte">
          <span className="text-[15px] font-bold text-dore">
            Specs — taille de filtre, code de peinture, n’importe quoi
          </span>
          {fiche.specs.map(([cle, valeur], i) => (
            <div key={i} className="flex items-center gap-2">
              <div className="flex min-w-0 flex-1 flex-col gap-2">
                <input
                  value={cle}
                  onChange={(e) =>
                    maj({ specs: fiche.specs.map((s, j) => (j === i ? [e.target.value, s[1]] : s)) })
                  }
                  placeholder="Nom (ex. filtre)"
                  aria-label="Nom de la spec"
                  className={classeChamp}
                />
                <input
                  value={valeur}
                  onChange={(e) =>
                    maj({ specs: fiche.specs.map((s, j) => (j === i ? [s[0], e.target.value] : s)) })
                  }
                  placeholder="Valeur (ex. 16 × 25 × 1)"
                  aria-label="Valeur de la spec"
                  className={classeChamp}
                />
              </div>
              <button
                type="button"
                aria-label="Retirer la spec"
                onClick={() => maj({ specs: fiche.specs.filter((_, j) => j !== i) })}
                className="flex size-11 shrink-0 items-center justify-center text-dore"
              >
                <X className="size-5" />
              </button>
            </div>
          ))}
          <button
            type="button"
            onClick={() => maj({ specs: [...fiche.specs, ['', '']] })}
            className="flex min-h-[44px] w-fit items-center gap-1.5 text-[15px] font-bold text-dore"
          >
            <Plus className="size-4" />
            Ajouter une spec
          </button>
        </div>

        <div className="flex items-stretch gap-2.5">
          <button
            type="button"
            onClick={() => {
              if (creation) {
                retourListe()
                return
              }
              // Équipement existant : Annuler restaure la fiche du serveur.
              if (detail !== undefined) {
                setFiche(versFiche(detail))
              }
            }}
            className="flex min-h-[52px] flex-1 items-center justify-center rounded-2xl border-2 border-tiret text-[16px] font-extrabold text-dore"
          >
            Annuler
          </button>
          <button
            type="button"
            disabled={fiche.nom.trim().length === 0 || enregistrer.isPending}
            onClick={() => enregistrer.mutate()}
            className="flex min-h-[52px] flex-1 items-center justify-center rounded-2xl bg-orange text-[16px] font-extrabold text-carte disabled:opacity-40"
          >
            Enregistrer
          </button>
        </div>

        {/* Documents liés — le téléversement reste au bureau : ici on consulte le
         * manuel devant l'appareil. */}
        {creation === false && detail !== undefined && (
          <div className="flex flex-col gap-2 rounded-[20px] bg-carte px-4 py-4 shadow-carte">
            <span className="text-[15px] font-bold text-dore">Documents</span>
            {detail.documents.length === 0 ? (
              <p className="text-[13px] text-dore">Aucun document encore.</p>
            ) : (
              detail.documents.map((document) => (
                <div
                  key={document.id}
                  className="flex min-h-[56px] items-center gap-2.5 rounded-xl bg-creux px-3 py-2"
                >
                  <VignetteDocument document={document} classe="size-10 bg-carte" />
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-[15px] font-bold text-encre">
                      {document.titre}
                    </span>
                    <span className="block text-[13px] text-dore">
                      {libelleTypeFichier(document)} ·{' '}
                      {(document.taille / 1024 / 1024).toFixed(1).replace('.', ',')} Mo
                    </span>
                  </span>
                  <a
                    href={`/api/documents/${document.id}/fichier`}
                    aria-label={`Télécharger ${document.nomFichier}`}
                    className="flex size-11 shrink-0 items-center justify-center text-dore"
                  >
                    <Download className="size-5" />
                  </a>
                </div>
              ))
            )}
          </div>
        )}

        {/* Supprimer est une commande à part entière : au doigt, pas de poubelle
         * révélée au survol comme au bureau. Le gabarit `[&>button]` porte la
         * cible tactile sur les deux états du composant partagé — sa pastille
         * « Vraiment? » est taillée pour la souris. */}
        {creation === false && (
          <div className="mt-1 [&>button]:flex [&>button]:min-h-[52px] [&>button]:w-full [&>button]:items-center [&>button]:justify-center [&>button]:gap-2 [&>button]:rounded-2xl [&>button]:px-4 [&>button]:text-[16px] [&>button]:font-bold">
            <ConfirmerSuppression
              key={equipementChoisi ?? 'aucun'}
              ariaLabel="Supprimer l'équipement"
              onConfirmer={() => supprimer.mutate()}
              className="border border-rouge/40 text-rouge"
            >
              <Trash2 className="size-5 shrink-0" />
              Supprimer l'équipement
            </ConfirmerSuppression>
          </div>
        )}
      </div>
    )
  }

  // La recherche filtre sur le nom seulement : au téléphone on cherche « fournaise »,
  // pas un numéro de série qu'on lirait plutôt sur l'appareil lui-même.
  const filtre = recherche.trim().toLocaleLowerCase('fr-CA')
  const visibles = (equipements ?? []).filter((e) =>
    filtre.length === 0 ? true : e.nom.toLocaleLowerCase('fr-CA').includes(filtre),
  )
  const parZone = new Map<string, typeof visibles>()
  for (const equipement of visibles) {
    const cle = equipement.zoneId ?? ''
    parZone.set(cle, [...(parZone.get(cle) ?? []), equipement])
  }

  return (
    <div className="flex flex-col gap-3 pt-1">
      <h1 className="font-titre text-[27px] leading-tight font-bold text-encre">Équipements</h1>

      <label className="flex min-h-[48px] items-center gap-2 rounded-2xl bg-carte px-3 shadow-carte">
        <Search className="size-5 shrink-0 text-dore" />
        <input
          value={recherche}
          onChange={(e) => setRecherche(e.target.value)}
          placeholder="Chercher un équipement"
          aria-label="Chercher un équipement"
          className="min-h-[48px] min-w-0 flex-1 bg-transparent text-[16px] text-texte focus:outline-none"
        />
        {recherche.length > 0 && (
          <button
            type="button"
            aria-label="Effacer la recherche"
            onClick={() => setRecherche('')}
            className="flex size-11 shrink-0 items-center justify-center text-dore"
          >
            <X className="size-5" />
          </button>
        )}
      </label>

      <button
        type="button"
        onClick={() => {
          setCreation(true)
          setEquipementChoisi(null)
        }}
        className="flex min-h-[52px] items-center justify-center gap-2 rounded-2xl bg-orange text-[16px] font-extrabold text-carte"
      >
        <Plus className="size-5" />
        Nouvel équipement
      </button>

      {equipementsEnErreur && (
        <ErreurChargement quoi="les équipements" onReessayer={() => void rechargerEquipements()} />
      )}

      {equipementsEnErreur === false && (equipements ?? []).length === 0 && (
        <p className="rounded-[20px] border border-dashed border-tiret px-5 py-10 text-center text-[15px] text-dore">
          Aucun équipement encore — inventorie la maison en t’installant.
        </p>
      )}

      {(equipements ?? []).length > 0 && visibles.length === 0 && (
        <p className="rounded-[20px] border border-dashed border-tiret px-5 py-10 text-center text-[15px] text-dore">
          Aucun équipement ne porte ce nom.
        </p>
      )}

      {[...parZone.entries()]
        .sort(([a], [b]) => nomZone(a).localeCompare(nomZone(b), 'fr-CA'))
        .map(([zoneId, liste]) => (
          <div key={zoneId || 'sans'} className="flex flex-col gap-2">
            <span className="text-[13px] font-extrabold tracking-[0.06em] text-dore uppercase">
              {nomZone(zoneId)}
            </span>
            {liste.map((equipement) => (
              <button
                key={equipement.id}
                type="button"
                onClick={() => {
                  setEquipementChoisi(equipement.id)
                  setCreation(false)
                }}
                className="flex min-h-[64px] items-center gap-3 rounded-[18px] bg-carte px-4 py-3 text-left shadow-carte"
              >
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-[17px] font-bold text-encre">
                    {equipement.nom}
                  </span>
                  {(equipement.marque !== null || equipement.modele !== null) && (
                    <span className="block truncate text-[13px] text-dore">
                      {[equipement.marque, equipement.modele].filter(Boolean).join(' · ')}
                    </span>
                  )}
                </span>
                {equipement.nbDocuments > 0 && (
                  <span className="flex shrink-0 items-center gap-1 text-[13px] font-bold text-dore">
                    <FileText className="size-4" />
                    {equipement.nbDocuments}
                  </span>
                )}
              </button>
            ))}
          </div>
        ))}
    </div>
  )
}
