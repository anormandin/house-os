---
type: decision
status: accepted
date: 2026-08-23
feature: "[[Comptes À Rebours]]"
tags: []
---

# Icônes Maison Comptes À Rebours

## Contexte

Chaque compte à rebours porte une icône. L'intérim V0 utilisait deux illustrations
SVG au trait (camion, sapin) dans la direction artistique chaleureuse
([[D-2026-08-23 Direction Artistique Cuisine Chaleureuse]]). Comment généraliser le
choix d'icône quand l'utilisateur crée un compte quelconque?

## Options considérées

- **Emoji libre** (recommandation de l'agent) — champ emoji optionnel, zéro dessin à
  maintenir, mais rompt avec le style au trait des illustrations maison.
- **Petit set d'icônes maison** — ~8 SVG dessinés dans le style existant, choisis
  dans un sélecteur ; cohérent avec la DA, mais chaque nouvelle icône demande du dessin.
- **Pas d'icône** — titre + pastille de couleur ; perd le charme de la carte.

## Décision

Petit set d'icônes maison. Huit SVG au trait (trait 2.5, couleurs de la palette du
thème) : camion, sapin, avion, valise, gâteau, cadeau, cœur, soleil. Chaque icône
porte sa couleur d'accent intrinsèque — l'utilisateur choisit une icône, jamais une
couleur séparément. Le choix se fait dans un sélecteur visuel du formulaire. Choix
de l'utilisateur, contre la recommandation emoji.

## Conséquences

- L'icône est un enum (`IconeCompteARebours`) stocké en texte, pas un champ libre —
  ajouter une icône = dessiner un SVG + ajouter une valeur d'enum (nouvelle
  décision non requise).
- Les illustrations Camion et Sapin existantes sont intégrées au set.

## Confirmation

Enum `IconeCompteARebours` dans le Domaine (8 valeurs à l'origine — le set peut
grandir sans nouvelle décision) ; composants SVG au trait correspondant à chaque
valeur dans `web/src/components/Illustrations.tsx` ; sélecteur d'icônes dans
le modal de gestion.
