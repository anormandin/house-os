---
type: plan
status: executed
date: 2026-08-25
feature: "[[Tâches]]"
tags: []
---

# Plan 2026-08-25 Ruban Des 7 Prochains Jours

Mettre en œuvre [[D-2026-08-25 Ruban Des 7 Prochains Jours]] — maquettes de
référence : artifact « Pièces à venir » (itération ruban, 4 planches dont
l'anatomie annotée). Frontend seulement, aucun changement d'API.

## Étapes

- [x] `web/src/lib/ruban.ts` — logique pure et testable : `couleurZone(zones, zoneId)`
  (palette fixe de 6 teintes, déterministe par ordre) et
  `regrouperRuban(occurrences, evenements, aujourdhui)` → { retard, jours[7]
  (aujourd'hui + 6), plusTard (jours 7 à 30, trié, avec total) }.
- [x] Tests Vitest de `ruban.ts` : retard exclu des jours, aujourd'hui séparé,
  bornes 7/30 jours, tâches sans échéance ignorées, événements externes intercalés
  dans leurs jours, tri de « Plus tard ».
- [x] `web/src/components/Ruban.tsx` — le composant : colonnes retard / aujourd'hui
  surligné / 6 jours (week-end teinté, vide = « · ») / Plus tard (6 lignes max puis
  « n de plus ») ; puces couleur-pièce + avatar, événements externes italique doré
  + icône non cliquables ; max 3 puces par jour puis « + n autres » dépliable sur
  place ; légende des pièces ; prop `focusZoneId` (estompage à 40 %) ; prop
  `onOuvrirTache` ; en-tête avec bouton de gestion des flux externes optionnel.
- [x] `web/src/pages/Aujourdhui.tsx` — ruban pleine largeur sous le héros ;
  retirer `CetteSemaine` ; le bouton CalendarPlus (gestion des flux) migre dans
  l'en-tête du ruban.
- [x] `web/src/pages/Pieces.tsx` — ruban au-dessus de la grille, `focusZoneId` =
  pièce choisie, clic de puce → éditeur ; ajouter la requête
  `evenements-externes`.
- [x] Vérifier : `npm test` (web), lint/tsc, puis coup d'œil dans l'app lancée.
- [x] Vault : spec [[Tâches]] à jour (section as-built), Recap, statuts du plan.

## Vérification

- Les tests Vitest du groupement passent ; `tsc` sans erreur.
- Aujourd'hui : le ruban remplace « Cette semaine », les collectes y figurent à
  leur jour, la latérale garde Météo/Bilan/Comptes à rebours.
- Pièces : choisir une pièce estompe le reste du ruban ; cliquer une puce ouvre
  l'éditeur de la bonne tâche.
