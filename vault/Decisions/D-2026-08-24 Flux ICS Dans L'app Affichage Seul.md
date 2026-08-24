---
type: decision
status: accepted
date: 2026-08-24
feature: "[[Flux Externes]]"
tags: []
---

# Flux ICS Dans L'app Affichage Seul

## Contexte

Les calendriers externes (collectes Recollect, calendriers scolaires…) entrent
par URL ICS. Trois forks : où vit la liste des abonnements, où s'affichent les
événements, et s'ils génèrent des tâches.

## Options considérées

- **Gestion** : table + CRUD + modal dans l'app · liste dans appsettings (zéro
  UI mais redémarrage à chaque ajout) · hybride.
- **Affichage** : bandeau du jour dans Aujourd'hui + intégration à Cette semaine ·
  carte dédiée · Cette semaine seulement.
- **Tâches auto** : affichage seul · occurrence « sortir le bac » générée la
  veille (dédup, annulations, assignation à gérer dès la v1).

## Décision

Choix de l'utilisateur (2026-08-24) : **gestion dans l'app** (nom, URL, type,
modal de gestion comme les comptes à rebours) ; **affichage dans Aujourd'hui**
(bandeau discret des événements du jour) **et Cette semaine** (style distinct,
non cochable — un événement est un fait, pas une tâche) ; **aucune tâche
générée** — les tâches restent pilotées par le moteur de récurrence, l'échéance
déclenchée par événement rejoint le chantier capteurs (v3).

## Conséquences

- Ajouter un calendrier = coller une URL dans l'app, sans toucher au serveur.
- Aucune interaction entre événements externes et le domaine Tâche/Occurrence —
  la tranche reste en lecture seule sur le reste du système.
- Un futur outil MCP « lister les collectes » devient trivial (lecture de table).

## Confirmation

- `grep -r "EvenementExterne" server/HouseOs.Api/Features/Taches` ne retourne
  rien (aucun couplage avec les tâches).
