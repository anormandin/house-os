---
type: decision
status: accepted
date: 2026-08-24
feature: "[[Documents]]"
supersedes: "[[D-2026-08-23 Fichiers Sur Disque]]"
tags: []
---

# Document Unifié Sur Disque

## Contexte

Le module [[Documents]] (phase 2) doit accueillir les papiers de la maison (acte,
assurances, factures, permis, plans…). Or [[Équipements]] a déjà une table
`PiecesJointes` (manuels PDF, photos) avec toute la plomberie fichiers. Deux tables
de fichiers, ou une seule entité partout?

## Options considérées

- **Tout unifier** — une seule entité `Document` ; migrer les `PieceJointe`
  existantes dedans ; la fiche équipement montre ses documents liés.
- **Module distinct, lien optionnel** — documents maison à part, pièces jointes
  d'équipements inchangées (deux plomberies parallèles).
- **Simple classeur** — fichiers + titre sans métadonnées.

## Décision

**Tout unifier** (choix de l'utilisateur). Une entité `Document` : fichier obligatoire
+ titre + catégorie + liens **optionnels** vers un équipement ou une zone + notes +
date du document + échéance optionnelle. La table `PiecesJointes` est migrée
(SQL dans la migration EF : PDF → catégorie Manuel, image → Photo, titre = nom de
fichier sans extension) puis supprimée ; **les octets sur disque ne bougent pas**.

Invariants repris de la décision supersédée : fichiers sur disque (dossier
`Fichiers:Chemin`, volume Docker inclus au backup), nom disque = id + extension
(jamais le nom client), 50 Mo max, PDF/images seulement.

**Changement de comportement** : supprimer un équipement ne supprime plus ses
fichiers — les documents survivent, déliés (`SET NULL`). Un document est une archive
de la maison ; la facture du lave-vaisselle ne meurt pas avec le lave-vaisselle.

## Conséquences

- Une seule plomberie upload/download/suppression (`Features/Documents/`) ; les
  endpoints `pieces-jointes` disparaissent, la fiche équipement passe par
  `/api/documents`.
- L'outil MCP `gerer_equipement` délègue la suppression de fichiers au module
  Documents.
- Migration de données irréversible (table supprimée) — le backup pg_dump reste la
  porte de sortie.

## Confirmation

`rg PieceJointe server/ web/` ne retourne que des migrations historiques ; la
migration `AjouterDocuments` contient l'INSERT‑SELECT et le DROP ; FK
`Documents.EquipementId` en `ON DELETE SET NULL` dans le snapshot EF.
