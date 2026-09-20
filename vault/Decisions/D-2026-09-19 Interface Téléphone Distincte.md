---
type: decision
status: accepted
date: 2026-09-19
feature: "[[Vue Téléphone]]"
supersedes: "[[D-2026-08-23 Interface Desktop Et Écran E-ink]]"
tags: []
---

# Interface Téléphone Distincte

## Contexte

[[D-2026-08-23 Interface Desktop Et Écran E-ink]] posait « desktop d'abord ; téléphone
non prioritaire (probablement jamais) ». Un audit de l'UI au gabarit iPhone 15
(393 × 852, émulation Playwright, 2026-09-19) a mesuré ce que ça donne en pratique :
aucun écran sauf la connexion ne tient dans 393 pt. Les largeurs exigées vont de 768 px
(Pièces, Tâches, Équipements) à 1161 px (Documents) et 1196 px (Budget → Flux) — Safari
réduit donc la page entre 51 % et 33 %. Trois défauts dépassent la simple étroitesse :

- Dans la console Tâches, le titre de la tâche tombe à **0 px de large** (il est
  `min-w-0 truncate` entre des voisins `shrink-0`) : on voit le rythme et la date,
  jamais le nom.
- Reporter / Passer / **Supprimer** sont `opacity-0` révélés au survol : sans `:hover`,
  ils sont invisibles et inatteignables au doigt.
- Les modales sont saines en soi, mais le débordement horizontal de la page étire le
  calque fixe : l'éditeur de tâche se centre dans 768 px et sort de l'écran.

L'utilisateur a tranché : le téléphone devient une cible, avec une UI **propre à lui**,
et les décisions qui visent le desktop ne s'y appliquent pas.

## Options considérées

- **Trois vues distinctes (desktop · téléphone · e-ink)** — chaque facteur de forme a
  son arbre de présentation ; la logique, les hooks de données et le domaine restent
  partagés. Le desktop ne bouge pas.
- **Un seul rendu responsive** — préfixes Tailwind dans les composants existants.
  Moins de code, mais Documents (tableau 6 colonnes) et Budget → Flux (sankey de
  1148 px) ne se replient pas par breakpoint : ils demandent une autre représentation,
  pas une autre largeur.
- **Statu quo desktop seulement** — écarté : le téléphone est l'appareil qu'on a dans
  la main quand on coche une tâche en passant.

## Décision

Le téléphone (et la tablette en portrait) reçoit une **interface distincte**, sœur de
la vue e-ink : même domaine, même API, présentation repensée. Le desktop reste tel
quel et garde la primauté. Les contraintes de forme des décisions desktop ne
s'appliquent pas à la vue téléphone — en particulier, la **barre d'onglets fixée en bas
est désormais permise** sur téléphone (elle restait interdite sur desktop).

Ce qui reste valide de [[D-2026-08-23 Interface Desktop Et Écran E-ink]] : le desktop
est la vue principale ; l'e-ink est une vue distincte à rendu serveur ; le stack
frontend de [[D-2026-08-23 Frontend Vite React PWA]] ne change pas. Ce qui est renversé :
« le téléphone ne sera probablement jamais une cible » et l'interdiction de la barre
d'onglets du bas.

La direction visuelle ne change pas : [[D-2026-08-23 Direction Artistique Cuisine Chaleureuse]]
gouverne les trois vues — mêmes tokens, même typographie.

## Conséquences

- Un point de bascule unique décide de l'arbre rendu ; la tablette en portrait suit le
  téléphone. Le seuil exact et le mécanisme d'aiguillage restent à arbitrer.
- Les écrans qui ne se replient pas demandent une **représentation** neuve, pas un
  reflow : le tableau de Documents devient une liste + facettes en feuille, le sankey
  horizontal de Budget devient un flux vertical.
- Les actions au survol doivent avoir un équivalent tactile explicite (feuille
  d'actions), y compris pour les gestes destructeurs — qui gardent leur double
  confirmation.
- Tout champ de saisie de la vue téléphone est à ≥ 16 px, sinon iOS zoome au focus.
- Coût récurrent : deux présentations à maintenir pour chaque tranche de feature. La
  parité MCP/API n'est pas touchée (les outils MCP ignorent la présentation).
- Maquettes des directions explorées : canevas Claude Design « House OS — UI téléphone »
  (2026-09-19), en complément de `design/maquettes/`.

## Confirmation

`web/src/components/Layout.tsx` cible toujours le desktop (en-tête, pas de barre
d'onglets du bas). L'aiguillage propre au téléphone se vérifie par un point de bascule
unique — `web/src/hooks/useFormatPhone.ts` doit rester le seul `matchMedia` de largeur
de l'app — et par l'existence de composants téléphone distincts sous
`web/src/components/telephone/` et `web/src/pages/telephone/`.

> [!note] Mise à jour de la seule Confirmation (2026-09-20)
> La clause « tant qu'il n'est pas implémenté, cette décision est une orientation
> (as of 2026-09) » a été remplacée par la vérification ci-dessus : livré dans `a77b04a`.
> Le corps de la décision n'a pas été touché — voir [[Vue Téléphone]].
