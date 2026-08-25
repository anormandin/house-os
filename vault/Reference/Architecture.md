---
type: reference
last-verified: 2026-08-25
verified-against: ccaeced
tags: []
---

# Architecture

Portrait d'ensemble de House OS. Les choix sont gouvernés par les décisions liées —
cette note décrit comment ça s'assemble.

## Vue d'ensemble

Un monolithe .NET 10 ([[D-2026-08-23 Monolithe Modulaire Tranches Verticales]]) sert
l'API ; une app web React française ([[D-2026-08-23 Frontend Vite React PWA]]),
**desktop d'abord** avec une future vue e-ink distincte
([[D-2026-08-23 Interface Desktop Et Écran E-ink]]), est la première interface ; PostgreSQL ([[D-2026-08-23 PostgreSQL]]) stocke tout. Le tout
tourne en Docker Compose sur une machine maison, joignable via Tailscale
([[D-2026-08-23 Hébergement Maison Tailscale Docker]]), dans un monorepo unique
([[D-2026-08-23 Monorepo]]).

## Structure du monorepo

- `server/HouseOs.Api` — API minimal, features en tranches verticales
  (`Features/<Module>/…`), domaine riche pour le moteur de récurrence.
- `server/HouseOs.Tests` — tests xunit : `Domaine/` + `Features/` (unitaires,
  harnais Sqlite in-memory) et `Integration/` (WebApplicationFactory +
  Testcontainers Postgres) ; voir [[Suite De Tests]].
- `web/` — Vite + TS + TanStack Query + Tailwind, installable (manifest sans
  service worker : [[D-2026-08-25 Retrait Du Service Worker]]), design « Cuisine
  chaleureuse » (shadcn retiré à la reconstruction UI de 2026-08-23) ; tests
  composants Vitest + Testing Library + MSW (`src/**/*.test.tsx`).
- `web/e2e/` — fumée E2E Playwright (Chromium) ; voir [[Suite De Tests]].
- `scripts/backup.sh` — backup quotidien (pg_dump + volume fichiers).
- `Dockerfile`, `docker-compose.yml`, `.env.example` — conteneurisation et prod
  ([[Déploiement]]).
- `design/` — maquettes retenues (canvas Artifact publié).
- `firmware/`, `hardware/cad`, `hardware/pcb` — phase 3 (vides pour l'instant).
- `vault/`, `docs/research/` — connaissance.
- `.mcp.json`, `.claude/skills/` — branchement Claude Code (voir ci-dessous).

## Interface agent (MCP)

Le backend expose un endpoint MCP `/mcp` ([[Serveur MCP]],
[[D-2026-08-24 Serveur MCP Intégré Au Backend]]) : Claude Code pilote l'app (créer un
lot de tâches, compléter, zones/équipements/comptes) avec une clé API partagée et une
identité déclarée par appel ([[D-2026-08-24 Clé API Partagée Et AgirComme]]).
`.mcp.json` vise le dev par défaut, la prod via `HOUSEOS_MCP_URL`/`HOUSEOS_MCP_KEY`.
Deux skills projet : `planifier-taches` (workflow plan → push) et `demarrer` (dev).

## Ingestion de données externes (phase 2)

Un `BackgroundService` par source ([[D-2026-08-23 Pas De N8n Dans Le Cœur]]) vers
des tables normalisées, payloads bruts archivés. Livrés (2026-08-24) : [[Météo]]
(Open-Meteo HRDPS + règles « bonne journée pour… » en classes C# évaluées à la
lecture) et [[Flux Externes]] (calendriers ICS — collectes Recollect,
calendriers scolaires — gérés dans l'app). À venir : Hydro-Québec
`evenements-pointe`. Détails : `docs/research/2026-08-23-donnees-externes-meteo.md`.

## IoT (phase 3)

MQTT + convention HA Discovery ([[D-2026-08-23 Standard IoT MQTT Discovery]]) ;
Mosquitto + Zigbee2MQTT en Docker ; tablette murale Fully Kiosk sur l'app web ; NFC
tap-pour-compléter ; e-ink et panneaux openHASP ensuite. Détails :
`docs/research/2026-08-23-affichages-iot-hardware.md`.

## Feuille de route (as of 2026-08)

1. **Phase 1a — V0 « Déménagement »** — livrée 2026-08-23 (tâches ponctuelles, vue
   Aujourd'hui, quick-add, login, PWA, design chaleureuse).
2. **Phase 1b — V1 « Emménagement »** — livrée 2026-08-23 (moteur de récurrence
   3 modes, zones + vue Pièces, module [[Équipements]] avec fichiers, flux iCal par
   personne, backups). **En prod depuis 2026-08-24** : `https://houseos.alainnormandin.dev`
   ([[Déploiement]] — LXC sur pve, NPM, backups cron + PBS ; le lab déménage avec
   la maison).
3. **Phase 2** (entamée) : météo + règles ([[Météo]]), [[Titre D'humeur]]
   serveur (Haiku 4.5), calendriers ICS ([[Flux Externes]]) et classeur
   [[Documents]] — livrés 2026-08-24 ; reste : Hydro-Québec, consommables.
4. **Phase 3** : IoT (hub MQTT, affichages, capteurs).
