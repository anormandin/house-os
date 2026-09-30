---
type: feature
status: implemented
last-verified: 2026-09-30
verified-against: 6d1fda0
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
  équipement) ; la fiche est éditable en place. Un champ « Chercher un
  équipement » filtre par nom, marque, modèle ou pièce (téléphone : nom seul),
  sans accents ; `/equipements?id=` ouvre une fiche — c'est là que mène la
  recherche globale ([[D-2026-09-28 Recherche Globale Sur La Touche Slash]]).
- Quand une enveloppe [[Budget]] de type Équipement est liée à l'équipement
  (lien exclusif posé côté Budget), la fiche l'affiche avec son solde — livré
  avec le module Budget (2026-08-27).
- **MCP** ([[Serveur MCP]]) : `lister_equipements`, `obtenir_equipement` et
  `gerer_equipement` (modifier = remplacement complet de la fiche ; supprimer
  délie les documents) — les fichiers restent dans l'interface web.
- **Catégorie** (2026-09-29, [[D-2026-09-28 Catégorie D'équipement En Liste Fermée]]) :
  un équipement porte une catégorie parmi dix (`Chauffage`, `EauChaude`, `Plomberie`,
  `Electricite`, `Toiture`, `Exterieur`, `PetitsMoteurs`, `Electromenager`, `Vehicule`,
  `Autre`) ou aucune (« pas encore classé »). REST et MCP refusent une valeur hors liste
  (400, casse tolérée à l'entrée). Le bureau montre la catégorie en pastille dans la
  liste et filtre par catégorie ; le téléphone la met en tête de la ligne de détail
  (catégorie · marque · modèle), sans filtre ; la fiche (bureau **et** téléphone — un
  PUT sans la catégorie la vide, la fiche téléphone doit donc la porter) a son
  sélecteur. `lister_equipements` et `obtenir_equipement` la renvoient.
- **Propositions d'entretien** (2026-09-29,
  [[D-2026-09-28 Packs D'entretien En Fichier De Données]]) : sur la fiche d'un
  équipement classé (hors `Autre`), « Proposer les entretiens » ouvre le panneau du pack
  de sa catégorie ; ce qu'une tâche liée à cet équipement porte déjà (même titre, sans
  accents ni casse) est coché-grisé, le reste part coché et se décoche ; « Créer N
  tâches » les crée en une transaction, liées à l'équipement et à sa pièce. Un
  équipement sans catégorie n'a pas le bouton. Bureau seulement ; côté agent, les
  outils MCP `proposer_entretiens` et `adopter_entretiens` portent le même contrat
  (`server/HouseOs.Api/Features/Mcp/OutilsEntretien.cs`). Contrat complet :
  [[Emménagement V2]].

## Hors périmètre

- Suivi de stock/consommables (module Consommables, v2+).
- Étiquettes QR à la Homebox (idée v2 si utile).

## Décisions

- [[D-2026-09-28 Recherche Globale Sur La Touche Slash]] — recherche de page + lien `?id=` depuis la palette.
- [[D-2026-09-28 Catégorie D'équipement En Liste Fermée]] — colonne nullable, dix valeurs.
- [[D-2026-09-28 Packs D'entretien En Fichier De Données]] — le pack de la catégorie,
  proposé depuis la fiche.
- [[D-2026-08-23 PostgreSQL]] — JSONB pour les specs flexibles.
- [[D-2026-08-24 Document Unifié Sur Disque]] — les fichiers de la fiche sont des
  documents (supersède [[D-2026-08-23 Fichiers Sur Disque]]).
- [[D-2026-08-23 Plateforme Hobby Cœur Custom]] — Homebox = référence de schéma,
  pas d'intégration.

## Ancres de code

- `server/HouseOs.Api/Domaine/Equipement.cs` — entité et `CategorieEquipement` (liste
  fermée) ; fichiers : voir [[Documents]]
- `server/HouseOs.Api/Features/Equipements/EquipementsEndpoints.cs` — CRUD + entretien
  (`ValiderAsync`, `LireCategorie`)
- `web/src/pages/Equipements.tsx` — liste par zone + fiche
- `web/src/pages/telephone/EquipementsTelephone.tsx` — la liste et la fiche du téléphone
- `web/src/components/VignetteDocument.tsx` — vignettes des documents liés
- `server/HouseOs.Api/Features/Mcp/OutilsMaison.cs` — outils MCP équipements
- `web/src/lib/categories-equipement.ts` — libellés et ordre des catégories
- `server/HouseOs.Api/Features/Entretien/` — packs d'entretien (proposer / adopter)
- `web/src/components/PropositionsEntretien.tsx` — le panneau des propositions

## Sources

- `docs/research/2026-08-23-logiciels-gestion-maison.md` — Homebox comme référence.

## Historique

Livré au fil des plans de [[Tâches]] (V1 « Emménagement ») et de [[Documents]]
(documents liés) — pas de plan dédié ; l'enveloppe liée vient du plan [[Budget]].
Catégorie et propositions d'entretien : [[Plan 2026-09-28 Emménagement V2]] (étapes 1 et 2).
