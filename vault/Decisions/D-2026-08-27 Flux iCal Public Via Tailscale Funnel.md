---
type: decision
status: accepted
date: 2026-08-27
feature: "[[Déploiement]]"
supersedes: "[[D-2026-08-23 Hébergement Maison Tailscale Docker]]"
tags: []
---

# Flux iCal Public Via Tailscale Funnel

## Contexte

Google Calendar ne peut pas s'abonner au flux iCal : ses serveurs récupèrent l'URL
eux-mêmes, et `houseos.alainnormandin.dev` résout publiquement vers l'IP privée de
NPM (LAN/Tailscale seulement). [[D-2026-08-23 Hébergement Maison Tailscale Docker]]
posait « aucune surface publique » ; [[Tâches]] portait la dette assumée « rotation
du jeton iCal à trancher avant toute exposition hors Tailscale ». Besoin exprimé le
2026-08-27 : abonner Google Calendar au flux. L'endpoint `/ical/{jeton}.ics` est déjà
conçu en capability URL ([[D-2026-08-23 Flux iCal Par Personne]]) : anonyme, lecture
seule, jeton 48 hex CSPRNG.

## Options considérées

- **Exposer seulement `/ical` via Tailscale Funnel** — aucun port ouvert, aucun
  changement DNS, réversible ; hostname `.ts.net`.
- **Cloudflare Tunnel (+ Access)** — domaine à soi, WAF ; exige de migrer la zone
  DNS `alainnormandin.dev` chez Cloudflare (changement de lab disproportionné).
- **Ouvrir 443 vers NPM** — login deux-comptes exposé au brute-force mondial,
  CVE NPM ; rejeté.
- **Pousser vers l'API Google Calendar** (inverser le flux) — zéro exposition mais
  OAuth Google + gestion des suppressions ; contredit « notifications v1 = flux iCal ».
- Sous-choix : sidecar compose / tailscaled dans le LXC / nœud tiers du lab ;
  une seule URL affichée / les deux ; rotation bouton+API+MCP / API+MCP seulement.

## Décision

Le principe d'hébergement est **reconduit** : machine maison + Tailscale + Docker
Compose, app et MCP jamais exposés à Internet. **Unique exception publique** :
`GET /ical/{jeton}.ics`, publié par un **sidecar Tailscale dans le compose**
(conteneur `tailscale/tailscale`, config serve/funnel versionnée dans le repo qui ne
monte que `/ical` vers `app:8080` ; `TS_AUTHKEY` dans le `.env` du serveur, état dans
un volume nommé). Le jeton reste la seule authentification, avec **rotation
self-service** : endpoint POST authentifié (chacun régénère son propre jeton), bouton
« Régénérer » dans « Mon calendrier », parité MCP. La boîte « Mon calendrier »
affiche **les deux URLs** : publique Funnel (pour Google Calendar) et interne
(abonnements des appareils sur Tailscale). Choix de l'utilisateur (2026-08-27,
les trois sous-choix = recommandations acceptées).

## Conséquences

- Google Calendar peut s'abonner (rafraîchissement lent côté Google, souvent
  12-24 h — les rappels du jour même restent l'affaire des abonnements internes).
- Un jeton fuité se révoque en un clic ; la rotation casse les abonnements
  existants (à ré-abonner) — assumé.
- La surface publique se limite à « deviner 192 bits sur un endpoint lecture
  seule » ; l'auth applicative simple reste acceptable.
- Nouvelle dépendance : Funnel (relais Tailscale, ports 443/8443/10000, policy du
  tailnet avec l'attribut `funnel` requis sur le nœud).
- Le lab déménage sans impact : le sidecar suit le compose.

## Confirmation

Service Tailscale dans `docker-compose.yml` dont la config serve ne référence que
`/ical` (aucun autre chemin monté) ; endpoint de rotation dans
`server/HouseOs.Api/Features/FluxIcal/FluxIcalEndpoints.cs` ; `TS_AUTHKEY` dans
`.env.example` ; aucun port publié en plus de ceux existants.
