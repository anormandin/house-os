---
type: decision
status: accepted
date: 2026-08-23
feature: "[[Comptes À Rebours]]"
tags: []
---

# Comptes À Rebours Au Flux iCal

## Contexte

La spec [[Comptes À Rebours]] classait le flux iCal « candidat naturel » mais le
laissait hors périmètre v1. Le flux personnel existe déjà
([[D-2026-08-23 Flux iCal Par Personne]]) et Ical.Net rend l'ajout trivial.

## Options considérées

- **Inclure dès cette version** — chaque compte devient un évènement
  toute-la-journée à sa date cible dans le flux existant.
- **Affichage seulement** — garder le flux aux occurrences de tâches, ajouter plus tard.

## Décision

Inclure dès cette version. Chaque compte à rebours dont la date cible est
aujourd'hui ou dans le futur est ajouté au flux personnel comme évènement
toute-la-journée (UID `compte-{id}@houseos`). Les comptes sont du foyer (pas
d'assignation) : les deux flux personnels les incluent. Les comptes passés sont
exclus du flux. Choix de l'utilisateur.

## Conséquences

- Les téléphones abonnés voient « Déménagement » le 6 octobre sans travail
  supplémentaire.
- Supprimer un compte le retire du flux à la prochaine synchronisation des
  téléphones (comportement iCal standard).

## Confirmation

`server/HouseOs.Api/Features/FluxIcal/FluxIcalEndpoints.cs` : évènements UID
`compte-{id}@houseos` ajoutés au calendrier après les occurrences.
