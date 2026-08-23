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
  statut: 'EnAttente' | 'Completee'
  assigneA: Utilisateur | null
  completeePar: Utilisateur | null
  completeeLe: string | null
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
  nbPiecesJointes: number
}

export type PieceJointe = {
  id: string
  nomFichier: string
  typeMime: string
  taille: number
  creeLe: string
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
  piecesJointes: PieceJointe[]
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
  throw new ApiError(reponse.status, `Erreur serveur (${reponse.status})`)
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
  televerserPieceJointe: (equipementId: string, fichier: File) => {
    const donnees = new FormData()
    donnees.append('fichier', fichier)
    return requete<PieceJointe>(`/api/equipements/${equipementId}/pieces-jointes`, {
      method: 'POST',
      body: donnees,
    })
  },
  supprimerPieceJointe: (id: string) =>
    requete<void>(`/api/pieces-jointes/${id}`, { method: 'DELETE' }),

  monFluxIcal: () => requete<{ chemin: string }>('/api/ical/mon-flux'),
}
