---
type: decision
status: accepted
date: 2026-08-26
feature: "[[Budget]]"
tags: []
---

# Page Budget Flux Raffiné Et Bascule Flux Tracé

## Contexte

Neuf maquettes en deux rondes (artefact « Maquettes Budget ») pour la page
Budget : ronde 1 A–E (tableau de bord, console dense, deux colonnes,
maître-détail, flux d'argent), ronde 2 E1–E4 (déclinaisons de la direction E
« Flux d'argent » retenue par Alain).

## Options considérées

- **E1 — Flux raffiné** : bandeau « chemin de l'argent » (entrée mensuelle →
  compte partitionné en barre empilée → sorties prévues), rythme mensuel des
  provisions, grille d'enveloppes enrichies, inbox de rapprochement.
- **E2 — Flux tracé** : diagramme de flux SVG (rubans proportionnels aux
  provisions, enveloppes-nœuds, sorties à droite).
- **E3 — Ruban temporel** : solde projeté sur un ruban défilant façon vue Année.
- **E4 — Flux + profondeur** : E1 avec enveloppes dépliables en accordéon.

## Décision

**La page Budget implémente E1 comme vue par défaut, avec un commutateur
segmenté vers la vue « Flux » d'E2 (diagramme de flux tracé), dernier mode
mémorisé — même patron que Liste | Année de la page Tâches** (choix d'Alain,
2026-08-26). Palette des segments : pâles dérivés des couleurs de chips
existantes (`#ded4e8`/`#ebe5f2` taxes, `#cdd9ec` projet, `#cfe0d8`/`#e0ebe4`
équipement, `#e8e0bd` réserve), non affecté en hachuré neutre.

## Conséquences

- Les deux vues partagent le même résumé serveur (soldes, provisions, sorties
  prévues) ; le diagramme E2 est du rendu pur côté client.
- L'échéancier et l'historique des mouvements (profondeur d'E4) restent
  disponibles via l'éditeur/détail d'enveloppe, pas en accordéon dans la grille.
- E3 (ruban temporel du solde projeté) reste une idée de vue future — hors v1.

## Confirmation

- `grep -r "Flux" web/src/pages/Budget.tsx` retourne le commutateur de vue.
- Un test Vitest couvre la bascule E1 ⇄ Flux et la persistance du mode.
