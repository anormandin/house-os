---
type: reference
last-verified: 2026-09-29
verified-against: 69ff696
tags: []
---

# Architecture

Portrait d'ensemble de House OS. Les choix sont gouvernés par les décisions liées —
cette note décrit comment ça s'assemble.

## Vue d'ensemble

Un monolithe .NET 10 ([[D-2026-08-23 Monolithe Modulaire Tranches Verticales]]) sert
l'API ; une app web React française ([[D-2026-08-23 Frontend Vite React PWA]]),
**desktop d'abord**, est la première interface. Elle rend trois présentations
distinctes du même domaine : le **bureau** (la vue complète), le **téléphone**
([[Vue Téléphone]] — compagnon tactile sous 900 px de large,
[[D-2026-09-19 Interface Téléphone Distincte]] et
[[D-2026-09-19 Portée De La Vue Téléphone]]) et l'**e-ink** mural, rendu côté
serveur. La parité écran-par-écran entre elles n'est pas un objectif ; PostgreSQL ([[D-2026-08-23 PostgreSQL]]) stocke tout. Le tout
tourne en Docker Compose sur une machine maison, joignable via Tailscale, avec
une unique exception au « jamais exposé » : le flux iCal publié par Tailscale
Funnel ([[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]], qui supersède
[[D-2026-08-23 Hébergement Maison Tailscale Docker]]), dans un monorepo unique
([[D-2026-08-23 Monorepo]]). Un healthcheck anonyme `GET /api/sante`
(`server/HouseOs.Api/Features/Sante/SanteEndpoint.cs`) répond au monitoring.

Une deuxième brique transversale, la journalisation ([[Observabilité]],
[[D-2026-08-29 Journalisation Structurée Serilog Et Seq]]) : Serilog émet des évènements
structurés corrélés par un `TraceId` unique — requêtes, durées de phase, écritures
métier, outils MCP, services d'arrière-plan et piste de session du navigateur — que le
conteneur Seq du compose collecte, sur le LAN/tailnet uniquement.

Un canal SignalR transversal ([[Synchro]]) pousse aux onglets ouverts de quoi se
rafraîchir. Sa particularité architecturale : le gros du signal n'est pas publié par les
slices mais **dérivé automatiquement des sauvegardes** par un intercepteur EF Core
([[D-2026-08-28 Événements Par Intercepteur EF]]) — c'est la seule infrastructure
d'événements du backend, et elle couvre d'office les écritures HTTP, MCP et
d'arrière-plan.

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
  ([[Déploiement]]) ; sidecar `tailscale` (profil compose `funnel`, absent en
  dev) et `infra/tailscale-serve.json` qui ne publie que `/ical`.
- `infra/` — Worker courriel Cloudflare, `tailscale-serve.json`, `exemples/`
  d'override compose propres à un hébergement.
- `design/` — maquettes retenues (canvas Artifact publié ; `design/README.md`).
- `firmware/`, `hardware/cad`, `hardware/pcb` — phase 3 (pas encore suivis : vides).
- `vault/`, `docs/` — connaissance : vault, `docs/installation.md`,
  `docs/configuration.md`, `docs/research/`.
- `README.md`, `CONTRIBUTING.md`, `LICENSE` (AGPL-3.0), `.github/workflows/ci.yml`,
  `server/global.json`, `.editorconfig` — dépôt public ([[Distribution]]).
- `.mcp.json`, `.claude/skills/`, `CLAUDE.md` — branchement Claude Code (voir
  ci-dessous) ; `CLAUDE.local.md` (gitignoré) porte le contexte personnel.

## Interface agent (MCP)

Le backend expose un endpoint MCP `/mcp` ([[Serveur MCP]],
[[D-2026-08-24 Serveur MCP Intégré Au Backend]]) : Claude Code pilote l'app (créer un
lot de tâches, compléter, zones/équipements/comptes) avec une clé API partagée et une
identité déclarée par appel ([[D-2026-08-24 Clé API Partagée Et AgirComme]]).
`.mcp.json` vise le dev par défaut, la prod via `HOUSEOS_MCP_URL`/`HOUSEOS_MCP_KEY`.
Trois skills projet : `planifier-taches` (workflow plan → push), `inventorier-maison`
(semis des équipements depuis un rapport d'inspection, puis packs d'entretien —
[[D-2026-09-28 Semis De La Maison Par Skill MCP]]) et `demarrer` (dev).

## Ingestion de données externes (phase 2)

Un `BackgroundService` par source ([[D-2026-08-23 Pas De N8n Dans Le Cœur]]) vers
des tables normalisées, payloads bruts archivés. Livrés (2026-08-24) : [[Météo]]
(Open-Meteo HRDPS + règles « bonne journée pour… » en classes C# évaluées à la
lecture) et [[Flux Externes]] (calendriers ICS — collectes Recollect,
calendriers scolaires — gérés dans l'app), plus les flux poussés (la ville,
[[D-2026-09-20 Flux Externe Poussé]]) et les normales climatiques tirées de l'archive
Open-Meteo ([[Fonds De Tiroir]]). Hydro-Québec `evenements-pointe` est **écarté tant
que le foyer n'est inscrit à aucune offre de pointe** (as of 2026-09, voir
[[Emménagement V2]]). Détails : `docs/research/2026-08-23-donnees-externes-meteo.md`.

## IoT (phase 3)

MQTT + convention HA Discovery ([[D-2026-08-23 Standard IoT MQTT Discovery]]) ;
Mosquitto + Zigbee2MQTT en Docker ; tablette murale Fully Kiosk sur l'app web ; NFC
tap-pour-compléter ; panneaux openHASP ensuite. L'écran e-ink mural est **livré**
([[Affichage E-ink]], reTerminal E1003 reçu et enrôlé le 2026-09-04) et rend le
[[Journal De La Maison]] : House OS sert le protocole du firmware TRMNL, pas MQTT. Le
reste de la phase 3 attend la remise en route du lab dans la nouvelle maison. Détails :
`docs/research/2026-08-23-affichages-iot-hardware.md` et
`docs/research/2026-09-03-ecrans-eink-candidats.md`.

## Feuille de route (as of 2026-09-29)

1. **Phase 1a — V0 « Déménagement »** — livrée 2026-08-23 (tâches ponctuelles, vue
   Aujourd'hui, quick-add, login, design chaleureuse).
2. **Phase 1b — V1 « Emménagement »** — livrée 2026-08-23 (moteur de récurrence
   3 modes, zones + vue Pièces, [[Équipements]] avec fichiers, flux iCal par personne,
   backups). **En prod depuis 2026-08-24** : `https://houseos.alainnormandin.dev`
   ([[Déploiement]]).
3. **Phase 2 — la maison qui sait des choses** — livrée entre le 2026-08-24 et le
   2026-09-21 : [[Météo]] + règles, [[Titre D'humeur]], [[Flux Externes]] (ICS puis
   poussés), [[Documents]] et [[Courriel Entrant]], [[Budget]], [[Synchro]],
   [[Observabilité]], [[Distribution]] (dépôt public AGPL), [[Vue Téléphone]],
   [[Fonds De Tiroir]], [[Journal De La Maison]] et [[Lettre Du Matin]]. Écartés :
   Hydro-Québec (pas d'inscription) et consommables (pas de besoin prouvé).
4. **Emménagement V2 — la maison qui s'entretient elle-même** (en cours,
   [[Emménagement V2]], approuvé 2026-09-28) : catégorie d'équipement, packs
   d'entretien de zone 4, skill de semis, budget sur le vrai compte, dette connue.
5. **Phase 3 — IoT** : l'écran e-ink est livré ([[Affichage E-ink]]) ; hub MQTT,
   NFC et capteurs après la remise en route du lab dans la nouvelle maison
   (2026-10-06), vraisemblablement en novembre 2026.
