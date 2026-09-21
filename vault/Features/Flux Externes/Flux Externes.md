---
type: feature
status: implemented
last-verified: 2026-09-21
verified-against: 957263e
tags: []
---

# Flux Externes

## Intention

Faire entrer dans House OS les calendriers du monde extérieur : collectes de la
municipalité (Recollect expose un flux iCal stable), calendriers scolaires ou
municipaux, n'importe quel ICS auquel on peut s'abonner par URL. Le but v1 :
« c'est quoi la collecte cette semaine » visible dans Aujourd'hui sans ouvrir un
autre calendrier. Deuxième source d'ingestion de la phase 2, sur le même patron
que [[Météo]].

## Comportement

- Un flux a deux **sources** possibles (as of 2026-09-21,
  [[D-2026-09-20 Flux Externe Poussé]]) : `Ics`, l'app télécharge son URL ; `Poussee`,
  il n'a pas d'URL et c'est un programme extérieur qui remplace ses événements par
  l'API. La source **ne se change pas** après coup — les deux chemins n'ont ni la même
  cadence ni le même maître ; on supprime et on recrée.
- Les abonnements sont gérés dans l'app
  ([[D-2026-08-24 Flux ICS Dans L'app Affichage Seul]]) :
  modal « Calendriers externes » (bouton calendrier dans
  l'en-tête du ruban des 7 jours) — nom, URL, type
  (`Collecte`/`Ecole`/`Municipal`/`Autre`), source. La création d'un flux `Ics`
  télécharge une fois pour valider l'URL (422 avec message clair sinon) ; un flux
  poussé naît vide et attend. Une URL sur un flux poussé est **refusée** plutôt que
  gardée en base à faire croire le contraire.
- Un `BackgroundService` rafraîchit chaque flux actif au démarrage puis toutes
  les 6 h ([[D-2026-08-24 Tables Flux Externes]],
  [[D-2026-08-23 Pas De N8n Dans Le Cœur]]) : Ical.Net expanse les récurrences
  sur hier → +60 jours, remplacement par flux en transaction ; échec → derniers
  événements conservés, erreur affichée dans la gestion, et **isolé** — chaque
  flux a son propre scope DbContext (QA 2026-08-28) : un flux qui casse (même
  supprimé pendant le passage) n'empêche ni les suivants ni leurs horodatages.
- Téléchargement durci (QA 2026-08-28) : réponse plafonnée à 4 Mo, timeout
  30 s, redirections bornées (3) re-validées à chaque saut, et garde SSRF —
  les hôtes loopback/RFC1918/link-local/multicast sont refusés avec un message
  clair (`GardeSsrf`) ; titres et UID tronqués aux longueurs du schéma pour
  qu'un SUMMARY interminable ne tue pas le flux.
- Lecture ICS (durcie 2026-08-25) : les heures **UTC (« …Z ») et TZID étrangers
  sont converties au fuseau de la maison** (la norme chez Recollect et les
  calendriers scolaires — sinon décalage de 4-5 h et bascule de date le soir) ;
  les heures flottantes restent murales ; `EXDATE` et `RECURRENCE-ID` (collectes
  décalées un férié) sont honorés ; `VTODO`/`VJOURNAL` ignorés.
- Modifier l'URL d'un flux (PUT) **purge ses événements et recharge tout de
  suite** — jamais jusqu'à 6 h de l'ancien calendrier sous le nouveau nom.
- Les événements sont des faits, jamais des tâches : bandeau discret des
  événements du jour au-dessus de la liste d'Aujourd'hui, et fusion dans le
  ruban des 7 jours avec style distinct (italique + icône, non cochable).
- L'UI lit uniquement les tables locales (`GET /api/evenements-externes`).

### La poussée (as of 2026-09-21)

`POST /api/flux-externes/{id}/evenements`, **hors du cookie de session** et sur **sa
propre clé** (`FluxExternes:ClePoussee`, `.env` : `HOUSEOS_POUSSEE_CLE`) : le programme
qui pousse vit souvent ailleurs que la maison et n'a aucune raison de porter la clé du
[[Serveur MCP]]. Clé absente = tout refusé, comme le MCP.

- Le corps est la **liste complète** du flux : remplacement en transaction, même
  contrat que le téléchargement ICS. Une liste vide est une réponse valide (« rien à
  annoncer »), pas une panne.
- Mêmes bornes d'écriture que l'ICS (`BornesDuFlux`), même fenêtre (d'hier à
  +60 jours) — ce qui en sort est **écarté**, pas refusé : une ville qui publie son
  année entière ne doit pas voir sa poussée rejetée. Plafond de 500 événements.
- Pousser dans un abonnement `Ics` est refusé (422) : la passe de six heures le
  viderait à la prochaine occasion.
- La gestion affiche la **dernière réception** d'un flux poussé, et la signale en rouge
  passé sept jours — c'est le moment exact où [[Fonds De Tiroir]] cesse de le publier.

> [!warning] Le piège du chantier : la passe de 6 h ne doit pas voir les flux poussés.
> Sans le filtre sur la source, la passe tenterait de télécharger une URL nulle et,
> au moindre changement du code d'échec, viderait des événements qu'un programme
> extérieur ne repousse qu'une fois par jour. Deux gardes plutôt qu'une : la requête
> filtre `Source == Ics`, et `Rafraichir` sort tout de suite sur une URL nulle. Un test
> vérifie que la passe ne les **liste** même pas (c'est le seul endroit où le filtre se
> prouve : la ceinture, elle, rendrait le test vert sans lui).

### Parité MCP (as of 2026-09-21)

`lister_flux_externes`, `gerer_flux_externe` (creer/modifier/supprimer, les deux
sources) et `pousser_evenements_flux` — ce que l'UI sait faire, l'agent le sait faire
([[Serveur MCP]]). Un champ vide veut dire « ne pas toucher », comme les autres outils
`gerer_*`.

## Hors périmètre

- Republier les flux externes dans les calendriers iCal personnels (les téléphones
  peuvent s'abonner à la source directement).
- Synchronisation Google Calendar bidirectionnelle (OAuth) — différée, v2+ (voir
  recherche).
- Scrapers spécifiques par municipalité (patron hacs_waste_collection_schedule) —
  v1 = ICS par URL seulement.

## Ce qui vit dehors

Le gratteur qui alimente un flux poussé n'est **pas** dans le dépôt
([[D-2026-09-20 Sources Municipales Séparées Par Solidité]]) : la ligne « scrapers
spécifiques par municipalité » du hors périmètre ci-dessus **reste vraie**, et c'est
elle qui rend la poussée nécessaire. House OS n'expose qu'un contrat générique —
« remplace les événements de ce calendrier » — documenté dans `docs/configuration.md`
pour que n'importe quel foyer branche sa propre source.

## Décisions

- [[D-2026-08-24 Flux ICS Dans L'app Affichage Seul]] — gestion in-app,
  affichage Aujourd'hui + Cette semaine, aucune tâche générée.
- [[D-2026-08-24 Tables Flux Externes]] — tables normalisées, cadence 6 h,
  fenêtre 60 jours, validation par téléchargement à la création.
- [[D-2026-09-20 Flux Externe Poussé]] — source poussée plutôt qu'une table de faits
  séparée ; sa propre clé plutôt que celle du MCP.
- [[D-2026-09-20 Sources Municipales Séparées Par Solidité]] — le gratteur vit dehors ;
  le dépôt ne gagne que la faculté de recevoir.

## Ancres de code

- `server/HouseOs.Api/Domaine/FluxExterne.cs` — abonnement + événement.
- `server/HouseOs.Api/Features/FluxExternes/` — lecture ICS, worker, endpoints,
  `OperationsPoussee.cs` (le remplacement, partagé avec le MCP), `PousseeEndpoints.cs`
  (le endpoint et sa clé), `BornesDuFlux.cs` (les bornes d'écriture des deux chemins).
- `server/HouseOs.Api/Features/Mcp/OutilsFlux.cs` — les trois outils MCP ;
  `AuthentificationCleApi.cs` — un schéma, deux clés (`OptionsCleApi`).
- `server/HouseOs.Tests/Features/FluxExternes/LectureIcsTests.cs` — fixtures ;
  `RafraichissementTests.cs` — la passe n'emporte pas les flux poussés.
- `web/src/components/FluxExternesGestion.tsx` — modal de gestion ;
  `web/src/pages/Aujourdhui.tsx` — bandeau du jour + fusion Cette semaine.

## Sources

- `docs/research/2026-08-23-donnees-externes-meteo.md` §2 et §4 — Recollect,
  calendriers scolaires, Ical.Net.

## Historique

- [[Plan 2026-08-24 Flux Externes V1]] · [[Recap Flux Externes]]
