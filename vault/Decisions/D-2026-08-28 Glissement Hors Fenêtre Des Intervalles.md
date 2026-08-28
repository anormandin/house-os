---
type: decision
status: accepted
date: 2026-08-28
feature: "[[Tâches]]"
tags: []
---

# Glissement Hors Fenêtre Des Intervalles

## Contexte

La ronde QA 2026-08 (issue #55, item T7) a montré qu'une tâche en mode
intervalle-depuis-complétion avec fenêtre saisonnière (« tondre aux 7 jours,
mai–octobre ») échue en fin de saison et jamais faite restait « en retard »
tout l'hiver dans Aujourd'hui : `RolloverService` ne faisait glisser que le
mode fixe. C'est précisément la culpabilisation que le rollover veut éviter.

## Options considérées

- **Statu quo** : « dû tant que pas fait », même hors saison. Simple, mais la
  tâche pollue Aujourd'hui pendant des mois alors qu'elle est inexécutable.
- **Glisser seulement si `rollover` est actif** : cohérent avec le flag, mais
  le flag est réservé au mode fixe (QA T9) et « en retard hors fenêtre » n'est
  pas un empilement d'occurrences — c'est un autre problème.
- **Glisser au début de la prochaine fenêtre** (retenue) : hors fenêtre, une
  occurrence échue glisse au premier jour de la prochaine fenêtre, comme le
  mode fixe le fait déjà. Indépendant du flag `rollover`.

## Décision

Choix d'Alain (triage QA 2026-08-28) : une occurrence en mode intervalle avec
fenêtre saisonnière, échue et non complétée quand la fenêtre se ferme,
**glisse au premier jour de la prochaine fenêtre**. Le glissement hors fenêtre
est une propriété de la fenêtre saisonnière, pas du flag `rollover` (qui reste
réservé au mode fixe).

## Conséquences

- Plus aucune tâche saisonnière « en retard » l'hiver ; elle réapparaît au
  début de la saison suivante, à sa juste place.
- `RolloverService` porte désormais deux règles distinctes : rollover du mode
  fixe (flag) et glissement hors fenêtre (intervalle + fenêtre, toujours).
- Le journal ne garde aucune trace de la non-exécution saisonnière — assumé,
  comme pour le rollover fixe.

## Confirmation

Test `RolloverServiceTests` couvrant « intervalle + fenêtre, échue, hors
fenêtre → échéance = premier jour de la prochaine fenêtre » (grep
`HorsFenetre` dans `server/HouseOs.Tests/Features/Taches/RolloverServiceTests.cs`).
