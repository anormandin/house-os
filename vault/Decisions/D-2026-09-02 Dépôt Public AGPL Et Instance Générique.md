---
type: decision
status: accepted
date: 2026-09-02
feature: "[[Distribution]]"
tags: []
---

# Dépôt Public AGPL Et Instance Générique

## Contexte

Des amis veulent voir le code, y contribuer et installer leur propre instance. Le
dépôt était privé, sans licence, et tout — compose, `appsettings.json`, prompts LLM,
`.mcp.json`, skills, `CLAUDE.md` — supposait le foyer et le lab du mainteneur
(comptes `alain`/`ariane`, IP du collecteur Seq, coordonnées de la maison, adresses
courriel dans `wrangler.toml`).

## Options considérées

- **Visibilité / licence** : privé + collaborateurs invités (aucune obligation, mais
  friction pour cloner et forker) ; public MIT (le plus simple, aucune obligation de
  partage) ; **public AGPL-3.0** (quiconque sert une version modifiée à d'autres
  partage sa source).
- **Généricité** : docs seulement (chacun édite `appsettings.json` — fragile aux
  mises à jour) ; **configuration seulement** (tout ce qui est propre à un foyer vient
  du `.env`, défauts neutres, aucune UI) ; configuration + page de premier démarrage
  (plus de travail, meilleur accueil — reportée).
- **Contexte personnel** : le garder dans le dépôt (bruit, et les sessions Claude des
  contributeurs suivraient les consignes du lab) ; **le sortir** vers `CLAUDE.local.md`
  gitignoré, le vault gardant l'historique tel quel.
- **Langue des docs** : français (cohérent avec l'UI, le domaine, le vault) ; anglais ;
  les deux.

## Décision

Les options en gras, choix de l'utilisateur (2026-09-02) : dépôt public sous
AGPL-3.0, docs en français, généricité par configuration seulement, contexte
personnel dans `CLAUDE.local.md`. Le vault reste tel quel (c'est l'historique du
projet, pas un secret).

## Conséquences

- Contrat d'installation = les variables du `.env` ([[Distribution]]) ; les
  variables `SEED_MDP_*` deviennent `COMPTE_n_*`, Seq et le driver GELF deviennent
  facultatifs (override compose), la position météo est requise.
- Défaut de `.mcp.json` = `localhost:5000` ; la prod du mainteneur se vise par ses
  exports de shell.
- Aucun nom de personne, adresse, IP ou domaine dans le code, les prompts, les
  descriptions d'outils MCP, les gabarits ou les défauts. Les fixtures de tests
  gardent des prénoms de test.
- L'`InsertData` des comptes à rebours d'origine est retiré de la migration (données
  du premier foyer) — sans effet sur une base déjà migrée.
- CI GitHub Actions et fichiers d'hygiène (LICENSE, CONTRIBUTING, `.editorconfig`,
  `global.json`, `.gitattributes`).

## Confirmation

- `LICENSE` commence par « GNU AFFERO GENERAL PUBLIC LICENSE ».
- `grep -rn "alain\|ariane\|alainnormandin\|192\.168\." server/HouseOs.Api web/src docker-compose.yml .env.example .mcp.json infra --exclude-dir=bin --exclude-dir=obj --exclude-dir=node_modules`
  ne retourne que `appsettings.Development.json` (fixtures dev).
- `test -f CLAUDE.local.md && git check-ignore -q CLAUDE.local.md`.
