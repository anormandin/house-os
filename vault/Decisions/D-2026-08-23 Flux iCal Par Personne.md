---
type: decision
status: accepted
date: 2026-08-23
feature: "[[Tâches]]"
tags: []
---

# Flux iCal Par Personne

## Contexte

Notifications v1 = abonnement iCal en lecture seule ([[D-2026-08-23 Notifications Par Flux iCal]]).
Restait à trancher la structure : un flux commun ou un par personne? Les apps de
calendrier ne savent pas envoyer de cookie — l'authentification doit vivre dans l'URL.

## Options considérées

- **Un flux par personne** — chacun voit son horaire sans le bruit de l'autre.
- **Un seul flux commun** — plus simple, mais les deux calendriers montrent tout.
- **Les deux** — flexibilité maximale, plus de surface.

## Décision

Un flux par personne : `GET /ical/{jeton}.ics`, anonyme, sécurisé par un **jeton
secret** (48 hex, généré à l'amorçage, colonne `Utilisateurs.JetonIcal`). Le flux
contient les occurrences en attente avec échéance **assignées à la personne + les
non-assignées**, en évènements toute-la-journée (Ical.Net). L'URL s'affiche dans la
boîte « Mon calendrier » (icône calendrier de l'en-tête). Un flux « maison » commun
pourra s'ajouter plus tard (écran mural). Choix de l'utilisateur.

## Conséquences

- Le jeton dans l'URL est le secret : ne jamais le publier ; régénération = simple
  UPDATE de la colonne (à outiller si jamais compromis).
- L'endpoint est `AllowAnonymous` — seul point non-cookie de l'API avec /connexion.

## Confirmation

`Features/FluxIcal/FluxIcalEndpoints.cs` existe et filtre
`AssigneAId == null || AssigneAId == utilisateur.Id` ; colonne `JetonIcal` dans la
migration `V1Emmenagement`.
