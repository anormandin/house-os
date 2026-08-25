---
type: recap
date: 2026-08-24
feature: "[[Documents]]"
plan: "[[Plan 2026-08-24 Documents V1]]"
---

# Recap Documents

Tranche livrée telle que planifiée :

- Entité `Document` unique (fichier + titre + catégorie fixe + liens optionnels
  équipement/zone + notes + date + échéance) ; migration `AjouterDocuments` avec
  INSERT‑SELECT depuis `PiecesJointes` (PDF → Manuel, image → Photo, titre = nom
  sans extension) puis DROP — fichiers disque intacts, vérifié sur la base dev.
- `Features/Documents/` : CRUD + upload multipart + téléchargement + suppression
  (fichier disque effacé) ; `CategorieParDefaut` testé ; équipements retouchés
  (détail liste les documents liés, `NbDocuments`, suppression sans effacement de
  fichiers — SET NULL vérifié bout en bout) ; MCP à parité : nouveaux outils
  `lister_documents` + `gerer_document` (modifier/supprimer, testés via JSON-RPC),
  fichiers binaires exclus du MCP.
- Web : page Documents (chips de catégorie, recherche, bloc « Échéances
  proches » ≤ 60 jours, fiche éditable, suppression deux-clics), entrée de nav,
  fiche équipement branchée sur `/api/documents` (catégorie déduite du type).

Vérifié : 137 tests verts (6 nouveaux), parcours navigateur complet (téléversement
page + fiche équipement, échéance « expire dans 22 jours », recatégorisation
Photo → Assurance, suppression deux-clics), téléchargement par curl.

Déviations : aucune. À noter : la confirmation deux-clics se désarme après 4 s
(comportement voulu du composant partagé, pas un bug).

## Incrément 2026-08-25 — vignettes, types, téléchargement direct

- Endpoint `GET /api/documents/{id}/miniature` (WebP ≤ 512 px, EXIF redressé,
  cache disque effacé avec le document, 404 si non décodable) —
  [[D-2026-08-25 Miniatures De Documents]] ; 3 tests `GenererMiniatureTests`.
- Web : `VignetteDocument` partagé (miniature avec repli icône), badge de type
  (PDF/JPG/…), bouton télécharger sur chaque rangée des pages Documents et
  Équipements, aperçu cliquable dans la fiche.
- Vérifié : 140 tests verts, build web propre. Parcours navigateur à refaire
  après redémarrage de l'API.
