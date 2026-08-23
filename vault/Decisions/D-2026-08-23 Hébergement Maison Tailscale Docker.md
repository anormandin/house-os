---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: []
---

# Hébergement Maison Tailscale Docker

## Contexte

Les deux téléphones doivent atteindre l'app à la maison et à l'extérieur. Le homelab
actuel change au déménagement (2026-10-06), donc rien ne doit dépendre d'une machine
précise.

## Options considérées

- **Machine maison + Tailscale** — coût nul, privé par défaut, aucune surface
  publique ; friction unique : installer Tailscale sur les téléphones.
- **VPS cloud** (~5-10 $/mois) — survit au déménagement, URL publique simple ; données
  hors site, les devices IoT devraient sortir vers le cloud.
- **Machine maison + HTTPS public** (reverse proxy + domaine) — pas de VPN, mais
  surface de sécurité à assumer et cassure pendant le déménagement.

## Décision

Machine maison toujours allumée + Tailscale sur les téléphones. Tout tourne en
Docker Compose pour rester portable d'une machine à l'autre pendant la transition.
Choix de l'utilisateur.

## Conséquences

- Aucune exposition publique ; l'auth applicative peut rester simple (voir
  [[D-2026-08-23 Auth Simple Deux Comptes]]).
- Le serveur doit être trivialement re-déployable : `docker compose up` + restauration
  d'un backup.
- Les futurs devices IoT vivent sur le LAN/tailnet, jamais sur Internet.

## Confirmation

`docker-compose.yml` existe à la racine et définit tous les services ; aucune config
de reverse proxy public dans le repo.
