---
type: feature
status: implemented
last-verified: 2026-08-23
verified-against: 7b407d4
tags: []
---

# Tâches

## Intention

Le cœur de House OS : gérer les tâches de la maison — récurrentes (hebdo, mensuelles,
annuelles, saisonnières) et ponctuelles, intérieures et extérieures — pour deux
adultes, avec attribution équitable et historique. Première utilisation réelle : les
tâches du déménagement du 2026-10-06 (V0).

## Comportement

Implémenté (V0, as-built) :

- Quand un utilisateur crée une tâche ponctuelle (titre, échéance?, assigné?), une
  occurrence unique est créée ; elle apparaît dans Tâches et, le jour venu ou en
  retard, dans Aujourd'hui.
- Quand un utilisateur complète une occurrence, le système journalise qui/quand dans
  le journal (table séparée) ; une occurrence déjà complétée est refusée (409) ; une
  ponctuelle ne génère pas d'occurrence suivante.
- Quand une tâche est supprimée, ses occurrences partent en cascade mais le journal
  survit (ids historiques, pas de FK).
- Toute l'API exige la session cookie (2 comptes) ; 401 sinon.
- La vue Aujourd'hui montre aussi les occurrences **complétées aujourd'hui** (rangée
  verte « bravo X ✓ 14 h 10 ») via le filtre API `faites` : le client fournit les
  bornes d'instants de sa journée locale (`de`/`a`), le serveur ne connaît pas le
  fuseau du client.
- L'UI applique le design final [[D-2026-08-23 Direction Artistique Cuisine Chaleureuse]] :
  desktop d'abord (en-tête Maison + onglets), héros illustré avec titre d'humeur
  (repli client de [[Titre D'humeur]]), cartes Cette semaine / L'équipe /
  [[Comptes À Rebours|Comptes à rebours]], quick-add repliable (⌘K).

Implémenté (V1 « Emménagement », as-built) :

- **Trois modes de récurrence** ([[D-2026-08-23 Moteur De Récurrence Trois Modes]]),
  stockés type + paramètres dans les colonnes de `Taches` (objet possédé
  `SpecRecurrence`) : Ponctuelle ; Fixe (jours de semaine, jour du mois clampé en
  fin de mois court, annuelle) ; Intervalle (N jours depuis la **complétion**).
- **Fenêtre saisonnière** (mois-jour à mois-jour, peut chevaucher l'an) combinable
  avec les deux modes : une échéance calculée hors fenêtre glisse au premier jour
  valide de la prochaine fenêtre.
- **Sémantique de complétion** : la prochaine occurrence est matérialisée à la
  complétion ; pour une fixe, à partir de max(complétion, échéance courante) — une
  complétion en avance ne double pas l'horaire ; une seule occurrence en attente
  par tâche, jamais d'empilement.
- **Rollover** (défaut activé, tâches fixes seulement) : un `BackgroundService`
  quotidien glisse les occurrences fixes manquées à leur prochaine date planifiée.
  Les intervalles ne glissent pas (« tondre » reste dû tant que ce n'est pas fait).
- **Stratégies d'assignation** appliquées à la matérialisation (l'assigné vit sur
  l'**occurrence**) : fixe ; alternance (l'autre que le dernier compléteur) ;
  moins-l'a-fait (journal 90 jours, égalité → alternance).
- Les tâches référencent une [[Glossaire#Zone|zone]] ([[D-2026-08-23 Zones Plates]])
  et un [[Équipements|équipement]] ; l'onglet **Pièces** (vue signature) montre la
  fraîcheur par zone (calcul client, libellés doux) avec gestion des zones inline.
- **Édition complète** (PUT) : l'occurrence en attente est réalignée sur la
  nouvelle définition (échéance recalculée si non fournie).
- **Flux iCal par personne** ([[D-2026-08-23 Flux iCal Par Personne]]) : jeton
  secret, occurrences assignées + non-assignées, URL copiable dans « Mon calendrier ».

## Hors périmètre

- Points, récompenses, features famille/enfants — jamais (pas d'enfants).
- Sous-tâches et projets multi-étapes (module Projets, v2+).
- Notifications push (v1 = flux iCal seulement).

## Décisions

- [[D-2026-08-23 Moteur De Récurrence Trois Modes]] — les 3 modes, matérialisation,
  journal séparé, stratégies d'assignation.
- [[D-2026-08-23 Notifications Par Flux iCal]] — canal de rappel v1.
- [[D-2026-08-23 Flux iCal Par Personne]] — structure des flux (jeton par compte).
- [[D-2026-08-23 Zones Plates]] — zones = liste plate CRUD.
- [[D-2026-08-23 Auth Simple Deux Comptes]] — attribution des complétions.

## Ancres de code

- `server/HouseOs.Api/Domaine/SpecRecurrence.cs` — spec type + paramètres, fenêtre
- `server/HouseOs.Api/Domaine/MoteurRecurrence.cs` — moteur pur (le cœur, le plus testé)
- `server/HouseOs.Api/Domaine/Assignation.cs` — stratégies d'assignation
- `server/HouseOs.Api/Domaine/Tache.cs` — créations ponctuelle/récurrente
- `server/HouseOs.Api/Domaine/Occurrence.cs` — complétion + entrée de journal
- `server/HouseOs.Api/Features/Taches/TachesEndpoints.cs` — API tâches/occurrences
- `server/HouseOs.Api/Features/Taches/RolloverService.cs` — glissement quotidien
- `server/HouseOs.Api/Features/FluxIcal/FluxIcalEndpoints.cs` — flux iCal
- `server/HouseOs.Tests/Domaine/` — tests du domaine (moteur, stratégies)
- `web/src/components/TacheEditeur.tsx` — éditeur complet (récurrence en français)
- `web/src/pages/Pieces.tsx` — vue Pièces (fraîcheur, gestion des zones)
- `web/src/pages/Aujourdhui.tsx`, `web/src/pages/Taches.tsx`, `web/src/components/QuickAdd.tsx` — UI
- `web/src/components/OccurrenceListe.tsx` — rangées de tâches (retard, complétée, échéance)
- `web/src/index.css` — tokens du design chaleureuse (source : maquettes)
- `web/src/lib/format.ts` — typographie québécoise (dates, « 14 h 10 », dodos)

## Sources

- `docs/research/2026-08-23-logiciels-gestion-maison.md` — patterns Grocy/Donetick.

## Historique

- [[Plan 2026-08-23 V0 Déménagement]] · [[Recap Tâches]]
- [[Plan 2026-08-23 V1 Emménagement]] · [[Recap V1 Emménagement]]
