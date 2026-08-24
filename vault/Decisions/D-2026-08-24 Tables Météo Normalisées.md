---
type: decision
status: accepted
date: 2026-08-24
feature: "[[Météo]]"
tags: []
---

# Tables Météo Normalisées

## Contexte

Où vivent les prévisions et où s'évaluent les règles « bonne journée pour… » :
l'UI pouvait appeler Open-Meteo directement, ou le serveur pouvait matérialiser les
verdicts, ou stocker des prévisions normalisées et évaluer à la lecture.

## Options considérées

- **Tables normalisées + règles à la lecture** — un worker écrit des lignes
  horaires/quotidiennes typées ; l'endpoint évalue les règles C# sur ces lignes à
  chaque requête (calcul trivial pour ~7 jours × 24 h).
- **Appel API dans le chemin de requête** — pas de schéma, mais latence, panne
  couplée, et contraire au principe « l'UI ne dépend jamais du réseau externe ».
- **Verdicts matérialisés** — le worker stocke aussi les résultats de règles ;
  découple encore plus, mais fige les seuils au moment du fetch et complique chaque
  ajustement de règle.

## Décision

Le worker (`BackgroundService`, cadence horaire, conforme à
[[D-2026-08-23 Pas De N8n Dans Le Cœur]]) remplace-fenêtre les prévisions dans deux
tables normalisées — horaire et quotidienne — et archive le payload brut (JSONB,
dernières récupérations seulement). Les règles restent des classes C# pures évaluées
à la lecture sur ce modèle normalisé ; elles ne voient jamais la forme de l'API.
Localisation en configuration (`Meteo:Latitude`/`Longitude`), pointée sur la
nouvelle maison.

## Conséquences

- Schéma : trois tables (`previsions_horaires`, `previsions_quotidiennes`,
  `releves_meteo` pour le brut) via migration EF.
- Open-Meteo hors ligne → l'app sert les dernières prévisions connues ; jamais
  d'appel externe dans le chemin de requête.
- Ajuster un seuil de règle = un changement de code + test, sans toucher aux
  données ; le brut archivé permet de renormaliser si le modèle évolue.

## Confirmation

- Une migration EF contenant les tables météo existe sous
  `server/HouseOs.Api/Infrastructure/Migrations/`.
- `grep -r "HttpClient" server/HouseOs.Api/Features/Meteo` n'apparaît que dans le
  worker d'ingestion, jamais dans les endpoints.
