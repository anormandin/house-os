---
type: decision
status: accepted
date: 2026-08-24
feature: "[[Déploiement]]"
tags: []
---

# Prod LXC Proxmox NPM GitHub

## Contexte

[[D-2026-08-23 Hébergement Maison Tailscale Docker]] a fixé le principe (machine
maison + Tailscale + Docker Compose) sans nommer la machine. Le lab existant
(Proxmox `pve`, NPM avec wildcard `*.alainnormandin.dev`, subnet router Tailscale,
PBS nocturne) permet de concrétiser. Le repo n'avait aucun remote git.

## Options considérées

- Cible : **LXC Docker sur pve** / VM Debian sur pve / archdev (PC dev).
- Accès téléphones : **NPM + HTTPS** (`houseos.alainnormandin.dev`, cert wildcard,
  requis pour la PWA) / port 8080 en HTTP nu.
- Secrets : **`.env` sur le serveur** (gabarit `.env.example` committé) / page de
  changement de mot de passe dans l'app (reportée).
- Backups : **cron `backup.sh` local + PBS** / PBS seul / dumps locaux seuls.
- Livraison du code : **GitHub privé** `anormandin/house-os` (pull + build sur le
  serveur, backup hors site du code et du vault) / rsync depuis le Mac / registry.

## Décision

Les options en gras — toutes des choix de l'utilisateur (2026-08-24). Concrètement :
LXC Debian avec nesting sur `pve` (démarre avec l'hôte), `docker compose up -d
--build` depuis un clone du repo GitHub privé ; NPM route
`houseos.alainnormandin.dev` → LXC:8080 avec le cert wildcard ; les téléphones
passent par le subnet router Tailscale déjà en place (rien à installer côté
serveur) ; secrets dans `/opt/house-os/.env` ; `scripts/backup.sh` en cron
quotidien dans le LXC + le LXC ajouté au job PBS de 2 h.

Contraintes d'exécution qui découlent du lab :
- **`TZ=America/Toronto` obligatoire** dans le conteneur app (le code vit en
  `DateTime.Now`/`TimeZoneInfo.Local` — récurrence, humeur, rollover).
- **`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`** derrière NPM (cookie de session
  marqué Secure via X-Forwarded-Proto).
- Postgres publié sur `127.0.0.1` seulement (admin local ; l'app passe par le
  réseau compose).
- Les mots de passe des comptes viennent du `.env` via la config `Seed__…` —
  **appliqués seulement à la création** (base vierge) ; en changer ensuite = SQL.

## Conséquences

- Le lab déménage avec la maison — rien à changer au déploiement le 6 octobre,
  seule l'adresse du LAN pourrait bouger.
- Mise à jour = `git pull && docker compose up -d --build` dans le LXC.
- Restauration totale = recréer le LXC + `.env` + dernier dump/`fichiers-*.tar.gz`
  (aide-mémoire dans `scripts/backup.sh`), ou restaurer le snapshot PBS.
- `.mcp.json` : la prod se vise avec `HOUSEOS_MCP_URL=https://houseos.alainnormandin.dev/mcp`.

## Confirmation

`Dockerfile` + `.env.example` à la racine ; `TZ` et `ASPNETCORE_FORWARDEDHEADERS_ENABLED`
dans `docker-compose.yml` ; remote `origin` → `github.com/anormandin/house-os` ;
LXC `house-os` sur pve avec cron `backup.sh`.
