---
type: decision
status: accepted
date: 2026-08-26
feature: "[[Budget]]"
tags: []
---

# Mouvements D'enveloppe En Journal

## Contexte

Le solde d'une enveloppe doit-il être une colonne mutée à chaque opération, ou
la somme d'un journal de mouvements ? Même fork que « journal de complétion =
table séparée, jamais une date mutée », déjà tranché pour les tâches
([[D-2026-08-23 Moteur De Récurrence Trois Modes]] et invariants d'occurrence).

## Options considérées

- **Colonne `SoldeCourant` mutée** — lectures rapides, mais perd l'historique
  (« pourquoi l'enveloppe toiture est à 2 100 $ ? »), fragile aux courses, et
  contraire à la philosophie du projet.
- **Journal `MouvementEnveloppe` (provision, retrait, ajustement, transfert),
  solde = Σ mouvements** — historique complet, rapprochement bancaire naturel
  (un mouvement peut pointer une transaction), volumétrie domestique triviale.

## Décision

**Journal de mouvements ; le solde d'enveloppe est toujours calculé**
(proposé par l'agent, design délégué par Alain, 2026-08-26). Un mouvement porte
des liens optionnels vers une `TransactionBancaire` et une `EntreeJournal`
(complétion) — la facture d'une réparation relie ainsi banque, enveloppe et
journal de la tâche.

## Conséquences

- Pas de colonne de solde à maintenir ni de course de mise à jour.
- Un transfert entre enveloppes = deux mouvements opposés, même date.
- L'historique alimente les vues « d'où vient / où va l'argent » sans schéma
  supplémentaire.

## Confirmation

- `grep -r "MouvementEnveloppe" server/HouseOs.Api/Domaine/` retourne l'entité.
- `grep -ri "SoldeCourant" server/HouseOs.Api/Domaine/Enveloppe.cs` ne retourne
  rien.
