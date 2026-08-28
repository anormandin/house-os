---
type: decision
status: accepted
date: 2026-08-28
feature: "[[Synchro]]"
tags: []
---

# Synchro Temps Réel Par SignalR

## Contexte

Un onglet ouvert pouvait mentir jusqu'à un refocus, et surtout les écritures du serveur
MCP n'atteignaient aucun navigateur : Claude poussait dix tâches, l'écran ne bougeait
pas. Il fallait un canal serveur→client, et d'abord trancher **ce que la feature est** :
les trois exemples donnés (complétion par l'autre, météo rafraîchie, phrase du jour
régénérée) sont tous des problèmes de « mon onglet est périmé », pas de « préviens-moi
sur mon téléphone ».

## Options considérées

**Portée** :

- **Live sync seulement** — événements éphémères qui rafraîchissent les UI ouvertes.
  Aucune persistance : un onglet fermé ne rate rien puisqu'il refait ses requêtes en
  s'ouvrant.
- **Live sync + petit historique en mémoire** — un panneau « ce qui vient d'arriver ».
- **Système de notifications complet** — boîte de réception par personne, lu/non-lu,
  livraison différée. Superséderait la décision iCal ; beaucoup de plomberie pour un
  foyer de deux.

**Transport** :

- **SignalR** — hub ASP.NET + client `@microsoft/signalr` ; reconnexion automatique,
  négociation de transport, prêt pour du bidirectionnel plus tard (tablette murale,
  IoT). Coût : un paquet npm et une dépendance de plus.
- **SSE** (`TypedResults.ServerSentEvents` de .NET 10 + `EventSource`) — zéro paquet
  des deux côtés, mais unidirectionnel et il aurait fallu régler le tampon du proxy NPM
  (non documenté, hors dépôt).
- **WebSocket brut** — tout à écrire à la main : protocole, reconnexion, keepalive.

## Décision

**Live sync seulement, sur SignalR**, en diffusion à tous les clients connectés (pas de
groupes ni de ciblage : deux comptes dans le foyer). Choix de l'utilisateur sur les deux
axes.

Le contrat est un **événement de domaine** — le serveur nomme le module et le geste, le
client décide quelles clés TanStack invalider. Le serveur ignore la structure du cache
web ; l'inverse (le serveur nommant les clés de requête) aurait couplé le backend à la
topologie du frontend.

Un champ `source` (`web` / `mcp`) accompagne l'acteur, parce que le MCP agit « au nom
de » quelqu'un ([[D-2026-08-24 Clé API Partagée Et AgirComme]]) : sans lui, la règle
« annoncer si l'acteur n'est pas moi » rendrait muettes les écritures de Claude faites
en mon nom — exactement le trou qu'on ferme.

## Conséquences

- [[D-2026-08-23 Notifications Par Flux iCal]] **reste en vigueur et n'est pas
  supersédée** : les portées sont disjointes. Le flux iCal est le canal « me rejoindre
  hors de l'app » ; la synchro ne sort jamais d'un onglet ouvert. La note de cette
  décision prévoyant « une décision de superseding pour le push » reste ouverte pour
  le jour où un vrai push sera voulu — ce n'est pas ce qui est fait ici.
- Le hub reste sur le tailnet/LAN : `infra/tailscale-serve.json` ne publie que `/ical`
  et ne doit pas gagner le chemin du hub.
- Le proxy NPM hors dépôt a déjà WebSockets activé (voir [[Recap Déploiement]]) ; rien
  à changer côté infra. Le keepalive SignalR (15 s) reste sous le `read timeout` par
  défaut.
- Ouvre la porte à une vue e-ink / tablette murale rafraîchie par le même canal
  ([[D-2026-08-23 Interface Desktop Et Écran E-ink]]) — mais un client sans cookie
  devrait alors passer par un jeton d'URL, comme le flux iCal.
- Le magasin de toasts devient une file : les annonces distantes coexistent avec la
  confirmation de mon propre geste.

## Confirmation

`AddSignalR()` et `MapHub<SynchroHub>` dans `server/HouseOs.Api/Program.cs` ;
`web/package.json` référence `@microsoft/signalr`. Le hub est authentifié et joignable :
tests `SynchroApiTests.UnClientConnecte_recoitLesEvenementsPousses` (vrai client SignalR
sur le TestServer — prouve que la négociation traverse l'antiforgery global) et
`SynchroApiTests.SansCookie_leHubRefuseLaConnexion`. La règle de la source MCP :
`synchro.test.ts`, « une écriture MCP s'annonce même quand elle agit en mon nom ».
`infra/tailscale-serve.json` ne doit contenir aucune entrée `/hubs`.
