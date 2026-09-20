---
type: decision
status: accepted
date: 2026-09-20
feature: "[[Fonds De Tiroir]]"
tags: []
---

# D-2026-09-20 Calendrier De Collectes Régénéré À La Main

## Contexte

[[D-2026-09-20 Sources Municipales Séparées Par Solidité]] fait du PDF annuel des
collectes un ICS produit hors dépôt. Reste à décider **qui le régénère et quand**.

Le PDF est la seule source municipale qui porte une **vraie échéance** : l'analyse
repose sur `pdftotext -bbox-layout` et sur l'alignement des codes `DS/DN OS/ON RS/RN…`
avec les colonnes de jours de la grille. Un décalage de colonne écrit de fausses dates
de collecte, en silence, pour un an.

## Options considérées

- **Tiré sur cadence** — le worker extérieur vérifie l'URL chaque semaine
  (`If-Modified-Since`), ré-analyse et régénère tout seul. On n'oublie jamais — mais une
  refonte du gabarit du PDF produit des dates fausses sans que personne regarde, sur la
  seule source à échéance ferme.
- **Cadence qui avertit seulement** — le worker surveille et émet un fait « le calendrier
  2027 est paru » ; la régénération reste humaine. Bon compromis, mais il faut écrire et
  héberger le worker de surveillance tout de suite.
- **À la main, gardé par une tâche annuelle** (retenue).

## Décision

La conversion PDF → ICS est un **geste humain annuel** (~15 min), fait avec un script
documenté hors dépôt. Le rappel est une **tâche récurrente annuelle** de House OS —
« régénérer le calendrier de collectes » — c'est-à-dire exactement ce que le moteur de
récurrence sait déjà faire ([[Tâches]]), sans une ligne de code neuve.

Choix d'Alain, en grillage.

## Conséquences

- **La panne est visible, jamais silencieuse** : si personne ne régénère, l'ICS
  s'épuise et le journal cesse d'annoncer des collectes — il n'en annonce pas de
  fausses.
- Aucune surveillance à écrire, aucun worker à héberger pour cette source.
- La tâche annuelle doit être créée **avant** que l'ICS 2026 s'épuise, avec une échéance
  en début d'année civile (la ville publie le calendrier de l'année en cours).
- Si un jour la régénération devient pénible, la voie de sortie est « la cadence qui
  avertit » — elle ne demande pas de revenir sur cette décision, seulement de l'étendre.

## Confirmation

Une tâche récurrente annuelle dont le titre contient « collectes » existe en prod
(`lister_taches` via [[Serveur MCP]]), et `grep -rn "pdftotext" .` dans le dépôt ne
retourne rien.
