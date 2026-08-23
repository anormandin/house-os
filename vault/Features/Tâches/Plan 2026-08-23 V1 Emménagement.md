---
type: plan
status: executed
date: 2026-08-23
feature: "[[Tâches]]"
tags: []
---

# Plan 2026-08-23 V1 Emménagement

Phase 1b, approuvée par Alain le 2026-08-23. Périmètre : moteur de récurrence 3 modes,
zones + vue Pièces, module [[Équipements]] avec fichiers, flux iCal par personne,
script de backup. Gouverné par [[D-2026-08-23 Moteur De Récurrence Trois Modes]],
[[D-2026-08-23 PostgreSQL]], [[D-2026-08-23 Notifications Par Flux iCal]],
[[D-2026-08-23 Direction Artistique Cuisine Chaleureuse]].

Réponses du grilling : iCal **par personne** (jeton secret, tâches assignées +
non-assignées) ; fichiers **dès V1** (disque + table PièceJointe) ; zones **CRUD
plat** (nom + intérieur/extérieur) avec vue Pièces incluse.

## Étapes

- [x] Domaine : `SpecRecurrence` (Fixe : jours de semaine / jour du mois / annuelle ;
      Intervalle ; fenêtre saisonnière chevauchant l'an ; rollover défaut activé) +
      `MoteurRecurrence` pur + stratégies d'assignation (fixe / alternance /
      moins-l'a-fait 90 j) — tests unitaires exhaustifs d'abord.
- [x] Migration EF : `Zones`, `Equipements` (specs JSONB), `PiecesJointes`,
      colonnes `Taches`/`Occurrences` (assigneAId), `Utilisateurs.JetonIcal`.
- [x] API : tranches Zones (CRUD), Equipements (CRUD + upload/download 50 Mo
      PDF/images + historique d'entretien), Taches (PUT édition + récurrence à la
      complétion), Ical (`/ical/{jeton}.ics`, Ical.Net, anonyme par jeton),
      `RolloverService` quotidien.
- [x] UI : éditeur de tâche complet (récurrence en français simple), onglet Pièces
      (fraîcheur + détail + gestion des zones inline), onglet Équipements (fiche +
      pièces jointes), section Calendrier (URL iCal à copier), nav 4 onglets.
- [x] Backup : `scripts/backup.sh` (pg_dump + volume fichiers, rétention 30 j) + doc.
- [x] Vault : décisions (iCal par personne, fichiers sur disque, zones plates),
      specs as-built, Glossaire, Architecture, Recaps ; validation CLEAN.

## Vérification

- `dotnet test` : moteur (hebdo, mensuel 31→février, annuel, intervalle, fenêtres
  chevauchant l'année, rollover) + stratégies.
- E2E navigateur : zones → équipement avec manuel PDF → tâche hebdo complétée →
  prochaine occurrence correcte → saisonnière hors fenêtre → vue Pièces.
- `curl` des deux flux `.ics` ; backup exécuté + restauration d'essai.
