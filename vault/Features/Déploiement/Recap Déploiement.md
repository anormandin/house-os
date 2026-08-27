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

## Volet Flux iCal public (2026-08-27 — [[Plan 2026-08-27 Flux iCal Public]])

Exposition du seul chemin `/ical` sur Internet pour l'abonnement Google Agenda
([[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]]) :

- Sidecar `tailscale/tailscale` dans le compose (profil `funnel`, userspace,
  état en volume), `infra/tailscale-serve.json` monte `/ical` → `app:8080/ical`
  et rien d'autre ; `.env.example` : `COMPOSE_PROFILES`, `TS_AUTHKEY`,
  `ICAL_URL_PUBLIQUE_BASE`.
- Rotation self-service du jeton : `POST /api/ical/rotation`, helper partagé
  `JetonIcal` (génération + réponse chemin/urlPublique), parité MCP
  (`mon_flux_ical` gagne `regenerer`), bouton « Régénérer » à deux temps dans
  « Mon calendrier », qui affiche désormais les deux URLs étiquetées.
- Tests : backend 398 → 403 (rotation révoque l'ancienne URL, composition de
  l'URL publique), web 67 → 69 (deux URLs affichées, rotation confirmée remplace
  l'URL).

Déviation : la config publique n'est pas testée via la factory d'intégration
(elle resterait figée pour toute la collection) — composition couverte en
unitaire, défaut null en intégration.

Déployé et vérifié en prod le 2026-08-27 : policy + auth key faits par Alain,
sidecar en ligne (tagué, cert émis), flux servi 200/65 évènements par le chemin
complet, préfixe `/ical` intact, racine et `/api` non servis. Seul accroc : le
control plane Tailscale a mis **~2 h 20** (doc : ~10 min) à publier le DNS
public du nom Funnel — résolu sans intervention (symptôme de
tailscale/tailscale#18652). En chemin, la clé du nœud `pve` (subnet router)
s'était avérée expirée depuis 3 jours — ré-authentifiée ; désactiver
l'expiration de clé sur pve reste à faire côté Alain. Google Agenda abonné le
jour même, et le flux se rend jusqu'au **Skylight** du foyer via Google — un
affichage mural du calendrier maison sans intégration à écrire (contexte utile
pour l'écran mural de la phase 3).
