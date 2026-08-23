---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: []
---

# Direction Artistique Cuisine Chaleureuse

## Contexte

Trois directions maquettées et comparées à contenu égal sur le canvas « Maquettes
Maison » (voir [[Directions Artistiques]]) : A Papier d'encre, B Tableau de bord
Kanagawa, C Cuisine chaleureuse. La recherche recommandait A comme base (meilleure
traduction e-ink) ; le couple a tranché autrement.

## Options considérées

- **A Papier d'encre** — le plus calme, e-ink quasi 1:1 ; jugé plus austère.
- **B Tableau Kanagawa** — spectaculaire au mur ; « app à gérer ».
- **C Cuisine chaleureuse** — l'attrait émotionnel d'un outil de couple ; l'e-ink
  perd la couleur (la chaleur devra passer par la voix et les formes).

## Décision

**Cuisine chaleureuse** (lignée Hearth) : crème chaude, arrondis généreux, Fraunces +
Nunito Sans, titre d'humeur, équilibre d'équipe plutôt que corvées. Choisie ensemble
par Alain et Ariane sur les maquettes (2026-08-23). Cinq variations de la direction
explorées ensuite pour fixer l'exécution.

## Conséquences

- Le layout desktop V1 sera reconstruit dans ce langage (l'UI V0 shadcn neutre est
  jetable).
- La vue e-ink devra porter la chaleur par le ton (titre d'humeur, Fraunces en
  bande-titre) et les formes arrondies — pas par la couleur.
- Discipline d'exécution : cette direction est la plus dépendante du goût — s'en
  tenir aux principes de [[Inspiration UI]] (pas de théâtre d'urgence, gradient pas
  culpabilité, résumé d'abord).

## Confirmation

`web/src/index.css` définit la palette chaleureuse (crème/Fraunces) une fois la V1
UI construite ; les maquettes retenues vivent dans `design/maquettes/`.
