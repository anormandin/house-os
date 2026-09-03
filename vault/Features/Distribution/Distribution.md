---
type: feature
status: implemented
last-verified: 2026-09-02
verified-against: 884b383
tags: []
---

# Distribution

## Intention

Qu'un autre foyer installe House OS chez lui avec un `git clone`, un `.env` et un
`docker compose up -d --build`, et qu'un contributeur sache en cinq minutes comment
démarrer, tester et proposer un changement — sans jamais tomber sur le foyer ou le
lab du mainteneur.

## Comportement

- **Dépôt public** `anormandin/house-os`, licence **AGPL-3.0** (`LICENSE`), docs en
  français : `README.md` (quoi, installer, développer, structure),
  `CONTRIBUTING.md` (prérequis, tests, conventions, flux PR, portée produit),
  `docs/installation.md` (auto-hébergement pas à pas), `docs/configuration.md`
  (référence de toutes les variables et clés).
- **Contrat d'installation = le `.env`** (`docker-compose.yml` + `.env.example`).
  Requis (`:?`, le compose refuse sinon) : `POSTGRES_PASSWORD`, `METEO_LATITUDE`,
  `METEO_LONGITUDE`, `COMPTE_1_NOM`, `COMPTE_1_MDP`, `HOUSEOS_MCP_KEY`. Facultatif :
  `COMPTE_1_AFFICHAGE`, `COMPTE_2_*`, `FUSEAU_HORAIRE` (défaut `America/Toronto`,
  alimente `TZ` et `Meteo:FuseauHoraire`), `APP_PORT_HOTE`, `POSTGRES_PORT_HOTE`,
  `RESEAU_PROXIES_CONNUS` (vide = aucun proxy de confiance), `ANTHROPIC_API_KEY`,
  `JOURNALISATION_SEQ_URL/CLE`, Funnel, `COURRIEL_R2_*`.
- **Comptes** : `Seed:Utilisateurs` est vide dans `appsettings.json` ; le compose
  alimente deux entrées depuis `COMPTE_n_*` ; l'amorçage ignore une entrée sans nom
  (second compte facultatif) ou sans mot de passe, et prend le nom d'utilisateur
  comme nom d'affichage si celui-ci est vide (`server/HouseOs.Api/Infrastructure/AmorcageDb.cs`).
  Les fixtures dev (`alain`/`ariane`) vivent en entier dans
  `appsettings.Development.json` ; `infra/exemples/.env.dev` fournit les valeurs
  jetables que Compose exige même pour `up -d postgres` (il interpole tout le fichier).
- **Aucun réglage d'hébergement dans le compose principal** : plus de driver GELF ni
  d'IP de collecteur ; un `docker-compose.override.yml` gitignoré porte ce genre de
  réglage (gabarit `infra/exemples/docker-compose.override.gelf.yml`).
- **Rien de personnel dans le code** : descriptions d'outils MCP sans prénoms
  (renvoi à `lister_utilisateurs` ; l'erreur `agirComme` liste les comptes réels), placeholder de login « nom
  d'utilisateur », teintes d'avatar par rang dans le foyer
  (`web/src/components/Avatar.tsx`, registre alimenté par `Layout`), prompts LLM
  « un couple québécois » / « une maison au Québec », exemples de dossier
  génériques, `wrangler.toml` sans adresses (`EXPEDITEURS_PERMIS` passé par
  `wrangler deploy --var`), défauts météo = ville de Québec.
- **Claude Code** : `.mcp.json` vise `http://localhost:5000/mcp` par défaut ;
  `CLAUDE.md` est le contexte projet, `CLAUDE.local.md` (gitignoré) le contexte
  personnel ; les skills `demarrer` et `planifier-taches` sont génériques.
- **CI** (`.github/workflows/ci.yml`) : `dotnet test` (intégration via
  Testcontainers), `npm run lint && npm test && npm run build`, `docker build`.
- **Hygiène** : `server/global.json` (SDK 10, `latestFeature`), `.editorconfig`,
  `.gitattributes`, `design/README.md` ; `web/README.md` (gabarit Vite) et
  `web/components.json` (shadcn, plus utilisé) supprimés.

## Hors périmètre

- Page de premier démarrage / création de comptes dans l'UI ; changement de mot de
  passe (limite documentée dans `docs/installation.md`).
- Publication d'une image sur un registre ; CD.
- Traduction de l'UI ou des docs.
- Nettoyer le vault ou `docs/research/` de leurs mentions personnelles (historique).

## Décisions

- [[D-2026-09-02 Dépôt Public AGPL Et Instance Générique]] — visibilité, licence,
  généricité par config, split du contexte personnel.

## Ancres de code

- `docker-compose.yml`, `.env.example` — le contrat d'installation.
- `server/HouseOs.Api/Infrastructure/AmorcageDb.cs` — seed des comptes.
- `web/src/components/Avatar.tsx` — teintes par rang.
- `infra/courriel-worker/wrangler.toml`, `infra/courriel-worker/README.md`.
- `.github/workflows/ci.yml`.

## Historique

- [[Plan 2026-09-02 Ouverture Du Dépôt]] · [[Recap Distribution]]
