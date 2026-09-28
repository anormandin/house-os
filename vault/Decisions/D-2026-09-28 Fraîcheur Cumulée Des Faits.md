---
type: decision
status: accepted
date: 2026-09-28
feature: "[[Fonds De Tiroir]]"
tags: [iot]
---

# D-2026-09-28 Fraîcheur Cumulée Des Faits

## Contexte

La première semaine complète du mur (21–28 septembre 2026) s'est lue comme la même
page huit fois. Deux causes, mesurées sur les éditions de prod et leur matière :

- **Les widgets.** Le premier gel (fenêtre de 38 jours), la dernière douceur (29) et
  la collecte spéciale ont paru les huit jours. Leur rareté vaut dix à vingt fois celle
  d'un fait quotidien, et la fraîcheur ne regardait que la **dernière** parution, en
  remontant linéairement depuis 0,15 : un fait sorti hier gardait 27 % de son score, ce
  qui suffisait à battre tout le reste. Le dicton, la durée du jour et « l'an dernier »
  ne sortaient jamais.
- **La prose.** `precedentes` ne portait que surtitre, manchette et chapeau. Le modèle
  ne voyait donc pas ses paragraphes et reprenait les mêmes faits (les séances depuis
  le 28 août, la cuisine jamais cochée, le gel du 3). Le chapeau répétait chaque matin
  le compte de tâches, les dodos et la météo — que la dateline et l'encadré montrent
  déjà —, calqué sur l'exemple du prompt.

## Options considérées

- **Abaisser le plancher seulement** (0,15 → 0,05, linéaire) — rejoué sur la semaine :
  le gel et la douceur sortent encore dix-sept jours sur vingt et un.
- **Réduire la rareté des faits à fenêtre** — ment sur la règle « la rareté est un
  compte de jours », et déplace le problème vers le prochain fait rare.
- **Fraîcheur cumulée, au carré** (retenue) — chaque parution de la semaine donne une
  pénalité `0,02 + 0,98 × (âge / 7)²`, et les pénalités se multiplient. Rejoué : les
  faits à fenêtre sortent un jour sur deux, les quotidiens tournent, le dicton paraît.
- **Côté prose** : ajouter les paragraphes à `precedentes`, donner au chapeau les
  interdits du surtitre, garder le compte à rebours hors des titres sauf la veille et
  le jour même, et demander de préférer ce que la semaine n'a pas dit.

## Décision

La fraîcheur est le produit des pénalités de chaque parution des sept derniers jours,
au carré de l'âge, plancher 0,02 par parution. Elle ne vaut jamais zéro : un fait
ressassé reste là quand il n'y a rien d'autre. La mémoire du prompt porte les
paragraphes, et le prompt interdit de refaire un fait déjà raconté sauf si sa valeur
a changé.

## Conséquences

- Un fait qui « engage la journée » (pertinence 3) ne sort plus chaque matin de sa
  fenêtre non plus : c'est voulu, la liste des tâches porte l'action.
- La matière conservée avant le 2026-09-28 n'a pas de paragraphes dans `precedentes` ;
  elle se relit quand même (paragraphes vides).
- Rien ne change dans le schéma : `Paragraphes` et `ClesPubliees` étaient déjà sur
  l'édition.

## Confirmation

- `FondsDeTiroirTests.Chaque_parution_de_la_semaine_pese` et
  `Un_fait_a_fenetre_sorti_hier_cede_la_place_a_un_fait_quotidien`.
- `RedactionLlmTests.SerialiserMatiere_ContientLesFaitsEtLaMemoire` vérifie les
  paragraphes dans la matière.
- En prod : `select "Date", "ClesPubliees" from "Editions"` sur une semaine — aucune clé
  hors `calendrier.compte-a-rebours` et `ville.collecte` ne paraît sept jours sur sept.
