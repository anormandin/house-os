---
type: decision
status: accepted
date: 2026-08-26
feature: "[[Tâches]]"
tags: [ux]
---

# Page Tâches — vue Rythmes avec bascule Année

## Contexte

La page Tâches était une liste plate d'occurrences (À faire / Complétées) qui
dupliquait le travail d'Aujourd'hui — la todo list propre — sans offrir de vue de
**gestion** des définitions (rythme, pièce, stratégie, prochaine échéance). Même
défaut d'échelle que Documents avant sa refonte à facettes
([[D-2026-08-26 Navigation Documents Par Facettes]]). Deux rondes de maquettes
(artifacts « Maquettes Tâches », 5 directions, puis « Itérations Tâches », 4
mécaniques de bascule).

## Options considérées

Ronde 1 : console à facettes (écho Documents) · **rythmes de la maison** ·
pièce par pièce · vue scindée · **année de la maison**. Alain a retenu rythmes,
pièces et année. Ronde 2 (mécanique de bascule) : **A** Rythmes ⇄ Année (binaire) ·
B Pièces ⇄ Année · C triple commutateur Rythmes · Pièces · Année · D année en
bandeau repliable sans mode.

## Décision

Itération **A**, recommandée par le designer et choisie par Alain le 2026-08-26
(« go with A, build it ») :

- La page Tâches devient une **console des définitions de tâches**, groupées par
  rythme : Chaque semaine · Aux quelques jours · Chaque mois · Chaque année & au
  fil des saisons · Ponctuelles (avec barre de progression X faites sur Y).
- Un **commutateur segmenté binaire « Liste | Année »** à droite du titre bascule
  vers la chronologie 12 mois : points mensuels/annuels, barres de fenêtres
  saisonnières (en cours / à venir), grappes de ponctuelles, ligne Aujourd'hui,
  jalons tirés des [[Comptes À Rebours|comptes à rebours]] ; les rythmes trop
  fréquents pour l'échelle (hebdo, intervalles sans fenêtre) vont dans un bandeau
  « Le tempo court ». Tout clic ouvre l'éditeur.
- Le **dernier mode choisi est mémorisé** (localStorage) — l'idée retenue de D.
- L'axe Pièces est écarté ici (B et C) : il duplique la page Pièces, exactement le
  défaut corrigé entre Tâches et Aujourd'hui ; son avenir est d'enrichir Pièces.
- Le filtre « Complétées » disparaît de la page (les complétées du jour restent
  sur Aujourd'hui ; l'historique complet attend un éventuel module journal).
- Nouveau contrat : **`GET /api/taches`** liste les définitions (récurrence,
  zone, équipement, stratégie, échéance et assigné de l'occurrence en attente,
  nombre de documents, flag complétée pour les ponctuelles) + parité MCP
  **`lister_taches`**.

## Conséquences

- `web/src/pages/Taches.tsx` réécrite (deux vues + commutateur) ; la logique de
  groupement et la géométrie de l'année vivent en fonctions pures testées
  (`web/src/lib/taches-vues.ts`), même patron que le ruban.
- L'API occurrences ne change pas ; Aujourd'hui et Pièces ne changent pas.
- Les vues Pièces-dans-Tâches et le bandeau intégré (D) restent des évolutions
  possibles, non retenues.

## Confirmation

`grep -l "lister_taches" server/HouseOs.Api/Features/Mcp/OutilsTaches.cs` retourne
le fichier ; `grep -l "Année" web/src/pages/Taches.tsx` retourne le fichier ; la
page ne rend plus le filtre « Complétées ».
