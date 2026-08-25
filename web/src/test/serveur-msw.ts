import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import type { TacheDetail, Utilisateur, Zone } from '@/lib/api'

export const ALAIN: Utilisateur = {
  id: 'u-alain',
  nomUtilisateur: 'alain',
  nomAffichage: 'Alain',
}
export const ARIANE: Utilisateur = {
  id: 'u-ariane',
  nomUtilisateur: 'ariane',
  nomAffichage: 'Ariane',
}
export const ZONES: Zone[] = [
  { id: 'z-bureau', nom: 'Bureau', type: 'Interieur', ordre: 0 },
  { id: 'z-cuisine', nom: 'Cuisine', type: 'Interieur', ordre: 1 },
]

/** Détail complet — chaque champ non nul pour attraper un champ oublié au chargement. */
export const TACHE_COMPLETE: TacheDetail = {
  id: 't-1',
  titre: 'SAAQ — changement d’adresse',
  description: 'Ligne 1\nLigne 2',
  echeance: '2026-08-30',
  assigneAId: ALAIN.id,
  zoneId: ZONES[1].id,
  equipementId: null,
  strategie: 'Fixe',
  recurrence: { mode: 'Ponctuelle' },
}

export const serveur = setupServer(
  http.get('/api/utilisateurs', () => HttpResponse.json([ALAIN, ARIANE])),
  http.get('/api/zones', () => HttpResponse.json(ZONES)),
  http.get('/api/equipements', () => HttpResponse.json([])),
  http.get('/api/taches/:id', () => HttpResponse.json(TACHE_COMPLETE)),
)
