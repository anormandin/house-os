---
type: recap
date: 2026-08-24
feature: "[[Flux Externes]]"
plan: "[[Plan 2026-08-24 Flux Externes V1]]"
---

# Recap Flux Externes

Tranche livrée telle que planifiée :

- Domaine : `FluxExterne` (abonnement, état de santé) + `EvenementExterne`
  (normalisé, uid par occurrence) ; cascade à la suppression.
- `LectureIcs` pur sur Ical.Net v5 (récurrences RRULE expansées, all-day et
  datés, fenêtre) ; worker `FluxExternesRafraichissement` (démarrage + 6 h,
  hier → +60 jours, remplacement par flux en transaction, erreur notée sur le
  flux) ; CRUD + `GET /api/evenements-externes` ; création validée par un
  téléchargement inline.
- Web : bandeau des événements du jour au-dessus des tâches, fusion dans Cette
  semaine (italique + icône, non cochable), modal `FluxExternesGestion`
  (bouton calendrier dans l'en-tête de Cette semaine, suppression deux-clics).

Vérifié : 131 tests verts (5 nouveaux sur fixtures ICS), flux réel ajouté via
l'UI (jours fériés canadiens de Google — 3 événements sur 60 jours), bandeau et
fusion vérifiés dans le navigateur avec un ICS de test servi localement, puis
nettoyé.

Déviations :

1. Filtres d'exception des trois workers corrigés au passage : un timeout HTTP
   est une `OperationCanceledException` — filtrer sur le jeton, pas le type,
   sinon le `BackgroundService` meurt silencieusement (bug latent de
   [[Météo]]/[[Titre D'humeur]] corrigé aussi).
2. Suppression avec confirmation deux-clics (pas de `confirm()` navigateur).
