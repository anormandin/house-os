---
type: decision
status: accepted
date: 2026-08-25
feature: "[[Titre D'humeur]]"
tags: []
---

# Phrase Du Jour Axée Tâches

## Contexte

La première version de l'état vu par le LLM donnait la météo à chaque génération
(min/max, probabilité de pluie, verdicts favorables). Résultat : Haiku parlait météo
presque chaque jour, au présent (« profitez de la belle météo ») — faux dès que le
temps changeait entre la génération (5 h 30) et la lecture, et répétitif. L'utilisateur
a tranché (2026-08-25) : la météo est très secondaire ; ce qui rend la phrase
intéressante, ce sont les tâches qui s'en viennent et la motivation.

## Options considérées

- **Météo toujours présente dans l'état** (statu quo) — répétitif, et pousse le LLM
  à des affirmations météo au présent qui périment.
- **Météo seulement quand elle sort de l'ordinaire, tâches nommées au premier
  plan** — l'état porte les titres des tâches du jour et de celles qui s'en
  viennent ; la météo n'apparaît que via un signal « remarquable » calculé en C#.
- **Régénérer la phrase plus souvent pour suivre la météo** — plus de mécanique et
  d'appels pour soigner un détail que l'utilisateur juge secondaire.

## Décision

L'état vu par le LLM (et par la banque de gabarits) devient axé tâches :

- **Tâches nommées** : titres des tâches du jour (≤ 5) et des prochaines de la
  semaine avec leur horizon en jours (≤ 4) — matière concrète pour une phrase
  motivante et spécifique.
- **Météo sur exception seulement** : un évaluateur C# testable
  (`MeteoRemarquable`) produit une courte description française uniquement quand la
  journée sort de l'ordinaire — orage, neige, pluie soutenue, chaleur ≥ 30 °C,
  froid ≤ −15 °C, grand vent, ou journée exceptionnellement belle. Sinon la météo
  est absente de l'état, et le prompt interdit d'en parler.
- **Ton** : inspirant et motivant, tourné vers ce qui s'en vient ; jamais de
  culpabilisation (principe existant), jamais d'affirmation météo au présent hors
  signal remarquable.

La cadence deux-créneaux et la cascade de replis de
[[D-2026-08-24 Phrase Du Jour Haiku Matin Et Soir]] restent inchangées.

## Conséquences

- `EtatMaison` perd `MeteoDuJour` (min/max/probabilité/verdicts) au profit de
  `TachesDuJour`, `ProchainesTaches` et `MeteoRemarquable?` — la banque de gabarits
  et la sérialisation LLM suivent.
- Les seuils du « remarquable » sont des constantes ajustables par code + test,
  comme les règles météo.
- La phrase peut nommer des tâches réelles : les titres saisis par les
  utilisateurs transitent par l'API Anthropic (même donnée, nouveau champ).

## Confirmation

- `grep "meteoRemarquable" server/HouseOs.Api/Features/Humeur/PolissageLlm.cs`
  existe et `grep "probabilitePluiePct" server/HouseOs.Api` ne retourne rien.
- Tests `MeteoRemarquableTests` dans `server/HouseOs.Tests/Domaine/`.
