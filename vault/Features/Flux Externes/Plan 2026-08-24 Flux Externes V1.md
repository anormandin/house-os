---
type: plan
status: executed
date: 2026-08-24
feature: "[[Flux Externes]]"
---

# Plan 2026-08-24 Flux Externes V1

Abonnements ICS gérés dans l'app → événements normalisés → bandeau du jour +
Cette semaine. Gouverné par [[D-2026-08-24 Flux ICS Dans L'app Affichage Seul]]
et [[D-2026-08-24 Tables Flux Externes]].

## Étapes

### Domaine

- [x] `Domaine/FluxExterne.cs` — abonnement : nom, URL, type
      (`Collecte`/`Ecole`/`Autre`), actif, dernier rafraîchissement, dernière
      erreur. `Domaine/EvenementExterne.cs` — événement normalisé : flux, uid,
      titre, date, heure (null = toute la journée).

### Infrastructure

- [x] DbSets + config (cascade flux → événements, index par date) ; migration EF
      `AjouterFluxExternes`.

### Tranche `Features/FluxExternes/`

- [x] `LectureIcs` — pur : bytes ICS → événements normalisés sur une fenêtre
      (Ical.Net, récurrences expansées, événements toute-la-journée et datés) ;
      testé sur fixtures (collecte hebdo RRULE, événement ponctuel, all-day).
- [x] `FluxExternesRafraichissement` (`BackgroundService`) — démarrage puis
      toutes les 6 h : chaque flux actif → télécharger, normaliser (hier → +60 j),
      remplacer en transaction ; échec → `DerniereErreur` sur le flux, événements
      conservés.
- [x] `FluxExternesEndpoints` — CRUD `/api/flux-externes` (la création télécharge
      une fois pour valider l'URL et amorcer les événements ; erreur → 422 clair) ;
      `GET /api/evenements-externes?jours=N` (défaut 7) depuis les tables.

### Web (Aujourd'hui)

- [x] `api.ts` — types + appels flux/événements.
- [x] Bandeau du jour au-dessus de la liste de tâches (« Recyclage aujourd'hui »,
      icône par type, non cochable).
- [x] Cette semaine : fusion tâches + événements triés par date, style distinct
      pour les événements.
- [x] `FluxExternesGestion` — modal (patron ComptesAReboursGestion) : liste avec
      état (dernier rafraîchissement / erreur), ajout nom + URL + type,
      suppression avec confirmation.

### Vérification et clôture

- [x] `dotnet test` vert ; flux réel ajouté via l'UI (Chrome connecté — sinon
      STOP et demander), événements visibles dans Aujourd'hui et Cette semaine.
- [x] Vault : spec as-built, Recap, stamps, validation.
