---
type: decision
status: accepted
date: 2026-08-26
feature: "[[Documents]]"
tags: [schema]
---

# Dossier de document

## Contexte

La navigation par facettes ([[D-2026-08-26 Navigation Documents Par Facettes]])
regroupe les documents par « lieu ou dossier » (ex. « 17 rue de la Colline »,
« 428 rue Fraser », « Déménagement »). Le modèle n'a rien pour ça : `ZoneId`
désigne une pièce de la maison, pas une propriété ni un dossier transversal.

## Options considérées

1. **Champ texte libre `Dossier` sur Document** (nullable, ≤ 100), autocomplété
   dans l'UI à partir des valeurs existantes (datalist).
2. **Entité `Dossier`** avec CRUD, FK depuis Document — plus lourd (gestion,
   suppression, MCP) pour deux utilisateurs.
3. **Réutiliser Zone** en y mettant les propriétés — corrompt le sens de Zone
   (pièce/extérieur) et son usage par les tâches.

## Décision

Option 1 : colonne `Dossier` (texte libre, nullable, 100 max) sur `Document`,
normalisée trim/vide→null. L'autocomplétion par datalist limite la dérive
orthographique ; si un jour il faut renommer en masse ou attacher des
métadonnées au dossier, on pourra promouvoir en entité (migration triviale
depuis des chaînes distinctes).

## Conséquences

- Migration `AjouterDossierDocument` ; champ exposé dans le DTO, le PUT
  (fiche complète — le tiroir doit le charger, voir l'invariant
  édition-sans-perte), le POST de téléversement, et les outils MCP
  `lister_documents` (filtre) / `gerer_document` (modification).
- « Sans dossier » est un état légitime et filtrable, pas une erreur.

## Confirmation

`grep -n "Dossier" server/HouseOs.Api/Domaine/Document.cs` retourne la
propriété ; le test d'intégration `Dossier_` dans
`server/HouseOs.Tests/Integration/DocumentsApiTests.cs` passe.
