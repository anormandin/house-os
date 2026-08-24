---
type: decision
status: accepted
date: 2026-08-24
feature: "[[Tâches]]"
tags: []
---

# Annulation Et Passage D'occurrences

## Contexte

Compléter une occurrence a trois effets (mutation du statut, ligne de journal,
matérialisation de la prochaine occurrence pour les récurrentes) et était
irréversible. Il manquait aussi un moyen de sauter une occurrence sans la marquer
faite (« passer ») et de glisser une échéance (« reporter »). Les stats d'équité
(« moins-l'a-fait ») et l'échéance des tâches à intervalle se calculent depuis le
journal, donc le sort de la ligne de journal à l'annulation est structurant.

## Options considérées

- **Annulation = effacement complet** (journal supprimé) vs **journal conservé marqué
  annulé** (trace historique, mais colonne à migrer et filtrage partout où le journal
  est lu).
- **Portée de l'annulation** : dernière complétion si la suivante est intacte vs
  aujourd'hui seulement vs n'importe quelle complétion (risque de ressusciter de
  vieilles occurrences et de casser l'invariant une-seule-en-attente).
- **Passer** : statut `Passee` conservé comme trace vs suppression + régénération.
- **UI d'annulation** : bouton sur la rangée verte vs toast avec action (aucun
  système de toast n'existe).

## Décision

(Choix d'Alain, 2026-08-24.)

- **Annuler efface tout** : ligne de journal supprimée, occurrence remise
  `EnAttente` (champs de complétion à null), occurrence suivante matérialisée
  supprimée. Équité et échéances restent cohérentes, aucun changement de schéma.
- **Garde-fou** : annulable seulement si c'est la complétion la plus récente de la
  tâche ET que la suivante est encore `EnAttente` ; sinon 409. L'occurrence annulée
  reprend son ancienne échéance et redevient éligible au rollover (voulu).
- **Passer** (récurrentes seulement) : troisième statut `Passee` daté (`PasseeLe`),
  aucune entrée de journal, la suivante est générée comme après une complétion
  aujourd'hui (même stratégie d'assignation).
- **Reporter** : change l'échéance de l'occurrence en attente seulement, jamais la
  définition ; dates passées refusées ; ponctuelles acceptées.
- **UI** : bouton « Annuler » au survol de la rangée verte ; pas de toast. Notes de
  complétion ajoutées post-hoc sur la rangée verte (la complétion reste à un clic).

## Conséquences

- L'enum `StatutOccurrence` gagne `Passee` (stocké en string — pas de migration de
  données) ; seule la colonne `PasseeLe` demande une migration.
- Les filtres existants (`completees`, `faites`, rollover, flux iCal) excluent
  naturellement `Passee` (ils testent `Completee`/`EnAttente`).
- Un « passer » n'est pas annulable (pas de journal, la suivante existe déjà).
- Après annulation puis re-complétion d'une tâche à intervalle, la prochaine
  échéance change légitimement (elle dépend de la date de complétion).

## Confirmation

Tests domaine `Annuler_completion_*` / `Passer_*` / `Reporter_*` dans
`server/HouseOs.Tests/Domaine/OccurrenceTests.cs` ; garde « pas la dernière » dans
`OperationsTaches.AnnulerCompletionAsync` ; absence d'écriture au journal dans
`PasserAsync` (`grep -n "db.Journal.Add" server/HouseOs.Api/Features/Taches/OperationsTaches.cs`
ne doit matcher que la complétion).
