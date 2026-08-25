---
type: feature
status: implemented
last-verified: 2026-08-25
verified-against: e546ee2
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
  (repli client de [[Titre D'humeur]]), cartes Cette semaine / Bilan /
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

Implémenté (cycle de vie des occurrences, 2026-08-24, as-built —
[[D-2026-08-24 Annulation Et Passage D'occurrences]]) :

- **Annuler une complétion** (bouton « Annuler » au survol de la rangée verte) :
  journal effacé, occurrence remise en attente avec son échéance d'origine (elle
  redevient éligible au rollover), occurrence suivante matérialisée supprimée.
  Garde-fou : seulement la complétion la plus récente de la tâche, et si la suivante
  est encore en attente (sinon 409).
- **Passer** (récurrentes seulement, icône au survol) : statut `Passee` daté, aucun
  journal, la suivante est générée comme après une complétion aujourd'hui (même
  stratégie d'assignation). Un passage n'est pas annulable.
- **Reporter** (icône au survol) : glisse l'échéance de l'occurrence en attente
  (préréglages demain / +2 j / +7 j, ou date libre ≥ aujourd'hui) sans toucher la
  définition.
- **Notes post-hoc** : la rangée verte permet d'ajouter/modifier la note de
  l'entrée de journal (la complétion reste à un clic) ; la note est retournée dans
  le DTO d'occurrence.
- **Confirmation avant suppression** partout (composant partagé « Vraiment ? »,
  extrait de l'idiome Pièces/Équipements) ; le bouton poubelle d'une rangée supprime
  toujours la tâche entière.
- **Erreurs API visibles** : messages ProblemDetails parsés côté client et affichés
  dans une bannière globale (toutes les mutations, 401 exclus).

Implémenté (bilan hebdo du ménage, 2026-08-25, as-built —
[[D-2026-08-25 Bilan Hebdo Du Ménage]]) :

- La carte « L'équipe » d'Aujourd'hui est remplacée par **« Bilan »** : total
  complété cette semaine + mini-histogramme des 8 dernières semaines (lundi au
  dimanche, semaine locale). Le total est celui du **ménage entier** — l'attribution
  individuelle (qui a cliqué) est indicative, jamais un fondement de feature.
- `GET /api/journal/bilan?de=&a=` retourne les instants de complétion dans `[de, a)` ;
  le client agrège par semaine locale (même patron que le filtre `faites`).
- Parité MCP : outil `bilan_taches` (comptes par semaine, heure du serveur).

Implémenté (éditeur de tâche, 2026-08-25, as-built) :

- `GET /api/taches/{id}` (et l'outil MCP `gerer_tache`, action `obtenir`) retourne
  maintenant **`echeance`** : celle de l'occurrence en attente. Corrige le bug où
  l'éditeur, n'affichant pas l'échéance existante, l'effaçait silencieusement à
  l'enregistrement d'une tâche ponctuelle.
- Éditeur : la description est un `textarea` de 4 lignes ; le quick-add (⌘K, pages
  Aujourd'hui et Tâches) et le panneau d'une pièce ouvrent le **modal complet**
  (zone préremplie depuis la pièce) ; Échap ferme le modal.
- Listes : la description s'affiche en doré, retours à la ligne préservés, y compris
  sur les tâches en retard (sous la ligne « depuis X jours »).

## Hors périmètre

- Points, récompenses, features famille/enfants — jamais (pas d'enfants).
- Sous-tâches et projets multi-étapes (module Projets, v2+).
- Notifications push (v1 = flux iCal seulement).
- **Documents liés à une tâche** (« fermer le spa → guide ») — besoin exprimé le
  2026-08-24, reporté au module Documents de la phase 2 ([[Architecture]]) ; prévoir
  l'attache de un ou plusieurs documents/guides à une tâche.

## Décisions

- [[D-2026-08-23 Moteur De Récurrence Trois Modes]] — les 3 modes, matérialisation,
  journal séparé, stratégies d'assignation.
- [[D-2026-08-23 Notifications Par Flux iCal]] — canal de rappel v1.
- [[D-2026-08-23 Flux iCal Par Personne]] — structure des flux (jeton par compte).
- [[D-2026-08-23 Zones Plates]] — zones = liste plate CRUD.
- [[D-2026-08-23 Auth Simple Deux Comptes]] — attribution des complétions.
- [[D-2026-08-24 Annulation Et Passage D'occurrences]] — annuler/passer/reporter,
  sort du journal, garde-fous.
- [[D-2026-08-25 Bilan Hebdo Du Ménage]] — carte Bilan (total par semaine) à la
  place de L'équipe ; attribution individuelle conservée mais indicative.

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
- `web/src/components/OccurrenceListe.tsx` — rangées de tâches (retard, complétée,
  échéance, annuler/passer/reporter/notes)
- `web/src/components/ConfirmerSuppression.tsx` — suppression en deux temps « Vraiment ? »
- `web/src/lib/erreurs.ts`, `web/src/components/BanniereErreur.tsx` — erreurs globales
- `server/HouseOs.Tests/Features/Taches/` — tests d'orchestration (harnais Sqlite in-memory)
- `web/src/index.css` — tokens du design chaleureuse (source : maquettes)
- `web/src/lib/format.ts` — typographie québécoise (dates, « 14 h 10 », dodos)

## Sources

- `docs/research/2026-08-23-logiciels-gestion-maison.md` — patterns Grocy/Donetick.

## Historique

- [[Plan 2026-08-23 V0 Déménagement]] · [[Recap Tâches]]
- [[Plan 2026-08-23 V1 Emménagement]] · [[Recap V1 Emménagement]]
- [[Plan 2026-08-24 Cycle De Vie Des Occurrences]] · [[Recap Cycle De Vie Des Occurrences]]
- 2026-08-25 Bilan hebdo du ménage — tranche directe sans plan, voir
  [[D-2026-08-25 Bilan Hebdo Du Ménage]] et la section as-built ci-dessus.
