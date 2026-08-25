---
type: recap
date: 2026-08-24
feature: "[[Déploiement]]"
plan: "[[Plan 2026-08-24 Déploiement V1]]"
---

# Recap Déploiement

House OS est en prod : **https://houseos.alainnormandin.dev** (LXC 105 `house-os`
sur pve, 192.168.4.146). Livré tel que planifié :

- Dockerfile multi-étages + `.dockerignore` + compose prod-ready (`TZ`, en-têtes
  proxy, secrets `.env`, Postgres sur 127.0.0.1) + `.env.example` — testé en
  local avant push (login, cookie Secure sous X-Forwarded-Proto, MCP fail-closed).
- GitHub privé `anormandin/house-os` (remote SSH) ; deploy key **lecture seule**
  générée dans le LXC.
- LXC Debian 13 unprivileged (nesting+keyctl, DHCP, onboot, 2 vCPU/2 Go/12 Go),
  Docker via get.docker.com, clone dans `/opt/house-os`, `.env` poussé par
  `pct push` (secrets générés, clé Anthropic copiée sans affichage),
  `docker compose up -d --build`.
- Cron `backup.sh` à 3 h (premier dump vérifié) ; le job PBS existant (`all`, 2 h)
  couvre le LXC automatiquement.
- NPM : proxy host avec cert wildcard, Force SSL, HTTP/2, WebSockets — créé via
  le navigateur (session admin d'Alain).

Vérifié en prod : santé 200, login 200 (HTTP/2, cookie Secure), redirection
http→301→https, manifest PWA 200, `date` du conteneur en EDT, météo ingérée,
**humeur générée via LLM**, `/mcp` 401 sans clé / 200 avec la clé du `.env`.

Déviations : aucune. Reste côté Alain : réservation DHCP de l'IP du LXC dans
UniFi ; mise à jour = `git pull && docker compose up -d --build` dans le LXC.
