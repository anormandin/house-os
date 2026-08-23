---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: []
---

# Frontend Vite React PWA

## Contexte

Première interface = web mobile-friendly (PWA installable sur les téléphones, pas
d'app store). Le stack travail est MUI + RTK Query ; l'occasion d'apprendre autre
chose se présentait.

## Options considérées

- **Vite + TS + TanStack Query + Tailwind + shadcn/ui** — SPA statique, pas de second
  runtime serveur, ergonomie mobile moderne, apprentissage réel.
- **Next.js auto-hébergé** — faisable en Docker (`output: 'standalone'`), mais ajoute
  un runtime Node pour un SSR inutile derrière Tailscale.
- **Vite + MUI + RTK Query** — stack travail transplanté, vélocité maximale, zéro
  apprentissage.

## Décision

Vite + TypeScript + TanStack Query + Tailwind + shadcn/ui, PWA via vite-plugin-pwa.
UI 100 % français, chaînes en dur (pas de lib i18n). Choix de l'utilisateur.

## Conséquences

- Un seul runtime serveur (.NET) ; le build web est servi statiquement.
- Courbe d'apprentissage Tailwind/shadcn/TanStack assumée.
- Si un besoin d'anglais apparaît un jour, l'extraction des chaînes sera un chantier.

## Confirmation

`web/package.json` contient `@tanstack/react-query`, `tailwindcss`,
`vite-plugin-pwa` ; absence de `next` et de `@mui/material`.
