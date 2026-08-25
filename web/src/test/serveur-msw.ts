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

/** Détail récurrent — chaque champ de la récurrence non neutre : le test « rien ne
 * se perd » ne peut pas attraper la perte d'une hebdo avec la seule ponctuelle. */
export const TACHE_RECURRENTE: TacheDetail = {
  id: 't-2',
  titre: 'Sortir le recyclage',
  description: 'Bac bleu au chemin',
  echeance: '2026-09-02',
  assigneAId: ARIANE.id,
  zoneId: ZONES[0].id,
  equipementId: null,
  strategie: 'Alternance',
  recurrence: {
    mode: 'Fixe',
    fixeType: 'JoursSemaine',
    joursSemaine: [1, 3, 5],
    fenetreDebutMois: 5,
    fenetreDebutJour: 1,
    fenetreFinMois: 10,
    fenetreFinJour: 31,
    rollover: true,
  },
}

export const serveur = setupServer(
  http.get('/api/utilisateurs', () => HttpResponse.json([ALAIN, ARIANE])),
  http.get('/api/zones', () => HttpResponse.json(ZONES)),
  http.get('/api/equipements', () => HttpResponse.json([])),
  http.get('/api/taches/:id', ({ params }) =>
    HttpResponse.json(params.id === TACHE_RECURRENTE.id ? TACHE_RECURRENTE : TACHE_COMPLETE)),
)
