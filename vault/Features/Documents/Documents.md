---
type: feature
status: implemented
last-verified: 2026-08-25
verified-against: ccaeced
tags: []
---

# Documents

## Intention

Le classeur de la maison : acte de vente, assurances, factures, garanties,
contrats, plans et permis, papiers d'impôts — numérisés, catégorisés, retrouvables
en trois secondes, et rattachés au bon équipement ou à la bonne pièce quand ça
s'applique. Absorbe aussi les manuels et photos d'équipements existants : une seule
entité fichier dans tout House OS ([[D-2026-08-24 Document Unifié Sur Disque]]).

## Comportement

- Un document = un fichier (50 Mo max ; liste blanche PDF/JPEG/PNG/WebP/HEIC) +
  titre + catégorie fixe
  ([[D-2026-08-24 Catégories Et Échéance De Document]]) + optionnels : lien vers
  un [[Équipements|équipement]] ou une zone, notes, date du document (facture,
  contrat…), échéance.
- **Page Documents** dans la nav : filtres par catégorie, recherche
  titre/notes/nom de fichier, téléversement en un clic (titre déduit du nom de
  fichier, catégorie du type MIME), fiche éditable ensuite (métadonnées),
  téléchargement, suppression deux-clics. Les documents dont l'échéance tombe
  dans les 60 jours **ou est déjà passée** (expirés en rouge, sans borne
  d'ancienneté) sont mis en évidence en tête de page ; aucune tâche n'est générée.
- Chaque rangée de liste montre une **vignette** (miniature pour les images, icône
  de catégorie sinon), un **badge de type** (PDF, JPG…) et un **bouton de
  téléchargement direct** ; la fiche montre un aperçu cliquable pour les images
  (hauteur fixe, masqué si la miniature échoue).
  Miniatures : WebP ≤ 512 px générées à la demande et mises en cache serveur,
  pas d'aperçu pour PDF/HEIC ([[D-2026-08-25 Miniatures De Documents]]).
- La **fiche équipement** montre ses documents liés (téléversement direct depuis
  la fiche : catégorie déduite du type — PDF → Manuel, image → Photo — puis
  modifiable).
- Supprimer un équipement **délie** ses documents (ils survivent dans la page
  Documents) ; supprimer un document efface le fichier disque.
- **MCP** ([[Serveur MCP]]) : `lister_documents` (filtres catégorie/équipement) et
  `gerer_document` (modifier les métadonnées, supprimer) — l'ajout et le
  téléchargement de fichiers restent dans l'interface web.

## Hors périmètre

- OCR / recherche plein texte dans les PDF (à la Paperless) — v2+ si le besoin
  se prouve.
- Tags libres.
- Génération de tâches de renouvellement à l'échéance.

## Décisions

- [[D-2026-08-24 Document Unifié Sur Disque]] — entité unique, migration des
  pièces jointes, fichiers survivent à l'équipement.
- [[D-2026-08-24 Catégories Et Échéance De Document]] — catégories fixes,
  échéance + rappel visuel 60 jours, page dédiée.
- [[D-2026-08-25 Miniatures De Documents]] — miniatures WebP côté serveur, cache
  disque, ImageSharp 3.1, HEIC sans aperçu.

## Ancres de code

- `server/HouseOs.Api/Domaine/Document.cs` — entité + `CategorieDocument`.
- `server/HouseOs.Api/Features/Documents/DocumentsEndpoints.cs` — CRUD + fichiers.
- `web/src/pages/Documents.tsx` — page ; `web/src/pages/Equipements.tsx` —
  documents liés sur la fiche.
- `web/src/components/VignetteDocument.tsx` — vignette + libellé de type partagés.

## Sources

- CLAUDE.md phase 2 (« documents ») ; grilling 2026-08-24.

## Historique

- [[Plan 2026-08-24 Documents V1]] · [[Recap Documents]]
