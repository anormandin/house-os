---
type: decision
status: accepted
date: 2026-09-21
feature: "[[Journal De La Maison]]"
tags: []
---

# D-2026-09-21 Matière Conservée Sur L'Édition

## Contexte

Le prompt de l'éditorialiste ([[D-2026-09-20 Édition Écrite Par Opus]]) se règle à
la lecture : on relit des éditions contre ce que le modèle a reçu, on note ce qui
cloche, on retouche, on compare. Or l'édition ne conservait que ce que le modèle a
**écrit** (surtitre, manchette, chapeau, corps, rubriques), jamais ce qu'il a **reçu**.
Et la matière ne se recompose pas à l'identique : le fonds de tiroir tire au hasard,
la météo change, la mémoire des sept jours glisse. Une journée passée ne pouvait donc
pas être rejouée contre un autre prompt ; chaque essai coûtait une régénération
complète, sur une matière différente, et deux prompts ne se comparaient jamais sur le
même état.

## Options considérées

- **Ne rien conserver, régénérer en dev** — le geste de l'étape 7 (sept journées à la
  suite par `regenerer_journal_mural`). Gratuit en code, mais chaque essai change la
  matière avec le prompt : on ne sait jamais ce qui a joué.
- **Écrire la matière dans un fichier, en dev seulement** — pas de schéma, mais rien
  de la prod, là où les vraies journées se passent et où le modèle se tait parfois.
- **Conserver la matière sur l'édition** (retenue) — le JSON envoyé au modèle, dans
  une colonne de plus ; nul quand personne n'a rien demandé.

## Décision

L'édition conserve la **matière telle que le modèle l'a reçue** (`Edition.Matiere`,
jsonb — qui en renormalise l'ordre des clés et les espaces, pas le contenu), posée à chaque fois qu'on lui a demandé d'écrire — **y compris quand il s'est
tu** et que le gabarit a pris la place : c'est précisément cette journée-là qu'on
voudra rejouer. Un gabarit posé par le rendu, qui ne demande rien, n'en a pas.

La forme sérialisée devient un contrat qui se **relit** (`RedactionLlm.DeserialiserMatiere`),
et un **atelier** hors de l'image Docker (`server/HouseOs.Essais`) imprime le prompt en
vigueur, extrait la matière d'une date, et la rejoue contre un fichier de prompt, autant
de fois qu'on veut, en montrant les longueurs et les refus. Le prompt de l'application
reste une constante du code : l'atelier sert à lire avant de toucher.

## Conséquences

- Une colonne jsonb de plus, quelques Ko par jour ; rien n'est purgé — sept jours
  suffisent à la mémoire, mais une année de matières est un corpus de réglage.
- Deux prompts se comparent sur **le même état**, et une journée de prod où le modèle
  a répondu hors contrat se rejoue chez soi avec le texte brut sous les yeux.
- La matière contient les titres de tâches et les noms de pièces du foyer, comme
  l'édition elle-même : rien de plus que ce qui est déjà dans la base.
- L'atelier n'entre pas dans `Dockerfile` (qui publie `HouseOs.Api` seul) ; la CI le
  compile avec la solution.

## Confirmation

`Edition.Matiere` existe et `GenerationEdition` la pose dans `GenererAsync` et
`ReessayerAsync` ; les tests `L_edition_garde_la_matiere_donnee_au_modele_meme_quand_il_s_est_tu`
et `La_matiere_conservee_se_relit_telle_quelle` passent ; `server/HouseOs.Essais` est
dans `HouseOs.sln` et absent du `Dockerfile`.
