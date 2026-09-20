---
type: decision
status: accepted
date: 2026-09-20
feature: "[[Journal De La Maison]]"
tags: []
---

# D-2026-09-20 Regroupement Sans Catégorie De Tâche

## Contexte

Le rang « 10 et + » du tableau de bascule demande un **sommaire groupé par rubrique** :
sans regroupement, quatorze tâches redeviennent quatorze lignes et le journal ne vaut
pas mieux que la liste d'aujourd'hui. Les maquettes montrent
« Gouvernements / Argent / Le reste » sur la vraie journée du 2026-10-20.

`Tache` (`server/HouseOs.Api/Domaine/Tache.cs`) porte `ZoneId` et `EquipementId`, mais
**ni catégorie ni étiquette**. Et les quatorze démarches d'adresse du 20 octobre sont
justement administratives : aucune n'a de zone.

Le rang 10+ ne se déclenche **qu'une seule fois dans toute la prod**.

## Options considérées

- **Ajouter `Categorie` sur `Tache`** — regroupement déterministe et stable d'un jour à
  l'autre, et la catégorie resservirait ailleurs (filtres, enveloppes). Mais c'est un T3
  de schéma (migration EF, DTO, éditeur, parité MCP) pour servir un rang qui tire une
  fois — et il faudrait ensuite saisir la catégorie sur chaque tâche existante pour que
  ça donne quoi que ce soit. Piège connu : l'éditeur de tâche remplace tout au PUT, donc
  un champ de plus est un champ de plus à charger partout.
- **Étiquettes many-to-many** — plus expressif, de loin le plus cher : schéma, UI de
  saisie, filtres, outils MCP.
- **Reporter le rang 10+** — le journal resterait la liste plate plafonnée à 10 lignes
  précisément le jour le plus chargé de l'année, en plein déménagement.
- **Dégradé sur ce qui existe** (retenue).

## Décision

Pas de changement de schéma. Le regroupement se fait sur ce qui existe déjà —
**`Tache.ZoneId`, puis `EquipementId`** — et le paquet restant, celui sans zone, est
**nommé par l'éditorialiste** : l'édition rend un champ de plus, le nom des rubriques et
l'ordre des tâches dedans.

Regrouper n'est pas inventer un fait, c'est le geste d'un chef de pupitre : la règle
« le LLM n'invente aucun fait » tient toujours, parce que les titres, les assignés et
les échéances viennent tous de la base et ne sont jamais réécrits.

**Repli obligatoire** : quand l'API ne répond pas, le sommaire retombe sur la liste
plate, groupée par zone quand elle existe. Le journal ne dépend jamais du LLM pour être
lisible.

Choix d'Alain, en grillage.

## Conséquences

- Zéro migration, zéro saisie rétroactive, et le rang 10+ est livrable dans le même lot
  que les autres.
- Les rubriques peuvent **changer d'un jour à l'autre** pour des tâches identiques.
  C'est accepté : un sommaire est une lecture du jour, pas une taxonomie.
- La sortie du LLM gagne une structure (rubriques + affectation des tâches) à valider
  strictement : toute tâche non affectée par le modèle retombe dans « Le reste », et
  toute tâche inventée est **rejetée** — on ne montre que des occurrences réelles.
- Le manque reste écrit dans [[Éditorialiste De L'Écran]] : si un jour la catégorie
  arrive pour d'autres raisons, le regroupement s'y branchera sans revenir ici.

## Confirmation

`server/HouseOs.Api/Domaine/Tache.cs` ne contient ni `Categorie` ni `Etiquette`, et un
test nommé d'après le repli (regroupement sans LLM) couvre le cas « aucune zone, aucune
rubrique rendue » dans `server/HouseOs.Tests/`.
