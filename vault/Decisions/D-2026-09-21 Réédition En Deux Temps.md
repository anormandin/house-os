---
type: decision
status: proposed
date: 2026-09-21
feature: "[[Journal De La Maison]]"
tags: [iot]
---

# D-2026-09-21 Réédition En Deux Temps

## Contexte

[[D-2026-09-20 Une Édition Par Jour Matérialisée]] pose deux exigences qui se
contredisent au premier abord : le plancher est **réévalué à chaque rendu** et, s'il se
déclenche après coup, il **redéclenche une édition** ; et l'appel LLM ne se fait
**jamais dans le chemin de requête** (le rendu de l'appareil est `GET /api/display`,
toutes les quinze minutes, avec Chromium qui attend la page). Opus prend dix à
soixante secondes pour écrire : le rendu ne peut pas l'attendre, et le mur ne peut pas
mentir en attendant.

La même question se pose pour une **édition manquante** : un redémarrage avant le
créneau du matin, ou le premier rendu après la migration.

## Options considérées

- **Rappeler Opus dans le rendu** — la manchette juste dès le premier réveil, mais un
  rendu qui dure une minute, un appel LLM dans une requête HTTP, et un appareil qui
  redemande son écran pendant que le précédent écrit encore.
- **Rééditer en gabarit seulement** — immédiat et sans coût, mais la journée du notaire
  ou du camion, celle qui compte le plus, serait la seule sans prose.
- **En deux temps** (retenue) — le rendu pose un gabarit tout de suite et réveille le
  service de fond, qui rappelle Opus hors requête.

## Décision

Quand le rendu constate que l'édition manque, ou que le plancher qu'il vient d'évaluer
n'est plus celui que l'édition porte, il **écrit un gabarit** (la manchette du
plancher, sa raison en surtitre, la phrase du matin en repli), lève le drapeau
`ReeditionEnAttente` sur l'édition, et réveille l'éditorialiste par un signal
(`SignalDeReedition`). Le service de fond, réveillé, réécrit l'édition avec Opus et
baisse le drapeau — **qu'il réussisse ou non** : un modèle qui a échoué ne se rappelle
pas à chaque réveil de l'appareil, il se rappelle le lendemain matin.

Le rendu ne persiste que pour la **journée vraie** ; une horloge d'essai sur un autre
jour compose l'édition de ce jour-là sans l'écrire.

Décision prise en implémentant l'étape 7 du [[Plan 2026-09-20 Journal Éditorial]]
(2026-09-21), **à confirmer par Alain**.

## Conséquences

- Le mur affiche la bonne manchette dès le premier réveil qui suit le plancher, et la
  prose d'Opus au suivant (quinze minutes plus tard au plus).
- Un appel LLM de plus par changement de plancher, borné par nature : un compte à
  rebours ne tombe à zéro qu'une fois, un retard ne passe trois jours qu'une fois.
- Le chemin de requête écrit en base (une ligne d'édition), ce que le titre d'humeur ne
  faisait pas. C'est une écriture idempotente et rare.

## Confirmation

`grep -rn "SignalDeReedition" server/HouseOs.Api/Features/Affichage/ComposerDonneesEcran.cs`
retourne au moins une ligne, et le test
`GenerationEditionTests.Le_plancher_qui_se_declenche_apres_coup_redeclenche_une_edition`
vérifie qu'un rendu n'appelle pas le modèle et que le service de fond le fait ensuite.
