---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: []
---

# Monorepo

## Contexte

Le projet contiendra à terme : serveur .NET, PWA React, firmwares ESP32, modèles 3D
(Fusion 360), PCBs (KiCad), specs. Un ou plusieurs dépôts git ?

## Options considérées

- **Un monorepo** — commits atomiques API+UI, un seul contexte Claude, un seul vault ;
  binaires CAD/firmware acceptables à cette échelle (LFS au besoin).
- **Dossier parent, un repo par partie** — séparation propre mais docs partagées et
  changements transversaux pénibles.
- **Deux repos logiciel/matériel** — compromis, historique logiciel sans binaires.

## Décision

Un seul monorepo (`house-os/`) : `server/`, `web/`, `firmware/`, `hardware/`,
`vault/`, `docs/`. Choix de l'utilisateur.

## Conséquences

- Toute la connaissance projet (vault, CLAUDE.md, recherches) vit avec le code.
- Si les binaires CAD grossissent, activer Git LFS plutôt que de scinder.

## Confirmation

Un seul `.git` à la racine ; `server/`, `web/`, `firmware/`, `hardware/` sont des
dossiers du même dépôt.
