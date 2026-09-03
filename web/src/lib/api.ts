import { journaliser } from '@/lib/journal'

export type Utilisateur = {
  id: string
  nomUtilisateur: string
  nomAffichage: string
}

export type Occurrence = {
  id: string
  tacheId: string
  titre: string
  description: string | null
  echeance: string | null
  statut: 'EnAttente' | 'Completee' | 'Passee'
  assigneA: Utilisateur | null
  completeePar: Utilisateur | null
  completeeLe: string | null
  notes: string | null
  zoneId: string | null
  equipementId: string | null
  modeRecurrence: 'Ponctuelle' | 'Fixe' | 'Intervalle'
}

export type Recurrence = {
  mode: 'Ponctuelle' | 'Fixe' | 'Intervalle'
  fixeType?: 'JoursSemaine' | 'JourDuMois' | 'Annuelle' | null
  joursSemaine?: number[] | null
  jourDuMois?: number | null
  moisAnnuel?: number | null
  jourAnnuel?: number | null
  intervalleJours?: number | null
  fenetreDebutMois?: number | null
  fenetreDebutJour?: number | null
  fenetreFinMois?: number | null
  fenetreFinJour?: number | null
  rollover?: boolean | null
}

export type TacheDetail = {
  id: string
  titre: string
  description: string | null
  echeance: string | null
  assigneAId: string | null
  zoneId: string | null
  equipementId: string | null
  strategie: 'Fixe' | 'Alternance' | 'MoinsLAFait'
  recurrence: Recurrence
  documentIds: string[]
}

export type TacheResume = {
  id: string
  titre: string
  description: string | null
  /** Occurrence en attente — permet de compléter depuis la console. */
  occurrenceId: string | null
  echeance: string | null
  assigneA: Utilisateur | null
  zoneId: string | null
  equipementId: string | null
  strategie: 'Fixe' | 'Alternance' | 'MoinsLAFait'
  recurrence: Recurrence
  nbDocuments: number
  /** Ponctuelle sans occurrence en attente : déjà faite. */
  completee: boolean
}

export type TacheDonnees = {
  titre: string
  description?: string
  echeance?: string
  assigneAId?: string
  zoneId?: string
  equipementId?: string
  strategie?: string
  recurrence?: Recurrence
  /** Liste complète des documents liés — absente = liens conservés, [] = tout délier. */
  documentIds?: string[]
}

export type Zone = {
  id: string
  nom: string
  type: 'Interieur' | 'Exterieur'
  ordre: number
}

export type EquipementResume = {
  id: string
  nom: string
  zoneId: string | null
  marque: string | null
  modele: string | null
  finGarantie: string | null
  nbDocuments: number
}

export type CategorieDocument =
  | 'Manuel'
  | 'Photo'
  | 'Assurance'
  | 'Facture'
  | 'Garantie'
  | 'Contrat'
  | 'PlanPermis'
  | 'ImpotsTaxes'
  | 'Autre'

export type Document = {
  id: string
  titre: string
  categorie: CategorieDocument
  equipementId: string | null
  nomEquipement: string | null
  zoneId: string | null
  nomZone: string | null
  dossier: string | null
  notes: string | null
  dateDocument: string | null
  echeance: string | null
  nomFichier: string
  typeMime: string
  taille: number
  creeLe: string
  /** Arrivé tout seul (courriel) et pas encore confirmé par un humain. */
  aClasser: boolean
  importCourrielId: string | null
}

export type DocumentDonnees = {
  titre: string
  categorie: CategorieDocument
  equipementId?: string | null
  zoneId?: string | null
  dossier?: string | null
  notes?: string | null
  dateDocument?: string | null
  echeance?: string | null
  /** Absent = inchangé ; false = sortir de la boîte À classer. */
  aClasser?: boolean | null
}

/** Aperçu d'un document .eml — texte extrait côté serveur, jamais de HTML. */
export type CourrielDocument = {
  de: string
  a: string
  date: string
  sujet: string
  texte: string
  piecesJointes: { nomFichier: string; typeMime: string; taille: number }[]
}

export type RapportReleve = {
  actif: boolean
  nbCourriels: number
  nbDocuments: number
  nbIgnores: number
  erreurs: string[]
}

export type Entretien = {
  completeeLe: string
  utilisateur: string
  titreTache: string | null
  notes: string | null
}

export type EquipementDetail = {
  id: string
  nom: string
  zoneId: string | null
  marque: string | null
  modele: string | null
  numeroSerie: string | null
  dateAchat: string | null
  finGarantie: string | null
  notes: string | null
  specs: Record<string, string>
  documents: Document[]
  entretiens: Entretien[]
}

export type EquipementDonnees = {
  nom: string
  zoneId?: string | null
  marque?: string | null
  modele?: string | null
  numeroSerie?: string | null
  dateAchat?: string | null
  finGarantie?: string | null
  notes?: string | null
  specs?: Record<string, string>
}

export type IconeCompte =
  | 'Camion'
  | 'Sapin'
  | 'Avion'
  | 'Valise'
  | 'Gateau'
  | 'Cadeau'
  | 'Coeur'
  | 'Soleil'
  | 'Flocon'
  | 'Citrouille'
  | 'Feuille'
  | 'Fleur'
  | 'Tente'
  | 'Velo'
  | 'Ballon'
  | 'Etoile'

export type CompteARebours = {
  id: string
  titre: string
  dateCible: string
  icone: IconeCompte
}

export type CompteAReboursDonnees = {
  titre: string
  dateCible: string
  icone: IconeCompte
}

export type JourMeteo = {
  date: string
  tempMin: number
  tempMax: number
  precipitationMm: number
  probabilitePrecipitation: number
  codeMeteo: number
}

export type VerdictMeteo = {
  regle: string
  etat: 'Bon' | 'Passable' | 'Defavorable'
  raison: string
}

export type MeteoMaintenant = {
  temperatureC: number
  codeMeteo: number
}

export type FluxIcal = {
  chemin: string
  // URL Funnel du flux — absente quand l'exposition publique n'est pas configurée.
  urlPublique: string | null
}

export type Meteo = {
  misAJourLe: string | null
  maintenant: MeteoMaintenant | null
  jours: JourMeteo[]
  verdicts: VerdictMeteo[]
}

export type TypeFluxExterne = 'Collecte' | 'Ecole' | 'Autre'

export type FluxExterne = {
  id: string
  nom: string
  url: string
  type: TypeFluxExterne
  actif: boolean
  dernierRafraichissementLe: string | null
  derniereErreur: string | null
  nbEvenements: number
}

export type FluxExterneDonnees = {
  nom: string
  url: string
  type: TypeFluxExterne
}

export type EvenementExterne = {
  titre: string
  type: TypeFluxExterne
  date: string
  heure: string | null
}

export type TypeEnveloppe = 'Equipement' | 'Taxes' | 'Projet' | 'Reserve'
export type TypeMouvement = 'Provision' | 'Retrait' | 'Ajustement' | 'Transfert'

export type Versement = { date: string; montant: number }

export type CompteBudget = {
  id: string
  nom: string
  institution: string | null
  soldeInitial: number
  dateAncrage: string
  tacheVirementId: string | null
  titreTacheVirement: string | null
}

export type EnveloppeBudget = {
  id: string
  nom: string
  type: TypeEnveloppe
  montantCible: number | null
  dateCible: string | null
  /** Échéance retenue : occurrence de la tâche liée, prochain versement, ou date saisie. */
  dateEffective: string | null
  tacheId: string | null
  titreTache: string | null
  equipementId: string | null
  nomEquipement: string | null
  echeancier: Versement[] | null
  statut: 'Active' | 'Fermee'
  solde: number
  provision: number
  enRetard: boolean
  echeancierARenouveler: boolean
}

export type SortiePrevue = { date: string; nom: string; montant: number; enveloppeId: string }

export type ResumeBudget = {
  compte: CompteBudget | null
  soldeCourant: number
  totalEnveloppes: number
  nonAffecte: number
  virementSuggere: number
  occurrenceVirementId: string | null
  enveloppes: EnveloppeBudget[]
  sorties: SortiePrevue[]
  nbTransactionsNouvelles: number
}

export type TransactionBudget = {
  id: string
  date: string
  montant: number
  description: string
  statut: 'Nouvelle' | 'Liee' | 'Ignoree'
  suggestionEnveloppeId: string | null
  suggestionNom: string | null
  suggererVentilation: boolean
}

export type MouvementBudget = {
  id: string
  date: string
  montant: number
  type: TypeMouvement
  note: string | null
  transactionBancaireId: string | null
  descriptionTransaction: string | null
  entreeJournalId: string | null
  soldeApres: number
}

export type EnveloppeBudgetDetail = {
  enveloppe: EnveloppeBudget
  mouvements: MouvementBudget[]
}

export type RapportImport = { importees: number; doublons: number; anterieures: number }

export type CompteBudgetDonnees = {
  nom: string
  institution?: string | null
  soldeInitial: number
  dateAncrage: string
  tacheVirementId?: string | null
}

export type EnveloppeBudgetDonnees = {
  nom: string
  type: TypeEnveloppe
  montantCible?: number | null
  dateCible?: string | null
  tacheId?: string | null
  equipementId?: string | null
  echeancier?: Versement[] | null
}

export type LigneEcran = { titre: string; assigne: string | null; faite: boolean; enRetard: boolean }

/** Tout ce que la vue e-ink (/ecran) affiche, composé par le serveur. */
export type DonneesEcran = {
  date: string
  renduLe: string
  phrase: { titre: string; sousTitre: string } | null
  lignes: LigneEcran[]
  lignesEnPlus: number
  ouvertes: number
  enRetard: number
  faites: number
  meteo: {
    codeMeteo: number
    temperatureC: number
    tempMin: number
    tempMax: number
    probabilitePrecipitation: number
    verdicts: VerdictMeteo[]
  } | null
  evenementsDuJour: EvenementExterne[]
  prochaineCollecte: EvenementExterne | null
  prochainCompte: { titre: string; dateCible: string } | null
}

export type PhraseDuJour = {
  titre: string
  sousTitre: string
  source: 'Gabarit' | 'Llm'
  genereLe: string
}

export class ApiError extends Error {
  statut: number
  /** Identifiant de la requête serveur (en-tête X-Trace-Id) : à coller dans Seq. */
  traceId?: string

  constructor(statut: number, message: string, traceId?: string) {
    super(message)
    this.statut = statut
    this.traceId = traceId
  }
}

async function requete<T>(url: string, options?: RequestInit): Promise<T> {
  const methode = options?.method ?? 'GET'
  const depart = performance.now()
  let reponse: Response
  try {
    reponse = await fetch(url, {
      headers:
        options?.body && !(options.body instanceof FormData)
          ? { 'Content-Type': 'application/json' }
          : undefined,
      ...options,
    })
  } catch (erreur) {
    // Le cas « la réponse n'est jamais arrivée » : sans cette ligne, il ne restait
    // qu'une bannière et aucune trace de ce qui avait été tenté.
    journaliser('error', 'Api', `${methode} ${url} — panne réseau`, {
      dureeMs: Math.round(performance.now() - depart),
      detail: erreur instanceof Error ? erreur.message : String(erreur),
    })
    throw erreur
  }

  const traceId = reponse.headers.get('X-Trace-Id') ?? undefined
  const dureeMs = Math.round(performance.now() - depart)

  if (reponse.ok) {
    journaliser('debug', 'Api', `${methode} ${url} → ${reponse.status}`, { dureeMs, traceId })
    return reponse.status === 204 ? (undefined as T) : reponse.json()
  }
  if (reponse.status === 401) {
    journaliser('info', 'Api', `${methode} ${url} → 401`, { dureeMs, traceId })
    throw new ApiError(401, 'Non authentifié', traceId)
  }

  const message = await messageErreur(reponse)
  journaliser('warn', 'Api', `${methode} ${url} → ${reponse.status}`, {
    dureeMs,
    traceId,
    message,
  })
  throw new ApiError(reponse.status, message, traceId)
}

// Extrait le message d'un corps d'erreur : { message } (conflits), ProblemDetails
// avec errors (ValidationProblem) ou title. Repli sur le statut HTTP.
async function messageErreur(reponse: Response): Promise<string> {
  // Seules des chaînes non vides sortent d'ici : un message inattendu (objet, nombre)
  // finirait rendu tel quel comme enfant React dans la bannière → écran blanc.
  const texte = (v: unknown) => (typeof v === 'string' && v.length > 0 ? v : null)
  try {
    const corps = await reponse.json()
    const premiereValidation =
      corps && typeof corps === 'object' && corps.errors
        ? (Object.values(corps.errors as Record<string, string[]>)[0]?.[0] ?? null)
        : null
    return (
      texte(corps?.message) ??
      texte(premiereValidation) ??
      texte(corps?.title) ??
      `Erreur serveur (${reponse.status})`
    )
  } catch {
    return `Erreur serveur (${reponse.status})`
  }
}

export function dateLocaleIso(date = new Date()): string {
  const annee = date.getFullYear()
  const mois = String(date.getMonth() + 1).padStart(2, '0')
  const jour = String(date.getDate()).padStart(2, '0')
  return `${annee}-${mois}-${jour}`
}

export const api = {
  moi: () => requete<Utilisateur>('/api/auth/moi'),
  connexion: (nomUtilisateur: string, motDePasse: string) =>
    requete<Utilisateur>('/api/auth/connexion', {
      method: 'POST',
      body: JSON.stringify({ nomUtilisateur, motDePasse }),
    }),
  deconnexion: () => requete<void>('/api/auth/deconnexion', { method: 'POST' }),
  utilisateurs: () => requete<Utilisateur[]>('/api/utilisateurs'),

  occurrences: (filtre: string) =>
    requete<Occurrence[]>(`/api/occurrences?filtre=${filtre}&date=${dateLocaleIso()}`),
  // Occurrences complétées dans une fenêtre d'instants (bornes de la journée locale).
  occurrencesFaites: (de: string, a: string) =>
    requete<Occurrence[]>(
      `/api/occurrences?filtre=faites&de=${encodeURIComponent(de)}&a=${encodeURIComponent(a)}`,
    ),
  // Instants de complétion du journal dans [de, a) — le client agrège par semaine locale.
  bilanJournal: (de: string, a: string) =>
    requete<string[]>(
      `/api/journal/bilan?de=${encodeURIComponent(de)}&a=${encodeURIComponent(a)}`,
    ),

  taches: () => requete<TacheResume[]>('/api/taches'),
  tache: (id: string) => requete<TacheDetail>(`/api/taches/${id}`),
  creerTache: (donnees: TacheDonnees) =>
    requete<{ id: string }>('/api/taches', {
      method: 'POST',
      body: JSON.stringify(donnees),
    }),
  modifierTache: (id: string, donnees: TacheDonnees) =>
    requete<void>(`/api/taches/${id}`, {
      method: 'PUT',
      body: JSON.stringify(donnees),
    }),
  completer: (occurrenceId: string, notes?: string) =>
    requete<void>(`/api/occurrences/${occurrenceId}/completer`, {
      method: 'POST',
      body: JSON.stringify({ notes: notes ?? null }),
    }),
  annulerCompletion: (occurrenceId: string) =>
    requete<void>(`/api/occurrences/${occurrenceId}/annuler-completion`, { method: 'POST' }),
  passer: (occurrenceId: string) =>
    requete<void>(`/api/occurrences/${occurrenceId}/passer`, { method: 'POST' }),
  reporter: (occurrenceId: string, echeance: string) =>
    requete<void>(`/api/occurrences/${occurrenceId}/reporter`, {
      method: 'POST',
      body: JSON.stringify({ echeance }),
    }),
  modifierNotes: (occurrenceId: string, notes: string | null) =>
    requete<void>(`/api/occurrences/${occurrenceId}/notes`, {
      method: 'PUT',
      body: JSON.stringify({ notes }),
    }),
  supprimerTache: (tacheId: string) =>
    requete<void>(`/api/taches/${tacheId}`, { method: 'DELETE' }),

  zones: () => requete<Zone[]>('/api/zones'),
  creerZone: (donnees: { nom: string; type?: string; ordre?: number }) =>
    requete<Zone>('/api/zones', { method: 'POST', body: JSON.stringify(donnees) }),
  modifierZone: (id: string, donnees: { nom: string; type?: string; ordre?: number }) =>
    requete<void>(`/api/zones/${id}`, { method: 'PUT', body: JSON.stringify(donnees) }),
  supprimerZone: (id: string) => requete<void>(`/api/zones/${id}`, { method: 'DELETE' }),

  equipements: () => requete<EquipementResume[]>('/api/equipements'),
  equipement: (id: string) => requete<EquipementDetail>(`/api/equipements/${id}`),
  creerEquipement: (donnees: EquipementDonnees) =>
    requete<{ id: string }>('/api/equipements', {
      method: 'POST',
      body: JSON.stringify(donnees),
    }),
  modifierEquipement: (id: string, donnees: EquipementDonnees) =>
    requete<void>(`/api/equipements/${id}`, {
      method: 'PUT',
      body: JSON.stringify(donnees),
    }),
  supprimerEquipement: (id: string) =>
    requete<void>(`/api/equipements/${id}`, { method: 'DELETE' }),
  documents: (filtres?: { categorie?: CategorieDocument; equipementId?: string; aClasser?: boolean }) => {
    const params = new URLSearchParams()
    if (filtres?.categorie) params.set('categorie', filtres.categorie)
    if (filtres?.equipementId) params.set('equipementId', filtres.equipementId)
    if (filtres?.aClasser !== undefined) params.set('aClasser', String(filtres.aClasser))
    const suffixe = params.size > 0 ? `?${params}` : ''
    return requete<Document[]>(`/api/documents${suffixe}`)
  },
  televerserDocument: (fichier: File, donnees?: Partial<DocumentDonnees>) => {
    const formulaire = new FormData()
    formulaire.append('fichier', fichier)
    for (const [champ, valeur] of Object.entries(donnees ?? {})) {
      if (typeof valeur === 'string' && valeur) formulaire.append(champ, valeur)
    }
    return requete<{ id: string }>('/api/documents', { method: 'POST', body: formulaire })
  },
  modifierDocument: (id: string, donnees: DocumentDonnees) =>
    requete<void>(`/api/documents/${id}`, { method: 'PUT', body: JSON.stringify(donnees) }),
  supprimerDocument: (id: string) =>
    requete<void>(`/api/documents/${id}`, { method: 'DELETE' }),
  courrielDocument: (id: string) => requete<CourrielDocument>(`/api/documents/${id}/courriel`),
  releverCourriels: () =>
    requete<RapportReleve>('/api/documents/relever-courriels', { method: 'POST' }),

  comptesARebours: () => requete<CompteARebours[]>('/api/comptes-a-rebours'),
  creerCompteARebours: (donnees: CompteAReboursDonnees) =>
    requete<CompteARebours>('/api/comptes-a-rebours', {
      method: 'POST',
      body: JSON.stringify(donnees),
    }),
  modifierCompteARebours: (id: string, donnees: CompteAReboursDonnees) =>
    requete<void>(`/api/comptes-a-rebours/${id}`, {
      method: 'PUT',
      body: JSON.stringify(donnees),
    }),
  supprimerCompteARebours: (id: string) =>
    requete<void>(`/api/comptes-a-rebours/${id}`, { method: 'DELETE' }),

  budget: () => requete<ResumeBudget>(`/api/budget?date=${dateLocaleIso()}`),
  ancrerCompteBudget: (donnees: CompteBudgetDonnees) =>
    requete<{ id: string }>('/api/budget/compte', {
      method: 'POST',
      body: JSON.stringify(donnees),
    }),
  modifierCompteBudget: (donnees: CompteBudgetDonnees) =>
    requete<void>('/api/budget/compte', { method: 'PUT', body: JSON.stringify(donnees) }),
  creerEnveloppe: (donnees: EnveloppeBudgetDonnees) =>
    requete<{ id: string }>('/api/budget/enveloppes', {
      method: 'POST',
      body: JSON.stringify(donnees),
    }),
  modifierEnveloppe: (id: string, donnees: EnveloppeBudgetDonnees) =>
    requete<void>(`/api/budget/enveloppes/${id}`, {
      method: 'PUT',
      body: JSON.stringify(donnees),
    }),
  enveloppeDetail: (id: string) =>
    requete<EnveloppeBudgetDetail>(`/api/budget/enveloppes/${id}?date=${dateLocaleIso()}`),
  fermerEnveloppe: (id: string) =>
    requete<void>(`/api/budget/enveloppes/${id}/fermer`, { method: 'POST' }),
  ajouterMouvement: (
    enveloppeId: string,
    donnees: { type: TypeMouvement; montant: number; date?: string; note?: string },
  ) =>
    requete<void>(`/api/budget/enveloppes/${enveloppeId}/mouvements`, {
      method: 'POST',
      body: JSON.stringify(donnees),
    }),
  transferer: (donnees: {
    deEnveloppeId: string
    versEnveloppeId: string
    montant: number
    note?: string
  }) =>
    requete<void>('/api/budget/transferts', { method: 'POST', body: JSON.stringify(donnees) }),
  transactionsBudget: (statut: 'Nouvelle' | 'Liee' | 'Ignoree' = 'Nouvelle') =>
    requete<TransactionBudget[]>(
      `/api/budget/transactions?statut=${statut}&date=${dateLocaleIso()}`,
    ),
  lierTransaction: (
    id: string,
    ventilation: { enveloppeId: string; montant: number }[],
    entreeJournalId?: string,
  ) =>
    requete<void>(`/api/budget/transactions/${id}/lier`, {
      method: 'POST',
      body: JSON.stringify({ ventilation, entreeJournalId: entreeJournalId ?? null }),
    }),
  ignorerTransaction: (id: string) =>
    requete<void>(`/api/budget/transactions/${id}/ignorer`, { method: 'POST' }),
  importerReleve: (fichier: File) => {
    const formulaire = new FormData()
    formulaire.append('fichier', fichier)
    return requete<RapportImport>('/api/budget/import', { method: 'POST', body: formulaire })
  },

  monFluxIcal: () => requete<FluxIcal>('/api/ical/mon-flux'),
  rotationFluxIcal: () => requete<FluxIcal>('/api/ical/rotation', { method: 'POST' }),

  meteo: () => requete<Meteo>('/api/meteo'),

  fluxExternes: () => requete<FluxExterne[]>('/api/flux-externes'),
  creerFluxExterne: (donnees: FluxExterneDonnees) =>
    requete<FluxExterne>('/api/flux-externes', {
      method: 'POST',
      body: JSON.stringify(donnees),
    }),
  modifierFluxExterne: (id: string, donnees: FluxExterneDonnees) =>
    requete<void>(`/api/flux-externes/${id}`, {
      method: 'PUT',
      body: JSON.stringify(donnees),
    }),
  supprimerFluxExterne: (id: string) =>
    requete<void>(`/api/flux-externes/${id}`, { method: 'DELETE' }),
  evenementsExternes: (jours = 7) =>
    requete<EvenementExterne[]>(`/api/evenements-externes?jours=${jours}`),

  // null quand aucune phrase n'est encore matérialisée : le client retombe sur
  // sa banque locale (humeur.ts).
  donneesEcran: () => requete<DonneesEcran>('/api/affichage/donnees'),

  phraseDuJour: async (): Promise<PhraseDuJour | null> => {
    try {
      return await requete<PhraseDuJour>('/api/phrase-du-jour')
    } catch (erreur) {
      if (erreur instanceof ApiError && erreur.statut === 404) {
        return null
      }
      throw erreur
    }
  },
}
