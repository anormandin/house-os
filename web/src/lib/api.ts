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
    headers: options?.body ? { 'Content-Type': 'application/json' } : undefined,
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
  creerTache: (donnees: {
    titre: string
    description?: string
    echeance?: string
    assigneAId?: string
  }) =>
    requete<{ id: string }>('/api/taches', {
      method: 'POST',
      body: JSON.stringify(donnees),
    }),
  completer: (occurrenceId: string, notes?: string) =>
    requete<void>(`/api/occurrences/${occurrenceId}/completer`, {
      method: 'POST',
      body: JSON.stringify({ notes: notes ?? null }),
    }),
  supprimerTache: (tacheId: string) =>
    requete<void>(`/api/taches/${tacheId}`, { method: 'DELETE' }),
}
