---
type: decision
status: accepted
date: 2026-08-28
feature: "[[Tâches]]"
tags: []
---

# Passer Conserve L'assigné

## Contexte

« Passer » une occurrence la clôt sans complétion et matérialise la suivante.
Le comportement initial (volontaire, documenté dans le XML de `PasserAsync`)
passait le cliqueur comme dernier compléteur : en stratégie alternance, sauter
son tour donnait le prochain tour à l'autre — qui héritait du tour sauté en
plus des siens. La ronde QA 2026-08 (issue #55, item T8) a remis ce choix en
question.

## Options considérées

- **Statu quo — décaler comme une complétion** : défendable si « passer »
  signifie « cette itération ne comptait pas », mais inéquitable : l'autre
  personne absorbe le tour sauté.
- **Conserver l'assigné** (retenue) : le tour n'a pas été pris, la prochaine
  occurrence reste à la même personne, pour l'alternance comme pour
  moins-l'a-fait.

## Décision

Choix d'Alain (triage QA 2026-08-28) : pour les stratégies tournantes
(alternance, moins-l'a-fait), **passer conserve l'assigné** de l'occurrence
passée. Seule une complétion fait tourner l'assignation. Supersède le
comportement initial (qui n'avait pas de décision dédiée).

## Conséquences

- Sauter son tour ne pénalise plus l'autre personne ; une tâche passée
  revient au même assigné jusqu'à ce qu'elle soit faite.
- Le XML de `PasserAsync` est réécrit pour documenter la nouvelle sémantique.

## Confirmation

Tests `OperationsTachesTests` : passer en alternance et en moins-l'a-fait
conserve l'assigné ; compléter décale toujours (grep `PasserConserve` dans
`server/HouseOs.Tests/Features/Taches/OperationsTachesTests.cs`).
