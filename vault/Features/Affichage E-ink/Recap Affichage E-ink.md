---
type: recap
date: 2026-09-03
feature: "[[Affichage E-ink]]"
plan: "[[Plan 2026-09-03 Affichage E-ink V1]]"
---

# Recap Affichage E-ink

Livré le 2026-09-03, le jour de la commande du reTerminal E1003, en une session :
recherche du marché, spec et décisions, page, capture, protocole, image Docker,
release prod. Appareil reçu, flashé et enrôlé le 2026-09-04 (voir « Mise en
service » dans [[Affichage E-ink]]) ; la calibration sur l'écran réel a été faite le
2026-09-21, avec la broadsheet (étape 9 du [[Plan 2026-09-20 Journal Éditorial]]).

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
  choisi Chromium pour la qualité d'image
  ([[D-2026-09-03 Rendu E-ink Par Chromium Headless]]) — la décision ImageSharp,
  encore `proposed`, a été remplacée.
- Plafond de lignes 8 → 10 (rangées réduites à la demande de l'utilisateur), la
  ligne « + N autres » occupe la dixième place quand ça déborde.
- Cadence 15 → 5 min (choix utilisateur) ; le tactile de l'E1003 est ignoré par
  le firmware TRMNL, le bouton *Refresh* est le rafraîchissement manuel.
- Deux corrections en passant : les bornes d'instants passées à Npgsql doivent
  être en UTC (deux fois), et un paramètre de requête qui ne se lie pas répond
  désormais 400 « Requête invalide » au lieu d'un 500 journalisé comme panne.

## Addendum 2026-09-20 — cadence ramenée à 15 min

Première donnée de pile sur la durée : 4,04 V à l'enrôlement (2026-09-04) →
3,79 V seize jours plus tard, à ~198 réveils Wi-Fi par jour. La cadence de jour
choisie le 2026-09-03 (5 min) était le seul vrai coût ; `CadenceJourSecondes`
passe de 300 à **900** — même famille de valeur que le stock TRMNL, latence
d'affichage ≤ 15 min, ~74 réveils/jour au lieu de ~206.

Plumberie ajoutée au passage : `ECRAN_CADENCE_SECONDES` et
`ECRAN_PLAFOND_SECONDES` dans `docker-compose.yml`, `.env.example` et
`docs/configuration.md` — aucune clé `Affichage:` n'était réglable sans rebuild
jusqu'ici. Les défauts sont **répétés dans le compose** plutôt que laissés
vides : une chaîne vide ne se convertit pas en `int` et ferait planter le binder
d'options au démarrage.

Corrigé aussi : `docs/configuration.md` et le docstring de `DelaiReveil`
promettaient que la nuit était **un seul sommeil** jusqu'au matin. C'est faux
depuis l'origine — `Math.Clamp(restant, cadence, plafond)` rabote à
`PlafondSecondes`, donc l'appareil se réveille toutes les heures toute la nuit.
Le test le savait déjà (« plafonné à une heure »), la doc non. Seul le texte a
changé ; le comportement est intact, faute de savoir si le firmware 1.8.10
honore un délai > 3600 s — c'est la case toujours ouverte du plan.

Nouveau test `Les_defauts_livres_menagent_la_pile` (`DelaiReveilTests`) épingle
le défaut livré, pour qu'un retour à 300 s ne passe pas inaperçu. 661 tests
backend verts.
