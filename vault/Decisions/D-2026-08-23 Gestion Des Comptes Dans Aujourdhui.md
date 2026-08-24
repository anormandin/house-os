---
type: decision
status: accepted
date: 2026-08-23
feature: "[[Comptes À Rebours]]"
tags: []
---

# Gestion Des Comptes Dans Aujourdhui

## Contexte

Où vit le CRUD des comptes à rebours? L'entité est légère (titre, date, icône) et le
foyer en aura une poignée à la fois.

## Options considérées

- **Dans la carte d'Aujourd'hui** — un bouton discret dans l'en-tête de la carte
  ouvre un modal de gestion ; pas de page dédiée.
- **Page dédiée** — entrée de navigation propre ; lourd pour 2-5 items.
- **Via le Quick-add des tâches** — mélange deux concepts distincts.

## Décision

Gestion dans la carte d'Aujourd'hui. Un bouton « + » dans l'en-tête de la carte
« Comptes à rebours » ouvre un modal de gestion unique : formulaire d'ajout (titre,
date cible, sélecteur d'icônes) et liste de tous les comptes — y compris les passés,
marqués « passé » — avec modification et suppression. La carte elle-même reste un
affichage pur. Elle est toujours visible (avec un état vide) puisqu'elle est le seul
point d'entrée de la gestion. Choix de l'utilisateur.

## Conséquences

- Pas d'entrée de navigation ni de route ; un seul composant modal
  (`ComptesAReboursGestion` ou équivalent) porté par la carte.
- L'état vide de la carte remplace le masquage complet de l'intérim V0 (on
  n'affiche pas de contrôle mort, mais un « + » actif n'est pas mort).

## Confirmation

`web/src/pages/Aujourdhui.tsx` : la carte Comptes à rebours porte le bouton « + » et
le modal ; aucune route `/comptes-a-rebours` dans `web/src/App.tsx`.
