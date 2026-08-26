---
type: plan
status: executed
date: 2026-08-26
feature: "[[Tâches]]"
---

# Plan — page Tâches : vue Rythmes avec bascule Année

Exécute [[D-2026-08-26 Page Tâches Rythmes Et Année]] (itération A des maquettes).
Réf. visuelle : artifacts « Maquettes Tâches » (maquettes 2 et 5) et « Itérations
Tâches » (itération A).

## Backend

- [x] `GET /api/taches` : nouveau DTO `TacheResumeDto` (id, titre, description,
      recurrence, zoneId, equipementId, strategie, nbDocuments, echeance +
      assigneA de l'occurrence en attente, `completee` pour une ponctuelle sans
      occurrence en attente). Tri : échéance puis titre. Implémentation dans
      `OperationsTaches.ListerTachesAsync` (réutilisée par le MCP).
- [x] Parité MCP : outil `lister_taches` dans `OutilsTaches.cs`.
- [x] Tests d'intégration : forme du DTO (échéance/assigné tirés de l'occurrence
      en attente, nbDocuments), ponctuelle complétée → `completee: true` sans
      échéance ; outil MCP exécuté pour vrai.

## Frontend

- [x] `web/src/lib/taches-vues.ts` (fonctions pures, patron ruban) :
      groupement par rythme (5 groupes, sous-titres « n en retard », progression
      des ponctuelles), libellés de chips (« ↻ hebdo · sam », « ☀ mai→oct »…),
      géométrie de l'année (points mensuels/annuels en % d'année, barres de
      fenêtres en cours/à venir, grappes de ponctuelles par jour, tempo court).
- [x] `web/src/pages/Taches.tsx` réécrite : commutateur segmenté « Liste |
      Année » (dernier mode en localStorage), bouton « + Nouvelle », ⌘K conservé
      (guard éditeur unique), vue Liste groupée (rangée → éditeur), vue Année
      (chronologie 12 mois, ligne Aujourd'hui, jalons des comptes à rebours,
      légende, bandeau tempo court, clic → éditeur). Le filtre À faire/Complétées
      et la barre quick-add pointillée disparaissent.
- [x] Tests Vitest : taches-vues (groupes, retards, géométrie, clusters, tempo
      court) ; page (bascule + persistance du mode, rangée ouvre l'éditeur).

## Vérification

- [x] `dotnet test` et `npm test` verts ; vérification visuelle des deux vues
      dans l'app (backend relancé).
- [x] Vault : spec [[Tâches]] mise à jour (as-built), Recap, validation des liens.
