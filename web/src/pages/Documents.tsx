import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  BadgeCheck, BookOpen, Download, File, FileSignature, FolderOpen, House, Image, Inbox,
  Landmark, Mail, Map, Plus, Receipt, Shield, TriangleAlert, Wrench, X,
} from 'lucide-react'
import ConfirmerSuppression from '@/components/ConfirmerSuppression'
import ErreurChargement from '@/components/ErreurChargement'
import { libelleTypeFichier, TYPE_MIME_COURRIEL } from '@/components/VignetteDocument'
import { api, dateLocaleIso, type CategorieDocument, type Document, type DocumentDonnees } from '@/lib/api'
import {
  comparer,
  joursAvant,
  LIBELLES_CATEGORIE,
  problemeTailleFichier,
  SANS_DOSSIER,
  type ColonneTri,
  type Tri,
} from '@/lib/documents-vues'
import { signalerErreur } from '@/lib/erreurs'
import { dateLisible } from '@/lib/format'
import { afficherToast } from '@/lib/toast'
import { cn } from '@/lib/utils'

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

/** Couleur du chip de catégorie dans la table (palette Cuisine chaleureuse). */
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
const TAILLE_PAGE = 25

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

/** Aperçu d'une image dans le tiroir — masqué si la miniature échoue (HEIC…). */
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
        className="h-36 max-w-full object-contain"
      />
    </a>
  )
}

/** De / Date / Sujet / texte d'un courriel archivé — le tiroir lit le .eml sans l'ouvrir. */
function ApercuCourriel({ id }: { id: string }) {
  const { data: courriel, isError } = useQuery({
    queryKey: ['document-courriel', id],
    queryFn: () => api.courrielDocument(id),
  })
  if (isError) {
    return <p className="text-xs text-sourdine">Aperçu du courriel indisponible.</p>
  }
  if (courriel === undefined) {
    return null
  }
  const taille = (octets: number) => `${Math.max(1, Math.round(octets / 1024))} Ko`
  return (
    <div className="flex flex-col gap-1.5 rounded-xl bg-creux px-3.5 py-3 text-xs">
      <div className="text-sourdine">
        <span className="font-bold text-texte">De</span> {courriel.de}
      </div>
      <div className="text-sourdine">
        <span className="font-bold text-texte">Le</span> {dateLisible(dateLocaleIso(new Date(courriel.date)))}
      </div>
      <div className="text-sourdine">
        <span className="font-bold text-texte">Sujet</span> {courriel.sujet || '—'}
      </div>
      <pre
        aria-label="Texte du courriel"
        className="mt-1 max-h-64 overflow-y-auto whitespace-pre-wrap break-words font-sans text-[12.5px] text-texte"
      >
        {courriel.texte || '(courriel sans texte)'}
      </pre>
      {courriel.piecesJointes.length > 0 && (
        <ul className="mt-1 text-sourdine">
          {courriel.piecesJointes.map((piece) => (
            <li key={piece.nomFichier}>
              {piece.nomFichier} · {piece.typeMime} · {taille(piece.taille)}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

/** Une entrée de facette : libellé + compteur, une seule active par bloc. */
function Facette({
  actif,
  onClick,
  icone: Icone,
  libelle,
  compte,
}: {
  actif: boolean
  onClick: () => void
  icone: typeof File
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
        'flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-[13.5px] transition-colors',
        actif ? 'bg-creux font-bold text-orange' : 'text-texte hover:bg-creux/60',
      )}
    >
      <Icone className="size-4 shrink-0 text-dore" />
      <span className="min-w-0 flex-1 truncate">{libelle}</span>
      <span
        className={cn(
          'shrink-0 rounded-full border px-2 text-xs font-bold tabular-nums',
          actif ? 'border-orange bg-carte text-orange' : 'border-tiret bg-creux text-sourdine',
        )}
      >
        {compte}
      </span>
    </button>
  )
}

export default function Documents() {
  const queryClient = useQueryClient()
  const [choisiId, setChoisiId] = useState<string | null>(null)
  const [facetteCategorie, setFacetteCategorie] = useState<CategorieDocument | null>(null)
  const [facetteDossier, setFacetteDossier] = useState<string | null>(null)
  const [facetteEquipement, setFacetteEquipement] = useState<string | null>(null)
  const [recherche, setRecherche] = useState('')
  const [tri, setTri] = useState<Tri>({ colonne: 'dateDocument', desc: true })
  const [page, setPage] = useState(0)
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

  // Échap ferme le tiroir (sans toucher aux filtres).
  useEffect(() => {
    if (choisiId === null) {
      return
    }
    const surTouche = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setChoisiId(null)
      }
    }
    window.addEventListener('keydown', surTouche)
    return () => window.removeEventListener('keydown', surTouche)
  }, [choisiId])

  const invalider = () => {
    queryClient.invalidateQueries({ queryKey: ['documents'] })
    queryClient.invalidateQueries({ queryKey: ['equipements'] })
    queryClient.invalidateQueries({ queryKey: ['equipement'] })
  }

  const effacerFiltres = () => {
    setFacetteCategorie(null)
    setFacetteDossier(null)
    setFacetteEquipement(null)
    setPage(0)
  }

  const televerser = useMutation({
    mutationFn: (fichier: File) => api.televerserDocument(fichier),
    onSuccess: ({ id }) => {
      invalider()
      effacerFiltres()
      setRecherche('')
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
  const compteCategorie = (categorie: CategorieDocument) =>
    tous.filter((d) => d.categorie === categorie).length

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
  const visibles = tous.filter(correspond).sort((a, b) => comparer(a, b, tri))

  const nbPages = Math.max(1, Math.ceil(visibles.length / TAILLE_PAGE))
  const pageBornee = Math.min(page, nbPages - 1)
  const pageDocs = visibles.slice(pageBornee * TAILLE_PAGE, (pageBornee + 1) * TAILLE_PAGE)

  const proches = tous
    .filter((d) => d.echeance !== null && joursAvant(d.echeance) <= SEUIL_ECHEANCE_JOURS)
    .sort((a, b) => a.echeance!.localeCompare(b.echeance!))

  const aClasser = tous
    .filter((d) => d.aClasser)
    .sort((a, b) => b.creeLe.localeCompare(a.creeLe))

  const basculerTri = (colonne: ColonneTri) => {
    setTri((ancien) =>
      ancien.colonne === colonne
        ? { colonne, desc: ancien.desc === false }
        : { colonne, desc: colonne === 'dateDocument' })
    setPage(0)
  }

  const jetons: { libelle: string; retirer: () => void }[] = []
  if (facetteCategorie !== null) {
    jetons.push({ libelle: LIBELLES_CATEGORIE[facetteCategorie], retirer: () => setFacetteCategorie(null) })
  }
  if (facetteDossier !== null) {
    jetons.push({
      libelle: facetteDossier === SANS_DOSSIER ? 'Sans dossier' : facetteDossier,
      retirer: () => setFacetteDossier(null),
    })
  }
  if (facetteEquipement !== null) {
    jetons.push({
      libelle: equipementsPresents.find((e) => e.id === facetteEquipement)?.nom ?? 'Équipement',
      retirer: () => setFacetteEquipement(null),
    })
  }

  const classeChamp =
    'rounded-xl bg-creux px-3 py-2 text-sm text-texte focus:outline-2 focus:outline-orange/60'
  const classeTd =
    'bg-carte px-3.5 py-2.5 text-[13.5px] whitespace-nowrap transition-colors group-hover:bg-creux/70'
  const classeTh =
    'px-3.5 pb-1.5 text-left text-[11px] font-extrabold uppercase tracking-wider text-tiret-texte'

  const enteteTri = (colonne: ColonneTri, libelle: string) => (
    <th className={classeTh}>
      <button
        type="button"
        onClick={() => basculerTri(colonne)}
        className={cn('uppercase tracking-wider', tri.colonne === colonne && 'text-dore')}
      >
        {libelle}&nbsp;{tri.colonne === colonne ? (tri.desc ? '↓' : '↑') : '↕'}
      </button>
    </th>
  )

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-center gap-3">
        <h1 className="mr-auto text-4xl font-bold">Documents</h1>
        <input
          value={recherche}
          onChange={(e) => {
            setRecherche(e.target.value)
            setPage(0)
          }}
          placeholder="Chercher…"
          aria-label="Chercher un document"
          className={cn(classeChamp, 'w-56')}
        />
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
          disabled={relever.isPending}
          onClick={() => relever.mutate()}
          title="Aller chercher tout de suite ce qui a été transféré à documents@"
          className="flex items-center gap-2 rounded-full border border-tiret bg-carte px-4 py-2 text-sm font-bold text-dore hover:text-orange disabled:opacity-40"
        >
          <Mail className="size-4" />
          {relever.isPending ? 'Relevé…' : 'Relever le courrier'}
        </button>
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

      <div className="flex items-start gap-5">
        {/* Barre latérale de facettes */}
        <div className="flex w-64 shrink-0 flex-col gap-3">
          {aClasser.length > 0 && (
            <div className="rounded-2xl border border-orange bg-carte px-3.5 py-3 shadow-carte">
              <div className="mb-1 flex items-center gap-2 text-[13.5px] font-bold text-orange">
                <Inbox className="size-4" /> À classer ({aClasser.length})
              </div>
              {aClasser.map((document) => (
                <button
                  key={document.id}
                  type="button"
                  onClick={() => setChoisiId(document.id)}
                  className="flex w-full items-start gap-2 rounded-lg px-1 py-1.5 text-left text-xs hover:bg-creux/60"
                >
                  <Mail className="mt-0.5 size-3.5 shrink-0 text-dore" />
                  <span className="min-w-0">
                    <span className="block truncate font-bold">{document.titre}</span>
                    <span className="text-sourdine">
                      {LIBELLES_CATEGORIE[document.categorie]} · {libelleTypeFichier(document)}
                    </span>
                  </span>
                </button>
              ))}
            </div>
          )}

          {proches.length > 0 && (
            <div className="rounded-2xl border border-jaune bg-carte px-3.5 py-3 shadow-carte">
              <div className="mb-1 flex items-center gap-2 text-[13.5px] font-bold text-rouge">
                <TriangleAlert className="size-4" /> Échéances proches
              </div>
              {proches.map((document) => {
                const jours = joursAvant(document.echeance!)
                return (
                  <button
                    key={document.id}
                    type="button"
                    onClick={() => setChoisiId(document.id)}
                    className="flex w-full items-start gap-2 rounded-lg px-1 py-1.5 text-left text-xs hover:bg-creux/60"
                  >
                    <span
                      className={cn(
                        'mt-1 size-2 shrink-0 rounded-full',
                        jours < 0 ? 'bg-rouge' : 'bg-jaune',
                      )}
                    />
                    <span className="min-w-0">
                      <span className="block truncate font-bold">{document.titre}</span>
                      <span className={cn('font-bold', jours < 0 ? 'text-rouge' : 'text-dore')}>
                        {jours < 0
                          ? `expiré depuis le ${dateLisible(document.echeance!)}`
                          : jours === 0
                            ? 'expire aujourd’hui'
                            : `expire dans ${jours} jour${jours > 1 ? 's' : ''}`}
                      </span>
                    </span>
                  </button>
                )
              })}
            </div>
          )}

          {categoriesPresentes.length > 0 && (
            <div className="rounded-2xl bg-carte px-3 py-3 shadow-carte">
              <h2 className="mb-1.5 px-1 text-[13.5px] font-bold">Catégories</h2>
              {categoriesPresentes.map((categorie) => (
                <Facette
                  key={categorie}
                  actif={facetteCategorie === categorie}
                  onClick={() => {
                    setFacetteCategorie(facetteCategorie === categorie ? null : categorie)
                    setPage(0)
                  }}
                  icone={ICONES_CATEGORIE[categorie]}
                  libelle={LIBELLES_CATEGORIE[categorie]}
                  compte={compteCategorie(categorie)}
                />
              ))}
            </div>
          )}

          {dossiers.length > 0 && (
            <div className="rounded-2xl bg-carte px-3 py-3 shadow-carte">
              <h2 className="mb-1.5 px-1 text-[13.5px] font-bold">Lieux &amp; dossiers</h2>
              {dossiers.map((dossier) => (
                <Facette
                  key={dossier}
                  actif={facetteDossier === dossier}
                  onClick={() => {
                    setFacetteDossier(facetteDossier === dossier ? null : dossier)
                    setPage(0)
                  }}
                  icone={House}
                  libelle={dossier}
                  compte={tous.filter((d) => d.dossier === dossier).length}
                />
              ))}
              {nbSansDossier > 0 && (
                <Facette
                  actif={facetteDossier === SANS_DOSSIER}
                  onClick={() => {
                    setFacetteDossier(facetteDossier === SANS_DOSSIER ? null : SANS_DOSSIER)
                    setPage(0)
                  }}
                  icone={FolderOpen}
                  libelle="Sans dossier"
                  compte={nbSansDossier}
                />
              )}
            </div>
          )}

          {equipementsPresents.length > 0 && (
            <div className="rounded-2xl bg-carte px-3 py-3 shadow-carte">
              <h2 className="mb-1.5 px-1 text-[13.5px] font-bold">Équipements</h2>
              {equipementsPresents.map((equipement) => (
                <Facette
                  key={equipement.id}
                  actif={facetteEquipement === equipement.id}
                  onClick={() => {
                    setFacetteEquipement(facetteEquipement === equipement.id ? null : equipement.id)
                    setPage(0)
                  }}
                  icone={Wrench}
                  libelle={equipement.nom}
                  compte={tous.filter((d) => d.equipementId === equipement.id).length}
                />
              ))}
            </div>
          )}
        </div>

        {/* Centre : jetons + table + pagination, tiroir par-dessus */}
        <div className={cn('relative min-w-0 flex-1', choisi !== null && 'min-h-[620px]')}>
          {(jetons.length > 0 || visibles.length > 0) && (
            <div className="mb-2.5 flex flex-wrap items-center gap-2">
              {jetons.map((jeton) => (
                <button
                  key={jeton.libelle}
                  type="button"
                  onClick={() => {
                    jeton.retirer()
                    setPage(0)
                  }}
                  aria-label={`Retirer le filtre ${jeton.libelle}`}
                  className="flex items-center gap-1.5 rounded-full bg-orange py-1 pl-3.5 pr-2 text-xs font-bold text-carte"
                >
                  {jeton.libelle}
                  <X className="size-3.5 rounded-full bg-carte/25 p-px" />
                </button>
              ))}
              {jetons.length > 0 && (
                <button
                  type="button"
                  onClick={effacerFiltres}
                  className="text-xs font-bold text-dore hover:text-orange"
                >
                  Effacer les filtres
                </button>
              )}
              <span className="ml-auto text-xs text-sourdine">
                {visibles.length} document{visibles.length > 1 ? 's' : ''}
              </span>
            </div>
          )}

          {documentsEnErreur && (
            <ErreurChargement quoi="les documents" onReessayer={() => rechargerDocuments()} />
          )}
          {documentsEnErreur === false && tous.length === 0 && (
            <p className="rounded-[20px] border-2 border-dashed border-tiret px-5 py-8 text-center text-sm font-bold text-tiret-texte">
              Aucun document encore — l’acte de vente, l’assurance, les factures : tout le
              classeur de la maison a sa place ici.
            </p>
          )}
          {tous.length > 0 && visibles.length === 0 && (
            <p className="px-5 py-8 text-center text-sm text-sourdine">Rien ne correspond.</p>
          )}

          {visibles.length > 0 && (
            <table className="w-full border-separate border-spacing-y-1.5">
              <thead>
                <tr>
                  {enteteTri('titre', 'Titre')}
                  {enteteTri('categorie', 'Catégorie')}
                  <th className={classeTh}>Lié à</th>
                  {enteteTri('dateDocument', 'Daté du')}
                  {enteteTri('echeance', 'Échéance')}
                  <th className={classeTh} />
                </tr>
              </thead>
              <tbody>
                {pageDocs.map((document) => {
                  const Icone = ICONES_CATEGORIE[document.categorie]
                  const jours = document.echeance === null ? null : joursAvant(document.echeance)
                  return (
                    <tr
                      key={document.id}
                      onClick={() => setChoisiId(document.id)}
                      className={cn(
                        'group cursor-pointer',
                        choisiId === document.id && 'outline-2 outline-orange/50',
                      )}
                    >
                      <td className={cn(classeTd, 'max-w-[300px] rounded-l-xl')}>
                        <span className="flex items-center gap-2.5 font-bold">
                          <Icone className="size-4 shrink-0 text-dore" />
                          <span className="truncate">{document.titre}</span>
                          <span className="shrink-0 rounded bg-creux px-1 py-px text-[10px] font-bold text-dore">
                            {libelleTypeFichier(document)}
                          </span>
                        </span>
                      </td>
                      <td className={classeTd}>
                        <span
                          className={cn(
                            'rounded-full border border-tiret bg-creux px-2.5 py-0.5 text-[11.5px] font-bold',
                            COULEURS_CATEGORIE[document.categorie],
                          )}
                        >
                          {LIBELLES_CATEGORIE[document.categorie]}
                        </span>
                      </td>
                      <td className={cn(classeTd, 'max-w-44 truncate text-sourdine')}>
                        {document.dossier ?? document.nomEquipement ?? document.nomZone ?? '—'}
                      </td>
                      <td className={cn(classeTd, 'tabular-nums text-sourdine')}>
                        {document.dateDocument === null ? '—' : dateLisible(document.dateDocument)}
                      </td>
                      <td
                        className={cn(
                          classeTd,
                          'tabular-nums',
                          jours === null
                            ? 'text-sourdine'
                            : jours < 0
                              ? 'font-bold text-rouge'
                              : jours <= SEUIL_ECHEANCE_JOURS
                                ? 'font-bold text-dore'
                                : 'text-sourdine',
                        )}
                      >
                        {document.echeance === null
                          ? '—'
                          : jours !== null && jours < 0
                            ? `Expirée · ${dateLisible(document.echeance)}`
                            : dateLisible(document.echeance)}
                      </td>
                      <td className={cn(classeTd, 'w-10 rounded-r-xl')}>
                        <a
                          href={`/api/documents/${document.id}/fichier`}
                          aria-label={`Télécharger ${document.nomFichier}`}
                          onClick={(e) => e.stopPropagation()}
                          className="text-tiret-texte hover:text-orange"
                        >
                          <Download className="size-4" />
                        </a>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          )}

          {visibles.length > TAILLE_PAGE && (
            <div className="mt-2 flex items-center gap-2.5 text-xs text-sourdine">
              <span className="mr-auto tabular-nums">
                {pageBornee * TAILLE_PAGE + 1}–{Math.min((pageBornee + 1) * TAILLE_PAGE, visibles.length)} sur{' '}
                {visibles.length} documents
              </span>
              <button
                type="button"
                disabled={pageBornee === 0}
                onClick={() => setPage(pageBornee - 1)}
                aria-label="Page précédente"
                className="grid size-8 place-items-center rounded-lg border border-tiret bg-carte font-bold text-dore disabled:text-tiret"
              >
                ‹
              </button>
              <button
                type="button"
                disabled={pageBornee >= nbPages - 1}
                onClick={() => setPage(pageBornee + 1)}
                aria-label="Page suivante"
                className="grid size-8 place-items-center rounded-lg border border-tiret bg-carte font-bold text-dore disabled:text-tiret"
              >
                ›
              </button>
            </div>
          )}

          {/* Tiroir de détail */}
          {choisi !== null && fiche !== null && (
            <div className="absolute inset-y-0 right-0 z-10 flex w-[400px] flex-col gap-3 overflow-y-auto rounded-3xl bg-carte px-6 py-5 shadow-[-10px_0_28px_rgba(84,84,100,0.18)]">
              <div className="flex items-start gap-2">
                <h2 className="min-w-0 flex-1 text-xl font-bold leading-snug">{choisi.titre}</h2>
                <a
                  href={`/api/documents/${choisi.id}/fichier`}
                  aria-label={`Télécharger ${choisi.nomFichier}`}
                  className="mt-1 text-sourdine hover:text-orange"
                >
                  <Download className="size-5" />
                </a>
                <ConfirmerSuppression
                  key={choisiId ?? 'aucun'}
                  ariaLabel="Supprimer le document"
                  onConfirmer={() => supprimer.mutate()}
                />
                <button
                  type="button"
                  onClick={() => setChoisiId(null)}
                  aria-label="Fermer la fiche"
                  className="mt-0.5 text-sourdine hover:text-orange"
                >
                  <X className="size-5" />
                </button>
              </div>

              <div className="flex flex-wrap items-center gap-2 text-xs text-sourdine">
                {choisi.aClasser && (
                  <span className="rounded-full bg-orange px-2 py-0.5 text-[11px] font-bold text-carte">
                    À classer
                  </span>
                )}
                <span>
                  {libelleTypeFichier(choisi)} · {choisi.nomFichier} ·{' '}
                  {(choisi.taille / 1024 / 1024).toFixed(1).replace('.', ',')} Mo ·
                  ajouté le {dateLisible(dateLocaleIso(new Date(choisi.creeLe)))}
                </span>
              </div>

              {choisi.typeMime.startsWith('image/') && (
                <ApercuImage key={choisi.id} document={choisi} />
              )}
              {choisi.typeMime === TYPE_MIME_COURRIEL && <ApercuCourriel key={choisi.id} id={choisi.id} />}

              <div className="grid gap-3">
                <input
                  value={fiche.titre}
                  onChange={(e) => maj({ titre: e.target.value })}
                  placeholder="Titre"
                  aria-label="Titre"
                  className={cn(classeChamp, 'font-bold')}
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
                <input
                  value={fiche.dossier}
                  onChange={(e) => maj({ dossier: e.target.value })}
                  placeholder="Dossier (Maison, Impôts 2026…)"
                  aria-label="Dossier"
                  list="dossiers-existants"
                  maxLength={100}
                  className={classeChamp}
                />
                <datalist id="dossiers-existants">
                  {dossiers.map((dossier) => (
                    <option key={dossier} value={dossier} />
                  ))}
                </datalist>
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
                <label className="flex items-center gap-2 text-xs text-sourdine">
                  Expire le
                  <input
                    type="date"
                    value={fiche.echeance}
                    onChange={(e) => maj({ echeance: e.target.value })}
                    className={cn(classeChamp, 'flex-1')}
                  />
                </label>
                <textarea
                  value={fiche.notes}
                  onChange={(e) => maj({ notes: e.target.value })}
                  placeholder="Notes"
                  aria-label="Notes"
                  rows={3}
                  maxLength={2000}
                  className={cn(classeChamp, 'resize-y')}
                />
              </div>

              <div className="mt-auto flex justify-end gap-2 pt-1">
                {choisi.aClasser && (
                  <button
                    type="button"
                    disabled={fiche.titre.trim().length === 0 || classer.isPending}
                    onClick={() => classer.mutate()}
                    className="rounded-xl bg-orange px-5 py-2 text-sm font-bold text-carte disabled:opacity-40"
                  >
                    Classer
                  </button>
                )}
                <button
                  type="button"
                  disabled={fiche.titre.trim().length === 0 || enregistrer.isPending}
                  onClick={() => enregistrer.mutate()}
                  className={cn(
                    'rounded-xl px-5 py-2 text-sm font-bold disabled:opacity-40',
                    choisi.aClasser ? 'border border-tiret bg-carte text-dore' : 'bg-orange text-carte',
                  )}
                >
                  Enregistrer
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
