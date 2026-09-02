---
type: feature
status: implemented
last-verified: 2026-09-02
verified-against: f29b6fd
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
  (facture, contrat…), échéance. Durci QA 2026-08-28 : le PUT valide FK et
  longueurs comme le POST ; les champs de formulaire d'upload malformés
  répondent 400 au lieu d'être avalés ; le contenu image/PDF est vérifié par
  magic bytes à l'upload ; les catégories tolèrent la casse et refusent les
  numériques (REST et MCP) ; le téléchargement sert `Content-Disposition:
  attachment`.
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
  Documents) ; supprimer un document efface le fichier disque **et ses liens
  vers des tâches** (jointure `TacheDocuments` — voir
  [[D-2026-08-26 Documents Liés Aux Tâches]] et la spec [[Tâches]]).
- **Par courriel** ([[Courriel Entrant]], 2026-09-02) : un courriel transféré à
  `documents@alainnormandin.dev` devient un document par pièce jointe permise, ou
  le `.eml` entier (type `message/rfc822`, accepté par l'ingestion seulement),
  marqué **à classer** ([[D-2026-09-02 Boîte À Classer Des Documents]]) avec des
  métadonnées proposées par LLM. La page montre un bloc « À classer (n) » en tête
  de barre latérale, un bouton « Relever le courrier », une chip « À classer » et
  un bouton primaire **Classer** dans le tiroir (enregistre la fiche corrigée +
  `aClasser:false` ; « Enregistrer » n'envoie jamais le champ), les notes en zone
  de texte multiligne, et pour un `.eml` un aperçu De / Date / Sujet / texte
  (`GET /api/documents/{id}/courriel`, texte seulement). `GET /api/documents`
  accepte `aClasser` ; le PUT accepte `aClasser` (null = inchangé).
- Toute écriture de document (web ou courriel) passe par
  `EnregistrementDocument.EnregistrerAsync` — mêmes validations, même nom disque,
  même rollback.
- **MCP** ([[Serveur MCP]]) : `lister_documents` (filtres catégorie/équipement/
  dossier/aClasser), `gerer_document` (modifier les métadonnées dossier inclus,
  classer, supprimer) et `relever_courriels` — l'ajout et le téléchargement de
  fichiers restent dans l'interface web (ou par courriel).

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
- [[D-2026-09-02 Boîte À Classer Des Documents]] — drapeau `AClasser`, archivage
  `.eml` des courriels sans pièce.

## Ancres de code

- `server/HouseOs.Api/Domaine/Document.cs` — entité + `CategorieDocument`.
- `server/HouseOs.Api/Features/Documents/DocumentsEndpoints.cs` — CRUD + fichiers.
- `server/HouseOs.Api/Features/Documents/EnregistrementDocument.cs` — service
  partagé d'écriture (REST + courriel).
- `web/src/pages/Documents.tsx` — page ; `web/src/pages/Equipements.tsx` —
  documents liés sur la fiche.
- `web/src/components/VignetteDocument.tsx` — vignette + libellé de type partagés.

## Sources

- CLAUDE.md phase 2 (« documents ») ; grilling 2026-08-24.

## Historique

- [[Plan 2026-08-24 Documents V1]] · [[Plan 2026-08-26 Documents Facettes]] ·
  [[Recap Documents]]
