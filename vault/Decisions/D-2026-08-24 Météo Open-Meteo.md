---
type: decision
status: accepted
date: 2026-08-24
feature: "[[Météo]]"
tags: []
---

# Météo Open-Meteo

## Contexte

La phase 2 démarre par la conscience météo : règles « bonne journée pour… »,
bandeau d'Aujourd'hui, plus tard [[Titre D'humeur]] serveur et e-ink. Il fallait
choisir la source de prévisions pour Sainte-Catherine-de-la-Jacques-Cartier.
Comparatif complet : `docs/research/2026-08-23-donnees-externes-meteo.md`.

## Options considérées

- **Open-Meteo `/v1/forecast`** — gratuit (10 000 appels/jour), **sans clé**, JSON
  plat, horaire sur ~7-16 jours, modèle canadien **GEM/HRDPS 2,5 km** choisi
  automatiquement, humidité du sol, UV, agrégats quotidiens avec lever/coucher.
- **ECCC GeoMet** — officiel canadien, gratuit, mais horaire limité à 24 h et API
  OGC plus lourde ; sa valeur unique (alertes FR + AQHI) n'est pas cette tranche.
- **OpenWeatherMap** — clé + carte de crédit, pas de modèle canadien ; piège de
  facturation.
- **Met.no / Pirate Weather** — pas de HRDPS, optimisés ailleurs.

## Décision

Open-Meteo `/v1/forecast` comme source unique de prévisions de la v1 (horaire +
quotidien, HRDPS). ECCC GeoMet reste le complément futur pour alertes françaises et
AQHI, hors de cette tranche. Choix de l'utilisateur (2026-08-24), aligné sur la
recommandation de la recherche.

## Conséquences

- Aucune clé ni secret à gérer ; un simple `HttpClient` typé suffit.
- Palier gratuit non commercial largement suffisant (≤ ~48 appels/jour).
- Le contrat externe est la forme JSON d'Open-Meteo — normalisée dès l'ingestion
  ([[D-2026-08-24 Tables Météo Normalisées]]) pour que rien d'autre n'en dépende.

## Confirmation

- `grep -r "open-meteo.com" server/HouseOs.Api` touche uniquement la tranche
  `Features/Meteo/`.
- Aucune autre API météo référencée dans `server/`.
