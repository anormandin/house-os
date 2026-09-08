---
type: plan
status: executed
date: 2026-09-02
feature: "[[Distribution]]"
---

# Ouverture Du Dépôt

## But

Rendre le dépôt partageable : instance générique par configuration, données
personnelles hors du code, docs d'installation et de contribution, CI, licence.

## Étapes

- [x] A1 Comptes depuis l'env : `appsettings.json` vidé, fixtures dev complètes dans
  `appsettings.Development.json`, compose `COMPTE_n_*`, garde « sans nom » dans
  `AmorcageDb.cs`, descriptions MCP, placeholder de login + locator E2E, avatar par
  rang, prompts LLM.
- [x] A2 Lieu/fuseau/réseau/logs : `METEO_*`, `FUSEAU_HORAIRE`, `RESEAU_PROXIES_CONNUS`
  vide par défaut, Seq facultatif, GELF sorti vers `infra/exemples/`, ports hôtes
  paramétrables, `.env.example` réécrit, `.mcp.json` → localhost.
- [x] A3 Données personnelles : adresse civique, courriels de tests, `wrangler.toml`,
  commentaires, `InsertData` de la migration ComptesARebours.
- [x] B Hygiène et docs : `LICENSE`, `README.md`, `CONTRIBUTING.md`,
  `docs/installation.md`, `docs/configuration.md`, `design/README.md`, CI,
  `global.json`, `.editorconfig`, `.gitattributes`, `CLAUDE.md` générique +
  `CLAUDE.local.md`, skills, README du worker, suppression de `web/README.md` et
  `web/components.json`.
- [x] C Vault : décision, cette feature, mises à jour Déploiement / Serveur MCP /
  Auth / Observabilité / Architecture / Home.
- [x] Vérification clone vierge (compose depuis une copie propre, comptes inventés) —
  santé 200, login, 2 comptes, 0 compte à rebours, MCP 401/200, faite le 2026-09-02.
- [x] Commit `6836675`, push, CI verte au premier passage (2026-09-03).
- [x] D Migration de la prod (2026-09-03) : `.env` du LXC réécrit (sauvegarde
  `.env.bak-20260903`), `docker-compose.override.yml` GELF, `git pull` +
  `up -d --build` ; santé 200, nouveau bundle, 2 comptes inchangés, driver gelf
  confirmé ; fiche LXC 105 mise à jour dans le dépôt infra.
- [x] Passage du dépôt GitHub en public (geste du mainteneur) — fait le 2026-09-08 : description, sujets, secret scanning + push protection activés.

## Vérification

- `dotnet test server/HouseOs.sln` ; `cd web && npm run lint && npm test && npx tsc -b`.
- Copie propre du dépôt + `.env` inventé + `docker compose up -d --build` : santé 200,
  login avec les comptes du `.env`, aucun compte à rebours préinstallé, MCP
  `tools/list` 200 avec la clé, 401 sans.
- Grep de confirmation de la décision.
