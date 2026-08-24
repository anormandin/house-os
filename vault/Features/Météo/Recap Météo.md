---
type: recap
date: 2026-08-24
feature: "[[Météo]]"
plan: "[[Plan 2026-08-24 Météo V1]]"
---

# Recap Météo

Tranche livrée telle que planifiée, en une session :

- Worker `MeteoIngestionService` : Open-Meteo `/v1/forecast` toutes les heures
  (hier + 7 jours, fuseau America/Toronto, vent en km/h), normalisation vers
  `PrevisionsHoraires`/`PrevisionsQuotidiennes`, brut archivé dans `RelevesMeteo`
  (purge 7 jours), le tout en transaction remplace-fenêtre.
- Trois règles pures et testées : Tondre, Aérer, Être dehors/en dedans —
  verdict `Bon`/`Passable`/`Defavorable` + raison française.
- `GET /api/meteo` évalue les règles à la lecture ; carte « Dehors » dans la
  colonne latérale d'Aujourd'hui (icônes WMO→lucide, prochains jours, pastilles).

Vérifié : 104 tests verts (21 nouveaux), ingestion réelle au démarrage
(192 heures / 8 jours), verdicts cohérents avec la vraie météo du jour via curl.

Déviations :

1. `Heure` mappée `timestamp without time zone` (heure locale, Kind=Unspecified) —
   le défaut Npgsql `timestamptz` exige de l'UTC ; découvert à la migration.
2. Tables nommées à la EF (`PrevisionsHoraires`…), pas en snake_case comme le
   corps de [[D-2026-08-24 Tables Météo Normalisées]] le suggérait — cohérent avec
   le reste du schéma.
3. Passe visuelle du bandeau dans le navigateur laissée à Alain (extension Chrome
   déconnectée pendant la session).
