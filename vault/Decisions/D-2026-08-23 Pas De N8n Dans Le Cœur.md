---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: []
---

# Pas De N8n Dans Le Cœur

## Contexte

L'ingestion de données externes (météo, flux ICS, Hydro-Québec) et les règles
(« bonne journée pour tondre ») pouvaient passer par un gestionnaire de workflows
(n8n, Node-RED) ou rester dans le serveur .NET.

## Options considérées

- **Workers .NET dans le cœur** — un `BackgroundService` par source, tables
  normalisées, payloads bruts archivés ; règles = classes C# typées et testées.
- **n8n/Node-RED** — ~400 connecteurs, prototypage en minutes ; mais la logique du
  domaine se scinde en flows JSON non testables, second runtime à opérer, debug dans
  un UI navigateur.

## Décision

Pas de gestionnaire de workflows dans le cœur : l'ingestion et les règles SONT le
domaine, en C# typé et testé. n8n reste une option future comme satellite de
prototypage, parlant à House OS uniquement via l'API publique, comme n'importe quel
client. Choix de l'utilisateur.

## Conséquences

- Chaque nouvelle source de données = une petite tranche verticale + un worker,
  testable.
- Les expérimentations rapides peuvent quand même passer par un n8n jetable plus
  tard, sans toucher l'architecture.

## Confirmation

Aucun service `n8n`/`node-red` dans `docker-compose.yml` ; les workers d'ingestion
vivent dans `server/HouseOs.Api`.
