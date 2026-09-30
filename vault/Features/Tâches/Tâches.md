---
type: feature
status: implemented
last-verified: 2026-09-30
verified-against: 6d1fda0
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
- Une tâche peut être liée à **plusieurs [[Documents|documents]] de référence**
  (jointure `TacheDocuments`, cascade du lien seulement — supprimer une tâche ou
  un document n'efface jamais l'autre). `documentIds` dans le détail, le
  POST/PUT et les outils MCP ; sémantique d'écriture : null = liens conservés,
  `[]` = tout délier, liste = remplacement complet
  ([[D-2026-08-26 Documents Liés Aux Tâches]]). L'éditeur montre la section
  « Documents de référence » : chips cliquables vers le fichier, retrait ×,
  ajout via select.
- L'UI applique le design final [[D-2026-08-23 Direction Artistique Cuisine Chaleureuse]] :
  desktop d'abord (en-tête Maison + onglets), héros illustré avec titre d'humeur
  (repli client de [[Titre D'humeur]]), ruban des 7 prochains jours (a remplacé
  la carte Cette semaine), cartes Bilan /
  [[Comptes À Rebours|Comptes à rebours]], quick-add (bouton + ⌘K) qui ouvre
  l'éditeur complet en modal.

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
- **Rollover** (défaut activé, tâches fixes seulement — le flag est refusé pour
  les autres modes et n'apparaît que sur les fixes dans les DTOs, QA 2026-08-28) :
  un `BackgroundService` quotidien glisse les occurrences fixes manquées à leur
  prochaine date planifiée. Une intervalle reste due tant que ce n'est pas fait
  **pendant sa saison** ; hors fenêtre saisonnière, l'occurrence échue glisse au
  premier jour de la prochaine fenêtre, indépendamment du flag
  ([[D-2026-08-28 Glissement Hors Fenêtre Des Intervalles]]).
- **Stratégies d'assignation** appliquées à la matérialisation (l'assigné vit sur
  l'**occurrence**) : fixe ; alternance (l'autre que le dernier compléteur) ;
  moins-l'a-fait (journal 90 jours, égalité → alternance).
- Les tâches référencent une [[Glossaire#Zone|zone]] ([[D-2026-08-23 Zones Plates]])
  et un [[Équipements|équipement]] ; l'onglet **Pièces** (vue signature) montre la
  fraîcheur par zone (calcul client, libellés doux) avec gestion des zones inline ;
  le panneau d'une pièce liste ses tâches (rangées cliquables → éditeur) et offre
  « Ajouter une tâche dans ‹pièce› » (zone préremplie).
- **Édition complète** (PUT) : pour une récurrente, l'occurrence en attente est
  réalignée sur la nouvelle définition (échéance recalculée si non fournie) ; pour
  une **ponctuelle**, l'échéance envoyée remplace telle quelle celle de l'occurrence
  en attente — l'omettre l'efface (d'où l'obligation pour tout client de la
  recharger avant d'enregistrer, voir la tranche éditeur du 2026-08-25).
- **Flux iCal par personne** ([[D-2026-08-23 Flux iCal Par Personne]]) : jeton
  secret, occurrences assignées + non-assignées, URL copiable dans « Mon calendrier ».
  Depuis le 2026-08-27 : **rotation self-service** du jeton (POST authentifié,
  bouton « Régénérer » avec confirmation — l'ancienne URL meurt immédiatement,
  parité MCP via `regenerer` sur `mon_flux_ical`) et **URL publique** (Funnel)
  affichée à côté de l'interne quand configurée
  ([[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]]).
- **Échéance ferme** (2026-09-21,
  [[D-2026-09-20 Échéance Ferme Explicite Sur La Tâche]]) : la tâche porte un booléen
  `echeanceFerme` (défaut faux), coché à la main dans l'éditeur — « Cette date ne se
  négocie pas (notaire, livraison, date légale) » —, jamais déduit. Il voyage dans le
  POST/PUT, le détail, `TacheResumeDto`, l'`OccurrenceDto` et les outils MCP
  (`creer_taches`, `gerer_tache` — omis en modification = conservé, alors que le PUT
  REST remplace tout : omis = faux). Les tâches n'en
  font rien elles-mêmes : c'est le [[Journal De La Maison]] et la [[Lettre Du Matin]]
  qui ne relèguent jamais une telle date.

Le reste du comportement vit dans deux sous-notes :

- [[Cycle De Vie Des Occurrences]] — annuler une complétion, passer, reporter, noter
  après coup ; courses et cas de bord fermés en base ; bilan hebdo du ménage.
- [[Écrans Des Tâches]] — l'éditeur unique, le ruban des 7 prochains jours, la console
  Rythmes ⇄ Année, la recherche, le programme de la maison.

## Dette connue

Aucune dette ouverte (as of 2026-09-30).

**Fermée le 2026-09-29 — la liste des tâches en une seule requête.**
`OperationsTaches.ListerTachesAsync` (`server/HouseOs.Api/Features/Taches/OperationsTaches.cs`)
charge deux collections (`Tache.Occurrences` filtrée sur l'en-attente, `Tache.Documents`)
dans une même requête, et EF avertissait d'un produit cartésien à chaque appel. Mesuré
sur la copie de prod : le produit est borné à 1 × documents par l'invariant « une seule
occurrence en attente par tâche » ([[D-2026-08-25 Invariants D'occurrence En Base]]) —
la requête unique est la bonne. `AsSingleQuery()` le dit à EF sur les cinq sites (la
liste, le GET et le PUT d'une tâche dans `TachesEndpoints.cs`, `obtenir` et `modifier`
de `gerer_tache` dans `Mcp/OutilsTaches.cs`). Ne pas y mettre `AsSplitQuery()` : le
diagnostic d'origine (« ça grossit en produit ») comptait les occurrences sans le
filtre. Mesure et récit : [[Recap Emménagement V2]].

## Hors périmètre

- Points, récompenses, features famille/enfants — jamais (pas d'enfants).
- Sous-tâches et projets multi-étapes (module Projets, v2+).
- Notifications push (v1 = flux iCal seulement). À ne pas confondre avec [[Synchro]],
  qui ne sort jamais d'un onglet ouvert : les gestes sur les occurrences y diffusent de
  quoi rafraîchir les autres écrans et annoncer « Ariane a complété … ».

## Décisions

- [[D-2026-08-23 Moteur De Récurrence Trois Modes]] — les 3 modes, matérialisation,
  journal séparé, stratégies d'assignation.
- [[D-2026-08-23 Notifications Par Flux iCal]] — canal de rappel v1.
- [[D-2026-08-28 Synchro Temps Réel Par SignalR]] — les gestes sur les occurrences
  diffusent acteur et libellé aux onglets ouverts (voir [[Synchro]]).
- [[D-2026-08-23 Flux iCal Par Personne]] — structure des flux (jeton par compte).
- [[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]] — exposition publique du
  flux + rotation du jeton.
- [[D-2026-08-23 Zones Plates]] — zones = liste plate CRUD.
- [[D-2026-08-23 Auth Simple Deux Comptes]] — attribution des complétions.
- [[D-2026-08-28 Glissement Hors Fenêtre Des Intervalles]] — une intervalle échue
  hors saison glisse à la prochaine fenêtre.
- [[D-2026-08-25 Invariants D'occurrence En Base]] — index uniques (une en-attente
  par tâche, une complétion par occurrence), courses converties en 409.
- [[D-2026-08-26 Documents Liés Aux Tâches]] — jointure many-to-many
  `TacheDocuments`, sémantique null/[]/liste de `documentIds`.
- [[D-2026-09-20 Échéance Ferme Explicite Sur La Tâche]] — le booléen `EcheanceFerme`
  sur la tâche, affirmation du foyer, lu par le journal mural et la lettre.

## Ancres de code

- `server/HouseOs.Api/Domaine/SpecRecurrence.cs` — spec type + paramètres, fenêtre
- `server/HouseOs.Api/Domaine/MoteurRecurrence.cs` — moteur pur (le cœur, le plus testé)
- `server/HouseOs.Api/Domaine/Assignation.cs` — stratégies d'assignation
- `server/HouseOs.Api/Domaine/Tache.cs` — créations ponctuelle/récurrente
- `server/HouseOs.Api/Domaine/Occurrence.cs` — complétion + entrée de journal
- `server/HouseOs.Api/Features/Taches/TachesEndpoints.cs` — API tâches/occurrences
- `server/HouseOs.Api/Features/Taches/RolloverService.cs` — glissement quotidien
- `server/HouseOs.Api/Features/Entretien/` — packs d'entretien (programme de la maison, proposer / adopter)
- `server/HouseOs.Api/Features/FluxIcal/FluxIcalEndpoints.cs` — flux iCal
- `server/HouseOs.Tests/Domaine/` — tests du domaine (moteur, stratégies)
- `web/src/pages/Pieces.tsx` — vue Pièces (fraîcheur, gestion des zones)
- `web/src/pages/Aujourdhui.tsx` — la page d'accueil
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
  [[Cycle De Vie Des Occurrences]].
- 2026-08-25 Éditeur de tâche / quick-add / pièces — tranche directe sans plan
  (échéance chargée dans l'éditeur, modal unique, tâches depuis une pièce), voir
  [[Écrans Des Tâches]].
- 2026-08-25 Durcissement cas de bord — audit de couverture (artifact « Angles
  morts de House OS »), correctifs + tests, voir [[Suite De Tests]] et
  [[D-2026-08-25 Invariants D'occurrence En Base]].
- [[Plan 2026-08-25 Ruban Des 7 Prochains Jours]] · [[Recap Ruban Des 7 Prochains Jours]]
  — maquettes : artifact « Pièces à venir » (5 directions puis itération ruban).
- [[Plan 2026-08-26 Documents Liés]] — documents de référence multiples par
  tâche (jointure), voir [[D-2026-08-26 Documents Liés Aux Tâches]] ; recap dans
  [[Recap Tâches]] (incrément 2026-08-26).
- [[Plan 2026-08-26 Tâches Rythmes Et Année]] — refonte de la page Tâches
  (itération A des maquettes « Maquettes Tâches » / « Itérations Tâches ») ;
  recap dans [[Recap Tâches]] (incrément 2026-08-26).
