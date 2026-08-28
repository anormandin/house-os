---
type: feature
status: implemented
last-verified: 2026-08-28
verified-against: 0d96d5f
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
  libres clé/valeur (JSONB — taille de filtre, code de peinture…). Validations
  (QA 2026-08-28, REST et MCP via le même `ValiderAsync`) : fin de garantie ≥
  date d'achat ; specs bornées à 100 entrées, clé ≤ 100 caractères, valeur
  ≤ 1000.
- **Documents liés** ([[D-2026-08-24 Document Unifié Sur Disque]], depuis
  2026-08-24) : manuels PDF et photos = des [[Documents]] rattachés à
  l'équipement, téléversés depuis la fiche (catégorie déduite du type). Chaque
  document lié affiche vignette, badge de type, taille, téléchargement direct
  ([[D-2026-08-25 Miniatures De Documents]]) et se supprime depuis la fiche
  (suppression deux-clics « Vraiment ? » — le document et son fichier disque
  sont effacés) ; supprimer l'équipement **délie** ses documents sans effacer
  les fichiers.
- Quand une [[Tâches|tâche]] référence un équipement, la fiche montre l'**historique
  d'entretien** (20 dernières complétions du journal des tâches liées — le journal
  survivant aux tâches supprimées, le titre affiche « Tâche retirée » au besoin) ;
  les notes de complétion s'affichent sous le titre (sourdine, retours à la ligne
  préservés, même idiome que les listes de tâches).
- L'onglet Équipements groupe la liste par zone (avec compteur de documents par
  équipement) ; la fiche est éditable en place.
- Quand une enveloppe [[Budget]] de type Équipement est liée à l'équipement
  (lien exclusif posé côté Budget), la fiche l'affiche avec son solde — livré
  avec le module Budget (2026-08-27).
- **MCP** ([[Serveur MCP]]) : `lister_equipements`, `obtenir_equipement` et
  `gerer_equipement` (modifier = remplacement complet de la fiche ; supprimer
  délie les documents) — les fichiers restent dans l'interface web.

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
- `web/src/components/VignetteDocument.tsx` — vignettes des documents liés
- `server/HouseOs.Api/Features/Mcp/OutilsMaison.cs` — outils MCP équipements

## Sources

- `docs/research/2026-08-23-logiciels-gestion-maison.md` — Homebox comme référence.

## Historique

Livré au fil des plans de [[Tâches]] (V1 « Emménagement ») et de [[Documents]]
(documents liés) — pas de plan dédié ; l'enveloppe liée vient du plan [[Budget]].
