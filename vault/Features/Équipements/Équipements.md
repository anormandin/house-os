---
type: feature
status: implemented
last-verified: 2026-08-24
verified-against: 233f89b
tags: []
---

# Équipements

## Intention

L'inventaire de la maison : fournaise, chauffe-eau, tondeuse, électroménagers — avec
manuels, garanties, dates d'achat et specs (tailles de filtres, codes de peinture).
C'est ce qui distingue House OS d'une todo-app : les tâches d'entretien s'attachent
aux équipements. À construire pour l'emménagement (V1) : inventorier la nouvelle
maison en s'installant.

## Comportement (V1, as-built)

- Quand un utilisateur crée un équipement, il peut le situer dans une zone et lui
  attacher marque/modèle/série, date d'achat, fin de garantie, notes, et des specs
  libres clé/valeur (JSONB — taille de filtre, code de peinture…).
- **Documents liés** ([[D-2026-08-24 Document Unifié Sur Disque]], depuis
  2026-08-24) : manuels PDF et photos = des [[Documents]] rattachés à
  l'équipement, téléversés depuis la fiche (catégorie déduite du type),
  téléchargeables ; supprimer l'équipement **délie** ses documents sans effacer
  les fichiers.
- Quand une [[Tâches|tâche]] référence un équipement, la fiche montre l'**historique
  d'entretien** (20 dernières complétions du journal des tâches liées — le journal
  survivant aux tâches supprimées, le titre affiche « Tâche retirée » au besoin).
- L'onglet Équipements groupe la liste par zone ; la fiche est éditable en place.

## Hors périmètre

- Suivi de stock/consommables (module Consommables, v2+).
- Étiquettes QR à la Homebox (idée v2 si utile).

## Décisions

- [[D-2026-08-23 PostgreSQL]] — JSONB pour les specs flexibles.
- [[D-2026-08-24 Document Unifié Sur Disque]] — les fichiers de la fiche sont des
  documents (supersède [[D-2026-08-23 Fichiers Sur Disque]]).
- [[D-2026-08-23 Plateforme Hobby Cœur Custom]] — Homebox = référence de schéma,
  pas d'intégration.

## Ancres de code

- `server/HouseOs.Api/Domaine/Equipement.cs` — entité (fichiers : voir [[Documents]])
- `server/HouseOs.Api/Features/Equipements/EquipementsEndpoints.cs` — CRUD + entretien
- `web/src/pages/Equipements.tsx` — liste par zone + fiche

## Sources

- `docs/research/2026-08-23-logiciels-gestion-maison.md` — Homebox comme référence.

## Historique

<!-- Plans et recaps à venir (V1). -->
