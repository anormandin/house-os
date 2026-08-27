---
type: decision
status: accepted
date: 2026-08-26
feature: "[[Budget]]"
tags: []
---

# Cibles D'enveloppe Dérivées Des Tâches

## Contexte

Une enveloppe d'équipement (« repeindre la toiture métallique ») a une cible en
dollars et une échéance. Fork : qui possède cette cible — l'enveloppe la porte
en dur, ou elle est dérivée de la tâche récurrente / de l'équipement que House OS
connaît déjà ? L'exemple fondateur d'Alain (toiture à repeindre, cycle ~10 ans)
est précisément une tâche récurrente avec un coût.

## Options considérées

- **Cible saisie sur l'enveloppe, sans lien** — simple, mais duplique une
  information que le moteur de récurrence possède déjà (l'échéance) et se
  désynchronise dès que la tâche bouge.
- **Cible portée par la tâche/l'équipement** — inverse le couplage : le moteur
  de tâches devrait connaître l'argent ; alourdit des entités stables.
- **Enveloppe optionnellement liée à une tâche (ou un équipement) ; montant
  cible sur l'enveloppe, échéance dérivée du lien** — l'enveloppe reste le seul
  objet « argent » ; la date vient de la prochaine occurrence de la tâche liée ;
  sans lien, cible et date sont saisies librement (projets).

## Décision

**L'enveloppe porte le montant cible ; l'échéance est dérivée de la tâche liée
quand un lien existe** (proposé par l'agent, design délégué par Alain,
2026-08-26). La provision mensuelle est un calcul pur, à la lecture :
(cible − solde de l'enveloppe) ÷ mois restants avant l'échéance ; pour une
enveloppe `Taxes`, la provision est lissée sur l'échéancier de versements.
Aucune provision n'est stockée.

## Conséquences

- Compléter ou repousser la tâche liée recalcule la provision sans écriture.
- Le calcul vit dans une classe domaine pure (`MoteurProvision`) testée
  unitairement, comme le moteur de récurrence.
- Différenciateur assumé : aucun outil du marché ne relie récurrence et épargne.

## Confirmation

- `grep -r "MoteurProvision" server/HouseOs.Api/Domaine/` retourne la classe.
- Des tests unitaires `MoteurProvision*` couvrent : tâche liée, taxes lissées,
  enveloppe libre, cible atteinte (provision 0).
- La migration `AjouterBudget` ne contient aucune colonne de provision stockée.
