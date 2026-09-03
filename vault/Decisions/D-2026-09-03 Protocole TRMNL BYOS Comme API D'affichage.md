---
type: decision
status: accepted
date: 2026-09-03
feature: "[[Affichage E-ink]]"
tags: [iot]
---

# Protocole TRMNL BYOS Comme API D'affichage

## Contexte

L'écran e-ink mural doit recevoir une image de House OS et dormir des mois sur
pile. Le relevé du marché (`docs/research/2026-09-03-ecrans-eink-candidats.md`)
montre que presque tous les appareils prêts à l'emploi (TRMNL OG/X, Seeed
reTerminal E1001–E1004, Kindle et Kobo via clients communautaires) parlent le
protocole du firmware TRMNL, ouvert (GPL) et gratuit côté serveur (« BYOS »). Le
reTerminal E1003 a été commandé le 2026-09-03 sur cette base.

## Options considérées

- **Implémenter le protocole TRMNL dans House OS** — trois routes HTTP
  (`/api/setup`, `/api/display`, `/api/log`) et une image PNG à la taille de
  l'écran. Firmware officiel, entretenu par d'autres, gestion de la pile et du
  Wi-Fi comprise. Coût : se plier à un contrat externe en tirage (pas de push).
- **Firmware maison ESPHome** (`online_image`) — plus de contrôle (gris, tactile
  un jour), mais gestion du sommeil profond, des boutons et de la pile à
  réinventer ; support E1003 tout neuf (ESPHome ≥ 2026.7).
- **Cloud Seeed (SenseCraft)** — seul chemin vers le tactile, mais le cloud devrait
  atteindre House OS (jamais exposé) et la mise en page vivrait hors du dépôt.
- **Cloud TRMNL + plugin privé (webhook)** — payant pour un appareil non-TRMNL
  (licence BYOD), 12 envois/h, et la vue vivrait dans un Liquid hors dépôt.

## Décision

House OS implémente le **protocole TRMNL BYOS** comme unique API d'affichage
e-ink. Le firmware TRMNL officiel est installé sur l'E1003. Choix de
l'utilisateur (achat du 2026-09-03 sur cette recommandation, feu vert le même
jour).

## Conséquences

- Le choix du matériel est découplé du logiciel : tout client TRMNL fonctionne.
- Le serveur ne pousse jamais ; la latence d'affichage égale la cadence de
  réveil. Accepté pour la pile.
- Les routes appareil sont hors cookie de session, avec une clé par appareil ;
  l'image est servie par URL signée.
- Le tactile de l'E1003 reste inutilisé (firmware SenseCraft seulement).
- Le contrat est externe : suivre les versions du firmware (en-têtes et champs
  optionnels ajoutés au fil des versions).

## Confirmation

`server/HouseOs.Api/Features/Affichage/` expose `GET /api/setup` et
`GET /api/display` ; aucun dossier `firmware/` ne contient de firmware maison pour
l'écran e-ink.
