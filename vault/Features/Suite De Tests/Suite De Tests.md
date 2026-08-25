---
type: feature
status: implemented
last-verified: 2026-08-25
verified-against: ccaeced
---

# Suite de tests

Filet de régression trois couches de House OS — gouverné par
[[D-2026-08-25 Stratégie De Tests Trois Couches]]. Objectif : chaque classe de bug
déjà rencontrée a une couche qui l'attrape avant le push-and-release.

## Les trois couches

| Couche | Où | Outil | Commande |
|---|---|---|---|
| Domaine + opérations (préexistante) | `server/HouseOs.Tests/{Domaine,Features}` | xunit + SQLite in-memory | `dotnet test` |
| Intégration API | `server/HouseOs.Tests/Integration/` | WebApplicationFactory + Testcontainers Postgres | `dotnet test` (Docker requis) |
| Composants frontend | `web/src/**/*.test.tsx` | Vitest + Testing Library + MSW | `npm test` (dans `web/`) |
| Fumée E2E | `web/e2e/` | Playwright (Chromium) | `npm run test:e2e` (pile dev démarrée) |

## Invariants clés (les tests qui portent le sens)

- **Édition sans perte** : GET le détail d'une tâche, renvoyer tel quel en PUT —
  aucune donnée ne change (échéance, assigné, zone, récurrence). Testé côté API
  (intégration) ET côté éditeur (le formulaire charge tous les champs, le payload
  d'un save sans modification est identique au détail chargé).
- **Auth** : toute l'API répond 401 sans cookie ; `/mcp` répond 401 sans clé API.
- **Cycle de vie des occurrences** via HTTP : compléter → journal + prochaine
  occurrence ; annuler ; passer ; reporter (passé → 400) ; notes post-hoc.
- **Fumée E2E** : login → créer une tâche → l'éditer sans perte → la compléter.

## Conventions

- Intégration : services d'arrière-plan (météo, humeur, flux externes, rollover)
  retirés de l'hôte de test ; deux comptes seedés par la config de test ; le
  cookie de session vient d'un vrai POST login.
- Frontend : MSW intercepte au niveau réseau — ne jamais mocker `@/lib/api`.
- E2E hors du `npm test` par défaut (exige la pile `demarrer`).

## Décisions liées

- [[D-2026-08-25 Stratégie De Tests Trois Couches]]

## Plans

- [[Plan 2026-08-25 Suite De Tests]] — exécuté, voir [[Recap Suite De Tests]]
