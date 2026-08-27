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
