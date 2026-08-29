---
name: demarrer
description: Lancer House OS en dev — Postgres (docker compose, port 5433), Seq (logs, localhost:8081), API .NET (localhost:5000), web Vite (localhost:5173) — exécuter les tests, lire les logs structurés, et vérifier/brancher le serveur MCP local. Utiliser pour démarrer l'app, la tester, lire une trace d'incident, ou diagnostiquer le MCP.
---

# Démarrer House OS en dev

## Services

```bash
# 1. Postgres + Seq (une fois ; volumes persistants)
docker compose up -d postgres seq              # → Seq : http://localhost:8081

# 2. API (.NET 10) — applique les migrations et le seed au démarrage
dotnet run --project server/HouseOs.Api        # → http://localhost:5000

# 3. Frontend (Vite, proxy /api et /ical vers :5000)
cd web && npm run dev                          # → http://localhost:5173
```

Comptes seed : `alain` / `ariane` (mot de passe dans `server/HouseOs.Api/appsettings.json`,
section `Seed`). Santé : `GET http://localhost:5000/api/sante`.

Si le port 5000 est occupé par une vieille instance : `pkill -f HouseOs.Api`.
(ControlCenter/AirPlay écoute aussi sur `*:5000` mais ne bloque pas le bind sur
127.0.0.1. Il répond **403** : une négociation de hub en 403 sans ligne
correspondante dans le log serveur, c'est lui — pas l'app.)

## Logs (Seq)

L'API émet des logs structurés vers Seq ([[Observabilité]] dans le vault) : une ligne
par requête avec statut et durée, les durées de phase de la complétion, les gestes du
navigateur. UI sur **http://localhost:8081** (`admin` / le mot de passe du `.env`) ;
ingestion sur 5342 — **pas 5341**, occupé par le Seq personnel du Mac.

Pour lire la trace complète d'une requête : copier la référence affichée sous le
message de la bannière d'erreur (ou l'en-tête `X-Trace-Id` de la réponse) et filtrer
`TraceId = '<valeur>'` dans Seq. La piste du navigateur y est aussi, sous
`SourceContext like 'HouseOs.Client.%'`.

L'API tourne sans Seq (le sink tamponne puis abandonne) : un Seq éteint ne bloque
jamais le dev.

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
au serveur **dev** (localhost:5000). Pour viser la **prod** (LXC 105 sur pve,
derrière NPM — joignable du LAN et via Tailscale), lancer la session avec les deux
variables :

```bash
HOUSEOS_MCP_URL="https://houseos.alainnormandin.dev/mcp" \
HOUSEOS_MCP_KEY="<HOUSEOS_MCP_KEY du /opt/house-os/.env sur le LXC>" \
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
