---
type: decision
status: accepted
date: 2026-09-28
feature: "[[Emménagement V2]]"
tags: []
---

# Packs D'entretien En Fichier De Données

## Contexte

Après le déménagement, House OS doit proposer l'entretien d'une maison de zone 4 au
lieu d'attendre qu'on le saisisse tâche par tâche : un pack par catégorie
d'équipement ([[D-2026-09-28 Catégorie D'équipement En Liste Fermée]]) et un programme
de la maison (gouttières, robinets extérieurs, coupe-froid, détecteurs…). La question
était où vivent ces packs et qui les propose.

## Options considérées

- **Dans l'app, fichier de données versionné** (retenue) — un JSON livré avec l'image,
  remplaçable par un chemin de config comme la banque du hasard
  ([[D-2026-09-20 Banque Du Hasard En Fichier De Données]]) ; un endpoint « proposer »
  et un « adopter », un panneau à cocher, les outils MCP en parité. Profite à tout
  foyer qui installe le dépôt.
- **Skill Claude Code seulement** — la liste vit dans le skill et passe par
  `creer_taches`. Zéro code serveur, mais rien dans l'UI, rien pour un foyer sans
  Claude Code, et un pack qu'on ne peut ni tester ni versionner comme de la donnée.
- **Table en base éditable dans l'app** — la plus souple, mais un éditeur de packs à
  bâtir pour deux personnes qui n'en écriront pas dix.

## Décision

Alain, 2026-09-28. Les packs sont un fichier JSON du dépôt
(`server/HouseOs.Api/Features/Entretien/packs-entretien.qc.json`), lu une fois au
démarrage, remplaçable par `ENTRETIEN_FICHIER`. Chaque item porte une clé stable, un
titre, une description, une récurrence complète au format du moteur (mode + paramètres
+ fenêtre) et une stratégie d'assignation. L'app **propose** (liste avec ce qui existe
déjà marqué) et **adopte** (crée les tâches cochées, liées à l'équipement et à sa
zone, en une transaction) ; MCP fait la même chose avec `proposer_entretiens` et
`adopter_entretiens`.

Un item déjà présent se reconnaît au **titre** (comparaison sans accents ni casse ;
pour un pack d'équipement, même titre sur une tâche liée au même équipement ; pour le
programme de la maison, même titre n'importe où). Pas de colonne « vient du pack » sur
la tâche : une tâche adoptée est une tâche ordinaire, renommable, supprimable.

## Conséquences

- Une nouvelle tranche `Features/Entretien/` ; les packs sont de la donnée d'édition
  testée comme telle (chaque item doit produire une `SpecRecurrence` valide).
- Renommer une tâche adoptée la fait réapparaître dans les propositions : c'est le prix
  de ne pas marquer les tâches, accepté.
- Les fenêtres sont écrites pour Sainte-Catherine
  ([[D-2026-09-28 Fenêtres Des Packs En Mois-Jour Absolus]]) ; un autre foyer remplace
  le fichier.

## Confirmation

- `test -f server/HouseOs.Api/Features/Entretien/packs-entretien.qc.json`.
- Test `PacksEntretienTests.Chaque_item_du_pack_livre_est_une_recurrence_valide`.
- `grep -n "proposer_entretiens\|adopter_entretiens"
  server/HouseOs.Api/Features/Mcp/*.cs` trouve les deux outils.
