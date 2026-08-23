---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: []
---

# Auth Simple Deux Comptes

## Contexte

L'app n'est joignable que via Tailscale (auth réseau), mais « qui a complété quoi »
compte pour un couple, et une couture d'auth doit exister pour le jour où quelque
chose dépasse le tailnet.

## Options considérées

- **Login simple, 2 comptes** — username + mot de passe, session cookie persistante ;
  attribution réelle, couture d'auth en place.
- **Sélecteur d'utilisateur sans mot de passe** — zéro friction mais tout device du
  tailnet est n'importe qui ; retrofit pénible.
- **Identité Tailscale** — élégant mais couple l'app à Tailscale et gère mal les
  écrans partagés (tablette murale).

## Décision

Login simple, 2 comptes, cookie de session longue durée. Les devices IoT auront des
clés API dédiées (phase 3), jamais de comptes humains. Choix de l'utilisateur.

## Conséquences

- Attribution des complétions fiable dès la V0.
- L'écran mural (device partagé) devra avoir son propre mode (lecture ou compte
  device) — à décider en phase 3.

## Confirmation

Endpoint de login dans `server/HouseOs.Api/Features/` ; table Utilisateur limitée à
des comptes nommés ; aucune dépendance à un provider OIDC externe.
