---
type: decision
status: accepted
date: 2026-09-21
feature: "[[Lettre Du Matin]]"
tags: []
---

# D-2026-09-21 Lettre Écrite À Part Sur La Même Matière

## Contexte

Le journal mural a déjà un éditorialiste ([[D-2026-09-20 Édition Écrite Par Opus]]) :
une matière (`MatiereDEdition`), un prompt réglé à la lecture depuis l'étape 7, une
mémoire des sept jours, un repli en gabarit, un second essai une heure plus tard
([[D-2026-09-21 Réédition En Deux Temps]]). La [[Lettre Du Matin]] veut la même
matière dans un autre registre : de la prose en plusieurs paragraphes, la maison qui
écrit à ses habitants, avec du contexte, des traits d'esprit, une pensée du jour.

## Options considérées

- **Son propre appel, même matière** (retenue) : un second prompt Opus, à son registre,
  nourri de la même matière plus ce que la lettre veut en plus (la semaine devant, ce
  qui a été fait, la série). Sa propre mémoire, son propre atelier. Deux appels par
  jour, négligeable.
- **Réécrire l'édition du jour** : un seul appel qui produit la une et la lettre.
  Moins cher, mais le prompt du mur se retrouve à servir deux formes ; une retouche
  pour l'une dérègle l'autre, et la lettre attend le créneau du mur.
- **Pas de modèle, gabarit C3 seulement** : zéro LLM, mais exactement ce que la
  maquette refuse comme ambition.

Sur le repli, trois options : envoyer la note C3 tout de suite ; **réessayer une heure
plus tard, sinon la note** (retenue, contre la recommandation « tout de suite ») ; ne
rien envoyer.

## Décision

**Un appel à part, sur la même matière étendue.** `RedactionLettre` a son prompt et son
contrat de sortie (sujet + paragraphes), validé aussi strictement que celui de
l'édition ; `MatiereDeLettre` enveloppe `MatiereDEdition` et y ajoute ce que la lettre
seule utilise. Le modèle est celui de l'édition (`Edition:Modele`), la clé est la même.
**Quand le modèle se tait** à l'heure d'envoi, la lettre n'est pas envoyée : un second
essai a lieu une heure plus tard, et s'il échoue aussi, la **note C3** composée en
gabarit part à sa place. La prose arrive plus souvent, parfois après le lever, et
c'est le compromis choisi.

## Conséquences

- Deux prompts à tenir, deux mémoires, deux ateliers dans `HouseOs.Essais` ; la
  matière conservée sur la lettre permet de rejouer
  ([[D-2026-09-21 Matière Conservée Sur L'Édition]], même principe).
- Le journal mural n'est pas touché : ni son prompt, ni son service.
- Un jour où le modèle se tait deux fois donne une note de quatre lignes à 7 h 30, pas
  de silence.

## Confirmation

`RedactionLettre.PromptParDefaut` existe et ne partage aucune constante avec
`RedactionLlm.PromptParDefaut` ; le test de `LettreService` prouve le second essai à
une heure puis la note en gabarit (fictif à compteur : deux appels, un envoi).
