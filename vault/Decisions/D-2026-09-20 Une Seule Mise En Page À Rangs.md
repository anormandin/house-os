---
type: decision
status: accepted
date: 2026-09-20
feature: "[[Journal De La Maison]]"
tags: [iot]
---

# D-2026-09-20 Une Seule Mise En Page À Rangs

## Contexte

Le tableau de bascule des maquettes ([[Éditorialiste De L'Écran]]) décrit six
remplissages, du jour vide au jour à quatorze tâches. Deux lectures possibles : une
grille unique qui se remplit plus ou moins, ou deux gabarits (la broadsheet et la
« manchette seule ») avec une règle qui choisit.

La ligne « plusieurs mises en page ou plugins » est explicitement **hors périmètre**
dans [[Affichage E-ink]] ; la retenir en deux gabarits aurait demandé de la rouvrir.

## Options considérées

- **Deux gabarits + règle de bascule** — plus franc à trois mètres quand il n'y a qu'une
  chose à dire. Mais deux rendus à calibrer, deux surfaces où un débordement peut
  passer (celui corrigé le 2026-09-20 se jouait à 50 px), et une règle de bascule qui
  doit devenir aussi sûre que le score lui-même.
- **Une grille + un rang « affiche »** — la manchette en pleine largeur au rang 0, sans
  seconde page à maintenir. Retenu de fait : c'est ce que fait le rang 0 ci-dessous.
- **Une seule mise en page à rangs** (retenue) — les six maquettes partagent
  littéralement les classes CSS ; seuls la grille du corps et le nombre de widgets
  changent. « Manchette seule » n'est que le rang extrême de la même grille.

## Décision

Une seule mise en page. Le rang est une **fonction du nombre de tâches dues et du
plancher**, pas un choix de gabarit ; les widgets **rétrécissent avant de disparaître**
(le tableau du ciel devient une phrase, puis une demi-phrase).

Les nombres sont ceux **mesurés sur les maquettes** : trois colonnes tiennent ~9 items
chacune, donc ~27 tâches avant saturation réelle, quand la journée la plus chargée de
toute la prod en compte 14 (le 2026-10-20). La contrainte n'est pas la place, c'est la
**manchette**, qui n'a plus de sens passé ~6 items.

Choix d'Alain, en grillage.

## Conséquences

- La ligne « plusieurs mises en page » du **hors périmètre** de [[Affichage E-ink]]
  **reste vraie** : rien à rouvrir, rien à superséder.
- Une seule surface à vérifier au seuillage 1872×1404, et un seul endroit où un
  débordement peut naître.
- Le rang doit être une fonction **pure et testable** : entrée = compte de tâches dues,
  plancher déclenché, budget ; sortie = grille du corps et nombre de widgets.
- Ce qui disparaît d'abord est la **manchette**, pas le texte : le journal n'a jamais le
  droit de rapetisser un corps pour faire tenir une ligne de plus
  ([[Affichage Mural Et E-ink]] : « élaguer, pas rapetisser »).

## Confirmation

Une seule page sert la vue e-ink : `web/src/pages/Ecran.tsx` reste l'unique fichier de
`web/src/pages/` importé par la route d'écran, et le choix de rang est une fonction pure
testée dans `web/src/lib/` (test nommé d'après le rang). Vérifier qu'aucune seconde
route de rendu n'est apparue :

```
grep -rn "apercu.png\|DemandeCapture" server/HouseOs.Api/Features/Affichage/ | grep -i "gabarit\|template\|vue="
```

doit ne rien retourner.
