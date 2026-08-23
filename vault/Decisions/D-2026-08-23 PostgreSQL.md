---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: []
---

# PostgreSQL

## Contexte

Base de données pour un serveur familial en Docker : deux utilisateurs, EF Core,
lectures de capteurs à venir (append-heavy), backups qui doivent être triviaux.

## Options considérées

- **PostgreSQL** — provider EF Core solide, JSONB pour métadonnées flexibles
  (specs d'équipements), correct pour les données capteurs à l'échelle maison ;
  backup = `pg_dump` ; apprentissage utile (le travail est sur SQL Server).
- **SQLite** — zéro conteneur, un fichier ; faible avec des écrivains concurrents si
  l'IoT poste fréquemment.
- **SQL Server** — le mieux connu, mais conteneur lourd (~2 Go RAM) et zéro
  apprentissage.

## Décision

PostgreSQL (image `postgres:17-alpine` dans Compose), accès via EF Core + Npgsql.
Choix de l'utilisateur.

## Conséquences

- Backups `pg_dump` planifiés vers un dossier synchronisé — à mettre en place en
  Phase 1b (script de backup).
- Les métadonnées flexibles (specs JSONB) évitent des migrations pour chaque nouveau
  champ d'équipement.

## Confirmation

`docker-compose.yml` définit un service `postgres` ; `HouseOs.Api` référence
`Npgsql.EntityFrameworkCore.PostgreSQL`.
