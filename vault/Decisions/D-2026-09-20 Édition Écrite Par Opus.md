---
type: decision
status: accepted
date: 2026-09-20
feature: "[[Journal De La Maison]]"
tags: []
---

# D-2026-09-20 Édition Écrite Par Opus

## Contexte

[[Titre D'humeur]] fait **deux appels Haiku par jour** pour une phrase de ~60
caractères ([[D-2026-08-24 Phrase Du Jour Haiku Matin Et Soir]]) — un polissage, pas de
l'écriture.

L'éditorialiste, lui, écrit **une fois par jour** un surtitre, une manchette, un chapeau
et deux paragraphes de corps avec une voix : la lettrine, le rythme, le rapprochement
entre deux faits qu'aucune liste ne ferait. C'est le produit, pas la décoration.

`HumeurOptions.Modele` est déjà configurable : rien n'oblige les deux couches à partager
un modèle.

## Options considérées

- **Haiku partout** — un seul modèle, une seule ligne de config, coût et latence au
  plancher. Mais si la manchette est fade, on aura fait tout ce chantier pour un gabarit
  déguisé.
- **Sonnet 5 pour l'édition** — bon compromis coût/qualité pour un appel par jour.
- **Opus 5 pour l'édition** (retenue).

## Décision

L'édition est écrite par **Opus 5** (`claude-opus-5`), un appel par jour, dans son propre
réglage de configuration. [[Titre D'humeur]] **garde Haiku** pour ses deux créneaux : les
deux couches ne partagent ni modèle ni prompt.

L'appel est hors du chemin de requête, dans un `BackgroundService` avec repli en
gabarit : la latence d'Opus ne se voit nulle part.

Choix d'Alain, en grillage.

## Conséquences

- Coût : un appel Opus par jour, quelques dollars par mois au plus — négligeable devant
  l'enjeu, qui est que la prose tienne debout.
- Le modèle est **configurable** et le repli en gabarit est obligatoire : le jour où
  l'API est absente (installation sans clé, panne), le journal sort quand même, marqué
  comme gabarit.
- Le prompt de l'édition doit porter ses propres garde-fous : **aucun fait inventé**,
  les chiffres et les titres viennent tous de l'état fourni, et la sortie est validée
  strictement avant d'être écrite (même patron défensif que `PolissageLlm.Extraire`).
- La mémoire des sept derniers jours est envoyée au modèle pour qu'il ne radote pas
  ([[D-2026-09-20 Une Édition Par Jour Matérialisée]]).

## Confirmation

Le modèle de l'édition est lu d'une option de configuration distincte de
`HumeurOptions.Modele`, avec `claude-opus-5` comme défaut, et un test couvre le repli en
gabarit quand aucune clé n'est disponible.
