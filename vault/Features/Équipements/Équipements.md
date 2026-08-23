---
type: feature
status: draft
last-verified: 2026-08-23
verified-against: init
tags: []
---

# Équipements

## Intention

L'inventaire de la maison : fournaise, chauffe-eau, tondeuse, électroménagers — avec
manuels, garanties, dates d'achat et specs (tailles de filtres, codes de peinture).
C'est ce qui distingue House OS d'une todo-app : les tâches d'entretien s'attachent
aux équipements. À construire pour l'emménagement (V1) : inventorier la nouvelle
maison en s'installant.

## Comportement

- Quand un utilisateur crée un équipement, il peut le situer dans une zone et lui
  attacher marque/modèle/série, date d'achat, fin de garantie, notes, photos/manuels,
  et des specs libres (JSONB).
- Quand une [[Tâches|tâche]] référence un équipement, l'historique d'entretien de
  l'équipement montre les complétions de cette tâche.

## Hors périmètre

- Suivi de stock/consommables (module Consommables, v2+).
- Étiquettes QR à la Homebox (idée v2 si utile).

## Décisions

- [[D-2026-08-23 PostgreSQL]] — JSONB pour les specs flexibles.
- [[D-2026-08-23 Plateforme Hobby Cœur Custom]] — Homebox = référence de schéma,
  pas d'intégration.

## Ancres de code

<!-- À remplir en V1 : server/HouseOs.Api/Features/Equipements/… -->

## Sources

- `docs/research/2026-08-23-logiciels-gestion-maison.md` — Homebox comme référence.

## Historique

<!-- Plans et recaps à venir (V1). -->
