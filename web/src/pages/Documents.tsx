import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  BadgeCheck, BookOpen, Download, File, FileSignature, Image, Landmark,
  Map, Plus, Receipt, Shield, TriangleAlert,
} from 'lucide-react'
import ConfirmerSuppression from '@/components/ConfirmerSuppression'
import VignetteDocument, { libelleTypeFichier } from '@/components/VignetteDocument'
import { api, dateLocaleIso, type CategorieDocument, type Document, type DocumentDonnees } from '@/lib/api'
import { dateLisible } from '@/lib/format'
import { cn } from '@/lib/utils'

export const LIBELLES_CATEGORIE: Record<CategorieDocument, string> = {
  Manuel: 'Manuel',
  Photo: 'Photo',
  Assurance: 'Assurance',
  Facture: 'Facture',
  Garantie: 'Garantie',
  Contrat: 'Contrat',
  PlanPermis: 'Plan & permis',
  ImpotsTaxes: 'Impôts & taxes',
  Autre: 'Autre',
}

const ICONES_CATEGORIE: Record<CategorieDocument, typeof File> = {
  Manuel: BookOpen,
  Photo: Image,
  Assurance: Shield,
  Facture: Receipt,
  Garantie: BadgeCheck,
  Contrat: FileSignature,
  PlanPermis: Map,
  ImpotsTaxes: Landmark,
  Autre: File,
}

const SEUIL_ECHEANCE_JOURS = 60

/** Jours entre aujourd'hui (local) et une date ISO — négatif si passée. */
function joursAvant(dateIso: string): number {
  const [a, m, j] = dateIso.split('-').map(Number)
  const [aa, am, aj] = dateLocaleIso().split('-').map(Number)
  return Math.round(
    (Date.UTC(a, m - 1, j) - Date.UTC(aa, am - 1, aj)) / (24 * 60 * 60 * 1000),
  )
}

type Fiche = {
  titre: string
  categorie: CategorieDocument
  equipementId: string
  zoneId: string
  notes: string
  dateDocument: string
  echeance: string
}

function versFiche(document: Document): Fiche {
  return {
    titre: document.titre,
    categorie: document.categorie,
    equipementId: document.equipementId ?? '',
    zoneId: document.zoneId ?? '',
    notes: document.notes ?? '',
    dateDocument: document.dateDocument ?? '',
    echeance: document.echeance ?? '',
  }
}

function versDonnees(fiche: Fiche): DocumentDonnees {
  return {
    titre: fiche.titre,
    categorie: fiche.categorie,
    equipementId: fiche.equipementId || null,
    zoneId: fiche.zoneId || null,
    notes: fiche.notes || null,
    dateDocument: fiche.dateDocument || null,
    echeance: fiche.echeance || null,
  }
}

/** Aperçu d'une image dans la fiche — masqué si la miniature échoue (HEIC…). */
function ApercuImage({ document }: { document: Document }) {
  const [enErreur, setEnErreur] = useState(false)
  if (enErreur) {
    return null
  }
  return (
    <a
      href={`/api/documents/${document.id}/fichier`}
      aria-label={`Télécharger ${document.nomFichier}`}
      className="self-start overflow-hidden rounded-xl bg-creux"
    >
      <img
        src={`/api/documents/${document.id}/miniature`}
        alt={`Aperçu de ${document.titre}`}
        loading="lazy"
        onError={() => setEnErreur(true)}
        className="max-h-56 max-w-full object-contain"
      />
    </a>
  )
}

export default function Documents() {
  const queryClient = useQueryClient()
  const [choisiId, setChoisiId] = useState<string | null>(null)
  const [filtreCategorie, setFiltreCategorie] = useState<CategorieDocument | null>(null)
  const [recherche, setRecherche] = useState('')
  const [fiche, setFiche] = useState<Fiche | null>(null)
  const champFichier = useRef<HTMLInputElement>(null)
  const maj = (champ: Partial<Fiche>) =>
    setFiche((ancienne) => (ancienne === null ? ancienne : { ...ancienne, ...champ }))

  const { data: documents } = useQuery({ queryKey: ['documents'], queryFn: () => api.documents() })
  const { data: zones } = useQuery({ queryKey: ['zones'], queryFn: api.zones })
  const { data: equipements } = useQuery({ queryKey: ['equipements'], queryFn: api.equipements })

  const choisi = documents?.find((d) => d.id === choisiId) ?? null

  useEffect(() => {
    setFiche(choisi === null ? null : versFiche(choisi))
  }, [choisi])

  const invalider = () => {
    queryClient.invalidateQueries({ queryKey: ['documents'] })
    queryClient.invalidateQueries({ queryKey: ['equipements'] })
    queryClient.invalidateQueries({ queryKey: ['equipement'] })
  }

  const televerser = useMutation({
    mutationFn: (fichier: File) => api.televerserDocument(fichier),
    onSuccess: ({ id }) => {
      invalider()
      setFiltreCategorie(null)
      setRecherche('')
      setChoisiId(id)
    },
  })

  const enregistrer = useMutation({
    mutationFn: () => api.modifierDocument(choisiId!, versDonnees(fiche!)),
    onSuccess: invalider,
  })

  const supprimer = useMutation({
    mutationFn: () => api.supprimerDocument(choisiId!),
    onSuccess: () => {
      setChoisiId(null)
      invalider()
    },
  })

  const rechercheMinuscule = recherche.trim().toLowerCase()
  const correspond = (document: Document) =>
    (filtreCategorie === null || document.categorie === filtreCategorie) &&
    (rechercheMinuscule.length === 0 ||
      [document.titre, document.notes ?? '', document.nomFichier]
        .some((texte) => texte.toLowerCase().includes(rechercheMinuscule)))
  const visibles = (documents ?? []).filter(correspond)

  const proches = (documents ?? [])
    .filter((d) => d.echeance !== null && joursAvant(d.echeance) <= SEUIL_ECHEANCE_JOURS)
    .sort((a, b) => a.echeance!.localeCompare(b.echeance!))

  const categoriesPresentes = (Object.keys(LIBELLES_CATEGORIE) as CategorieDocument[])
    .filter((categorie) => documents?.some((d) => d.categorie === categorie))

  const classeChamp =
    'rounded-xl bg-creux px-3 py-2 text-sm text-texte focus:outline-2 focus:outline-orange/60'

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-end justify-between">
        <h1 className="text-4xl font-bold">Documents</h1>
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
          className="flex items-center gap-2 rounded-full bg-orange px-4 py-2 text-sm font-bold text-carte disabled:opacity-40"
        >
          <Plus className="size-4" />
          {televerser.isPending ? 'Téléversement…' : 'Ajouter un document'}
        </button>
      </div>

      {/* Échéances proches */}
      {proches.length > 0 && (
        <div className="rounded-[20px] bg-carte px-5 py-4 shadow-carte">
          <div className="mb-2 flex items-center gap-2 text-sm font-bold text-jaune">
            <TriangleAlert className="size-4" /> Échéances proches
          </div>
          <div className="flex flex-wrap gap-2">
            {proches.map((document) => {
              const jours = joursAvant(document.echeance!)
              return (
                <button
                  key={document.id}
                  type="button"
                  onClick={() => setChoisiId(document.id)}
                  className={cn(
                    'flex items-center gap-2 rounded-full bg-creux px-3 py-1.5 text-xs font-bold',
                    jours < 0 ? 'text-rouge' : 'text-jaune',
                  )}
                >
                  <span className="max-w-52 truncate">{document.titre}</span>
                  <span className="font-normal text-sourdine">
                    {jours < 0
                      ? `expiré depuis le ${dateLisible(document.echeance!)}`
                      : jours === 0
                        ? 'expire aujourd’hui'
                        : `expire dans ${jours} jour${jours > 1 ? 's' : ''}`}
                  </span>
                </button>
              )
            })}
          </div>
        </div>
      )}

      {/* Filtres */}
      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          onClick={() => setFiltreCategorie(null)}
          className={cn(
            'rounded-full px-3 py-1.5 text-xs font-bold transition-colors',
            filtreCategorie === null ? 'bg-orange text-carte' : 'bg-creux text-sourdine hover:text-dore',
          )}
        >
          Tous
        </button>
        {categoriesPresentes.map((categorie) => (
          <button
            key={categorie}
            type="button"
            onClick={() => setFiltreCategorie(filtreCategorie === categorie ? null : categorie)}
            className={cn(
              'rounded-full px-3 py-1.5 text-xs font-bold transition-colors',
              filtreCategorie === categorie
                ? 'bg-orange text-carte'
                : 'bg-creux text-sourdine hover:text-dore',
            )}
          >
            {LIBELLES_CATEGORIE[categorie]}
          </button>
        ))}
        <input
          value={recherche}
          onChange={(e) => setRecherche(e.target.value)}
          placeholder="Chercher…"
          aria-label="Chercher un document"
          className={cn(classeChamp, 'ml-auto w-52')}
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-[1fr_1.5fr]">
        {/* Liste */}
        <div className="flex flex-col gap-2">
          {(documents ?? []).length === 0 && (
            <p className="rounded-[20px] border-2 border-dashed border-tiret px-5 py-8 text-center text-sm font-bold text-tiret-texte">
              Aucun document encore — l’acte de vente, l’assurance, les factures : tout le
              classeur de la maison a sa place ici.
            </p>
          )}
          {(documents ?? []).length > 0 && visibles.length === 0 && (
            <p className="px-5 py-8 text-center text-sm text-sourdine">Rien ne correspond.</p>
          )}
          {visibles.map((document) => (
            <div
              key={document.id}
              className={cn(
                'flex items-center gap-3 rounded-[18px] bg-carte px-4 py-3 shadow-carte transition-shadow hover:shadow-carte-lg',
                choisiId === document.id && 'outline-2 outline-orange/50',
              )}
            >
              <button
                type="button"
                onClick={() => setChoisiId(document.id)}
                className="flex min-w-0 flex-1 items-center gap-3 text-left"
              >
                <VignetteDocument document={document} icone={ICONES_CATEGORIE[document.categorie]} />
                <div className="min-w-0 flex-1">
                  <div className="truncate text-[15px] font-bold">{document.titre}</div>
                  <div className="flex items-center gap-1.5 text-xs text-sourdine">
                    <span className="shrink-0 rounded bg-creux px-1 py-px text-[10px] font-bold text-dore">
                      {libelleTypeFichier(document)}
                    </span>
                    <span className="truncate">
                      {[
                        LIBELLES_CATEGORIE[document.categorie],
                        document.nomEquipement,
                        document.nomZone,
                        document.dateDocument !== null ? dateLisible(document.dateDocument) : null,
                      ].filter(Boolean).join(' · ')}
                    </span>
                  </div>
                </div>
              </button>
              {document.echeance !== null && (
                <span
                  className={cn(
                    'shrink-0 text-xs font-bold',
                    joursAvant(document.echeance) < 0
                      ? 'text-rouge'
                      : joursAvant(document.echeance) <= SEUIL_ECHEANCE_JOURS
                        ? 'text-jaune'
                        : 'text-sourdine',
                  )}
                >
                  {dateLisible(document.echeance)}
                </span>
              )}
              <a
                href={`/api/documents/${document.id}/fichier`}
                aria-label={`Télécharger ${document.nomFichier}`}
                className="shrink-0 text-sourdine hover:text-orange"
              >
                <Download className="size-4" />
              </a>
            </div>
          ))}
        </div>

        {/* Fiche */}
        <div className="flex min-h-[420px] flex-col gap-4 self-start rounded-3xl bg-carte px-7 py-6 shadow-carte-lg">
          {choisi === null || fiche === null ? (
            <p className="m-auto max-w-64 text-center text-sm text-sourdine">
              Choisis un document, ou téléverses-en un nouveau — il sera catégorisé
              automatiquement, à ajuster ensuite.
            </p>
          ) : (
            <>
              <div className="flex items-center gap-2">
                <h2 className="min-w-0 flex-1 truncate text-2xl font-bold">{choisi.titre}</h2>
                <a
                  href={`/api/documents/${choisi.id}/fichier`}
                  aria-label={`Télécharger ${choisi.nomFichier}`}
                  className="text-sourdine hover:text-orange"
                >
                  <Download className="size-5" />
                </a>
                <ConfirmerSuppression
                  key={choisiId ?? 'aucun'}
                  ariaLabel="Supprimer le document"
                  onConfirmer={() => supprimer.mutate()}
                />
              </div>

              <div className="text-xs text-sourdine">
                {libelleTypeFichier(choisi)} · {choisi.nomFichier} ·{' '}
                {(choisi.taille / 1024 / 1024).toFixed(1).replace('.', ',')} Mo ·
                ajouté le {dateLisible(dateLocaleIso(new Date(choisi.creeLe)))}
              </div>

              {choisi.typeMime.startsWith('image/') && (
                <ApercuImage key={choisi.id} document={choisi} />
              )}

              <div className="grid gap-3 sm:grid-cols-2">
                <input
                  value={fiche.titre}
                  onChange={(e) => maj({ titre: e.target.value })}
                  placeholder="Titre"
                  aria-label="Titre"
                  className={cn(classeChamp, 'font-bold sm:col-span-2')}
                />
                <select
                  value={fiche.categorie}
                  onChange={(e) => maj({ categorie: e.target.value as CategorieDocument })}
                  aria-label="Catégorie"
                  className={classeChamp}
                >
                  {(Object.keys(LIBELLES_CATEGORIE) as CategorieDocument[]).map((categorie) => (
                    <option key={categorie} value={categorie}>
                      {LIBELLES_CATEGORIE[categorie]}
                    </option>
                  ))}
                </select>
                <select
                  value={fiche.equipementId}
                  onChange={(e) => maj({ equipementId: e.target.value })}
                  aria-label="Équipement lié"
                  className={classeChamp}
                >
                  <option value="">Sans équipement</option>
                  {equipements?.map((equipement) => (
                    <option key={equipement.id} value={equipement.id}>{equipement.nom}</option>
                  ))}
                </select>
                <select
                  value={fiche.zoneId}
                  onChange={(e) => maj({ zoneId: e.target.value })}
                  aria-label="Pièce liée"
                  className={classeChamp}
                >
                  <option value="">Sans pièce</option>
                  {zones?.map((zone) => (
                    <option key={zone.id} value={zone.id}>{zone.nom}</option>
                  ))}
                </select>
                <label className="flex items-center gap-2 text-xs text-sourdine">
                  Daté du
                  <input
                    type="date"
                    value={fiche.dateDocument}
                    onChange={(e) => maj({ dateDocument: e.target.value })}
                    className={cn(classeChamp, 'flex-1')}
                  />
                </label>
                <label className="flex items-center gap-2 text-xs text-sourdine sm:col-start-2">
                  Expire le
                  <input
                    type="date"
                    value={fiche.echeance}
                    onChange={(e) => maj({ echeance: e.target.value })}
                    className={cn(classeChamp, 'flex-1')}
                  />
                </label>
                <input
                  value={fiche.notes}
                  onChange={(e) => maj({ notes: e.target.value })}
                  placeholder="Notes"
                  aria-label="Notes"
                  className={cn(classeChamp, 'sm:col-span-2')}
                />
              </div>

              <div className="mt-auto flex justify-end">
                <button
                  type="button"
                  disabled={fiche.titre.trim().length === 0 || enregistrer.isPending}
                  onClick={() => enregistrer.mutate()}
                  className="rounded-xl bg-orange px-5 py-2 text-sm font-bold text-carte disabled:opacity-40"
                >
                  Enregistrer
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
