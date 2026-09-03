---
name: demarrer
description: Lancer House OS en dev — Postgres (docker compose, port 5433), API .NET (localhost:5000), web Vite (localhost:5173) — exécuter les tests, lire les logs structurés, et vérifier/brancher le serveur MCP local. Utiliser pour démarrer l'app, la tester, lire une trace d'incident, ou diagnostiquer le MCP.
---

# Démarrer House OS en dev

## Services

```bash
# 1. Postgres (une fois ; volume persistant). Le .env de dev : cp infra/exemples/.env.dev .env
docker compose up -d postgres                  # port 5433 côté hôte

# 2. API (.NET 10) — applique les migrations et le seed au démarrage
dotnet run --project server/HouseOs.Api        # → http://localhost:5000

# 3. Frontend (Vite, proxy /api, /ical et /hubs vers :5000)
cd web && npm run dev                          # → http://localhost:5173
```

Comptes seed : `alain` / `ariane`, mot de passe `45234523`
(`server/HouseOs.Api/appsettings.Development.json`, section `Seed`). Santé :
`GET http://localhost:5000/api/sante`.

Si le port 5000 est occupé par une vieille instance : `pkill -f HouseOs.Api`. Le
backend ne hot-reload pas : relancer `dotnet run` après un changement C#.

## Vue e-ink (Chromium dans l'API)

L'API capture la page `/ecran` avec Playwright (`Features/Affichage/RenduEcran.cs`).
Il lui faut le Chromium de la version du paquet, une fois par machine :

```bash
pwsh server/HouseOs.Api/bin/Debug/net10.0/playwright.ps1 install chromium
```

(`pwsh` : `dotnet tool install --global PowerShell` s'il manque. Le `npx playwright`
du dossier `web/` installe la même révision quand les versions concordent.) En dev,
la capture vise Vite (`Affichage:UrlEcran` = `http://localhost:5173/ecran`,
`appsettings.Development.json`) : Vite doit tourner. Aperçu du bitmap exact :
`http://localhost:5000/api/affichage/apercu.png?pile=87` (connecté ; `&brut=1` pour
la capture avant seuillage), page vivante : `http://localhost:5173/ecran`.

## Logs

L'API émet des logs structurés (Serilog) : une ligne par requête avec statut et durée,
les durées de phase de la complétion, les gestes du navigateur (`HouseOs.Client.*`).
En console par défaut ; vers un Seq si `Journalisation:Seq:Url` est réglé
(`appsettings.local.json`, gitignoré). L'API tourne sans Seq — le sink tamponne puis
abandonne.

Pour relire une requête : copier la référence affichée sous la bannière d'erreur (ou
l'en-tête `X-Trace-Id`) et filtrer `TraceId = '<valeur>'` dans Seq.

## Tests

```bash
dotnet test server/HouseOs.sln     # domaine + opérations (SQLite) + intégration (Docker : Testcontainers Postgres)
cd web && npm run lint && npm test # oxlint + vitest (MSW, hermétique)
cd web && npm run test:e2e         # Playwright contre la pile dev (Postgres + API démarrés ; Vite auto)
```

## MCP (serveur intégré, endpoint /mcp)

- Config Claude Code : `.mcp.json` à la racine — URL `${HOUSEOS_MCP_URL:-http://localhost:5000/mcp}`,
  clé `${HOUSEOS_MCP_KEY}` (sans défaut : clé absente = 401, jamais un repli silencieux).
  L'expansion `${…}` se fait **au démarrage de Claude Code** : changer d'instance =
  relancer la session. `claude mcp list` affiche l'URL résolue.
- Clé dev côté serveur : `Mcp:Cle` = `dev-cle-mcp-houseos` (`appsettings.Development.json`) ;
  en prod, `HOUSEOS_MCP_KEY` du `.env` via le compose. Clé vide = tout refusé (fail closed).
- Lancer une session sur l'API dev :

```bash
HOUSEOS_MCP_URL="http://localhost:5000/mcp" HOUSEOS_MCP_KEY="dev-cle-mcp-houseos" claude
```

- Smoke test à la main (le transport exige l'en-tête `Accept` double) :

```bash
# 401 attendu sans clé
curl -s -o /dev/null -w '%{http_code}\n' -X POST http://localhost:5000/mcp \
  -H 'Content-Type: application/json' -H 'Accept: application/json, text/event-stream' \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'

# 200 + liste des outils avec la clé dev
curl -s -X POST http://localhost:5000/mcp \
  -H 'Authorization: Bearer dev-cle-mcp-houseos' \
  -H 'Content-Type: application/json' -H 'Accept: application/json, text/event-stream' \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

- Repli si l'expansion `${…}` de `.mcp.json` pose problème :
  `claude mcp add --transport http house-os http://localhost:5000/mcp --header "Authorization: Bearer dev-cle-mcp-houseos"`
