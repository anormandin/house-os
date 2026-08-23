---
type: feature
status: building
last-verified: 2026-08-23
verified-against: 3c8df13
tags: []
---

# Tâches

## Intention

Le cœur de House OS : gérer les tâches de la maison — récurrentes (hebdo, mensuelles,
annuelles, saisonnières) et ponctuelles, intérieures et extérieures — pour deux
adultes, avec attribution équitable et historique. Première utilisation réelle : les
tâches du déménagement du 2026-10-06 (V0).

## Comportement

Implémenté (V0, as-built) :

- Quand un utilisateur crée une tâche ponctuelle (titre, échéance?, assigné?), une
  occurrence unique est créée ; elle apparaît dans Tâches et, le jour venu ou en
  retard, dans Aujourd'hui.
- Quand un utilisateur complète une occurrence, le système journalise qui/quand dans
  le journal (table séparée) ; une occurrence déjà complétée est refusée (409) ; une
  ponctuelle ne génère pas d'occurrence suivante.
- Quand une tâche est supprimée, ses occurrences partent en cascade mais le journal
  survit (ids historiques, pas de FK).
- Toute l'API exige la session cookie (2 comptes) ; 401 sinon.
- La vue Aujourd'hui montre aussi les occurrences **complétées aujourd'hui** (rangée
  verte « bravo X ✓ 14 h 10 ») via le filtre API `faites` : le client fournit les
  bornes d'instants de sa journée locale (`de`/`a`), le serveur ne connaît pas le
  fuseau du client.
- L'UI applique le design final [[D-2026-08-23 Direction Artistique Cuisine Chaleureuse]] :
  desktop d'abord (en-tête Maison + onglets), héros illustré avec titre d'humeur
  (repli client de [[Titre D'humeur]]), cartes Cette semaine / L'équipe /
  [[Comptes À Rebours|Comptes à rebours]], quick-add repliable (⌘K).

Prévu (V1) :

- Tâche à intervalle : la prochaine occurrence est matérialisée à partir de la date
  de complétion ; tâche fixe : selon l'horaire.
- Fenêtre saisonnière : aucune occurrence générée hors fenêtre.
- Rollover : une occurrence manquée glisse au lieu de s'accumuler en retard.
- Les tâches pourront référencer une [[Glossaire#Zone|zone]] et un
  [[Équipements|équipement]].

Détail des modes de récurrence : voir
[[D-2026-08-23 Moteur De Récurrence Trois Modes]].

## Hors périmètre

- Points, récompenses, features famille/enfants — jamais (pas d'enfants).
- Sous-tâches et projets multi-étapes (module Projets, v2+).
- Notifications push (v1 = flux iCal seulement).

## Décisions

- [[D-2026-08-23 Moteur De Récurrence Trois Modes]] — les 3 modes, matérialisation,
  journal séparé, stratégies d'assignation.
- [[D-2026-08-23 Notifications Par Flux iCal]] — canal de rappel v1.
- [[D-2026-08-23 Auth Simple Deux Comptes]] — attribution des complétions.

## Ancres de code

- `server/HouseOs.Api/Domaine/Tache.cs` — création ponctuelle, génération prochaine occurrence
- `server/HouseOs.Api/Domaine/Occurrence.cs` — complétion + entrée de journal
- `server/HouseOs.Api/Features/Taches/TachesEndpoints.cs` — API tâches/occurrences
- `server/HouseOs.Api/Features/Auth/AuthEndpoints.cs` — connexion/session
- `server/HouseOs.Tests/Domaine/` — tests du domaine
- `web/src/pages/Aujourdhui.tsx`, `web/src/pages/Taches.tsx`, `web/src/components/QuickAdd.tsx` — UI
- `web/src/components/OccurrenceListe.tsx` — rangées de tâches (retard, complétée, échéance)
- `web/src/index.css` — tokens du design chaleureuse (source : maquettes)
- `web/src/lib/format.ts` — typographie québécoise (dates, « 14 h 10 », dodos)

## Sources

- `docs/research/2026-08-23-logiciels-gestion-maison.md` — patterns Grocy/Donetick.

## Historique

- [[Plan 2026-08-23 V0 Déménagement]] · [[Recap Tâches]]
