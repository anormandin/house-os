---
type: feature
status: draft
last-verified: 2026-08-23
verified-against: init
tags: []
---

# Tâches

## Intention

Le cœur de House OS : gérer les tâches de la maison — récurrentes (hebdo, mensuelles,
annuelles, saisonnières) et ponctuelles, intérieures et extérieures — pour deux
adultes, avec attribution équitable et historique. Première utilisation réelle : les
tâches du déménagement du 2026-10-06 (V0).

## Comportement

- Quand un utilisateur crée une tâche ponctuelle avec échéance, elle apparaît dans la
  liste et dans la vue Aujourd'hui le jour venu.
- Quand un utilisateur complète une occurrence, le système journalise qui/quand et,
  pour une tâche à intervalle, matérialise la prochaine occurrence à partir de la
  date de complétion.
- Quand une tâche a une fenêtre saisonnière, aucune occurrence n'est générée hors
  fenêtre.
- Quand une occurrence avec flag rollover est manquée, elle glisse au lendemain au
  lieu de s'accumuler en retard.
- Les tâches peuvent référencer une [[Glossaire#Zone|zone]] et un
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

<!-- À remplir dès la V0 : server/HouseOs.Api/Features/Taches/… -->

## Sources

- `docs/research/2026-08-23-logiciels-gestion-maison.md` — patterns Grocy/Donetick.

## Historique

<!-- Plans et recaps à venir (V0 Déménagement, V1 récurrence). -->
