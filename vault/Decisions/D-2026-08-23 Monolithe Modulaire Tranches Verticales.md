---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: []
---

# Monolithe Modulaire Tranches Verticales

## Contexte

L'utilisateur pratique Clean Architecture + CQRS au travail (MCM). Pour un serveur
personnel à deux utilisateurs, la cérémonie par feature devait être choisie
consciemment.

## Options considérées

- **Monolithe modulaire, tranches verticales** — un projet ASP.NET Core, une feature =
  un dossier (endpoint + handler + data), minimal APIs, EF Core ; domaine riche et
  testé pour le moteur de récurrence.
- **Clean Architecture + CQRS** — discipline familière mais ~4 fichiers par petite
  feature ; utile seulement comme architecture de référence.
- **Le plus léger possible** — services + EF sans structure ; rapide mais fragile à
  5+ modules.

## Décision

Monolithe modulaire en tranches verticales, .NET 10, minimal APIs, EF Core + Npgsql.
La logique de domaine (moteur de récurrence) garde des types riches + tests
unitaires. Choix de l'utilisateur.

## Conséquences

- Vitesse d'ajout de features en solo ; durcissement possible plus tard si un module
  le mérite.
- La discipline de frontières entre modules (Tâches, Équipements, Appareils…) repose
  sur la revue, pas sur des projets séparés.

## Confirmation

`server/` contient un seul projet applicatif `HouseOs.Api` avec un dossier
`Features/` ; pas de projets `*.Application`/`*.Infrastructure` séparés.
