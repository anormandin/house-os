---
type: decision
status: accepted
date: 2026-08-23
feature: "[[Comptes À Rebours]]"
tags: []
---

# Célébration Puis Masquage Des Comptes

## Contexte

La spec [[Comptes À Rebours]] laissait ouvert ce qui arrive quand la date cible est
passée (« s'archive ou se célèbre — à préciser »). L'intérim V0 masquait le compte
dès que dodos ≤ 0, ratant le jour J.

## Options considérées

- **Célébrer puis masquer** — le jour J, la carte affiche « C'est aujourd'hui ! » ;
  dès le lendemain le compte disparaît de la carte, conservé en base.
- **Masquer dès le jour J** — comportement de l'intérim ; simple mais rate le moment.
- **Rester affiché jusqu'à suppression manuelle** — du bruit sur Aujourd'hui.

## Décision

Célébrer puis masquer. Le jour J (dodos = 0), la ligne affiche « C'est
aujourd'hui ! » au lieu du compte de dodos. À partir du lendemain (dodos < 0), la
ligne est masquée de la carte mais la donnée reste en base ; elle demeure visible et
supprimable dans le modal de gestion (marquée « passé »). Aucune suppression ni
archivage automatique. Choix de l'utilisateur.

## Conséquences

- Le filtrage est purement côté client (dodos ≥ 0 pour la carte) — pas d'état
  « archivé » en base, pas de job serveur.
- Les comptes passés s'accumulent en base jusqu'à suppression manuelle dans la
  gestion ; acceptable au volume domestique (quelques-uns par année).

## Confirmation

`web/src/pages/Aujourdhui.tsx` : la carte filtre sur dodos ≥ 0 et affiche le libellé
de célébration à dodos = 0 ; l'API `GET /api/comptes-a-rebours` retourne aussi les
comptes passés.
