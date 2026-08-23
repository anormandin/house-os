---
type: plan
status: executed
date: 2026-08-23
feature: "[[Tâches]]"
---

# Plan 2026-08-23 V0 Déménagement

## But

Livrer la V0 : tâches ponctuelles seulement (créer / assigner / échéance / compléter),
vue Aujourd'hui, quick-add, login 2 comptes (Alain, Ariane), PWA française — le tout
démarrable en Docker Compose. Les tâches du déménagement deviennent les premières
vraies données. Le modèle porte déjà la spec de récurrence (mode `none`) pour que la
V1 n'ait rien à retrofitter.

## Étapes

- [x] Domaine : `Utilisateur`, `Tache` (mode + rollover portés directement sur la
      tâche ; les paramètres fixe/intervalle arriveront avec la migration V1),
      `Occurrence`, `EntreeJournal` — `server/HouseOs.Api/Domaine/`
- [x] `HouseOsDbContext` + migration EF `Initiale` (Npgsql, port hôte 5433 en dev) —
      `server/HouseOs.Api/Infrastructure/`
- [x] Seed au démarrage : 2 comptes depuis la config (`Seed:Utilisateurs`), hash PBKDF2
- [x] Auth cookie : `POST /api/auth/connexion`, `POST /api/auth/deconnexion`,
      `GET /api/auth/moi` ; le reste de l'API exige l'authentification —
      `Features/Auth/`
- [x] Tranche Tâches : `POST /api/taches` (quick-add : titre, échéance?, assigné?),
      `GET /api/occurrences` (filtres aujourdhui / avenir / en-attente / completees),
      `POST /api/occurrences/{id}/completer`, `DELETE /api/taches/{id}`,
      `GET /api/utilisateurs` — `Features/Taches/`
- [x] Compléter = statut + entrée de journal (qui/quand) ; une ponctuelle ne génère
      pas d'occurrence suivante — 7 tests unitaires dans `server/HouseOs.Tests/Domaine/`
- [x] Front : react-router + composants shadcn (thème Kanagawa clair/sombre) ; pages
      Connexion, Aujourd'hui, Tâches ; quick-add ; état auth via `/api/auth/moi` —
      `web/src/`
- [x] Dockerfile multi-étages (build web → wwwroot du serveur .NET, un seul runtime)
      + service `app` dans `docker-compose.yml` (port 8080)
- [x] Vérification de bout en bout (ci-dessous) — API via curl sur le conteneur ;
      vérification visuelle de l'UI laissée à l'utilisateur (extension Chrome non
      connectée pendant la session)

## Vérification

- `dotnet test` vert (complétion, journal, pas de récurrence pour une ponctuelle).
- Parcours manuel : login Ariane → quick-add « Changement d'adresse » échéance demain →
  visible dans Tâches ; échéance aujourd'hui → visible dans Aujourd'hui → compléter →
  journal avec qui/quand.
- `docker compose up` : app + postgres démarrent ; PWA servie par le conteneur app.
