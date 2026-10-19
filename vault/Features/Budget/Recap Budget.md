---
type: recap
date: 2026-08-26
feature: "[[Budget]]"
plan: "[[Plan 2026-08-26 Budget V1]]"
---

# Recap Budget

Budget v1 livré tel que planifié, avec un grilling pré-implémentation (7 forks,
dont [[D-2026-08-26 Ventilation Du Dépôt Multi-Enveloppes]]) et deux rondes de
maquettes ([[D-2026-08-26 Page Budget Flux Raffiné Et Bascule Flux Tracé]]).

- **Domaine** : 4 entités + `MoteurProvision` pur (mois restants = 1ᵉʳˢ du mois,
  plancher 1 ; taxes lissées à couverture chronologique ; rien de stocké).
- **API** : tranche `Features/Budget/` (résumé, compte, enveloppes, mouvements,
  transferts, liaison ventilée, ignorer, import) + `IFournisseurTransactions`
  (OFX FITID + CSV Desjardins positionnel). MCP : `gerer_budget` + `bilan_budget`
  sur les mêmes cœurs partagés.
- **Web** : page Budget bicéphale — Aperçu (E1 : chemin de l'argent, rythme
  mensuel, grille, inbox) ⇄ Flux (E2 : diagramme SVG calculé par
  `budget-vues.ts`), mode mémorisé ; ancrage comme premier écran ; fiche
  Équipement montre l'enveloppe liée.
- **Tests** : backend 347 → 398 (MoteurProvision, lecteurs, suggestion,
  intégration BudgetApi + garde 401), web 61 → 67. Parcours manuel complet
  (ancrage, tâche décennale, OFX ×2 sans doublon, ventilation, invariant en vrai
  décimal) vérifié le 2026-08-26.

Écarts au plan : la sélection d'une entrée de journal lors d'une liaison est
servie par l'API et le MCP mais pas encore par le web (aucun endpoint de liste
du journal — à faire si le besoin se présente) ; le lecteur CSV lit les
colonnes Desjardins positionnellement (retrait, dépôt, solde) faute d'un export
réel — à confronter au premier vrai fichier AccWeb.

## Ajout 2026-10-19 — saisie manuelle et enveloppes fermées

- Premier vrai relevé : le compte d'épargne AccèsD n'exporte **rien** (ni CSV ni
  OFX). Trois lignes sont entrées par un CSV écrit à la main ; le lecteur AccWeb
  positionnel les a lues correctement (retrait, dépôt, solde ; virgule décimale).
- D'où [[D-2026-10-19 Saisie Manuelle De Transaction]] : `POST
  /api/budget/transactions` et `gerer_budget` `saisir_transaction`, opération
  partagée `SaisirAsync` dans `ImportTransactionsEndpoints.cs` ; doublon exact
  refusé sauf `forcer`. Formulaire « Saisir une ligne » sous l'inbox.
- Les enveloppes fermées, jusque-là invisibles (la grille filtrait les actives),
  sont repliées sous la grille et s'ouvrent en lecture seule.
- Tests : +3 intégration (saisie, doublon/forcer, refus), +2 MCP, +2 web.
