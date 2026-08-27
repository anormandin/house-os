---
type: decision
status: accepted
date: 2026-08-26
feature: "[[Budget]]"
tags: []
---

# Compte Unique Et Enveloppes Virtuelles

## Contexte

Alain veut un fonds de prévoyance pour la maison : mettre de l'argent de côté
chaque mois, y puiser pour les taxes et pour les équipements à réparer ou
remplacer (ex. repeindre la toiture métallique), et financer des projets
(rénover une pièce). Fork : plusieurs vrais comptes bancaires (un par but), ou
un seul compte réel partitionné logiquement dans l'app ?

## Options considérées

- **Un compte réel par but** — lisible à la banque, mais multiplie les comptes,
  les virements et les frais ; la banque devient l'outil de gestion, pas House OS.
- **Un seul compte réel + enveloppes virtuelles dans House OS** — le patron
  éprouvé d'Actual/YNAB : un compte « fonds de prévoyance », des enveloppes
  logiques (équipement, taxes, projet, réserve) qui le partitionnent.
- **Pas de compte dédié, tout virtuel sur le compte courant** — rejeté : le but
  d'Alain est justement de séparer physiquement l'argent de la maison.

## Décision

**Un seul compte bancaire réel dédié au fonds de prévoyance, partitionné en
enveloppes virtuelles dans House OS** (choix d'Alain, 2026-08-26 ; modèle
enveloppes proposé par l'agent et intégré au design délégué). Types
d'enveloppe : `Equipement` (gros entretien/remplacement), `Taxes` (échéancier
connu), `Projet` (cible libre), `Reserve` (imprévus). Invariant de
rapprochement : solde du compte = Σ soldes d'enveloppes + « Non affecté » ;
un Non affecté négatif (sur-allocation) déclenche un avertissement visible.

## Conséquences

- V1 modélise un seul compte (`CompteBudget`), mais rien dans le schéma
  n'empêche d'en ajouter plus tard.
- Le solde courant du compte est dérivé (solde initial ancré + transactions
  importées), jamais saisi à la main après l'ancrage.
- L'invariant devient un test de régression et un élément d'UI permanent.

## Confirmation

- `grep -r "CompteBudget" server/HouseOs.Api/Domaine/` retourne l'entité.
- Un test nommé `*NonAffecte*` ou `*Invariant*` existe dans
  `server/HouseOs.Tests/` et vérifie solde = Σ enveloppes + non affecté.
