---
type: recap
date: 2026-09-03
feature: "[[Affichage E-ink]]"
plan: "[[Plan 2026-09-03 Affichage E-ink V1]]"
---

# Recap Affichage E-ink

Livré le 2026-09-03, le jour de la commande du reTerminal E1003, en une session :
recherche du marché, spec et décisions, page, capture, protocole, image Docker,
release prod. Reste l'étape 5 du plan (flasher et calibrer à la réception).

## Ce qui a été construit

- **`/ecran`** : page React hors session, noir sur blanc, Fraunces + Nunito Sans ;
  bande inversée (date + titre d'humeur), liste du jour ≤ 10 rangées (initiales à
  deux lettres — Alain et Ariane partagent la même initiale), zones Dehors /
  Collecte / compte à rebours, pied (heure, pile). Écran d'accueil `?accueil=`.
- **Capture** : Playwright + Chromium dans l'API, un navigateur gardé, contexte par
  capture avec le jeton de rendu ; seuillage ImageSharp en PNG 1-bit (≈ 22 Ko en
  1872×1404, 0,6–0,7 s). `apercu.png` pour concevoir et diagnostiquer.
- **Protocole TRMNL** : `/api/setup` (enrôlement auto), `/api/display`
  (télémétrie, rendu à la taille annoncée, URL signée HMAC, `refresh_rate` jour
  5 min / nuit d'un trait plafonnée à 1 h), route d'image avec cache mémoire,
  `/api/log` vers Serilog. Registre `AppareilsAffichage` (migration EF), REST
  humains + deux outils MCP.
- **Docker** : Chromium installé par le CLI du paquet (`dotnet exec … install
  chromium`), `install-deps` et DejaVu en image finale, node du pilote rendu
  exécutable pour l'utilisateur non-root.

## Écarts par rapport au plan

- Le rendu était d'abord proposé en dessin C# (ImageSharp) ; l'utilisateur a
  choisi Chromium pour la qualité d'image ([[D-2026-09-03 Rendu E-ink Par Chromium
  Headless]]) — la décision ImageSharp, encore `proposed`, a été remplacée.
- Plafond de lignes 8 → 10 (rangées réduites à la demande de l'utilisateur), la
  ligne « + N autres » occupe la dixième place quand ça déborde.
- Cadence 15 → 5 min (choix utilisateur) ; le tactile de l'E1003 est ignoré par
  le firmware TRMNL, le bouton *Refresh* est le rafraîchissement manuel.
- Deux corrections en passant : les bornes d'instants passées à Npgsql doivent
  être en UTC (deux fois), et un paramètre de requête qui ne se lie pas répond
  désormais 400 « Requête invalide » au lieu d'un 500 journalisé comme panne.
