---
type: decision
status: accepted
date: 2026-08-25
feature: "[[Météo]]"
tags: []
---

# Code Météo Horaire Et Conditions Du Moment

## Contexte

Bogue observé en prod (2026-08-25, matin de bruine) : la carte « Dehors » affichait
une icône de pluie (code quotidien 51) pendant que le héros vantait « la belle
météo ». Cause racine : la série horaire ingérée n'incluait pas `weather_code` — la
bruine tombe souvent à 0,0 mm et sous 30 % de probabilité, donc les règles la
comptaient comme une heure confortable. De plus, la grosse icône de la carte venait
du code *quotidien* (le pire de la journée) présenté comme s'il décrivait le moment.

## Options considérées

- **Ajouter `weather_code` à la série horaire et dériver « maintenant » de l'heure
  courante** — une colonne de plus, aucun appel supplémentaire, les conditions du
  moment sortent des tables normalisées existantes.
- **Bloc `current` d'Open-Meteo dans une table dédiée** — plus précis (pas de 15 min),
  mais une table et un chemin d'ingestion de plus pour un tableau de bord rafraîchi
  aux 15 min.
- **Ne rien stocker et corriger seulement le prompt** — laisse les règles aveugles à
  la bruine ; symptôme traité, pas la cause.

## Décision

Ajouter `weather_code` à la série horaire (colonne `CodeMeteo` sur
`PrevisionsHoraires`, migration EF). Les règles traitent tout code ≥ 51 comme de la
précipitation (bruine incluse), même à 0 mm. `GET /api/meteo` expose un objet
`maintenant` (température + code) dérivé de la ligne horaire de l'heure courante ;
la carte « Dehors » affiche ce moment-là en grand, le code quotidien restant pour
les jours suivants. Pas de bloc `current` ni de table dédiée.

## Conséquences

- Migration EF additive ; les lignes existantes sont remplacées à l'ingestion
  suivante (cadence horaire), aucun backfill nécessaire.
- Le contrat d'URL Open-Meteo gagne `weather_code` dans `hourly` — toujours confiné
  à la tranche `Features/Meteo/` ([[D-2026-08-24 Météo Open-Meteo]] inchangée).
- Les trois règles (« Tondre », « Aérer », « Être dehors ») deviennent sensibles à
  la bruine via un prédicat commun sur le code.

## Confirmation

- `grep "weather_code" server/HouseOs.Api/Features/Meteo/MeteoIngestionService.cs`
  montre la série dans `hourly=`.
- `grep -r "CodeMeteo" server/HouseOs.Api/Domaine/Meteo/PrevisionHoraire.cs` existe.
- Test `Dehors_Bruine_SansMillimetres_PasBon` dans
  `server/HouseOs.Tests/Domaine/ReglesJourneeTests.cs`.
