---
type: feature
status: implemented
last-verified: 2026-08-25
verified-against: eb830ec
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
  (intégration) ET côté éditeur — avec les deux fixtures `TACHE_COMPLETE` et
  `TACHE_RECURRENTE` (une hebdo en saison : la ponctuelle seule ne peut pas
  attraper la perte d'une récurrence).
- **Auth** : test-garde 401 sur toutes les routes `/api` + liste explicite des
  routes anonymes (`GardeAuthTests`) ; `/mcp` répond 401 sans clé API.
- **Cycle de vie des occurrences** via HTTP : compléter → journal + prochaine
  occurrence ; annuler ; passer ; reporter (passé → 400) ; notes post-hoc ;
  **courses de complétion** tranchées par les index uniques
  ([[D-2026-08-25 Invariants D'occurrence En Base]]).
- **Fumée E2E** : login → créer une tâche → l'éditer sans perte → la compléter.

## Campagne cas de bord (2026-08-25)

Audit de couverture par balayage complet (rapport : artifact « Angles morts de
House OS ») → ~30 bogues latents corrigés puis figés par **+170 tests backend**
(339 au total) et **+26 tests frontend** (36) : moteur/fenêtres/rollover, cycle
de vie complet, outils MCP exécutés pour vrai, workers météo/flux (fixtures DST,
ICS en UTC), tranches Auth/Zones/iCal/Comptes/Documents/Équipements en
intégration, et `web/src/lib/` (format, humeur, erreurs API) en Vitest.

## Conventions

- Intégration : services d'arrière-plan (météo, humeur, flux externes, rollover)
  retirés de l'hôte de test ; deux comptes seedés par la config de test ; le
  cookie de session vient d'un vrai POST login ; `Fichiers:Chemin` pointe un
  dossier temporaire (les uploads ne touchent jamais le dépôt).
- Sqlite : les `DateTimeOffset` comparés ou triés en SQL passent par des
  converters binaires dans `ContexteSqlite` ; les FK et le jsonb ne sont fidèles
  qu'en intégration Postgres — choisir la couche en conséquence.
- `InternalsVisibleTo("HouseOs.Tests")` : les seams internes (rollover, créneaux
  humeur, stratégies) se testent directement.
- Frontend : MSW intercepte au niveau réseau — ne jamais mocker `@/lib/api` ;
  sous fake timers, préférer `fireEvent` à `userEvent`.
- E2E hors du `npm test` par défaut (exige la pile `demarrer`).

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
