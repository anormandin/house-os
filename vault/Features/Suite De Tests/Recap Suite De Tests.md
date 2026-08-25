---
type: recap
date: 2026-08-25
feature: "[[Suite De Tests]]"
plan: "[[Plan 2026-08-25 Suite De Tests]]"
---

# Recap Suite de tests

Livré tel que planifié, tout vert dans la session :

- **Intégration API** (11 tests) : `WebApplicationFactory<Program>` sur Postgres
  Testcontainers (`postgres:17-alpine`, un conteneur partagé par collection),
  services d'arrière-plan retirés, deux comptes seedés, login cookie réel.
  Couvre : 401 sans cookie, détail avec `echeance`, **édition sans perte**,
  compléter/annuler/récurrence-matérialise-la-prochaine, passer (400 sur
  ponctuelle), reporter (400 dans le passé), notes post-hoc (409 avant
  complétion), suppression en cascade, `/mcp` 401 sans clé / ping 200 avec.
- **Composants frontend** (10 tests) : Vitest + Testing Library + MSW
  (`web/src/test/`). Couvre : l'éditeur charge tous les champs (échéance
  incluse) et un save sans modification renvoie le détail tel quel ;
  `zoneInitialeId` ; Échap ; quick-add et ⌘K ouvrent le modal complet ;
  description multi-ligne visible même en retard ; complétion ; note post-hoc ;
  clic-pour-modifier.
- **Fumée E2E** (1 parcours Playwright/Chromium, `web/e2e/`) : login → créer →
  éditer sans perte → compléter → annuler → supprimer, contre la pile dev
  réelle (`npm run test:e2e`, hors du `npm test`).

Déviations : aucune de fond. Détail découvert : `/mcp` répond 401 (pas 400) à un
corps non-MCP même avec la bonne clé — le test de clé valide utilise donc un vrai
ping JSON-RPC. Totaux : 169 tests backend, 10 frontend, 1 E2E.
