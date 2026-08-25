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
  assigneAId: string | null
  zoneId: string | null
  equipementId: string | null
  strategie: 'Fixe' | 'Alternance' | 'MoinsLAFait'
  recurrence: Recurrence
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
  notes: string | null
  dateDocument: string | null
  echeance: string | null
  nomFichier: string
  typeMime: string
  taille: number
  creeLe: string
}

export type DocumentDonnees = {
  titre: string
  categorie: CategorieDocument
  equipementId?: string | null
  zoneId?: string | null
  notes?: string | null
  dateDocument?: string | null
  echeance?: string | null
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

export type PhraseDuJour = {
  titre: string
  sousTitre: string
  source: 'Gabarit' | 'Llm'
  genereLe: string
}

export class ApiError extends Error {
  statut: number

  constructor(statut: number, message: string) {
    super(message)
    this.statut = statut
  }
}

async function requete<T>(url: string, options?: RequestInit): Promise<T> {
  const reponse = await fetch(url, {
    headers:
      options?.body && !(options.body instanceof FormData)
        ? { 'Content-Type': 'application/json' }
        : undefined,
    ...options,
  })
  if (reponse.status === 401) {
    throw new ApiError(401, 'Non authentifié')
  }
  if (reponse.ok) {
    return reponse.status === 204 ? (undefined as T) : reponse.json()
  }
  throw new ApiError(reponse.status, await messageErreur(reponse))
}

// Extrait le message d'un corps d'erreur : { message } (conflits), ProblemDetails
// avec errors (ValidationProblem) ou title. Repli sur le statut HTTP.
async function messageErreur(reponse: Response): Promise<string> {
  try {
    const corps = await reponse.json()
    const premiereValidation = corps.errors
      ? (Object.values(corps.errors as Record<string, string[]>)[0]?.[0] ?? null)
      : null
    return corps.message ?? premiereValidation ?? corps.title ?? `Erreur serveur (${reponse.status})`
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
  documents: (filtres?: { categorie?: CategorieDocument; equipementId?: string }) => {
    const params = new URLSearchParams()
    if (filtres?.categorie) params.set('categorie', filtres.categorie)
    if (filtres?.equipementId) params.set('equipementId', filtres.equipementId)
    const suffixe = params.size > 0 ? `?${params}` : ''
    return requete<Document[]>(`/api/documents${suffixe}`)
  },
  televerserDocument: (fichier: File, donnees?: Partial<DocumentDonnees>) => {
    const formulaire = new FormData()
    formulaire.append('fichier', fichier)
    for (const [champ, valeur] of Object.entries(donnees ?? {})) {
      if (valeur) formulaire.append(champ, valeur)
    }
    return requete<{ id: string }>('/api/documents', { method: 'POST', body: formulaire })
  },
  modifierDocument: (id: string, donnees: DocumentDonnees) =>
    requete<void>(`/api/documents/${id}`, { method: 'PUT', body: JSON.stringify(donnees) }),
  supprimerDocument: (id: string) =>
    requete<void>(`/api/documents/${id}`, { method: 'DELETE' }),

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

  monFluxIcal: () => requete<{ chemin: string }>('/api/ical/mon-flux'),

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
