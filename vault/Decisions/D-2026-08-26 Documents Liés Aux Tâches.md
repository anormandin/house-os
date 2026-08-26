---
type: decision
status: accepted
date: 2026-08-26
feature: "[[Tâches]]"
tags: [schema]
---

# Documents liés aux tâches

## Contexte

Les tâches issues du rapport d'inspection référencent le document source en texte
libre (« voir Rapport d'inspection, p. 51 ») — aucun lien navigable. Alain veut un
vrai lien Tâche → Document, et **plusieurs documents par tâche** (demande explicite
2026-08-26 : « we can have more than one doc on a task »).

## Options considérées

1. **FK simple `DocumentId` sur Tache** — un seul document ; rejetée par Alain.
2. **Table de jointure `TacheDocuments`** (many-to-many, navigation skip EF).
3. Miroir inverse (FK `TacheId` sur Document) — un document sert plusieurs tâches
   (le rapport d'inspection ↔ 9 tâches), impossible.

## Décision

Option 2 : jointure `TacheDocuments` (TacheId, DocumentId), cascade des deux côtés
sur la **jointure seulement** — supprimer une tâche ou un document ne supprime
jamais l'autre entité. Sémantique d'écriture : `documentIds` **null = liens
conservés, `[]` = tout délier, liste = remplacement complet** (même esprit que la
garde `echeance` omise de `gerer_tache`). L'éditeur web envoie toujours la liste
complète (invariant édition-sans-perte).

## Conséquences

- Migration `AjouterDocumentsDeTache` ; `documentIds` dans le détail de tâche, le
  POST/PUT, et les outils MCP `creer_taches`/`gerer_tache` (parité).
- `TacheEditeur` : section « Documents liés » (chips cliquables → fichier,
  retrait ×, ajout via select) ; la fiche charge `documentIds` comme tout champ.
- Ids validés contre la table Documents — jamais de lien fantôme.

## Confirmation

`grep -n "TacheDocuments" server/HouseOs.Api/Infrastructure/HouseOsDbContext.cs`
retourne la config ; le test d'intégration `DocumentsLies_` dans
`server/HouseOs.Tests/Integration/TachesApiTests.cs` passe.
