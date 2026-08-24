---
type: plan
status: executed
date: 2026-08-24
feature: "[[Tâches]]"
---

# Plan 2026-08-24 — Cycle de vie des occurrences

Ajouter les actions manquantes du cycle de vie : annuler une complétion, confirmer
avant suppression, passer une occurrence, reporter une échéance, notes post-hoc,
erreurs API visibles. Décisions de sémantique dans
[[D-2026-08-24 Annulation Et Passage D'occurrences]].

## Étapes

- [x] Domaine (`server/HouseOs.Api/Domaine/Occurrence.cs`) : statut `Passee` +
  `PasseeLe`, méthodes `AnnulerCompletion()`, `Passer()`, `Reporter()` + tests domaine.
- [x] Migration EF `PasseeSurOccurrence` (colonne `PasseeLe` seulement — statut
  stocké en string).
- [x] `OperationsTaches` : `AnnulerCompletionAsync` (garde dernière-complétion via le
  journal, suppression de la suivante matérialisée), `PasserAsync` (ponctuelle
  refusée, même stratégie d'assignation que la complétion), `ReporterAsync` (date
  passée refusée), `AjouterNotesAsync` (post-hoc via le journal), `Notes` dans
  `OccurrenceDto`.
- [x] REST : `POST /api/occurrences/{id}/annuler-completion` · `/passer` ·
  `/reporter` · `PUT /api/occurrences/{id}/notes` (conventions 204/404/409/400).
- [x] MCP : outil action-dispatch `gerer_occurrence` (annuler-completion / passer /
  reporter), description de `lister_occurrences` mise à jour.
- [x] Frontend : parsing ProblemDetails dans `api.ts` ; bannière d'erreurs globale
  (`MutationCache.onError`, 401 exclus) ; composant `ConfirmerSuppression` (idiome
  « Vraiment ? ») dans OccurrenceListe, TacheEditeur, Pieces, Equipements ; rangée
  verte : « Annuler » + note post-hoc ; rangée en attente : Reporter (préréglages)
  + Passer (récurrentes seulement).
- [x] Tests d'orchestration `OperationsTachesTests` sur harnais Sqlite in-memory
  (différer si le converter `jsonb` résiste).
- [x] Vault : spec [[Tâches]] as-built + Recap.

## Vérification

`dotnet test` ; parcours manuel via le skill `demarrer` (annuler → suivante
supprimée ; annuler une non-dernière → 409 visible en bannière ; passer → trace
`Passee` + suivante générée ; reporter ; « Vraiment ? » avant suppression) ; outils
MCP `gerer_occurrence`.
