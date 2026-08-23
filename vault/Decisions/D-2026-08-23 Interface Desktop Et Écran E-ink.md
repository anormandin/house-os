---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: []
---

# Interface Desktop Et Écran E-ink

## Contexte

La V0 a été construite avec une hypothèse mobile-first (PWA téléphone). L'utilisateur
a corrigé : l'interface principale est le **navigateur desktop** ; le téléphone ne
sera probablement jamais une cible ; un **écran e-ink mural** sera une seconde vue
distincte (probablement rendue côté serveur), pas une variante responsive.

## Options considérées

- **Desktop d'abord, e-ink comme vue distincte** — layout multi-colonnes, densité
  d'écran large, navigation latérale ; l'e-ink a son propre rendu adapté (contraste,
  gros caractères, rafraîchissement lent).
- **Mobile-first responsive** (hypothèse V0) — thumb-zone, nav du bas ; gaspille
  l'espace desktop et ne sert personne ici.
- **Un seul rendu responsive pour tout** — l'e-ink a des contraintes (1-bit,
  rafraîchissement, distance de lecture) incompatibles avec un simple breakpoint.

## Décision

Desktop d'abord ; téléphone non prioritaire (probablement jamais) ; l'écran e-ink
est une **seconde vue distincte** avec son propre design. Le stack frontend
([[D-2026-08-23 Frontend Vite React PWA]]) reste inchangé — seul le facteur de
forme visé change. Choix de l'utilisateur.

## Conséquences

- Le layout V0 (max-w-lg, nav du bas) doit être repensé pour le desktop — mockups
  d'abord, refonte ensuite.
- Le plugin PWA devient accessoire (inoffensif, on le garde pour l'install desktop).
- La vue e-ink sera probablement un rendu serveur (PNG/HTML statique) poussé vers
  l'écran — à concevoir avec la phase 3.
- Les notifications iCal ([[D-2026-08-23 Notifications Par Flux iCal]]) restent
  valables (calendriers consultés sur n'importe quel appareil).

## Confirmation

Le layout principal de `web/src/components/Layout.tsx` cible le desktop (navigation
latérale ou en-tête, pas de barre d'onglets mobile fixée en bas).
