---
type: plan
status: executed
date: 2026-08-24
feature: "[[Documents]]"
---

# Plan 2026-08-24 Documents V1

Entité `Document` unique (migration des pièces jointes d'équipements), catégories
fixes, échéance + rappel visuel, page Documents. Gouverné par
[[D-2026-08-24 Document Unifié Sur Disque]] et
[[D-2026-08-24 Catégories Et Échéance De Document]].

## Étapes

### Domaine

- [x] `Domaine/Document.cs` — entité (fichier requis : nom client, chemin disque,
      type MIME, taille ; titre, `CategorieDocument`, `EquipementId?`, `ZoneId?`,
      notes, date du document, échéance, créé le) + enum `CategorieDocument`
      (Manuel, Photo, Assurance, Facture, Garantie, Contrat, PlanPermis,
      ImpotsTaxes, Autre). Supprimer `PieceJointe.cs` et la collection sur
      `Equipement`.

### Infrastructure

- [x] DbContext : DbSet `Documents`, enum en texte, FK équipement/zone en
      `SET NULL`, index sur catégorie et échéance ; retirer `PiecesJointes`.
- [x] Migration `AjouterDocuments` : créer la table, **INSERT‑SELECT** depuis
      `PiecesJointes` (PDF → Manuel, image → Photo, titre = nom sans extension),
      puis DROP — les fichiers disque ne bougent pas.

### Tranche `Features/Documents/`

- [x] `DocumentsEndpoints` — `GET /api/documents` (filtres catégorie /
      équipement), `POST /api/documents` (multipart : fichier + métadonnées,
      mêmes gardes 50 Mo / PDF-images), `PUT /api/documents/{id}` (métadonnées),
      `GET /api/documents/{id}/fichier`, `DELETE /api/documents/{id}` (efface le
      fichier disque). Le helper `DossierFichiers` déménage ici.
- [x] `CategorieParDefaut(typeMime)` pur (PDF → Manuel, image → Photo) — testé.

### Retouches équipements

- [x] `EquipementsEndpoints` — retirer les routes pièces jointes ; le détail liste
      les documents liés ; `NbPiecesJointes` → `NbDocuments` ; suppression d'un
      équipement sans effacement de fichiers (SET NULL fait le travail).
- [x] MCP `OutilsMaison.gerer_equipement` — la suppression de pièce jointe devient
      suppression de document (même garde-fou).

### Web

- [x] `api.ts` — types + appels documents ; retirer `PieceJointe`.
- [x] `pages/Documents.tsx` — liste filtrable par catégorie (chips), recherche
      titre/notes, bloc « Échéances proches » (≤ 60 jours), téléversement +
      édition (patron modal existant), téléchargement, suppression deux-clics.
- [x] Route `/documents` + entrée de nav dans `Layout`.
- [x] `pages/Equipements.tsx` — section fichiers branchée sur `/api/documents`
      (téléversement depuis la fiche avec équipement prérempli, catégorie déduite).

### Vérification et clôture

- [x] `dotnet test` vert ; migration appliquée sur la base dev — les pièces
      jointes existantes réapparaissent comme documents (fichiers intacts).
- [x] Parcours navigateur (Chrome connecté — sinon STOP et demander) :
      téléverser un document avec échéance proche, filtrer, télécharger, lier à
      un équipement, vérifier la fiche équipement, supprimer.
- [x] Vault : spec as-built, Recap, stamps, validation.

Note de vérification : le téléchargement a été validé par `curl` (200, bons octets)
et les chips de filtre par revue du code (filtre client trivial) — le reste du
parcours (téléversement ×2, échéance, recatégorisation, fiche équipement,
suppression deux-clics, SET NULL) l'a été dans le navigateur.
