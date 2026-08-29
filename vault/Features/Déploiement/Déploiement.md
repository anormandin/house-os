---
type: feature
status: implemented
last-verified: 2026-08-29
verified-against: 5082ea1
tags: []
---

# Déploiement

## Intention

Faire passer House OS du `dotnet run` sur le Mac à un service de maison qui tourne
tout seul : conteneur sur le lab Proxmox, HTTPS via NPM, accessible des téléphones
(Tailscale déjà en place), sauvegardé chaque nuit, re-déployable en une commande.
Prêt avant le déménagement du 2026-10-06 — et le lab déménage avec la maison.

## Comportement

- **Image unique** : Dockerfile multi-étages (build web Vite → `wwwroot`, publish
  .NET) ; l'API sert la SPA, applique les migrations et le seed au démarrage.
  Depuis la ronde QA 2026-08-28 : conteneur **non-root** (`USER $APP_UID`) et
  image tailscale **épinglée** (plus de `:latest`).
- **`docker compose up -d --build`** dans le LXC `house-os` sur `pve` : Postgres 17
  + app (port 8080), volumes nommés (`postgres-data`, `fichiers`), redémarrage
  automatique, `TZ=America/Toronto`, en-têtes proxy restreints aux proxys connus
  (`RESEAU_PROXIES_CONNUS`, défaut l'IP du NPM — remplace l'ancien
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED` tous-azimuts), healthcheck compose sur
  `/api/sante` (qui sonde la DB)
  ([[D-2026-08-24 Prod LXC Proxmox NPM GitHub]]).
- **Accès** : `https://houseos.alainnormandin.dev` (NPM, cert wildcard) — app
  installable (manifest seulement, sans service worker :
  [[D-2026-08-25 Retrait Du Service Worker]]) ; à l'extérieur, Tailscale (subnet
  router du lab). Le MCP prod se branche sur `/mcp` de la même URL.
- **Flux iCal public** ([[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]]) :
  seul chemin exposé à Internet — le sidecar Tailscale du compose (profil
  `funnel`, activé par `COMPOSE_PROFILES` dans le `.env` du serveur, absent en
  dev) publie `/ical` et rien d'autre via Funnel sur
  `https://houseos.<tailnet>.ts.net`, pour l'abonnement Google Agenda.
  `ICAL_URL_PUBLIQUE_BASE` fait afficher l'URL publique dans « Mon calendrier ».
  Activation manuelle : attribut `funnel` dans la policy du tailnet + auth key
  taguée (`TS_AUTHKEY`).
- **DNS** : enregistrement local UniFi `houseos.alainnormandin.dev` → NPM
  (192.168.4.50, jamais vers le LXC : c'est NPM qui termine le TLS) — l'app reste
  joignable du wifi même sans Internet ; + réservation DHCP de l'IP du LXC
  (192.168.4.146). Règle : tout nom `*.alainnormandin.dev` servi en HTTPS pointe
  vers NPM.
- **Secrets** : `.env` sur le serveur seulement (`.env.example` committé) —
  mot de passe Postgres, clé MCP, clé Anthropic, mots de passe initiaux des
  2 comptes. Depuis la ronde QA 2026-08-28, le compose **refuse de démarrer**
  sans `POSTGRES_PASSWORD`, `SEED_MDP_ALAIN`, `SEED_MDP_ARIANE` (syntaxe `:?`,
  comme la clé MCP) — plus aucun repli committé ; les valeurs dev vivent dans
  `appsettings.Development.json`. Cookie : `SecurePolicy.SameAsRequest`
  (Secure via NPM/HTTPS ; l'accès http direct sur le LAN reste un risque
  résiduel accepté — foyer de 2, trafic Tailscale chiffré).

> [!note] Premier déploiement après la ronde QA — fait le 2026-08-28
> Le `.env` du LXC était déjà complet (`POSTGRES_PASSWORD` aléatoire fonctionnel —
> la crainte du repli `houseos-dev` ne s'est pas matérialisée —, `SEED_MDP_*` posés) ;
> `RESEAU_PROXIES_CONNUS` laissé au défaut NPM (cookie `Secure` confirmé au login
> HTTPS). `chown` unique du volume fichiers exécuté
> (`docker run --rm -v house-os_fichiers:/f mcr.microsoft.com/dotnet/aspnet:10.0 chown -R 1654:1654 /f`,
> commande aussi en commentaire du `Dockerfile`). Vérifié : santé 200 (db ok),
> login 200 + cookie `secure`, flux Funnel `.ics` 200/66 évènements avec
> `cache-control: private, no-store`, racine Funnel 404, nouveau bundle servi.
- **Backups** : `scripts/backup.sh` en cron quotidien dans le LXC (dumps +
  archive fichiers, rétention 30 j) + snapshot PBS nocturne du LXC.
- **Code** : GitHub privé `anormandin/house-os` ; mise à jour par
  `git pull && docker compose up -d --build`.

## Hors périmètre

- Page de changement de mot de passe dans l'app (les mots de passe initiaux
  viennent du `.env` ; changer ensuite = SQL).
- CI/CD, registry d'images — inutile à cette échelle.
- Métriques et alertes — rien ne réveille personne la nuit dans une maison de deux.
  (La journalisation, elle, est entrée au compose le 2026-08-29 : voir
  [[Observabilité]] et [[D-2026-08-29 Journalisation Structurée Serilog Et Seq]].)
- Exposition publique de l'app ou du MCP (jamais) — seule exception : le flux iCal
  ([[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]]).

## Décisions

- [[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]] — le principe (hébergement
  maison + Tailscale, reconduit) + l'unique exception publique `/ical` ; supersède
  [[D-2026-08-23 Hébergement Maison Tailscale Docker]].
- [[D-2026-08-24 Prod LXC Proxmox NPM GitHub]] — la concrétisation (cible, HTTPS,
  secrets, backups, remote git).

## Ancres de code

- `Dockerfile`, `.dockerignore`, `.env.example`, `docker-compose.yml` — racine.
- `infra/tailscale-serve.json` — config serve/funnel du sidecar (monte `/ical` seulement).
- `scripts/backup.sh` — backup + aide-mémoire de restauration.
- `server/HouseOs.Api/Infrastructure/AmorcageDb.cs` — migrations + seed au démarrage.

## Sources

- `~/Documents/dev.nosync/unifi/CLAUDE.md` (lab Proxmox/NPM/PBS — hors repo).

## Historique

- [[Plan 2026-08-24 Déploiement V1]] · [[Plan 2026-08-27 Flux iCal Public]] ·
  [[Recap Déploiement]]
