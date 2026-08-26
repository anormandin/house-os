---
type: plan
status: executed
date: 2026-08-26
feature: "[[Tâches]]"
---

# Plan — Documents liés aux tâches

Implémente [[D-2026-08-26 Documents Liés Aux Tâches]].

## Backend

- [x] `Tache.Documents` (navigation many-to-many) + config `TacheDocuments` dans
  `HouseOsDbContext` ; migration `AjouterDocumentsDeTache`.
- [x] `CreerTacheRequete`/`ModifierTacheRequete`/`TacheDto` : `documentIds` ;
  helper de validation+application partagé dans `OperationsTaches`
  (null = conserver, [] = délier, liste = remplacer) ; GET détail et PUT
  chargent la collection.
- [x] MCP : `TacheAPlanifier.DocumentIds`, `creer_taches` valide et lie,
  `gerer_tache` obtenir/modifier expose et applique (parité API↔MCP).
- [x] Tests d'intégration : créer avec documents → détail les rend ; PUT null
  conserve / [] déliaison ; suppression d'un document retire le lien sans
  toucher la tâche ; id inconnu → 400.

## Frontend

- [x] `lib/api.ts` : `documentIds` sur `TacheDetail` et `TacheDonnees`.
- [x] `TacheEditeur` : section « Documents liés » — chips cliquables (titre →
  fichier), retrait ×, ajout via select alimenté par `api.documents()` ; la
  fiche charge `documentIds` (invariant édition-sans-perte).
- [x] Tests : fixture `TACHE_COMPLETE` avec `documentIds` non vide, test
  « rien ne se perd » couvre le champ, ajout/retrait d'un document lié.

## Vérification

- [x] `dotnet test` + `npx vitest run` verts ; backend relancé, éditeur vérifié.
- [x] Vault : spec [[Tâches]] et [[Documents]] à jour, Recap, validation.
- [x] Après release : lier le rapport d'inspection aux 9 tâches en prod.
