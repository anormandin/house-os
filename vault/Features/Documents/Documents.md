---
type: feature
status: implemented
last-verified: 2026-08-26
verified-against: d52fad1
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

- Un document = un fichier (50 Mo max — Kestrel monté à 51 Mo pour que la limite
  soit atteignable ; liste blanche PDF/JPEG/PNG/WebP/HEIC, types MIME normalisés
  casse/paramètres, alias `image/jpg` accepté) + titre + catégorie fixe
  ([[D-2026-08-24 Catégories Et Échéance De Document]]) + optionnels : lien vers
  un [[Équipements|équipement]] ou une zone, **dossier libre** (≤ 100, trim,
  vide→null — [[D-2026-08-26 Dossier De Document]]), notes, date du document
  (facture, contrat…), échéance.
- **Page Documents** (réécrite 2026-08-26 —
  [[D-2026-08-26 Navigation Documents Par Facettes]]) : barre latérale de
  facettes à compteurs (Catégories, Lieux & dossiers avec « Sans dossier »,
  Équipements — une sélection par bloc, combinées en ET, jetons orange +
  « Effacer les filtres ») ; **table dense triable** (Titre, Catégorie en chip
  colorée, Lié à = dossier → équipement → pièce, Daté du, Échéance colorée) avec
  tri par défaut Daté du ↓ (dates nulles en fin) et **pagination client
  25/page** ; recherche titre/notes/nom de fichier/dossier dans l'en-tête ;
  téléversement en un clic (titre déduit du nom de fichier, catégorie du type
  MIME). La **fiche éditable vit dans un tiroir** par-dessus le bord droit
  (ouverture au clic de rangée, fermeture Échap/×, champ Dossier autocomplété
  par datalist, suppression deux-clics). Le filtrage/tri/compteurs restent côté
  client — l'API liste tout ; `GET /api/documents` accepte quand même un filtre
  `dossier` (parité MCP).
- Les documents dont l'échéance tombe dans les 60 jours **ou est déjà passée**
  (expirés en rouge, sans borne d'ancienneté) sont épinglés dans le bloc
  « Échéances proches » en tête de barre latérale (lignes cliquables → tiroir) ;
  aucune tâche n'est générée. La colonne Échéance reprend le même code couleur
  (rouge « Expirée · date », doré ≤ 60 jours).
- Chaque rangée de table montre l'icône de catégorie, un **badge de type**
  (PDF, JPG…) et un **bouton de téléchargement direct** ; le tiroir montre un
  aperçu cliquable pour les images (hauteur fixe, masqué si la miniature échoue).
- Upload durci (2026-08-25) : toutes les validations (liens, longueurs) précèdent
  l'écriture disque — jamais de fichier orphelin ; le nom disque = id + extension
  **dérivée du MIME** (jamais celle du client), le nom d'affichage est assaini
  (séparateurs de chemin, caractères de contrôle, 255 max) ; garde anti-bombe de
  décompression sur les miniatures (dimensions lues dans l'en-tête avant tout
  décodage) ; suppressions concurrentes → 404 ; `.tmp` du cache nettoyés.
  Miniatures : WebP ≤ 512 px générées à la demande et mises en cache serveur,
  pas d'aperçu pour PDF/HEIC ([[D-2026-08-25 Miniatures De Documents]]).
- La **fiche équipement** montre ses documents liés (téléversement direct depuis
  la fiche : catégorie déduite du type — PDF → Manuel, image → Photo — puis
  modifiable).
- Supprimer un équipement **délie** ses documents (ils survivent dans la page
  Documents) ; supprimer un document efface le fichier disque.
- **MCP** ([[Serveur MCP]]) : `lister_documents` (filtres catégorie/équipement/
  dossier) et `gerer_document` (modifier les métadonnées dossier inclus,
  supprimer) — l'ajout et le téléchargement de fichiers restent dans l'interface
  web.

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
- [[D-2026-08-26 Navigation Documents Par Facettes]] — facettes latérales +
  table dense + tiroir de détail + pagination client.
- [[D-2026-08-26 Dossier De Document]] — champ texte libre, pas d'entité.

## Ancres de code

- `server/HouseOs.Api/Domaine/Document.cs` — entité + `CategorieDocument`.
- `server/HouseOs.Api/Features/Documents/DocumentsEndpoints.cs` — CRUD + fichiers.
- `web/src/pages/Documents.tsx` — page ; `web/src/pages/Equipements.tsx` —
  documents liés sur la fiche.
- `web/src/components/VignetteDocument.tsx` — vignette + libellé de type partagés.

## Sources

- CLAUDE.md phase 2 (« documents ») ; grilling 2026-08-24.

## Historique

- [[Plan 2026-08-24 Documents V1]] · [[Plan 2026-08-26 Documents Facettes]] ·
  [[Recap Documents]]
