---
type: decision
status: accepted
date: 2026-08-29
feature: "[[Observabilité]]"
tags: []
---

# D-2026-08-29 Collecteur Dans Son Propre LXC

## Contexte

[[D-2026-08-29 Journalisation Structurée Serilog Et Seq]] a fait entrer Seq dans le
compose de house-os, faute d'avoir posé la question de son périmètre. À la relecture,
ça faisait dépendre les logs de **toutes** les apps du lab du cycle de vie d'une seule :
la commande de release de house-os le redémarrait, un `docker compose down -v` aurait
effacé les logs des autres, et le mot de passe vivait dans le `.env` de house-os.

Le lab compte déjà d'autres candidats à la journalisation (Nginx Proxy Manager,
homepage) et le LXC 105 n'avait que ~1,2 Go disponibles sur ses 2 Go, déjà partagés
avec Postgres.

## Options considérées

- **Le laisser dans house-os** — zéro travail ; les autres apps peuvent quand même
  expédier sur son port. Mais lifecycle, volume et secret restent ceux de house-os.
- **Projet compose séparé sur le LXC 105** — cycle de vie indépendant, réseau docker
  externe partagé ; ne règle ni la RAM ni la solidarité de panne du LXC.
- **Son propre LXC** (retenu) — l'hôte Proxmox a 64 Go dont 39 libres. Isolation réelle,
  sauvegarde PBS distincte, et le collecteur survit à un rebuild de house-os.

## Décision

Tranché par Alain le 2026-08-29 : **LXC 106 « observabilite »** (Debian 13, unprivileged,
nesting+keyctl, 2 vCPU / 2 Go / 8 Go, DHCP, onboot), dépôt `/opt/observabilite`, Seq en
Docker Compose. house-os y expédie par le LAN. Une clé d'ingestion par application, et
**`RequireApiKeyForWritingEvents = True`** : l'ingestion anonyme est fermée.

## Conséquences

- Le service `seq` a quitté `docker-compose.yml` de house-os ; `JOURNALISATION_SEQ_CLE`
  y devient une variable **requise** (le compose refuse de démarrer sans).
- house-os expédie par le LAN, plus par le réseau compose : le collecteur doit être
  joignable. Le tampon disque du sink couvre son absence, donc jamais bloquant.
- **L'IP est en DHCP** (192.168.4.36 as of 2026-08-29) : une réservation DHCP au routeur
  est à faire, sinon une nouvelle adresse casse l'expédition en silence.
- Dev et prod partagent le collecteur, séparés par la propriété `Environnement`
  (`house-os-dev` la pose à `dev` ; la clé de prod ne pose rien).
- Le disque du LXC est petit (8 Go) **exprès** : le pool thin `local-lvm` de l'hôte est
  à ~85 %, et un pool plein casse tous les invités. Rétention 14 jours.
- Sauvegarde : le job vzdump `all 1` de 02:00 vers PBS couvre le 106 sans rien ajouter.

## Confirmation

- `grep -c "seq:" docker-compose.yml` vaut 0 dans le dépôt house-os.
- `grep JOURNALISATION_SEQ_CLE .env.example` la documente comme requise.
- `ssh proxmox "pct config 106"` montre le LXC ; `pct exec 106 -- docker compose -f
  /opt/observabilite/docker-compose.yml ps` montre Seq healthy.
- Une ingestion sans clé sur `http://<106>:5341/api/events/raw` doit rendre **401**.
