---
type: decision
status: accepted
date: 2026-08-25
tags: []
---

# Retrait du service worker

## Contexte

La PWA livrée en V0 ([[D-2026-08-23 Frontend Vite React PWA]]) incluait un service
worker de pré-cache (vite-plugin-pwa, `generateSW`). Après le déploiement des
miniatures de documents, le navigateur a servi l'ancienne version en cache — chaque
déploiement exigeait un rechargement de plus pour voir la nouvelle version. Alain a
tranché : l'app est **desktop web d'abord**, servie sur le LAN — pas d'« obsession
PWA ».

## Options considérées

1. **Garder le SW + rechargement auto à la mise à jour** — conserve l'offline,
   mais complexité maintenue pour un bénéfice nul sur un serveur LAN maison.
2. **Retirer le service worker, garder le manifest** — l'app reste installable
   (icône, fenêtre standalone) ; plus aucun cache d'assets côté client.

## Décision

Option 2 : `selfDestroying: true` dans la config vite-plugin-pwa — le plugin publie
un service worker kamikaze sous le même nom qui désenregistre ceux déjà installés
chez les clients, vide leurs caches et recharge la page. Le manifest reste généré.
Plus largement : **pas de couche offline/service worker sans besoin prouvé**.

## Conséquences

- Chaque déploiement est visible au premier chargement suivant — plus de version
  décalée d'un rechargement.
- Plus de démarrage hors-ligne (assumé : le serveur est sur le LAN, à la maison).
- L'option se réactive en retirant `selfDestroying` si un vrai besoin offline
  apparaît (ex. tablette murale en zone wifi fragile — à décider alors).
- [[D-2026-08-23 Frontend Vite React PWA]] reste valide pour le stack ; seule la
  portée « PWA » se réduit à « manifest installable ».

## Confirmation

- `rg "selfDestroying" web/vite.config.ts` — présent tant que des clients peuvent
  encore porter l'ancien SW (le retirer plus tard = nouvelle décision).
- `web/dist/sw.js` après build contient `self.registration.unregister`.
