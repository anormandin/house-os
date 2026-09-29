---
type: decision
status: accepted
date: 2026-09-28
feature: "[[Emménagement V2]]"
tags: []
---

# Semis De La Maison Par Skill MCP

## Contexte

Le rapport d'inspection pré-achat du 17 rue de la Colline (2026-03-11, dans le
classeur) décrit les systèmes de la maison : chauffage, eau chaude, panneau, toiture,
plomberie, fondations. Au 2026-09-28, dix équipements sont saisis à la main. La
[[Banque D'idées]] note « semer depuis les données publiques » (Dwellin, HomeBinder) :
un rapport d'inspection devrait donner la liste de départ.

## Options considérées

- **Skill Claude Code via MCP** (retenue) — un skill projet `inventorier-maison` lit le
  PDF, propose un tableau, attend la confirmation, pousse par `gerer_equipement` (avec
  catégorie) puis enchaîne sur `proposer_entretiens`. Même patron que
  `planifier-taches` ; zéro code serveur.
- **Import dans l'app** — un bouton « Extraire les équipements » sur un document, le
  serveur appelle le LLM comme pour l'enrichissement du courriel entrant, et propose des
  fiches à cocher. Utile aux foyers sans Claude Code, mais une tranche complète (prompt,
  endpoint, panneau, tests) pour un geste qui se fait une fois par maison.
- **À la main** — dix fiches de plus se saisissent en une soirée.

## Décision

Alain, 2026-09-28. Le semis passe par un skill Claude Code (`.claude/skills/
inventorier-maison/`), qui suit le contrat de `planifier-taches` : conversation →
tableau → confirmation explicite → un lot d'appels MCP → vérification. Les fichiers
restant web seulement ([[Serveur MCP]]), le skill lit le PDF depuis un chemin local
que l'utilisateur fournit.

## Conséquences

- Le geste reste réservé à qui a Claude Code branché sur l'instance ; les autres foyers
  saisissent à la main ou attendent un import dans l'app, à décider si la demande
  vient.
- Le skill doit connaître les catégories
  ([[D-2026-09-28 Catégorie D'équipement En Liste Fermée]]) et ne jamais inventer un
  `zoneId`.

## Confirmation

- `test -f .claude/skills/inventorier-maison/SKILL.md`.
- Aucun endpoint `extraire` ni prompt d'inventaire côté serveur :
  `grep -rn "inventaire\|extraire les équipements" server/HouseOs.Api/Features` ne
  trouve rien.
