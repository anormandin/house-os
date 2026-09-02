import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import type { Document, TacheDetail, Utilisateur, Zone } from '@/lib/api'

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

/** Document liable dans l'éditeur de tâche. */
export const DOCUMENT_REFERENCE: Document = {
  id: 'd-rapport',
  titre: 'Rapport d’inspection',
  categorie: 'Autre',
  equipementId: null,
  nomEquipement: null,
  zoneId: null,
  nomZone: null,
  dossier: '17 rue de la Colline',
  notes: null,
  dateDocument: '2026-03-11',
  echeance: null,
  nomFichier: 'rapport.pdf',
  typeMime: 'application/pdf',
  taille: 1024,
  creeLe: '2026-08-01T12:00:00Z',
  aClasser: false,
  importCourrielId: null,
}

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
  documentIds: [DOCUMENT_REFERENCE.id],
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
  documentIds: [],
}

/** Résumé Budget neutre (aucun compte ancré) — les tests le remplacent au besoin. */
export const BUDGET_VIDE = {
  compte: null,
  soldeCourant: 0,
  totalEnveloppes: 0,
  nonAffecte: 0,
  virementSuggere: 0,
  occurrenceVirementId: null,
  enveloppes: [],
  sorties: [],
  nbTransactionsNouvelles: 0,
}

export const FLUX_ICAL = {
  chemin: '/ical/jeton-initial.ics',
  urlPublique: 'https://houseos.tailnet.ts.net/ical/jeton-initial.ics',
}

export const FLUX_ICAL_TOURNE = {
  chemin: '/ical/jeton-neuf.ics',
  urlPublique: 'https://houseos.tailnet.ts.net/ical/jeton-neuf.ics',
}

export const serveur = setupServer(
  // Le journal de session vide son tampon depuis n'importe quel test dès qu'il
  // atteint son seuil ; sans ce handler, onUnhandledRequest: 'error' ferait
  // échouer un test au hasard, très loin de sa cause.
  http.post('/api/journal-client', () => new HttpResponse(null, { status: 202 })),
  http.get('/api/utilisateurs', () => HttpResponse.json([ALAIN, ARIANE])),
  http.get('/api/ical/mon-flux', () => HttpResponse.json(FLUX_ICAL)),
  http.post('/api/ical/rotation', () => HttpResponse.json(FLUX_ICAL_TOURNE)),
  http.get('/api/budget', () => HttpResponse.json(BUDGET_VIDE)),
  http.get('/api/budget/transactions', () => HttpResponse.json([])),
  http.get('/api/zones', () => HttpResponse.json(ZONES)),
  http.get('/api/equipements', () => HttpResponse.json([])),
  http.get('/api/documents', () => HttpResponse.json([DOCUMENT_REFERENCE])),
  http.get('/api/taches/:id', ({ params }) =>
    HttpResponse.json(params.id === TACHE_RECURRENTE.id ? TACHE_RECURRENTE : TACHE_COMPLETE)),
)
