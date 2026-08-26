---
type: plan
status: executed
date: 2026-08-26
feature: "[[Documents]]"
---

# Plan — Documents : facettes + table dense + tiroir

Implémente [[D-2026-08-26 Navigation Documents Par Facettes]] et
[[D-2026-08-26 Dossier De Document]]. Maquette de référence : itération C
(+ tiroir de A) de l'artifact « Facettes Documents ».

## Backend

- [x] `Domaine/Document.cs` : propriété `Dossier` (string?, ≤ 100) ; config
  EF `HasMaxLength(100)` dans `HouseOsDbContext`.
- [x] Migration `AjouterDossierDocument`.
- [x] `DocumentsEndpoints` : `Dossier` dans `DocumentDto` et `DocumentRequete` ;
  PUT valide ≤ 100 et normalise trim/vide→null ; POST lit le champ de
  formulaire `dossier` avec les mêmes règles ; GET accepte un filtre `dossier`.
- [x] MCP `OutilsMaison` : `DocumentDonnees.Dossier`, `gerer_document` l'écrit,
  `lister_documents` filtre par dossier (parité API↔MCP).
- [x] Tests d'intégration : aller-retour dossier (upload avec dossier → GET →
  PUT modifie → filtre `?dossier=`), dossier trop long → 400.

## Frontend

- [x] `lib/api.ts` : `dossier` sur `Document` et `DocumentDonnees` ;
  téléversement transmet `dossier` si fourni.
- [x] Réécrire `pages/Documents.tsx` :
  - Barre latérale : bloc « Échéances proches » épinglé (lignes cliquables),
    blocs Catégories / Lieux & dossiers (+ « Sans dossier ») / Équipements avec
    compteurs, une facette active par bloc, combinables en ET.
  - Centre : jetons des filtres actifs + « Effacer les filtres », compte de
    résultats, table dense triable (Titre, Catégorie en chip colorée, Lié à,
    Daté du, Échéance colorée rouge/jaune, téléchargement), tri par défaut
    Daté du ↓ (nulls en fin), pagination client 25/page.
  - Tiroir de détail par-dessus le bord droit : fiche éditable complète
    (tous les champs, dont Dossier avec datalist), aperçu image, télécharger,
    suppression deux-clics, fermeture Échap/×.
  - La recherche titre/notes/nom de fichier reste dans l'en-tête.
- [x] Tests composant (Vitest + MSW) : facettes combinées filtrent la table,
  tri, pagination, et invariant édition-sans-perte du tiroir (fixture document
  à champs tous non nuls, Enregistrer ne perd rien).

## Vérification

- [x] `dotnet test` (backend) et `npm test` (web) verts.
- [x] Backend relancé, page vérifiée dans l'app réelle avec le corpus existant.
- [x] Vault : spec [[Documents]] à jour, Recap complété, validation des liens.
