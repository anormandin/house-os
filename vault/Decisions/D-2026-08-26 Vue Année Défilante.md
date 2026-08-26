---
type: decision
status: accepted
date: 2026-08-26
feature: "[[Tâches]]"
tags: [ux]
---

# Vue Année défilante — ruban, mini-carte et accordéon

## Contexte

La vue Année de [[D-2026-08-26 Page Tâches Rythmes Et Année]] (12 mois pleine
largeur, ~3 px/jour) n'a pas survécu aux vraies données : étiquettes de jalons
superposées (3 jalons en 6 semaines), ~57 ponctuelles de déménagement écrasées en
une bouillie de pastilles, rangées maigres aux titres tronqués alors que la page
restait aux 3/4 vide. Alain : « detail the tasks, we have a lot of space
vertically ; keep 3-4 months in the view and make it scrollable ». Ronde 3 de
maquettes (artifact « Année Tâches », options A-D).

## Options considérées

- **A — Ruban défilant** : fenêtre ~3 mois, scroll horizontal libre, mini-carte.
- **B — Pages de saison** : pagination par trimestre, zéro scroll ; refusionne
  les grappes voisines.
- **C — Ponctuelles déployées** : ruban + bande semaine-par-semaine permanente.
- **D — Synthèse** : ruban + mini-carte + bande en accordéon.

## Décision

Option **D**, recommandée par le designer et choisie par Alain (« go with D ») :

- **Ruban défilant** à ~11 px/jour, domaine du 1ᵉʳ du mois courant au 31 déc
  (pas de retour dans le passé), aujourd'hui ancré à ~20 % au montage, colonne
  d'étiquettes sticky (titre complet, chips, lieu, avatar), voile dégradé à
  droite. **Mini-carte** de l'année au-dessus : fenêtre visible synchronisée au
  défilement, tics des jalons, note « janv → X : rien à afficher », clic =
  téléportation.
- **Jalons sur deux voies** d'étiquettes (jamais de chevauchement), lignes
  pointillées pleine hauteur ; étiquette bornée au bord droit du ruban.
- **Grappes de ponctuelles par jour exact** (plus de fusion) avec date sous la
  pastille ; une voisine à < 2,5 jours descend sur une voie basse. Grappe d'une
  tâche → éditeur ; grappe multiple → déplie la bande et s'y rend.
- **Bande accordéon « Les ponctuelles »** sous le ruban : bloc « En retard »
  (rouge) + 3 prochaines semaines dépliées (4 tâches max par semaine puis
  « + n autres… »), semaines suivantes en pilules-compteurs, « tout déplier » /
  « Replier ». La bande disparaît avec les ponctuelles.
- **Longs intervalles (> 15 jours) sur la chronologie** (pastille à l'échéance,
  note « ensuite ≈ … » quand la suivante déborde l'année) ; le tempo court ne
  garde que les cadences ≤ 15 jours sans fenêtre. L'échéance réelle d'une
  annuelle (glissée par rollover/fenêtre) prime sur sa date théorique.

Aucun changement d'API — tout est côté client sur `GET /api/taches`.

## Conséquences

- `web/src/lib/taches-vues.ts` : `construireAnnee` (positions en % d'année)
  remplacée par `construireRuban` (offsets en jours, semaines, retards).
- `web/src/pages/Taches.tsx` : `VueRuban` (scroll, mini-carte, accordéon).
- B et C restent documentées dans l'artifact si le besoin évolue.

## Confirmation

`grep -l "construireRuban" web/src/lib/taches-vues.ts` retourne le fichier ;
`grep -l "téléporter\|teleporter" web/src/pages/Taches.tsx` retourne le fichier.
