---
type: feature
status: implemented
last-verified: 2026-08-24
verified-against: fcbdbf2
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

- Les abonnements sont gérés dans l'app
  ([[D-2026-08-24 Flux ICS Dans L'app Affichage Seul]]) :
  modal « Calendriers externes » (bouton calendrier dans
  l'en-tête de Cette semaine) — nom, URL, type (`Collecte`/`Ecole`/`Autre`).
  La création télécharge le flux une fois pour valider l'URL (422 avec message
  clair sinon).
- Un `BackgroundService` rafraîchit chaque flux actif au démarrage puis toutes
  les 6 h ([[D-2026-08-24 Tables Flux Externes]],
  [[D-2026-08-23 Pas De N8n Dans Le Cœur]]) : Ical.Net expanse les récurrences
  sur hier → +60 jours, remplacement par flux en transaction ; échec → derniers
  événements conservés, erreur affichée dans la gestion.
- Les événements sont des faits, jamais des tâches : bandeau discret des
  événements du jour au-dessus de la liste d'Aujourd'hui, et fusion dans Cette
  semaine avec style distinct (italique + icône, non cochable).
- L'UI lit uniquement les tables locales (`GET /api/evenements-externes`).

## Hors périmètre

- Republier les flux externes dans les calendriers iCal personnels (les téléphones
  peuvent s'abonner à la source directement).
- Synchronisation Google Calendar bidirectionnelle (OAuth) — différée, v2+ (voir
  recherche).
- Scrapers spécifiques par municipalité (patron hacs_waste_collection_schedule) —
  v1 = ICS par URL seulement.

## Décisions

- [[D-2026-08-24 Flux ICS Dans L'app Affichage Seul]] — gestion in-app,
  affichage Aujourd'hui + Cette semaine, aucune tâche générée.
- [[D-2026-08-24 Tables Flux Externes]] — tables normalisées, cadence 6 h,
  fenêtre 60 jours, validation par téléchargement à la création.

## Ancres de code

- `server/HouseOs.Api/Domaine/FluxExterne.cs` — abonnement + événement.
- `server/HouseOs.Api/Features/FluxExternes/` — lecture ICS, worker, endpoints.
- `server/HouseOs.Tests/Features/FluxExternes/LectureIcsTests.cs` — fixtures.
- `web/src/components/FluxExternesGestion.tsx` — modal de gestion ;
  `web/src/pages/Aujourdhui.tsx` — bandeau du jour + fusion Cette semaine.

## Sources

- `docs/research/2026-08-23-donnees-externes-meteo.md` §2 et §4 — Recollect,
  calendriers scolaires, Ical.Net.

## Historique

- [[Plan 2026-08-24 Flux Externes V1]] · [[Recap Flux Externes]]
