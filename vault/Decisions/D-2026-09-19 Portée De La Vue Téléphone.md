---
type: decision
status: accepted
date: 2026-09-19
feature:
tags: []
---

# Portée De La Vue Téléphone

## Contexte

**Principe directeur, donné par l'utilisateur (2026-09-19) : la vue téléphone n'a
jamais vocation à remplacer l'application desktop — c'est un compagnon de commodité.**
C'est elle qui tranche les arbitrages de portée ci-dessous, et celles à venir.

[[D-2026-09-19 Interface Téléphone Distincte]] acte que le téléphone a sa propre
interface, mais laisse ouverte la question de savoir si **toutes** les vues du desktop
doivent y exister. Deux vues du desktop sont des outils d'analyse sur grand écran :

- **Budget → Flux** : le sankey du « chemin de l'argent », dessiné sur un canevas fixe
  de 1148 px avec des nœuds positionnés en absolu.
- **Tâches → Année** : le ruban annuel défilant, ~3700 px de piste et une colonne
  d'étiquettes collante de 224 px.

Une première ronde de maquettes a montré qu'on peut les retourner (sankey vertical,
ruban en défileur), mais le résultat demande beaucoup de place pour une lecture qu'on
ne fait pas debout dans un corridor.

## Options considérées

- **Les réinventer pour le téléphone** — sankey vertical, ruban annuel en défileur.
  Faisable (maquetté), mais coûteux à maintenir pour un usage rare sur cet appareil.
- **Les omettre de la vue téléphone** — le téléphone porte ce qu'on fait en passant
  (cocher, consulter, capturer) ; l'analyse reste sur grand écran.
- **Les dégrader en lecture seule** — une image ou un tableau ; ni l'un ni l'autre ne
  rend la lecture que ces vues servent.

## Décision

La vue téléphone **n'inclut ni Budget → Flux ni Tâches → Année**. Elles restent
desktop seulement. Le téléphone ne doit pas annoncer une vue qu'il n'a pas : les
commutateurs « Aperçu | Flux » et « Liste | Année » disparaissent de l'interface
téléphone plutôt que d'afficher un segment inerte. Choix de l'utilisateur.

Corollaire pour la suite : la parité écran-par-écran entre desktop et téléphone n'est
pas un objectif, et ne doit pas le devenir. Le téléphone porte ce qu'on fait en
passant — cocher, consulter, capturer. Chaque nouvelle tranche décide si elle a une
présentation téléphone, et l'absence est une réponse valable : le desktop reste
l'application complète. Un écran téléphone qui commence à imiter la densité du desktop
est un signal qu'on s'éloigne du rôle de compagnon.

## Conséquences

- Budget sur téléphone = Aperçu seulement (stats, rythme mensuel, enveloppes).
- Tâches sur téléphone = Liste seulement, groupée par rythme.
- Ce qui n'existe que sur desktop doit rester atteignable : la vue e-ink et le desktop
  se consultent ailleurs, aucun lien mort ne doit pointer vers une vue absente.
- Annule la conséquence « le sankey horizontal de Budget devient un flux vertical »
  esquissée dans [[D-2026-09-19 Interface Téléphone Distincte]] — celle-ci restait une
  illustration du principe « repenser plutôt que reflow », pas un engagement.
- Réduit d'autant la surface à maintenir en double.

## Confirmation

Aucun composant de la vue téléphone ne rend de commutateur de vue pour Budget ou
Tâches, et aucun lien de la vue téléphone ne mène à un rendu Flux ou Année
(as of 2026-09 : orientation, la vue téléphone n'est pas encore implémentée).
