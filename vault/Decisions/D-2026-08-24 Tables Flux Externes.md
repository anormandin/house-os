---
type: decision
status: accepted
date: 2026-08-24
feature: "[[Flux Externes]]"
tags: []
---

# Tables Flux Externes

## Contexte

Où vivent les abonnements ICS et leurs événements, et quand le serveur les
rafraîchit. Même famille de choix que [[D-2026-08-24 Tables Météo Normalisées]],
adaptée à des données quasi statiques (un calendrier de collectes change
quelques fois par année).

## Options considérées

- **Tables normalisées + rafraîchissement périodique** — `flux_externes`
  (abonnements) et `evenements_externes` (occurrences normalisées, récurrences
  RRULE expansées par Ical.Net) ; remplacement par flux à chaque passage.
- **Parser l'ICS à la lecture** — pas de table d'événements, mais le réseau
  externe entre dans le chemin de requête ; contraire au patron établi.
- **Archiver l'ICS brut** — comme la météo ; refusé ici : un flux est
  re-téléchargeable et sans valeur historique.

## Décision

Un `BackgroundService` rafraîchit les flux actifs au démarrage puis toutes les
**6 heures** : téléchargement, expansion des récurrences sur une fenêtre
d'**hier à +60 jours**, remplacement des événements du flux en transaction ;
échec → derniers événements conservés + erreur enregistrée sur le flux.
**Exception assumée au « jamais d'appel externe dans une requête »** : la
création d'un abonnement télécharge le flux une fois, inline — c'est la
validation de l'URL (une action d'administration explicite, pas un chemin de
lecture). Pas d'archive brute.

## Conséquences

- Schéma : deux tables via migration EF ; suppression d'un flux → cascade sur
  ses événements.
- L'UI lit les tables ; un flux mort s'affiche avec sa dernière erreur dans la
  gestion au lieu d'échouer silencieusement.
- La fenêtre de 60 jours borne la table (quelques centaines de lignes max).

## Confirmation

- Une migration EF contenant `FluxExternes`/`EvenementsExternes` existe sous
  `server/HouseOs.Api/Infrastructure/Migrations/`.
- Hors worker, `FluxExternesRafraichissement.Rafraichir` n'est appelé que par les
  gestes d'administration d'un flux : création, et modification quand l'URL change
  (`Features/FluxExternes/FluxExternesEndpoints.cs`, et leurs pendants MCP dans
  `Features/Mcp/OutilsFlux.cs`). Aucune lecture ne télécharge.

> [!note] Mise à jour de la seule Confirmation (2026-09-30)
> La clause disait « le seul téléchargement hors worker est dans le handler de
> création ». Le changement d'URL et la parité MCP s'y sont ajoutés ; le principe (aucune
> lecture ne dépend du réseau externe) tient. Le corps de la décision n'a pas été touché
> — voir [[Flux Externes]].
