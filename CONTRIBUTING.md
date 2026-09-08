# Contribuer à House OS

Merci de vouloir mettre la main à la pâte. Ce guide couvre le démarrage en dev, les
tests, les conventions du dépôt et le flux de contribution.

## Prérequis

- .NET 10 SDK (`server/global.json` fixe la version)
- Node 26 (le Dockerfile bâtit avec `node:26-alpine`)
- Docker (Postgres en dev, et les tests d'intégration démarrent un Postgres jetable)

## Démarrer en dev

```bash
cp infra/exemples/.env.dev .env          # valeurs de dev jetables (Compose interpole tout le fichier)
docker compose up -d postgres            # Postgres sur 127.0.0.1:5433
dotnet run --project server/HouseOs.Api  # http://localhost:5000 — applique migrations et seed
cd web && npm install && npm run dev     # http://localhost:5173 — proxy /api, /ical, /hubs vers :5000
```

Comptes seed : `alain` / `ariane`, mot de passe `45234523` (dans
`server/HouseOs.Api/appsettings.Development.json`). Ce mot de passe est public et
réservé au dev local : ne jamais le réutiliser sur une instance réelle, qui définit
ses comptes dans le `.env` (`COMPTE_n_MDP`). Santé : `GET /api/sante`.
Le backend ne recharge pas à chaud : relancer `dotnet run` après un changement C#.

Réglages personnels de dev (clé Anthropic, URL d'un Seq…) : dans
`server/HouseOs.Api/appsettings.local.json`, gitignoré.

## Tests

| Couche | Où | Commande | Besoin |
|---|---|---|---|
| Domaine + opérations | `server/HouseOs.Tests/{Domaine,Features}` | `dotnet test server/HouseOs.sln` | — (SQLite en mémoire) |
| Intégration API | `server/HouseOs.Tests/Integration` | idem | **Docker** (Testcontainers Postgres) |
| Composants web | `web/src/**/*.test.tsx` | `cd web && npm test` | — (MSW) |
| Fumée E2E | `web/e2e` | `cd web && npm run test:e2e` | pile dev démarrée (Postgres + API) |

Aussi : `cd web && npm run lint` (oxlint) et `npx tsc -b`. La CI (`.github/workflows/ci.yml`)
exécute tout sauf l'E2E, plus le build de l'image Docker.

## Conventions

- **Langue** : UI, domaine, noms de fichiers du vault, messages de commit en français.
  Code technique (infra, helpers) en anglais quand c'est plus naturel.
- **Backend** : tranches verticales dans `server/HouseOs.Api/Features/<Module>/`
  (endpoints + opérations + DTOs). Domaine riche dans `Domaine/`, testé sans base.
  Récurrence stockée type + paramètres, jamais de RRULE/cron. Prochaine échéance
  matérialisée à la complétion. Journal de complétion = table séparée.
- **Parité MCP / API** : toute tranche REST qui ajoute ou change une opération met à
  jour les outils MCP (`Features/Mcp/`) dans la même PR. Les fichiers (upload)
  restent web seulement.
- **Web** : chaînes en dur en français, pas de lib i18n ; desktop d'abord ; pas de
  service worker.
- **Migrations** : générées par `dotnet ef migrations add` — jamais de schéma modifié
  à la main.
- **Vault** : `vault/` est la source de vérité (specs, décisions). Un changement de
  comportement, d'API ou de modèle met à jour la note de feature touchée dans la même
  PR ; un choix coûteux à renverser (schéma, sécurité, convention) ajoute une décision
  `vault/Decisions/D-AAAA-MM-JJ <Titre>.md`. Voir `vault/Home.md` pour l'échelle de
  cérémonie. Les notes s'ouvrent dans Obsidian (liens `[[wiki]]`).
- **Commits** : `type: description` en français (`feat:`, `fix:`, `docs:`, `chore:`,
  `refactor:`, `test:`), un sujet court, le pourquoi dans le corps si besoin.

## Proposer un changement

1. Ouvrir une issue pour discuter d'abord si c'est plus qu'une correction.
2. Brancher depuis `main`, garder la PR petite et ciblée.
3. Tests verts localement, CI verte sur la PR ; ajouter des tests avec le comportement.
4. Décrire dans la PR ce qui change pour l'utilisateur et, s'il y a lieu, la note du
   vault mise à jour.

## Portée du produit

House OS vise un foyer d'adultes : aucun module enfants, points, récompenses ou
tableau de corvées gamifié ne sera accepté — c'est une décision produit, pas un
manque. Les intégrations d'outils tiers (Grocy, Home Assistant…) ne sont pas non plus
dans la cible ; l'IoT viendra par MQTT en phase 3.
