---
type: decision
status: accepted
date: 2026-08-25
feature: "[[Documents]]"
tags: []
---

# Miniatures de documents

## Contexte

La page [[Documents]] listait les fichiers sans aperçu ni type visible, et le
téléchargement exigeait d'ouvrir la fiche. Afficher les photos directement dans la
liste pose la question du poids : les photos de téléphone font plusieurs Mo chacune.

## Options considérées

1. **Servir l'original dans `<img>`** (lazy loading + cache navigateur) — zéro code
   serveur, mais des listes à plusieurs centaines de Mo et aucun rendu pour le HEIC.
2. **Miniatures générées côté serveur, cache disque** — un endpoint dédié, WebP
   réduit, généré à la première demande (couvre aussi les pièces jointes migrées).
3. Miniatures générées au téléversement — plus simple à servir, mais ne couvre pas
   l'existant sans script de rattrapage.

## Décision

Option 2 : `GET /api/documents/{id}/miniature` génère à la demande un WebP redressé
(EXIF) d'au plus 512 px de côté, mis en cache dans `miniatures/` sous le dossier des
fichiers, effacé avec le document. 404 quand il n'y a pas d'aperçu possible (PDF,
HEIC non décodable, fichier manquant) — le client replie sur une icône. Bibliothèque :
**SixLabors.ImageSharp 3.1** (pas la 4.x, qui exige une clé de licence Six Labors ;
la 3.1 reste sous la split license libre pour un projet personnel). Le HEIC n'est pas
décodable par ImageSharp : pas d'aperçu, assumé (v2 si le besoin se prouve).

## Conséquences

- Les listes restent légères ; l'aperçu couvre JPG/PNG/WebP, pas HEIC ni PDF.
- Un dossier `miniatures/` purement dérivé vit à côté des fichiers — exclu d'aucun
  backup particulier, régénérable à volonté.
- L'endpoint fichier/miniature reste hors MCP ([[Serveur MCP]] : les fichiers
  binaires sont web-only).

## Confirmation

- `rg "miniature" server/HouseOs.Api/Features/Documents/DocumentsEndpoints.cs` — endpoint et cache présents.
- Tests `GenererMiniatureTests` (réduction, non-agrandissement, non-décodable → false).
- `rg 'SixLabors.ImageSharp" Version="3\.1' server/HouseOs.Api/HouseOs.Api.csproj` — pas de montée en 4.x sans nouvelle décision.
