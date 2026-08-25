---
type: decision
status: accepted
date: 2026-08-25
feature: "[[Suite De Tests]]"
tags: []
---

# Stratégie de tests trois couches

## Contexte

Les 158 tests existants couvrent le domaine (moteur de récurrence, opérations sur
SQLite in-memory) mais **aucune couture** : zéro test de endpoints HTTP, zéro test
frontend, zéro test bout-en-bout. Les bugs récents vivaient exactement dans ces
coutures — l'éditeur qui effaçait l'échéance parce que le DTO de détail ne la
retournait pas ([[Tâches]], as-built 2026-08-25) n'était détectable par aucun test
existant. Alain demande une suite de tests complète.

## Options considérées

1. **Étendre seulement les tests d'opérations SQLite** — rapide, mais rate les
   contrats HTTP (DTO, auth, ProblemDetails) et tout le frontend.
2. **Trois couches : intégration API (WebApplicationFactory + Testcontainers
   Postgres) + composants frontend (Vitest, Testing Library, MSW) + fumée E2E
   (Playwright)** — chaque classe de bug récente a une couche qui l'attrape.
3. **Tout miser sur l'E2E** — couvre large mais lent, fragile, et localise mal
   les régressions.

## Décision

Option 2 — trois couches, chacune avec un rôle précis :

- **Intégration API** (`server/HouseOs.Tests/Integration/`) : vraie pile HTTP via
  `WebApplicationFactory<Program>` sur un **Postgres Testcontainers** (vraies
  migrations, vrai jsonb) ; services d'arrière-plan (météo, humeur, flux, rollover)
  retirés ; deux comptes seedés, login cookie réel. Invariant clé : **l'édition
  sans modification est sans perte** (GET détail → PUT identique → rien ne change).
- **Composants frontend** (`web/src/**/*.test.tsx`) : Vitest + Testing Library +
  MSW (mock au niveau réseau, jamais des modules). Invariant clé : le formulaire
  d'édition charge **tous** les champs que le PUT remplace (leçon du bug d'échéance
  du 2026-08-25).
- **Fumée E2E** (`web/e2e/`) : Playwright/Chromium contre la pile dev réelle
  (Postgres 5433 + API 5000 + Vite 5173) — login, créer, éditer-sans-perte,
  compléter. Hors du `npm test` par défaut (exige la pile démarrée).

## Conséquences

- `dotnet test` exige Docker en marche (Testcontainers) — déjà le cas en dev
  (Postgres compose). Les tests domaine restent sur SQLite (rapides).
- Chaque bug corrigé ajoute d'abord un test rouge dans la couche qui l'aurait
  attrapé — la suite est le filet de régression du push-and-release.
- Le frontend gagne une dépendance de test (Vitest/MSW) mais aucune en production.

## Confirmation

- `ls server/HouseOs.Tests/Integration` — la couche intégration existe.
- `rg "vitest" web/package.json` — la couche composants existe.
- `ls web/e2e` — la couche fumée existe.
- `rg "EditionSansModification_EstSansPerte" server` — l'invariant clé est testé.
