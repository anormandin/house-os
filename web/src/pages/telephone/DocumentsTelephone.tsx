import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  BadgeCheck, BookOpen, ChevronLeft, Download, File as FileIcon, FileSignature, FolderOpen, House,
  Image, Landmark, Mail, Map, Plus, Receipt, Shield, SlidersHorizontal, Wrench, type LucideIcon,
} from 'lucide-react'
import ConfirmerSuppression from '@/components/ConfirmerSuppression'
import ErreurChargement from '@/components/ErreurChargement'
import { libelleTypeFichier } from '@/components/VignetteDocument'
import { api, dateLocaleIso, type CategorieDocument, type Document, type DocumentDonnees } from '@/lib/api'
import {
  comparer,
  joursAvant,
  LIBELLES_CATEGORIE,
  problemeTailleFichier,
  SANS_DOSSIER,
  type Tri,
} from '@/lib/documents-vues'
import { signalerErreur } from '@/lib/erreurs'
import { dateLisible } from '@/lib/format'
import { afficherToast } from '@/lib/toast'
import { cn } from '@/lib/utils'

const ICONES_CATEGORIE: Record<CategorieDocument, LucideIcon> = {
  Manuel: BookOpen,
  Photo: Image,
  Assurance: Shield,
  Facture: Receipt,
  Garantie: BadgeCheck,
  Contrat: FileSignature,
  PlanPermis: Map,
  ImpotsTaxes: Landmark,
  Autre: FileIcon,
}

/** Couleur du chip de catégorie (palette Cuisine chaleureuse, comme au bureau). */
const COULEURS_CATEGORIE: Record<CategorieDocument, string> = {
  Contrat: 'text-[#624c83]',
  Assurance: 'text-[#597b75]',
  Facture: 'text-[#4d699b]',
  PlanPermis: 'text-[#6f894e]',
  ImpotsTaxes: 'text-[#77713f]',
  Garantie: 'text-orange',
  Manuel: 'text-[#4d699b]',
  Photo: 'text-[#597b75]',
  Autre: 'text-sourdine',
}

const SEUIL_ECHEANCE_JOURS = 60

/** Le téléphone n'a pas de pagination : on allonge la liste par lots au doigt. */
const TAILLE_LOT = 25

/** Le tri du bureau est piloté par les en-têtes de table ; sans table, on garde
 * le tri par défaut (le plus récemment daté d'abord). */
const TRI_TELEPHONE: Tri = { colonne: 'dateDocument', desc: true }

const CLASSE_CHAMP =
  'min-h-[48px] w-full rounded-xl bg-creux px-3.5 text-[16px] text-texte focus:outline-2 focus:outline-orange/60'

type Fiche = {
  titre: string
  categorie: CategorieDocument
  equipementId: string
  zoneId: string
  dossier: string
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
    dossier: document.dossier ?? '',
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
    dossier: fiche.dossier.trim() || null,
    notes: fiche.notes || null,
    dateDocument: fiche.dateDocument || null,
    echeance: fiche.echeance || null,
  }
}

/** Une entrée de facette dans la feuille : cible de 52 px, compteur et état pressé
 * (au bureau c'est une ligne de 28 px dans la barre latérale — intouchable au doigt). */
function Facette({
  actif,
  onClick,
  icone: Icone,
  libelle,
  compte,
}: {
  actif: boolean
  onClick: () => void
  icone: LucideIcon
  libelle: string
  compte: number
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={`Filtrer : ${libelle}`}
      aria-pressed={actif}
      className={cn(
        'flex min-h-[52px] w-full items-center gap-3 rounded-xl px-3 text-left text-[16px]',
        actif ? 'bg-creux font-extrabold text-orange' : 'font-bold text-texte',
      )}
    >
      <Icone className="size-5 shrink-0 text-dore" />
      <span className="min-w-0 flex-1 truncate">{libelle}</span>
      <span
        className={cn(
          'shrink-0 rounded-full border px-2 py-0.5 text-[13px] font-bold tabular-nums',
          actif ? 'border-orange bg-carte text-orange' : 'border-tiret bg-creux text-sourdine',
        )}
      >
        {compte}
      </span>
    </button>
  )
}

/** La barre latérale de facettes du bureau (256 px) devient une feuille du bas :
 * les mêmes groupes, les mêmes compteurs, mais au pouce. */
function FeuilleFiltres({
  categories,
  dossiers,
  nbSansDossier,
  equipements,
  compteCategorie,
  compteDossier,
  compteEquipement,
  facetteCategorie,
  facetteDossier,
  facetteEquipement,
  onCategorie,
  onDossier,
  onEquipement,
  onEffacer,
  nbFiltres,
  onFermer,
}: {
  categories: CategorieDocument[]
  dossiers: string[]
  nbSansDossier: number
  equipements: { id: string; nom: string }[]
  compteCategorie: (categorie: CategorieDocument) => number
  compteDossier: (dossier: string) => number
  compteEquipement: (equipementId: string) => number
  facetteCategorie: CategorieDocument | null
  facetteDossier: string | null
  facetteEquipement: string | null
  onCategorie: (categorie: CategorieDocument) => void
  onDossier: (dossier: string) => void
  onEquipement: (equipementId: string) => void
  onEffacer: () => void
  nbFiltres: number
  onFermer: () => void
}) {
  return (
    <div className="fixed inset-0 z-50 flex flex-col justify-end" role="dialog" aria-modal="true">
      <button
        type="button"
        aria-label="Fermer"
        onClick={onFermer}
        className="absolute inset-0 cursor-default bg-encre/35"
      />
      <div className="relative flex max-h-[85dvh] flex-col overflow-y-auto rounded-t-[28px] bg-carte pb-[calc(env(safe-area-inset-bottom)+12px)] shadow-carte-lg">
        <div className="flex justify-center pt-2.5 pb-1">
          <span className="h-1 w-10 rounded-full bg-tiret" />
        </div>
        <div className="flex items-center gap-2 px-5 pt-1 pb-2">
          <h2 className="flex-1 font-titre text-[19px] font-bold text-encre">Filtrer</h2>
          {nbFiltres > 0 && (
            <button
              type="button"
              onClick={onEffacer}
              className="min-h-[44px] text-[15px] font-bold text-dore"
            >
              Effacer les filtres
            </button>
          )}
        </div>

        {categories.length > 0 && (
          <section className="px-2 pb-2">
            <h3 className="px-3 pb-1 text-[13px] font-extrabold tracking-[0.08em] text-dore uppercase">
              Catégories
            </h3>
            {categories.map((categorie) => (
              <Facette
                key={categorie}
                actif={facetteCategorie === categorie}
                onClick={() => onCategorie(categorie)}
                icone={ICONES_CATEGORIE[categorie]}
                libelle={LIBELLES_CATEGORIE[categorie]}
                compte={compteCategorie(categorie)}
              />
            ))}
          </section>
        )}

        {(dossiers.length > 0 || nbSansDossier > 0) && (
          <section className="border-t border-creux px-2 py-2">
            <h3 className="px-3 pb-1 text-[13px] font-extrabold tracking-[0.08em] text-dore uppercase">
              Lieux &amp; dossiers
            </h3>
            {dossiers.map((dossier) => (
              <Facette
                key={dossier}
                actif={facetteDossier === dossier}
                onClick={() => onDossier(dossier)}
                icone={House}
                libelle={dossier}
                compte={compteDossier(dossier)}
              />
            ))}
            {nbSansDossier > 0 && (
              <Facette
                actif={facetteDossier === SANS_DOSSIER}
                onClick={() => onDossier(SANS_DOSSIER)}
                icone={FolderOpen}
                libelle="Sans dossier"
                compte={nbSansDossier}
              />
            )}
          </section>
        )}

        {equipements.length > 0 && (
          <section className="border-t border-creux px-2 py-2">
            <h3 className="px-3 pb-1 text-[13px] font-extrabold tracking-[0.08em] text-dore uppercase">
              Équipements
            </h3>
            {equipements.map((equipement) => (
              <Facette
                key={equipement.id}
                actif={facetteEquipement === equipement.id}
                onClick={() => onEquipement(equipement.id)}
                icone={Wrench}
                libelle={equipement.nom}
                compte={compteEquipement(equipement.id)}
              />
            ))}
          </section>
        )}

        <button
          type="button"
          onClick={onFermer}
          className="mx-5 mt-2 flex min-h-[52px] items-center justify-center rounded-2xl bg-orange text-[17px] font-extrabold text-carte"
        >
          Voir les documents
        </button>
      </div>
    </div>
  )
}

/**
 * Les documents au téléphone : une liste de cartes à une colonne, pas de table.
 * Le bureau demande 1161 px (barre de facettes de 256 px + six colonnes en
 * `whitespace-nowrap`) — ici les facettes passent dans une feuille du bas et le
 * tiroir de 400 px devient un second écran, en état local plutôt qu'en route.
 */
export default function DocumentsTelephone() {
  const queryClient = useQueryClient()
  const [choisiId, setChoisiId] = useState<string | null>(null)
  const [facetteCategorie, setFacetteCategorie] = useState<CategorieDocument | null>(null)
  const [facetteDossier, setFacetteDossier] = useState<string | null>(null)
  const [facetteEquipement, setFacetteEquipement] = useState<string | null>(null)
  const [recherche, setRecherche] = useState('')
  const [filtresOuverts, setFiltresOuverts] = useState(false)
  const [nbAffiches, setNbAffiches] = useState(TAILLE_LOT)
  const [fiche, setFiche] = useState<Fiche | null>(null)
  const champFichier = useRef<HTMLInputElement>(null)
  const maj = (champ: Partial<Fiche>) =>
    setFiche((ancienne) => (ancienne === null ? ancienne : { ...ancienne, ...champ }))

  const {
    data: documents,
    isError: documentsEnErreur,
    refetch: rechargerDocuments,
  } = useQuery({ queryKey: ['documents'], queryFn: () => api.documents() })
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

  const effacerFiltres = () => {
    setFacetteCategorie(null)
    setFacetteDossier(null)
    setFacetteEquipement(null)
    setNbAffiches(TAILLE_LOT)
  }

  const televerser = useMutation({
    mutationFn: (fichier: File) => api.televerserDocument(fichier),
    onSuccess: ({ id }) => {
      invalider()
      effacerFiltres()
      setRecherche('')
      // Le document arrive sans titre utile : on ouvre sa fiche tout de suite.
      setChoisiId(id)
    },
  })

  const enregistrer = useMutation({
    mutationFn: () => api.modifierDocument(choisiId!, versDonnees(fiche!)),
    onSuccess: invalider,
  })

  // Classer = enregistrer la fiche corrigée ET sortir de la boîte, en un geste.
  const classer = useMutation({
    mutationFn: () => api.modifierDocument(choisiId!, { ...versDonnees(fiche!), aClasser: false }),
    onSuccess: invalider,
  })

  const relever = useMutation({
    mutationFn: api.releverCourriels,
    onSuccess: (rapport) => {
      invalider()
      afficherToast({
        message: rapport.nbDocuments > 0
          ? `${rapport.nbDocuments} document${rapport.nbDocuments > 1 ? 's' : ''} reçu${rapport.nbDocuments > 1 ? 's' : ''} par courriel`
          : 'Rien de nouveau dans le courrier',
        sousTitre: rapport.erreurs.length > 0 ? rapport.erreurs[0] : undefined,
        ton: 'succes',
      })
    },
    onError: (erreur) => signalerErreur(erreur instanceof Error ? erreur.message : 'Relevé impossible'),
  })

  const supprimer = useMutation({
    mutationFn: () => api.supprimerDocument(choisiId!),
    onSuccess: () => {
      setChoisiId(null)
      invalider()
    },
  })

  const tous = documents ?? []

  // Facettes disponibles, avec compteurs sur tout le corpus (aperçu stable).
  const categoriesPresentes = (Object.keys(LIBELLES_CATEGORIE) as CategorieDocument[])
    .filter((categorie) => tous.some((d) => d.categorie === categorie))
  const dossiers = [...new Set(tous.map((d) => d.dossier).filter((d): d is string => d !== null))]
    .sort((a, b) => a.localeCompare(b, 'fr'))
  const nbSansDossier = tous.filter((d) => d.dossier === null).length
  const equipementsPresents = (equipements ?? [])
    .filter((e) => tous.some((d) => d.equipementId === e.id))

  // Filtres combinés en ET ; « Sans dossier » est une facette légitime.
  const rechercheMinuscule = recherche.trim().toLowerCase()
  const correspond = (document: Document) =>
    (facetteCategorie === null || document.categorie === facetteCategorie) &&
    (facetteDossier === null ||
      (facetteDossier === SANS_DOSSIER ? document.dossier === null : document.dossier === facetteDossier)) &&
    (facetteEquipement === null || document.equipementId === facetteEquipement) &&
    (rechercheMinuscule.length === 0 ||
      [document.titre, document.notes ?? '', document.nomFichier, document.dossier ?? '']
        .some((texte) => texte.toLowerCase().includes(rechercheMinuscule)))
  const visibles = tous.filter(correspond).sort((a, b) => comparer(a, b, TRI_TELEPHONE))
  const lot = visibles.slice(0, nbAffiches)

  const nbFiltres = [facetteCategorie, facetteDossier, facetteEquipement]
    .filter((facette) => facette !== null).length

  // ——— Écran de détail : il remplace le tiroir de 400 px du bureau ———
  if (choisi !== null && fiche !== null) {
    const expiration = choisi.echeance === null ? null : joursAvant(choisi.echeance)
    return (
      <div className="flex flex-col gap-3 pt-1">
        <div className="flex items-center gap-1">
          <button
            type="button"
            aria-label="Retour à la liste des documents"
            onClick={() => setChoisiId(null)}
            className="-ml-2 flex size-11 shrink-0 items-center justify-center rounded-2xl text-dore"
          >
            <ChevronLeft className="size-6" />
          </button>
          <span className="flex-1 text-[15px] font-bold text-dore">Documents</span>
          <a
            href={`/api/documents/${choisi.id}/fichier`}
            aria-label={`Télécharger ${choisi.nomFichier}`}
            className="flex size-11 items-center justify-center rounded-2xl text-dore"
          >
            <Download className="size-5" />
          </a>
          <ConfirmerSuppression
            key={choisi.id}
            ariaLabel="Supprimer le document"
            onConfirmer={() => supprimer.mutate()}
            className="flex size-11 items-center justify-center rounded-2xl"
          />
        </div>

        <h1 className="font-titre text-[25px] leading-tight font-bold text-encre">{choisi.titre}</h1>

        <div className="flex flex-wrap items-center gap-2 text-[13px] text-sourdine">
          {choisi.aClasser && (
            <span className="rounded-full bg-orange px-2.5 py-0.5 text-[13px] font-bold text-carte">
              À classer
            </span>
          )}
          <span>
            {libelleTypeFichier(choisi)} · {choisi.nomFichier} ·{' '}
            {(choisi.taille / 1024 / 1024).toFixed(1).replace('.', ',')} Mo · ajouté le{' '}
            {dateLisible(dateLocaleIso(new Date(choisi.creeLe)))}
          </span>
        </div>

        {expiration !== null && choisi.echeance !== null && (
          <p
            className={cn(
              'text-[15px] font-bold',
              expiration < 0 ? 'text-rouge' : expiration <= SEUIL_ECHEANCE_JOURS ? 'text-dore' : 'text-sourdine',
            )}
          >
            {expiration < 0
              ? `Expiré depuis le ${dateLisible(choisi.echeance)}`
              : expiration === 0
                ? 'Expire aujourd’hui'
                : `Expire dans ${expiration} jour${expiration > 1 ? 's' : ''}`}
          </p>
        )}

        <div className="flex flex-col gap-3">
          <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
            Titre
            <input
              value={fiche.titre}
              onChange={(e) => maj({ titre: e.target.value })}
              aria-label="Titre"
              className={cn(CLASSE_CHAMP, 'font-bold')}
            />
          </label>
          <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
            Catégorie
            <select
              value={fiche.categorie}
              onChange={(e) => maj({ categorie: e.target.value as CategorieDocument })}
              aria-label="Catégorie"
              className={CLASSE_CHAMP}
            >
              {(Object.keys(LIBELLES_CATEGORIE) as CategorieDocument[]).map((categorie) => (
                <option key={categorie} value={categorie}>
                  {LIBELLES_CATEGORIE[categorie]}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
            Dossier
            <input
              value={fiche.dossier}
              onChange={(e) => maj({ dossier: e.target.value })}
              placeholder="Dossier (Maison, Impôts 2026…)"
              aria-label="Dossier"
              list="dossiers-existants-telephone"
              maxLength={100}
              className={CLASSE_CHAMP}
            />
          </label>
          <datalist id="dossiers-existants-telephone">
            {dossiers.map((dossier) => (
              <option key={dossier} value={dossier} />
            ))}
          </datalist>
          <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
            Équipement lié
            <select
              value={fiche.equipementId}
              onChange={(e) => maj({ equipementId: e.target.value })}
              aria-label="Équipement lié"
              className={CLASSE_CHAMP}
            >
              <option value="">Sans équipement</option>
              {equipements?.map((equipement) => (
                <option key={equipement.id} value={equipement.id}>{equipement.nom}</option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
            Pièce liée
            <select
              value={fiche.zoneId}
              onChange={(e) => maj({ zoneId: e.target.value })}
              aria-label="Pièce liée"
              className={CLASSE_CHAMP}
            >
              <option value="">Sans pièce</option>
              {zones?.map((zone) => (
                <option key={zone.id} value={zone.id}>{zone.nom}</option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
            Daté du
            <input
              type="date"
              value={fiche.dateDocument}
              onChange={(e) => maj({ dateDocument: e.target.value })}
              aria-label="Daté du"
              className={CLASSE_CHAMP}
            />
          </label>
          <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
            Expire le
            <input
              type="date"
              value={fiche.echeance}
              onChange={(e) => maj({ echeance: e.target.value })}
              aria-label="Expire le"
              className={CLASSE_CHAMP}
            />
          </label>
          <label className="flex flex-col gap-1 text-[13px] font-bold text-dore">
            Notes
            <textarea
              value={fiche.notes}
              onChange={(e) => maj({ notes: e.target.value })}
              placeholder="Notes"
              aria-label="Notes"
              rows={4}
              maxLength={2000}
              className={cn(CLASSE_CHAMP, 'resize-y py-2.5')}
            />
          </label>
        </div>

        <a
          href={`/api/documents/${choisi.id}/fichier`}
          aria-label={`Télécharger ${choisi.nomFichier}`}
          className="flex min-h-[52px] items-center justify-center gap-2 rounded-2xl border-2 border-tiret text-[17px] font-extrabold text-encre"
        >
          <Download className="size-5" />
          Télécharger
        </a>

        <div className="flex items-stretch gap-2.5">
          {choisi.aClasser && (
            <button
              type="button"
              disabled={fiche.titre.trim().length === 0 || classer.isPending}
              onClick={() => classer.mutate()}
              className="flex min-h-[56px] flex-1 items-center justify-center rounded-2xl bg-orange text-[17px] font-extrabold text-carte disabled:opacity-40"
            >
              Classer
            </button>
          )}
          <button
            type="button"
            disabled={fiche.titre.trim().length === 0 || enregistrer.isPending}
            onClick={() => enregistrer.mutate()}
            className={cn(
              'flex min-h-[56px] flex-1 items-center justify-center rounded-2xl text-[17px] font-extrabold disabled:opacity-40',
              choisi.aClasser ? 'border-2 border-tiret text-encre' : 'bg-orange text-carte',
            )}
          >
            Enregistrer
          </button>
        </div>
      </div>
    )
  }

  // ——— Écran de liste ———
  return (
    <div className="flex flex-col gap-3 pt-1">
      <div className="flex items-baseline gap-3">
        <h1 className="font-titre text-[27px] leading-tight font-bold text-encre">Documents</h1>
        <span className="ml-auto text-[13px] font-bold text-dore tabular-nums">
          {visibles.length} document{visibles.length > 1 ? 's' : ''}
        </span>
      </div>

      <input
        value={recherche}
        onChange={(e) => {
          setRecherche(e.target.value)
          setNbAffiches(TAILLE_LOT)
        }}
        placeholder="Chercher un document"
        aria-label="Chercher un document"
        className={CLASSE_CHAMP}
      />

      <div className="flex items-stretch gap-2.5">
        <button
          type="button"
          onClick={() => setFiltresOuverts(true)}
          aria-label={nbFiltres > 0 ? `Filtrer — ${nbFiltres} filtre${nbFiltres > 1 ? 's' : ''} actif${nbFiltres > 1 ? 's' : ''}` : 'Filtrer'}
          className={cn(
            'flex min-h-[48px] flex-1 items-center justify-center gap-2 rounded-2xl text-[16px] font-extrabold',
            nbFiltres > 0 ? 'bg-orange text-carte' : 'border-2 border-tiret text-encre',
          )}
        >
          <SlidersHorizontal className="size-5" />
          Filtrer
          {nbFiltres > 0 && (
            <span className="rounded-full bg-carte px-2 py-0.5 text-[13px] font-extrabold text-orange tabular-nums">
              {nbFiltres}
            </span>
          )}
        </button>
        <button
          type="button"
          disabled={relever.isPending}
          onClick={() => relever.mutate()}
          className="flex min-h-[48px] flex-1 items-center justify-center gap-2 rounded-2xl border-2 border-tiret text-[16px] font-extrabold text-dore disabled:opacity-40"
        >
          <Mail className="size-5" />
          {relever.isPending ? 'Relevé…' : 'Relever le courrier'}
        </button>
      </div>

      <input
        ref={champFichier}
        type="file"
        accept="application/pdf,image/*"
        className="hidden"
        onChange={(e) => {
          const fichier = e.target.files?.[0]
          if (fichier !== undefined) {
            const probleme = problemeTailleFichier(fichier)
            if (probleme === null) {
              televerser.mutate(fichier)
            } else {
              signalerErreur(probleme)
            }
            e.target.value = ''
          }
        }}
      />
      <button
        type="button"
        disabled={televerser.isPending}
        onClick={() => champFichier.current?.click()}
        className="flex min-h-[52px] items-center justify-center gap-2 rounded-2xl bg-orange text-[17px] font-extrabold text-carte disabled:opacity-40"
      >
        <Plus className="size-5" />
        {televerser.isPending ? 'Téléversement…' : 'Ajouter un document'}
      </button>

      {documentsEnErreur && (
        <ErreurChargement quoi="les documents" onReessayer={() => void rechargerDocuments()} />
      )}
      {documentsEnErreur === false && tous.length === 0 && (
        <p className="rounded-[20px] border border-dashed border-tiret px-5 py-8 text-center text-[15px] text-dore">
          Aucun document encore — l’acte de vente, l’assurance, les factures : tout le
          classeur de la maison a sa place ici.
        </p>
      )}
      {tous.length > 0 && visibles.length === 0 && (
        <p className="px-5 py-8 text-center text-[15px] text-sourdine">Rien ne correspond.</p>
      )}

      {lot.length > 0 && (
        <ul className="flex flex-col gap-2">
          {lot.map((document) => {
            const Icone = ICONES_CATEGORIE[document.categorie]
            const jours = document.echeance === null ? null : joursAvant(document.echeance)
            const lieA = document.dossier ?? document.nomEquipement ?? document.nomZone ?? '—'
            return (
              <li
                key={document.id}
                className="flex items-stretch gap-1 rounded-[20px] bg-carte pr-1 shadow-carte"
              >
                <button
                  type="button"
                  onClick={() => setChoisiId(document.id)}
                  className="flex min-h-[76px] min-w-0 flex-1 items-start gap-3 px-4 py-3 text-left"
                >
                  <Icone className="mt-0.5 size-5 shrink-0 text-dore" />
                  <span className="flex min-w-0 flex-1 flex-col gap-1.5">
                    <span className="truncate text-[16px] font-bold text-encre">{document.titre}</span>
                    <span className="flex flex-wrap items-center gap-1.5">
                      <span
                        className={cn(
                          'rounded-full border border-tiret bg-creux px-2.5 py-px text-[12px] font-bold',
                          COULEURS_CATEGORIE[document.categorie],
                        )}
                      >
                        {LIBELLES_CATEGORIE[document.categorie]}
                      </span>
                      <span className="rounded bg-creux px-1.5 py-px text-[12px] font-bold text-dore">
                        {libelleTypeFichier(document)}
                      </span>
                    </span>
                    <span className="truncate text-[13px] text-sourdine tabular-nums">
                      {lieA} ·{' '}
                      {document.dateDocument === null ? '—' : dateLisible(document.dateDocument)}
                    </span>
                    {jours !== null && document.echeance !== null && jours <= SEUIL_ECHEANCE_JOURS && (
                      <span
                        className={cn(
                          'text-[13px] font-bold',
                          jours < 0 ? 'text-rouge' : 'text-dore',
                        )}
                      >
                        {jours < 0
                          ? `Expiré · ${dateLisible(document.echeance)}`
                          : `Expire le ${dateLisible(document.echeance)}`}
                      </span>
                    )}
                  </span>
                </button>
                <a
                  href={`/api/documents/${document.id}/fichier`}
                  aria-label={`Télécharger ${document.nomFichier}`}
                  className="flex size-11 shrink-0 self-center items-center justify-center rounded-2xl text-tiret-texte"
                >
                  <Download className="size-5" />
                </a>
              </li>
            )
          })}
        </ul>
      )}

      {visibles.length > lot.length && (
        <button
          type="button"
          onClick={() => setNbAffiches((n) => n + TAILLE_LOT)}
          className="flex min-h-[52px] items-center justify-center rounded-2xl border border-dashed border-tiret text-[16px] font-extrabold text-dore"
        >
          Voir les {Math.min(TAILLE_LOT, visibles.length - lot.length)} suivants
        </button>
      )}

      {filtresOuverts && (
        <FeuilleFiltres
          categories={categoriesPresentes}
          dossiers={dossiers}
          nbSansDossier={nbSansDossier}
          equipements={equipementsPresents}
          compteCategorie={(categorie) => tous.filter((d) => d.categorie === categorie).length}
          compteDossier={(dossier) => tous.filter((d) => d.dossier === dossier).length}
          compteEquipement={(equipementId) => tous.filter((d) => d.equipementId === equipementId).length}
          facetteCategorie={facetteCategorie}
          facetteDossier={facetteDossier}
          facetteEquipement={facetteEquipement}
          onCategorie={(categorie) => {
            setFacetteCategorie(facetteCategorie === categorie ? null : categorie)
            setNbAffiches(TAILLE_LOT)
          }}
          onDossier={(dossier) => {
            setFacetteDossier(facetteDossier === dossier ? null : dossier)
            setNbAffiches(TAILLE_LOT)
          }}
          onEquipement={(equipementId) => {
            setFacetteEquipement(facetteEquipement === equipementId ? null : equipementId)
            setNbAffiches(TAILLE_LOT)
          }}
          onEffacer={effacerFiltres}
          nbFiltres={nbFiltres}
          onFermer={() => setFiltresOuverts(false)}
        />
      )}
    </div>
  )
}
