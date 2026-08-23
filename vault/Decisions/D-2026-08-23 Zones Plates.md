---
type: decision
status: accepted
date: 2026-08-23
feature: "[[Tâches]]"
tags: []
---

# Zones Plates

## Contexte

Les zones (pièces/extérieur) situent tâches et équipements et portent la vue
signature « Pièces ». Fallait-il une hiérarchie (étage → pièce), une liste figée en
config, ou une liste plate gérée dans l'UI?

## Options considérées

- **Liste plate CRUD** (nom + type intérieur/extérieur) — souple pour une maison
  qu'on découvre en s'installant.
- **Liste figée en config** — zéro UI mais changer une pièce = redéploiement.
- **Hiérarchie** — sur-modélisation pour une maison unifamiliale.

## Décision

Liste plate, gérée directement dans la vue Pièces (ajouter/renommer/supprimer),
avec un simple `Type` Interieur|Exterieur et un `Ordre` d'affichage. La suppression
d'une zone laisse vivre ses tâches et équipements (FK SetNull). Choix de l'utilisateur.

## Conséquences

- Pas de « étage » ni de zones imbriquées ; si le besoin émerge un jour, ce sera une
  nouvelle décision (supersédant celle-ci).
- La vue Pièces calcule la fraîcheur côté client à partir des occurrences de la zone.

## Confirmation

Table `Zones` (Nom, Type, Ordre) dans la migration `V1Emmenagement` ; FK
`Taches.ZoneId` et `Equipements.ZoneId` en SetNull ; CRUD dans
`Features/Zones/ZonesEndpoints.cs`.
