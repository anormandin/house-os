---
type: decision
status: accepted
date: 2026-08-26
feature: "[[Budget]]"
tags: []
---

# Ventilation Du Dépôt Multi-Enveloppes

## Contexte

La spec initiale disait « lier une transaction crée **le** mouvement d'enveloppe
correspondant » — un mouvement, une enveloppe. Or le cas central du module est un
dépôt mensuel unique (le virement) qui doit nourrir plusieurs enveloppes à la
fois (310 $ taxes + 185 $ bureau + …). Tel qu'écrit, le contrat ne permettait
pas de ventiler un dépôt. Fork levé au grilling pré-implémentation.

## Options considérées

- **Répartition multi-enveloppes à la liaison** — lier un dépôt ouvre une
  ventilation pré-remplie avec les provisions suggérées du `MoteurProvision` ;
  on ajuste, on confirme → N mouvements `Provision` liés à la même transaction.
- **Dépôt → Non affecté, bouton séparé « Provisionner le mois »** — plus
  simple, mais casse le fil banque → enveloppe (mouvements orphelins de la
  transaction).
- **Mono-enveloppe strict (spec à la lettre)** — ventiler exigerait des
  transferts manuels ensuite ; friction mensuelle réelle.

## Décision

**Lier un dépôt entrant ouvre une ventilation multi-enveloppes, pré-remplie par
les provisions suggérées du `MoteurProvision` ; la confirmation crée N
mouvements `Provision` pointant la même `TransactionBancaire` ; la part non
ventilée reste en Non affecté (aucun mouvement). Un retrait (dépense) reste
mono-enveloppe** (choix d'Alain au grilling, 2026-08-26).

## Conséquences

- L'endpoint de liaison accepte une liste {enveloppeId, montant} pour un dépôt
  (somme ≤ montant de la transaction) et une seule enveloppe pour un retrait.
- Le journal de mouvements ([[D-2026-08-26 Mouvements D'enveloppe En Journal]])
  reste inchangé : la ventilation n'est que plusieurs mouvements partageant un
  lien de transaction.
- Le MCP `gerer_budget` expose la même ventilation (parité).

## Confirmation

- Un test d'intégration lie un dépôt à ≥ 2 enveloppes et vérifie N mouvements
  pointant la même transaction + l'invariant.
- `grep -ri "ventilation" server/HouseOs.Api/Features/Budget/` retourne le
  contrat de liaison.
