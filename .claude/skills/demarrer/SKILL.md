---
name: demarrer
description: Lancer House OS en dev — Postgres (docker compose, port 5433), API .NET (localhost:5000), web Vite (localhost:5173) — exécuter les tests, et vérifier/brancher le serveur MCP local. Utiliser pour démarrer l'app, la tester, ou diagnostiquer le MCP.
---

# Démarrer House OS en dev

## Services

```bash
# 1. Postgres (une fois ; volume persistant, port hôte 5433)
docker compose up -d postgres

# 2. API (.NET 10) — applique les migrations et le seed au démarrage
dotnet run --project server/HouseOs.Api        # → http://localhost:5000

# 3. Frontend (Vite, proxy /api et /ical vers :5000)
cd web && npm run dev                          # → http://localhost:5173
```

Comptes seed : `alain` / `ariane` (mot de passe dans `server/HouseOs.Api/appsettings.json`,
section `Seed`). Santé : `GET http://localhost:5000/api/sante`.

Si le port 5000 est occupé par une vieille instance : `pkill -f HouseOs.Api`.
(ControlCenter/AirPlay écoute aussi sur `*:5000` mais ne bloque pas le bind sur
127.0.0.1.)

## Tests

```bash
dotnet test server/HouseOs.sln
```

Aucune DB requise (tests purs domaine + conversions). Le backend ne hot-reload pas :
relancer `dotnet run` après un changement C#.

## MCP (serveur intégré, endpoint /mcp)

- Config Claude Code : `.mcp.json` à la racine — URL `${HOUSEOS_MCP_URL:-http://localhost:5000/mcp}`,
  clé `${HOUSEOS_MCP_KEY:-dev-cle-mcp-houseos}`.

### Choisir l'environnement (dev vs prod)

L'expansion `${…}` se fait **au démarrage de Claude Code** : sans variables, on parle
au serveur **dev** (localhost:5000). Pour viser la **prod** (serveur maison via
Tailscale), lancer la session avec les deux variables :

```bash
HOUSEOS_MCP_URL="http://<hôte-tailscale>:8080/mcp" \
HOUSEOS_MCP_KEY="<clé du .env du serveur>" \
claude
```

(pratique : en faire un alias `claude-maison` dans ~/.zshrc). Changer d'environnement
= relancer la session. En cas de doute sur l'environnement courant, `lister_occurrences`
révèle vite quelles données on regarde ; les données dev sont jetables, la prod non.
Quand l'app sera en service permanent, on pourra inverser le défaut de `.mcp.json`
vers l'URL Tailscale et surcharger pour le dev.
- Vérifier la connexion : `claude mcp list` → `house-os` doit être connecté.
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
- La clé serveur vit dans `Mcp:Cle` (`appsettings.Development.json` en dev, env
  `Mcp__Cle` en prod via docker-compose). Clé vide = tout refusé (fail closed).
