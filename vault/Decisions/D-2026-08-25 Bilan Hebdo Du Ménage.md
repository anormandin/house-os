---
type: decision
status: accepted
date: 2026-08-25
feature: "[[Tâches]]"
tags: []
---

# Bilan Hebdo Du Ménage

## Contexte

Constat d'usage (2026-08-25) : le foyer ne se soucie pas vraiment de *qui* fait une
tâche — la personne qui clique « fait » n'est souvent pas celle qui l'a faite.
L'attribution individuelle du journal est donc **indicative, pas fiable**. Ce qui
manquait : voir le total de tâches complétées par le ménage, semaine après semaine.
La carte « L'équipe » d'Aujourd'hui (tâches à faire par personne assignée) mettait en
avant une donnée par-personne dont le foyer ne se sert pas.

## Options considérées

- **Remplacer « L'équipe » par une carte « Bilan »** (total complété par semaine,
  ménage entier) — l'accueil reflète ce qui compte vraiment ; l'assignation
  par personne quitte l'accueil.
- **Ajouter une carte Bilan à côté de « L'équipe »** — garde tout, mais charge la
  colonne latérale d'une carte peu utilisée.
- **Un simple chiffre discret dans une carte existante** — minimal, mais pas
  d'historique par semaine.

Et pour l'attribution existante : la retirer (jette du code testé), l'anonymiser en
UI, ou la garder telle quelle en la déclarant indicative.

## Décision

Remplacer la carte « L'équipe » par une carte **« Bilan »** : total complété cette
semaine en grand + mini-histogramme des 8 dernières semaines (lundi au dimanche,
semaine locale du client). L'attribution existante est **conservée telle quelle**
(journal `qui`, rangée « bravo X », stratégies Alternance/MoinsLAFait par tâche),
mais déclarée indicative — aucune feature future ne doit supposer qu'elle est exacte.

Contrat : `GET /api/journal/bilan?de=&a=` retourne les instants de complétion du
journal dans `[de, a)` ; le client fournit les bornes et agrège par semaine locale
(même patron que le filtre `faites` — le serveur ne connaît pas le fuseau). Parité
MCP : outil `bilan_taches` (comptes par semaine, heure du serveur).

## Conséquences

- L'accueil ne montre plus la charge par personne ; l'assignation reste visible sur
  les rangées de tâches et dans l'éditeur.
- Les stratégies MoinsLAFait/Alternance tournent sur une attribution approximative —
  acceptable pour un foyer de deux, à ne pas raffiner.
- Le harnais Sqlite des tests convertit `EntreeJournal.CompleteeLe` en binaire
  (le fournisseur Sqlite ne traduit pas les comparaisons `DateTimeOffset`).

## Confirmation

- `grep "journal/bilan" server/HouseOs.Api/Features/Taches/TachesEndpoints.cs` existe.
- `grep "bilan_taches" server/HouseOs.Api/Features/Mcp/OutilsTaches.cs` existe.
- `grep "function Bilan" web/src/pages/Aujourdhui.tsx` existe (et `function Equipe`
  n'existe plus).
- Test `Bilan_retourne_les_completions_de_la_fenetre_seulement` dans
  `server/HouseOs.Tests/Features/Taches/OperationsTachesTests.cs`.
