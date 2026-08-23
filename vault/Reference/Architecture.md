---
type: reference
last-verified: 2026-08-23
verified-against: init
tags: []
---

# Architecture

Portrait d'ensemble de House OS. Les choix sont gouvernés par les décisions liées —
cette note décrit comment ça s'assemble.

## Vue d'ensemble

Un monolithe .NET 10 ([[D-2026-08-23 Monolithe Modulaire Tranches Verticales]]) sert
l'API ; une PWA React française ([[D-2026-08-23 Frontend Vite React PWA]]) est la
première interface ; PostgreSQL ([[D-2026-08-23 PostgreSQL]]) stocke tout. Le tout
tourne en Docker Compose sur une machine maison, joignable via Tailscale
([[D-2026-08-23 Hébergement Maison Tailscale Docker]]), dans un monorepo unique
([[D-2026-08-23 Monorepo]]).

## Structure du monorepo

- `server/HouseOs.Api` — API minimal, features en tranches verticales
  (`Features/<Module>/…`), domaine riche pour le moteur de récurrence.
- `server/HouseOs.Tests` — tests unitaires (moteur de récurrence d'abord).
- `web/` — Vite + TS + TanStack Query + Tailwind + shadcn/ui, PWA.
- `firmware/`, `hardware/cad`, `hardware/pcb` — phase 3 (vides pour l'instant).
- `vault/`, `docs/research/` — connaissance.

## Ingestion de données externes (phase 2)

Un `BackgroundService` par source ([[D-2026-08-23 Pas De N8n Dans Le Cœur]]) :
Open-Meteo (météo horaire, modèle canadien HRDPS), flux ICS (collectes Recollect,
calendriers), Hydro-Québec `evenements-pointe`. Tables normalisées, payloads bruts
archivés. Règles « bonne journée pour… » = classes C# sur un modèle
`DailyOutlook`/`HourlyOutlook`. Détails : `docs/research/2026-08-23-donnees-externes-meteo.md`.

## IoT (phase 3)

MQTT + convention HA Discovery ([[D-2026-08-23 Standard IoT MQTT Discovery]]) ;
Mosquitto + Zigbee2MQTT en Docker ; tablette murale Fully Kiosk sur l'app web ; NFC
tap-pour-compléter ; e-ink et panneaux openHASP ensuite. Détails :
`docs/research/2026-08-23-affichages-iot-hardware.md`.

## Feuille de route (as of 2026-08)

1. **Phase 1a — V0 « Déménagement »** (mi-sept. 2026) : tâches ponctuelles, vue
   Aujourd'hui, quick-add, login, PWA déployée.
2. **Phase 1b — V1** (autour du 2026-10-06) : moteur de récurrence 3 modes, zones,
   module [[Équipements]], flux iCal, backups.
3. **Phase 2** : météo + règles, ICS, Hydro-Québec, documents, consommables.
4. **Phase 3** : IoT (hub MQTT, affichages, capteurs).
