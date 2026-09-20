---
type: decision
status: accepted
date: 2026-09-20
feature: "[[Journal De La Maison]]"
tags: [iot]
---

# D-2026-09-20 Échéance Ferme Explicite Sur La Tâche

## Contexte

Le plancher non négociable de [[Journal De La Maison]] couvre trois cas : une tâche en
retard de plus de trois jours, un compte à rebours à zéro, et une **échéance ferme**
(notaire, livraison payée, date légale). Les deux premiers se calculent ; le troisième
n'a **aucune représentation dans le modèle**. `Tache` et `Occurrence` portent une
échéance, jamais sa nature : rien ne distingue « passer le balai avant vendredi » de
« signer chez le notaire le 2 octobre à 14 h ».

Constaté en livrant l'étape 1 du [[Plan 2026-09-20 Journal Éditorial]] : le plancher a
été livré sur deux cas sur trois. À l'étape 7, l'éditorialiste devra savoir ce qu'il
n'a **pas le droit** de reléguer sous un widget — c'est le moment où l'approximation
cesse d'être tolérable.

## Options considérées

- **Déduire du modèle existant** — une ponctuelle avec échéance explicite et sans
  rollover serait « ferme ». Aucune migration, disponible tout de suite. Mais c'est
  faux dans les deux sens : la plupart des ponctuelles sont parfaitement négociables
  (« acheter un boyau d'arrosage »), et une récurrente peut porter une date imposée
  (le versement d'impôt du 30 avril). Un plancher qui crie pour rien perd exactement
  la crédibilité qu'il existe pour protéger.
- **Un enum `NatureEcheance` (négociable | ferme | légale)** — plus fin, et une
  troisième valeur ouvrirait un traitement distinct pour les dates légales. Mais
  personne n'a demandé la nuance, et un enum à trois valeurs dans l'éditeur demande une
  explication que le foyer n'a pas à lire.
- **Un booléen `EcheanceFerme` sur `Tache`** (retenue) — un seul champ, une case à
  cocher dans l'éditeur, une sémantique qui tient en une phrase : *cette date vient du
  dehors, elle ne se négocie pas*.

## Décision

`Tache` gagne un booléen **`EcheanceFerme`** (défaut `false`), porté jusqu'à
l'`OccurrenceDto` et jusqu'au DTO de l'écran. Coché à la main dans l'éditeur de tâche —
aucune déduction, aucune heuristique : c'est une affirmation du foyer sur le monde, pas
un calcul.

**Bâti à l'étape 7**, dans la **même migration** que l'entité d'édition, pas à l'étape 2
(qui est explicitement sans schéma). Jusque-là le plancher tourne sur deux cas, ce qui
est l'état livré et vérifié en prod le 2026-09-20.

Choix d'Alain, 2026-09-20, sur les trois options ci-dessus.

## Conséquences

- Une migration EF, mutualisée avec celle de l'édition — une seule, pas deux.
- `plancher()` (`web/src/lib/ecran-vues.ts`) gagne un troisième cas et une raison
  `'ferme'` ; son surtitre de manchette suit. La fonction reste pure et testée.
- **Parité MCP** ([[Serveur MCP]]) : `gerer_tache` expose le
  champ dans la même tranche, sinon un agent ne peut pas créer la tâche du notaire
  correctement.
- `TacheEditeur` doit **charger** le champ : un `PUT` de tâche remplace tout, et un
  champ non chargé est effacé au save — le piège connu de cet éditeur.
- Le champ est **facultatif et par défaut faux** : un foyer qui ne s'en sert jamais ne
  voit aucune différence, et le journal se comporte comme aujourd'hui.

## Confirmation

À partir de l'étape 7 du [[Plan 2026-09-20 Journal Éditorial]] :

```
grep -rn "EcheanceFerme" server/HouseOs.Api/Domaine/Tache.cs server/HouseOs.Api/Features/Mcp/
grep -n "'ferme'" web/src/lib/ecran-vues.ts web/src/lib/ecran-vues.test.ts
```

doivent retourner au moins une ligne chacun. Avant l'étape 7, la décision est **prise et
non bâtie** : le plancher tourne sur deux cas, et c'est voulu.
