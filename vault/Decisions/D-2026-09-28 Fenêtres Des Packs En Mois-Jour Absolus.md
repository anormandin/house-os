---
type: decision
status: accepted
date: 2026-09-28
feature: "[[Emménagement V2]]"
tags: []
---

# Fenêtres Des Packs En Mois-Jour Absolus

## Contexte

Les fenêtres saisonnières dérivées du climat local sont le différenciateur annoncé de
House OS ([[Banque D'idées]], « Fenêtres saisonnières calculées, pas saisies »), et le
[[Fonds De Tiroir]] connaît déjà les normales du lieu (premier gel, dernière douceur,
première neige — `EtatDuClimat`). Les packs d'entretien
([[D-2026-09-28 Packs D'entretien En Fichier De Données]]) pouvaient donc exprimer
« trois semaines avant le premier gel » plutôt qu'une date.

## Options considérées

- **Mois-jour absolus, écrits pour la zone 4** (retenue) — « gouttières : 15 oct –
  15 nov ». Le moteur le supporte tel quel, le pack se lit et se corrige à l'œil, et
  il est écrit pour Sainte-Catherine.
- **Ancrées aux normales** — la proposition résout l'ancre en mois-jour depuis les
  normales au moment d'adopter. Une couche de plus à tester, et des normales absentes
  la première année d'une nouvelle installation (l'archive Open-Meteo se tire au
  premier démarrage, mais les saisons observées mettent du temps à compter).

## Décision

Alain, 2026-09-28. Les fenêtres des packs sont des mois-jour absolus, au format du
moteur (`FenetreDebutMois/Jour`, `FenetreFinMois/Jour`). La dérivation des normales
reste une évolution possible du **format du pack** (une ancre optionnelle à côté de la
date), pas du moteur de récurrence, qui continue de stocker des mois-jour.

## Conséquences

- Un foyer d'une autre zone climatique remplace le fichier de packs
  (`ENTRETIEN_FICHIER`) ; c'est documenté dans `docs/configuration.md`.
- Le jour où l'ancre aux normales arrive, une tâche adoptée garde sa fenêtre : la
  résolution se fait à l'adoption, jamais après.

## Confirmation

- `grep -c "premierGel\|ancre" server/HouseOs.Api/Features/Entretien/packs-entretien.qc.json`
  vaut 0 tant que cette décision tient.
