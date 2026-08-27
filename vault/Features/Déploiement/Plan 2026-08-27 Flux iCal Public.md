---
type: plan
status: executed
date: 2026-08-27
feature: "[[Déploiement]]"
tags: []
---

# Plan 2026-08-27 Flux iCal Public

Exposer `/ical/{jeton}.ics` sur Internet via un sidecar Tailscale Funnel, ajouter la
rotation self-service du jeton, afficher les deux URLs dans « Mon calendrier ».
Gouverné par [[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]].

## Étapes

### Backend — rotation du jeton

- [x] Extraire la génération du jeton (`AmorcageDb.GenererJeton`,
  `server/HouseOs.Api/Infrastructure/AmorcageDb.cs`) en helper partagé.
- [x] `POST /api/ical/rotation` (cookie requis) dans
  `server/HouseOs.Api/Features/FluxIcal/FluxIcalEndpoints.cs` : régénère le
  `JetonIcal` de la personne connectée, retourne le nouveau `chemin`.
- [x] `GET /api/ical/mon-flux` retourne aussi `urlPublique` (config
  `Ical__UrlPubliqueBase`, vide en dev → l'UI n'affiche que l'interne).
- [x] Tests backend : rotation change le jeton, l'ancien donne 404, le nouveau 200 ;
  mon-flux reflète la config publique.

### Parité MCP

- [x] `mon_flux_ical` (`server/HouseOs.Api/Features/Mcp/OutilsIcal.cs`) : paramètre
  optionnel `regenerer` (défaut null/false — contrainte SDK des paramètres
  optionnels) ; retourne chemin + urlPublique.

### Web — « Mon calendrier »

- [x] Boîte calendrier (`web/src/components/Layout.tsx`) : deux URLs étiquetées
  (« Pour Google Calendar » = publique quand configurée ; « Sur Tailscale » =
  interne), bouton « Régénérer » avec confirmation (avertit que les abonnements
  existants cassent), copie en un clic.
- [x] Test web : la rotation remplace l'URL affichée.

### Infra — sidecar Funnel

- [x] Service `tailscale` dans `docker-compose.yml` : image `tailscale/tailscale`,
  hostname `houseos`, `TS_AUTHKEY` depuis `.env`, état dans un volume nommé,
  config serve montée (`infra/tailscale-serve.json`) : Funnel 443, unique handler
  `/ical` → `http://app:8080/ical`.
- [x] Vérifier le comportement de préfixe des mounts serve (strip du chemin) avec
  un curl au déploiement ; corriger la cible si besoin.
- [x] `.env.example` : `TS_AUTHKEY`, `ICAL_URL_PUBLIQUE_BASE`
  (`https://houseos.<tailnet>.ts.net`).
- [x] **Manuel (Alain)** : activer Funnel dans la policy du tailnet (attribut
  `funnel` sur le tag du nœud) + générer une auth key taguée, la mettre dans
  `/opt/house-os/.env`.

### Déploiement et vérification

- [x] Push GitHub, `git pull && docker compose up -d --build` dans le LXC.
- [x] `curl https://houseos.<tailnet>.ts.net/ical/<jeton>.ics` → 200 `text/calendar`
  depuis un réseau hors tailnet ; jeton bidon → 404 ; `/` et `/api` → non servis.
- [x] Abonner Google Calendar à l'URL publique et confirmer l'apparition des
  évènements.
- [x] (2026-08-27, correctif) L'enregistrement DNS public de
  `houseos.taila5ccb5.ts.net` n'est pas créé par le control plane Tailscale
  (>1 h, NXDOMAIN aux 4 autoritaires dnsimple ; cert émis, Funnel on,
  IngressEnabled true, chemin complet vérifié 200 via le tailnet — symptôme
  identique à tailscale/tailscale#18652, côté serveur). Surveillance en
  cours ; si rien après ~4 h : re-toggle Funnel, sinon support Tailscale.
  **Résolu tout seul** : enregistrement publié ~2 h 20 après l'activation
  (ingress 199.38.181.54/209.177.145.137 + IPv6) ; chemin public vérifié
  (TLS ingress → app, 404 jeton bidon, racine non servie).

### Clôture vault

- [x] [[Déploiement]] : exposition Funnel dans Comportement, retirer « exposition
  publique = jamais » du Hors périmètre, lier la décision, stamps.
- [x] [[Tâches]] : retirer la dette « rotation du jeton iCal » du Hors périmètre,
  décrire la rotation dans Comportement, stamps.
- [x] [[Recap Déploiement]] : ajouter le volet Funnel + rotation.
- [x] Valider le vault (`validate-vault.py`).
