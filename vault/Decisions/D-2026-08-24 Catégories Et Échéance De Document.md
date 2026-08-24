---
type: decision
status: accepted
date: 2026-08-24
feature: "[[Documents]]"
tags: []
---

# Catégories Et Échéance De Document

## Contexte

Comment organiser et retrouver les documents ([[Documents]]), et quoi faire des
documents qui expirent (assurance, permis, contrat)?

## Options considérées

- Catégories fixes / tags libres à la Paperless / les deux.
- Échéance : champ + rappel visuel / génération de tâche « renouveler X » /
  pas d'échéance.

## Décision

- **Catégories fixes** (enum français, stocké en texte) : `Manuel`, `Photo`,
  `Assurance`, `Facture`, `Garantie`, `Contrat`, `PlanPermis`, `ImpotsTaxes`,
  `Autre`. Pas de tags libres — zéro taxonomie à entretenir ; filtres par
  catégorie + recherche sur titre/notes.
- **Échéance = champ optionnel + rappel visuel** : la page Documents met en
  évidence ce qui expire dans les 60 jours. **Aucune tâche générée** — les
  documents sont des faits, pas des obligations (même philosophie que
  [[D-2026-08-24 Flux ICS Dans L'app Affichage Seul]]).
- **Page Documents** dans la nav principale ; la fiche équipement montre aussi
  ses documents liés.

## Conséquences

- `Manuel` et `Photo` existent surtout pour accueillir la migration des pièces
  jointes d'équipements ([[D-2026-08-24 Document Unifié Sur Disque]]).
- Ajouter une catégorie plus tard = ajout à l'enum + migration triviale (texte).

## Confirmation

Enum `CategorieDocument` dans `server/HouseOs.Api/Domaine/Document.cs` ; seuil
60 jours et page `web/src/pages/Documents.tsx` ; route `/documents` dans
`web/src/App.tsx`.
