---
type: reference
last-verified: 2026-09-30
verified-against: 6d1fda0
tags: []
---

# Écrans Des Tâches

Sous-note de [[Tâches]] : où les tâches se voient et se manipulent au bureau —
l'éditeur unique, le ruban des 7 prochains jours, la console Rythmes ⇄ Année, la
recherche et le programme de la maison. Le moteur de récurrence et le contrat de la
tâche restent dans [[Tâches]] ; les gestes sur une occurrence, dans
[[Cycle De Vie Des Occurrences]] ; le téléphone, dans [[Vue Téléphone]].

## Comportement

### L'éditeur de tâche (2026-08-25)

- `GET /api/taches/{id}` (et l'outil MCP `gerer_tache`, action `obtenir`) retourne
  maintenant **`echeance`** : celle de l'occurrence en attente. Corrige le bug où
  l'éditeur, n'affichant pas l'échéance existante, l'effaçait silencieusement à
  l'enregistrement d'une tâche ponctuelle.
- Éditeur : la description est un `textarea` de 4 lignes ; le quick-add (⌘K, pages
  Aujourd'hui et Tâches) et le panneau d'une pièce ouvrent le **modal complet**
  (zone préremplie depuis la pièce) ; Échap ferme le modal. `TacheEditeur` est
  l'unique point d'entrée de création **et** d'édition ; cliquer une rangée
  d'occurrence (Aujourd'hui, Tâches, Pièces) ouvre l'éditeur de sa tâche.
- Listes : la description s'affiche en doré, retours à la ligne préservés, y compris
  sur les tâches en retard (sous la ligne « depuis X jours ») ; les notes de la
  rangée verte préservent aussi leurs retours à la ligne (en sourdine).

### Le ruban des 7 prochains jours (2026-08-25)

Gouverné par [[D-2026-08-25 Ruban Des 7 Prochains Jours]].

- **Ruban des 7 prochains jours** (`web/src/components/Ruban.tsx`), composant
  canonique des tâches à venir : colonnes En retard · aujourd'hui (surligné) ·
  6 jours (week-end teinté, jour vide = point) · « Plus tard → » (jours 7 à 30,
  6 lignes max). Puces couleur-de-pièce + avatar, clic → éditeur ; max 3 puces
  par jour puis « + n autres » dépliable sur place.
- Sur **Aujourd'hui** : pleine largeur sous le héros ; remplace la carte « Cette
  semaine » (code retiré) et absorbe les **événements externes** (italique doré
  + icône, non cliquables) ; le bouton de gestion des flux vit dans son en-tête.
- Sur **Pièces** : au-dessus de la grille ; choisir une pièce **estompe le reste
  à 40 %** (événements externes compris).
- **Horizon : un cycle d'avance, plafonné à 30 jours** — réduit côté client à
  `échéance ≤ aujourd'hui + 30` (`web/src/lib/ruban.ts`), la matérialisation à
  la complétion garantissant qu'une occurrence en attente est à au plus un cycle.
  Aucun changement d'API, pas de parité MCP à toucher.
- **Couleurs de pièces** : palette fixe de 6 teintes, attribution déterministe
  par position dans la liste des zones (pas de colonne en base).

### La console Rythmes ⇄ Année (2026-08-26)

Gouvernée par [[D-2026-08-26 Page Tâches Rythmes Et Année]].

- La page Tâches est la **console des définitions** (Aujourd'hui reste la todo
  list) : vue **Rythmes** par défaut — groupes Chaque semaine · Aux quelques
  jours · Chaque mois · Chaque année & au fil des saisons · Ponctuelles (barre
  de progression « X faites sur Y », les faites n'apparaissent plus) — avec
  chips de récurrence en français, lieu (zone · équipement · n docs), échéance
  colorée (retard rouge / ≤ 3 jours doré), avatar + stratégie ; rangée → éditeur.
- **Commutateur segmenté « Liste | Année »** à droite du titre ; le dernier mode
  est mémorisé (localStorage, accès protégé). Le filtre À faire/Complétées et la
  barre quick-add pointillée ont disparu (⌘K demeure).
- Vue **Année** ([[D-2026-08-26 Vue Année Défilante]], révision du même jour —
  la version 12-mois pleine largeur ne survivait pas aux vraies données) :
  **ruban défilant** à ~11 px/jour, domaine du 1ᵉʳ du mois courant au 31 déc,
  aujourd'hui ancré à ~20 % au montage, colonne d'étiquettes sticky (titre
  complet, chips, lieu, avatar) ; **mini-carte** de l'année (fenêtre visible
  synchronisée, tics de jalons, clic = téléportation) ; jalons des
  [[Comptes À Rebours|comptes à rebours]] sur deux voies d'étiquettes ; points
  mensuels (passés estompés) et annuels datés, barres de fenêtres saisonnières
  (vert en cours / jaune à venir), **longs intervalles (> 15 j) sur la
  chronologie** avec note « ensuite ≈ … » ; grappes de ponctuelles par jour
  (voie basse pour les voisines), grappe multiple → **bande « Les ponctuelles —
  semaine par semaine »** (bloc En retard en rouge + toutes les semaines
  entièrement dépliées — demande d'Alain du 2026-08-26, remplace l'accordéon
  3-semaines de la décision ; « Replier » réduit à la barre-titre) ; bandeau
  « Le tempo court » réduit aux
  cadences ≤ 15 jours sans fenêtre ; tout clic → éditeur.
- Nouveau contrat : `GET /api/taches` retourne les définitions
  (`TacheResumeDto` : récurrence complète, stratégie, `occurrenceId`, échéance
  et assigné de l'occurrence en attente, `nbDocuments`, `completee`), tri
  échéance puis titre ; parité MCP `lister_taches`. La logique des deux vues est
  pure et testée (`web/src/lib/taches-vues.ts`, patron ruban).
- **Compléter depuis la console** (2026-08-26) : case à cocher sur chaque rangée
  de la vue Liste et de la bande semaine-par-semaine (via `occurrenceId`) — on
  peut prendre de l'avance, le moteur matérialisant la suivante à partir de
  max(complétion, échéance). L'annulation reste sur Aujourd'hui (rangée verte
  du jour). Compléter/annuler depuis Aujourd'hui invalide aussi la requête
  `taches` de la console.
- **Recherche** (2026-09-28, [[D-2026-09-28 Recherche Globale Sur La Touche Slash]]) : la console du bureau a un champ
  « Chercher une tâche » (titre, description, pièce/équipement ; sans accents, mots
  dans n'importe quel ordre) qui filtre **avant** de grouper — compteurs et
  « en retard » parlent de ce qui est à l'écran — et vaut pour les deux vues. La
  console téléphone cherche sur le titre seul, même comparateur. La touche `/`
  (ou la loupe de l'en-tête) ouvre la recherche globale du bureau ; une tâche
  trouvée s'ouvre dans son éditeur sur place. ⌘K reste le quick-add.
- L'éditeur lie un document par un **choix cherchable** (filtre titre/dossier)
  plutôt qu'un `<select>` de tout le classeur.
- **Programme de la maison** (2026-09-29,
  [[D-2026-09-28 Packs D'entretien En Fichier De Données]]) : le bouton « Programme
  de la maison » de l'en-tête ouvre le panneau des entretiens qu'une maison de zone 4
  demande au fil des saisons (gouttières, robinets extérieurs, détecteurs, coupe-froid…),
  lus d'un fichier de données ; une tâche de même titre déjà présente (n'importe où,
  sans accents ni casse) est cochée-grisée, le reste se crée en une transaction par
  le moteur ordinaire (première occurrence matérialisée, stratégie du pack). Les packs
  par équipement vivent sur la fiche de l'[[Équipements|équipement]] ; contrat complet
  dans [[Emménagement V2]].

## Décisions

- [[D-2026-08-25 Ruban Des 7 Prochains Jours]] — le ruban comme composant
  canonique des tâches à venir ; horizon un-cycle plafonné à 30 jours ;
  placement Aujourd'hui + Pièces (focus) ; couleurs de zones déterministes.
- [[D-2026-08-26 Page Tâches Rythmes Et Année]] — la page Tâches comme console
  des définitions : vue Rythmes + bascule Année, axe Pièces écarté, filtre
  Complétées retiré, `GET /api/taches` + `lister_taches`.
- [[D-2026-08-26 Vue Année Défilante]] — le ruban ~11 px/jour + mini-carte +
  bande accordéon des ponctuelles (option D, ronde 3) ; longs intervalles sur
  la chronologie, tempo court ≤ 15 jours.
- [[D-2026-09-28 Recherche Globale Sur La Touche Slash]] — `/` ouvre la recherche
  globale, ⌘K reste le quick-add ; comparateur partagé sans accents.
- [[D-2026-09-28 Packs D'entretien En Fichier De Données]] — le programme de la maison.

## Ancres de code

- `web/src/components/TacheEditeur.tsx` — éditeur complet (récurrence en français),
  unique point d'entrée création/édition (`zoneInitialeId`, fermeture par Échap).
- `web/src/components/Ruban.tsx`, `web/src/lib/ruban.ts` — ruban des 7 prochains
  jours (groupement pur testé, couleurs de zones).
- `web/src/pages/Taches.tsx`, `web/src/lib/taches-vues.ts` — console Rythmes ⇄
  Année (groupement et géométrie purs testés).
- `web/src/lib/recherche.ts` — comparateur partagé (accents, mots en désordre) ;
  `web/src/components/ChampRecherche.tsx`, `ChoixCherchable.tsx`,
  `RechercheGlobale.tsx` — champ de page, picker cherchable, palette `/`.
- `web/src/components/QuickAdd.tsx` — le quick-add (⌘K).
- `web/src/components/PropositionsEntretien.tsx` — le panneau du programme de la maison.

## Historique

- [[Plan 2026-08-25 Ruban Des 7 Prochains Jours]] · [[Recap Ruban Des 7 Prochains Jours]]
  — maquettes : artifact « Pièces à venir » (5 directions puis itération ruban).
- [[Plan 2026-08-26 Tâches Rythmes Et Année]] · [[Recap Tâches]]
- Éditeur de tâche / quick-add / pièces : tranche directe sans plan (2026-08-25).
