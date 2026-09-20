---
type: decision
status: accepted
date: 2026-09-20
feature: "[[Fonds De Tiroir]]"
tags: [iot]
---

# D-2026-09-20 Fonds De Tiroir Séparé Du Journal

## Contexte

Trois tours de maquettes ([[Éditorialiste De L'Écran]]) ont retenu une refonte
éditoriale de la seconde vue : une broadsheet à densité variable, alimentée par une
trentaine de « petites choses vraies » (le ciel, le climat, la maison, le calendrier,
la ville, le hasard) dont un éditorialiste LLM tire ce qui adonne.

Deux choses très différentes se cachent là-dedans : **produire et classer des faits**
(calculs d'éphémérides, normales, lectures du journal de complétion, ingestion
municipale, règle de score) et **publier** (la grille, les rangs, la manchette, la
prose). La seconde forme retenue des maquettes — la lettre du matin (`C1`) — veut
exactement la même matière sans jamais toucher à l'e-ink.

## Options considérées

- **Une évolution de [[Affichage E-ink]]** — une seule note à tenir. Mais elle
  mélangerait le protocole d'un appareil (TRMNL, pile, cadence, capture Chromium) avec
  un moteur éditorial qui n'a rien à voir avec l'e-ink, et le jour où la lettre du matin
  arrive, la moitié de la spec de l'écran devient sa dépendance.
- **Une seule feature « Journal »** — cohérente pour une livraison d'un bloc, mais elle
  enferme le fonds de tiroir derrière une mise en page : rien n'empêcherait le score de
  supposer trois colonnes et un budget de widgets.
- **Deux features** (retenue) — [[Fonds De Tiroir]] produit des faits classés,
  indépendants de toute sortie ; [[Journal De La Maison]] les met en page. Deux specs à
  ouvrir et refermer pour un travail qui se livre en une seule fois.

## Décision

Deux features. **[[Fonds De Tiroir]]** expose des faits datés, scorés et sans mise en
forme ; il ne connaît ni pixel, ni colonne, ni 1-bit. **[[Journal De La Maison]]** est
son premier consommateur ; la lettre du matin sera le second, dans sa propre feature.
[[Affichage E-ink]] reste la spec de l'appareil et ne bouge pas.

Choix d'Alain, en grillage — motif donné : « le fonds de tiroir pourrait aussi servir
au courriel du matin ».

## Conséquences

- Le contrat du fonds de tiroir est une **liste de faits ordonnés par score**, pas un
  gabarit : chaque fait porte son texte long et son texte court, et c'est le
  consommateur qui choisit lequel il a la place de montrer.
- Le budget de widgets, les rangs et la lettrine appartiennent au journal, jamais au
  fonds de tiroir.
- Corollaire de test : le fonds de tiroir doit se tester sans rendu, sans Playwright et
  sans appareil.
- Deux notes de feature à garder en phase ; le plan d'exécution est commun et vit chez
  [[Journal De La Maison]].

## Confirmation

Aucun fichier sous `server/HouseOs.Api/Features/FondsDeTiroir/` ne référence
`Affichage`, `Playwright`, `Seuillage` ni une dimension en pixels :

```
grep -rn "Affichage\|Playwright\|Seuillage\|1872\|1404" server/HouseOs.Api/Features/FondsDeTiroir/
```

doit ne rien retourner.
