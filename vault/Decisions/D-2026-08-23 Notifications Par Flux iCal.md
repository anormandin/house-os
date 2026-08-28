---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: []
---

# Notifications Par Flux iCal

## Contexte

Un système de tâches sans rappels meurt en silence. Il fallait choisir le canal v1
sans alourdir le démarrage.

## Options considérées

- **Flux iCal** — le serveur publie des flux `.ics` (Ical.Net) ; chacun s'abonne dans
  son app calendrier ; zéro service tiers. Gotcha : Google rafraîchit les abonnements
  paresseusement (~12-24 h) — acceptable pour des corvées.
- **Web push PWA** — riche mais plus de plomberie (service worker, VAPID).
- **ntfy auto-hébergé** — fiable mais une app de plus à installer.
- **Digest quotidien** — calme mais « vu puis oublié ».

## Décision

Flux iCal seulement pour la v1. Push/ntfy réévalués après usage réel. Choix de
l'utilisateur.

## Conséquences

- Les tâches apparaissent dans les calendriers existants du couple, à côté de la vraie
  vie — bon vecteur d'adoption.
- Pas de rappel « urgent » possible en v1 (latence de rafraîchissement) ; si le besoin
  émerge, décision de superseding pour le push.

## Confirmation

Endpoint `/ical/{jeton}.ics` dans le serveur
(`server/HouseOs.Api/Features/FluxIcal/FluxIcalEndpoints.cs`) ; paquet NuGet
`Ical.Net` référencé ; aucun code web-push/VAPID en v1. (Confirmation réécrite
le 2026-08-28 : le chemin `/calendars/*.ics` avait été écrit avant
l'implémentation — la décision elle-même est inchangée.)
