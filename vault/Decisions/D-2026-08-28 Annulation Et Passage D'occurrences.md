---
type: decision
status: accepted
date: 2026-08-28
feature: "[[Tâches]]"
supersedes: "[[D-2026-08-24 Annulation Et Passage D'occurrences]]"
tags: []
---

# Annulation Et Passage D'occurrences

## Contexte

Reprend [[D-2026-08-24 Annulation Et Passage D'occurrences]], dont la sémantique tient
toujours mais dont la clause UI est devenue fausse. Cette clause disait « bouton
"Annuler" au survol de la rangée verte ; **pas de toast** » — écrite alors qu'aucun
système de toast n'existait dans l'app. L'issue #60 en a introduit un (confirmation
d'un geste portant son action inverse), et [[D-2026-08-28 Toast Carte Posée]] vient
d'en fixer la forme. Le vault affirmait donc le contraire du code.

Le rappel du contexte d'origine : compléter une occurrence a trois effets (mutation du
statut, ligne de journal, matérialisation de la prochaine occurrence pour les
récurrentes) et était irréversible. Il manquait aussi un moyen de sauter une occurrence
sans la marquer faite (« passer ») et de glisser une échéance (« reporter »). Les stats
d'équité (« moins-l'a-fait ») et l'échéance des tâches à intervalle se calculent depuis
le journal, donc le sort de la ligne de journal à l'annulation est structurant.

## Options considérées

Celles de la décision d'origine (annulation = effacement vs journal marqué annulé ;
portée de l'annulation ; `Passee` conservé vs suppression + régénération), toutes
tranchées le 2026-08-24 et reconduites ici sans changement.

Le fork rouvert est le dernier, celui de l'UI d'annulation :

- **Bouton au survol de la rangée verte seul** (le choix de 2026-08-24) — durable,
  mais invisible tant qu'on ne survole pas : rien ne rattrape le mauvais clic sur le
  moment.
- **Toast avec action inverse seul** — attrape le mauvais clic, mais s'éteint en 6 s :
  quiconque regarde ailleurs perd son recours.
- **Les deux, chacun dans son rôle** — un doublon apparent, à condition d'assumer que
  les deux ne répondent pas à la même question.

## Décision

(Choix d'Alain. Sémantique reconduite du 2026-08-24 ; clause UI tranchée le
2026-08-28.)

Inchangé depuis la décision d'origine :

- **Annuler efface tout** : ligne de journal supprimée, occurrence remise `EnAttente`
  (champs de complétion à null), occurrence suivante matérialisée supprimée. Équité et
  échéances restent cohérentes, aucun changement de schéma.
- **Garde-fou** : annulable seulement si c'est la complétion la plus récente de la
  tâche ET que la suivante est encore `EnAttente` ; sinon 409. L'occurrence annulée
  reprend son ancienne échéance et redevient éligible au rollover (voulu).
- **Passer** (récurrentes seulement) : troisième statut `Passee` daté (`PasseeLe`),
  aucune entrée de journal, la suivante est générée comme après une complétion
  aujourd'hui — mais **sans faire tourner l'assignation** : le tour n'a pas été pris,
  il reste à la même personne ([[D-2026-08-28 Passer Conserve L'assigné]], qui a
  révisé sur ce point la décision d'origine).
- **Reporter** : change l'échéance de l'occurrence en attente seulement, jamais la
  définition ; dates passées refusées ; ponctuelles acceptées.
- **Notes de complétion** ajoutées post-hoc sur la rangée verte — la complétion reste
  à un clic.

Révisé :

- **UI d'annulation — les deux, chacun dans son rôle.** Le **toast** est le filet
  immédiat : il confirme le geste et porte son action inverse pendant 6 s, avec le
  temps restant visible dans le bouton ([[D-2026-08-28 Toast Carte Posée]]). Le
  **bouton « Annuler » au survol de la rangée verte** est le recours durable : il ne
  s'éteint jamais. Le premier répond « je viens de me tromper », le second « je me
  suis trompé tantôt ». C'est ce qui autorise le toast à être éphémère sans que
  personne ne perde son recours — donc aucun des deux ne doit disparaître au profit
  de l'autre.

## Conséquences

- L'enum `StatutOccurrence` gagne `Passee` (stocké en string — pas de migration de
  données) ; seule la colonne `PasseeLe` demande une migration. (Fait depuis
  2026-08-24.)
- Les filtres existants (`completees`, `faites`, rollover, flux iCal) excluent
  naturellement `Passee` (ils testent `Completee`/`EnAttente`).
- Un « passer » n'est pas annulable (pas de journal, la suivante existe déjà).
- Après annulation puis re-complétion d'une tâche à intervalle, la prochaine échéance
  change légitimement (elle dépend de la date de complétion).
- Toute évolution du toast doit préserver le bouton au survol, et inversement :
  supprimer l'un fait porter à l'autre une charge pour laquelle il n'est pas fait.

## Confirmation

Tests domaine `Annuler_completion_*` / `Passer_*` / `Reporter_*` dans
`server/HouseOs.Tests/Domaine/OccurrenceTests.cs` ; garde « pas la dernière » dans
`OperationsTaches.AnnulerCompletionAsync` ; absence d'écriture au journal dans
`PasserAsync` (`grep -n "db.Journal.Add" server/HouseOs.Api/Features/Taches/OperationsTaches.cs`
ne doit matcher que la complétion). Pour la clause UI : les deux recours doivent
coexister dans `web/src/components/OccurrenceListe.tsx` — le bouton au survol
(`aria-label` « Annuler la complétion de … ») et le toast à action inverse, couvert
par le test « compléter affiche un toast dont "Annuler" défait la complétion » dans
`web/src/components/OccurrenceListe.test.tsx`.
