---
type: plan
status: executed
date: 2026-08-25
feature: "[[Suite De Tests]]"
---

# Plan 2026-08-25 — Suite de tests

Mise en place des trois couches de [[D-2026-08-25 Stratégie De Tests Trois Couches]].

## Étapes

### Intégration API

- [x] Ajouter `Microsoft.AspNetCore.Mvc.Testing` + `Testcontainers.PostgreSql` à
      `HouseOs.Tests.csproj` ; marqueur `public partial class Program` dans
      `Program.cs`.
- [x] `Integration/HouseOsFactory.cs` : WebApplicationFactory sur Postgres
      Testcontainers (collection xunit partagée), retrait des IHostedService,
      seed de deux comptes, helper `ClientConnecte()` (login cookie réel).
- [x] `Integration/TachesApiTests.cs` : 401 sans cookie ; création ; GET détail
      retourne `echeance` ; **édition sans perte** ; cycle de vie occurrences
      (compléter/annuler/passer/reporter/notes) ; suppression.
- [x] `Integration/McpApiTests.cs` : `/mcp` 401 sans clé, 200 avec clé.

### Composants frontend

- [x] Installer vitest, @testing-library/react, @testing-library/user-event,
      @testing-library/jest-dom, jsdom, msw ; config vitest + setup MSW ;
      script `npm test`.
- [x] `TacheEditeur.test.tsx` : charge tous les champs du détail (échéance
      incluse) ; save sans modification → payload identique au détail ;
      `zoneInitialeId` préremplit la pièce ; Échap ferme.
- [x] `OccurrenceListe.test.tsx` : description multi-ligne visible (aussi en
      retard) ; compléter appelle l'API ; note post-hoc sur une rangée faite.
- [x] `QuickAdd.test.tsx` : le bouton et ⌘K ouvrent le modal complet.

### Fumée E2E

- [x] Installer @playwright/test + Chromium ; `web/e2e/` ; config baseURL 5173,
      hors de `npm test` (script `test:e2e`).
- [x] `fumee.spec.ts` : login → créer une tâche → éditer sans perte (l'échéance
      survit) → compléter → nettoyage.

### Clôture

- [x] Tout passe : `dotnet test`, `npm test`, `npm run test:e2e` (pile démarrée).
- [x] Recap + spec as-built + validation du vault.

## Vérification

Chaque couche exécutée localement dans la session ; l'invariant « édition sans
perte » doit être rouge si on réintroduit le bug d'échéance (vérifié en le
simulant mentalement contre le diff du fix `6a0c130`).
