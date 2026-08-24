---
type: recap
date: 2026-08-24
feature: "[[Tâches]]"
plan: "[[Plan 2026-08-24 Cycle De Vie Des Occurrences]]"
---

# Recap — Cycle de vie des occurrences

Construit tel que planifié : annuler une complétion (journal effacé, suivante
supprimée, garde « dernière complétion + suivante intacte »), statut `Passee` daté,
reporter, notes post-hoc sur la rangée verte, composant partagé
`ConfirmerSuppression` (« Vraiment ? », remplace les copies de Pièces/Équipements),
bannière d'erreurs globale (`MutationCache.onError` + parsing ProblemDetails),
outil MCP `gerer_occurrence`. Vérifié par 82 tests + parcours REST/MCP/UI complet.

Déviations du plan :

- La garde « dernière complétion » est évaluée **côté client** (liste des
  horodatages du journal de la tâche) plutôt qu'en SQL : Sqlite (harnais de tests)
  ne traduit ni `ORDER BY` ni les comparaisons sur `DateTimeOffset`, et le journal
  d'une tâche reste minuscule pour deux personnes.
- `Occurrence.Completer` refuse maintenant tout statut non-`EnAttente` (« Occurrence
  déjà traitée ») pour couvrir aussi `Passee`.
- Le harnais Sqlite (envisagé « à différer au besoin ») a été livré :
  `ContexteSqlite.cs` re-mappe la seule colonne `jsonb` (`Equipement.Specs`) en TEXT.
- Besoin noté pour la phase 2 : documents liés à une tâche (guide « fermer le spa »),
  consigné dans [[Tâches]] (hors périmètre).
