---
type: decision
status: accepted
date: 2026-09-03
feature: "[[Affichage E-ink]]"
tags: [iot, securite]
---

# Registre Des Appareils D'affichage

## Contexte

Le protocole TRMNL attribue une clé par appareil à l'enrôlement (`/api/setup`)
et renvoie de la télémétrie (tension de pile, RSSI, firmware, dimensions) à
chaque réveil. Il faut décider où vit cette identité et si l'enrôlement demande
une action humaine.

## Options considérées

- **Table `AppareilAffichage` + enrôlement automatique** — MAC, identifiant court,
  clé (aléatoire, stockée hachée ou en clair ? voir Décision), nom, modèle,
  dimensions, firmware, dernier contact, tension, RSSI. Un appareil inconnu qui
  appelle `/api/setup` est créé et reçoit sa clé. Coût : une migration EF ; un
  appareil sur le LAN peut s'enrôler seul (acceptable : réseau jamais exposé,
  et un intrus n'y lirait que la liste des corvées).
- **Enrôlement par approbation** — l'appareil est créé « en attente » et reçoit
  l'écran d'accueil jusqu'à ce qu'un humain l'approuve dans l'UI ou par MCP.
  Plus sûr, mais exige une UI de gestion dès la v1 pour un seul appareil.
- **Configuration statique** (MAC + clé dans le `.env`) — pas de migration, mais
  la télémétrie n'a nulle part où aller et chaque appareil de plus = un
  redémarrage.

## Décision

**Table avec enrôlement automatique**, clé stockée en clair (elle doit
être renvoyée telle quelle par `/api/setup` à un appareil connu qui redemande sa
configuration ; le hachage n'apporterait rien contre un accès à la base). La
révocation (supprimer la ligne) et le renommage passent par MCP en v1, par une
page web en v2. Choix de l'utilisateur (2026-09-03).

## Conséquences

- Migration EF `AppareilsAffichage` (T3 : schéma).
- Télémétrie conservée sur la ligne (dernière valeur) ; un historique de pile
  n'est pas nécessaire tant qu'aucune règle ne le consomme.
- Le `.env` reste inchangé : rien de propre à un foyer
  ([[D-2026-09-02 Dépôt Public AGPL Et Instance Générique]]).
- Le compose `docs/configuration.md` gagne trois clés facultatives de cadence
  (`Affichage:*`), pas de secret.

## Confirmation

Une migration EF crée la table `AppareilsAffichage` ; `GET /api/setup` crée une
ligne pour une MAC inconnue (test d'intégration nommé
`Setup_EnroleUnAppareilInconnu`).
