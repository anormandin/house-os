---
type: plan
status: executed
date: 2026-08-24
feature: "[[Déploiement]]"
---

# Plan 2026-08-24 Déploiement V1

Conteneuriser, pousser sur GitHub privé, provisionner le LXC sur pve, router dans
NPM, brancher les backups. Gouverné par
[[D-2026-08-24 Prod LXC Proxmox NPM GitHub]].

## Étapes

### Repo (conteneurisation)

- [x] `Dockerfile` multi-étages : `node` (build `web/` → `dist`) + `dotnet/sdk`
      (publish `HouseOs.Api`) → `dotnet/aspnet` (Debian, tzdata inclus), `dist`
      copié dans `wwwroot`, port 8080.
- [x] `.dockerignore` (node_modules, bin/obj, backups, donnees, vault, docs…).
- [x] `docker-compose.yml` : `TZ=America/Toronto` +
      `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` + `ANTHROPIC_API_KEY` + mots de
      passe seed via `Seed__Utilisateurs__…` ; Postgres publié sur `127.0.0.1:5433`.
- [x] `.env.example` (POSTGRES_PASSWORD, HOUSEOS_MCP_KEY, ANTHROPIC_API_KEY,
      SEED_MDP_ALAIN, SEED_MDP_ARIANE) ; `.env` déjà gitignoré — vérifier.
- [x] **Test local sur le Mac** : `docker compose up --build` avec un `.env` de
      test, login + une page + `/mcp` 401 sans clé, puis nettoyage.

### GitHub

- [x] `gh repo create anormandin/house-os --private` + push `main` (demander à
      Alain de se connecter si `gh` n'est pas authentifié).

### LXC sur pve (SSH `proxmox_auto`)

- [x] Créer le LXC `house-os` : Debian 13, nesting activé, 2 vCPU / 2 Go / 16 Go,
      IP fixe sur `192.168.4.0/24`, démarrage automatique.
- [x] Installer Docker + git ; cloner le repo dans `/opt/house-os` (deploy key en
      lecture seule) ; écrire `.env` (secrets générés sur place, clé Anthropic
      fournie par Alain) ; `docker compose up -d --build`.
- [x] Cron quotidien `scripts/backup.sh` (3 h) ; vérifier que le job PBS couvre le
      nouveau LXC (l'ajouter sinon).

### NPM + accès

- [x] Proxy host `houseos.alainnormandin.dev` → IP du LXC:8080 (cert wildcard,
      WebSockets au besoin) — via l'admin NPM (accès à demander à Alain).
- [x] Vérifier du navigateur : `https://houseos.alainnormandin.dev` → login,
      Aujourd'hui avec météo/humeur (workers vivants), heure locale correcte,
      cookie Secure, manifest PWA servi.
- [x] `/mcp` : 401 sans clé, 200 avec la clé du `.env` ; noter la commande
      `claude-maison` (variables `HOUSEOS_MCP_URL`/`HOUSEOS_MCP_KEY`).

### Clôture

- [x] Vault : spec as-built, Recap, stamps ; mémoire projet si leçons apprises.

Notes de vérification : le login prod et le cookie Secure ont été validés par
`curl` (l'agent n'entre jamais de mots de passe dans un formulaire) ; les workers
par les logs du conteneur (météo ingérée, humeur générée via LLM) ; la page de
connexion HTTPS vue dans le navigateur — la traversée post-login dans le
navigateur reste à Alain. Fait après coup par Alain dans UniFi (vérifié le
2026-08-24) : réservation DHCP de 192.168.4.146 (MAC BC:24:11:88:6B:5C) et
enregistrement DNS local `houseos.alainnormandin.dev` → NPM (une première version
pointait vers le LXC — corrigée : rien n'écoute sur 443 dans le LXC).
