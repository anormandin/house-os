---
type: decision
status: accepted
date: 2026-08-25
feature: "[[Tâches]]"
tags: []
---

# Ruban Des 7 Prochains Jours

## Contexte

Constat d'usage (2026-08-25) : les tâches à venir sont invisibles — une pièce dont
tout est fait dit « tout est frais » même si cinq choses atterrissent cette semaine,
et la carte « Cette semaine » d'Aujourd'hui (liste verticale de 7 lignes max) ne
montre ni la charge par journée ni le retard. Alain veut voir venir : une récurrente
un cycle avant son échéance, une ponctuelle environ un mois avant.

Cinq directions ont été maquettées (artifact « Pièces à venir »,
`design/maquettes/` pour les canvas antérieurs) : mini-liste sur les cartes de
pièces ; jauge d'usure du cycle à la Tody ; panneau de détail en buckets ; ruban
maison entière des 7 prochains jours ; météo des corvées. Une recherche
concurrentielle (Tody, Sweepy, Grocy, Donetick, Todoist, Things 3, Reminders,
Linear) a établi : aucun produit n'a d'horizon relatif à la fréquence (les horizons
existants sont fixes — « due soon days » Grocy, ≤ 7 jours Linear) ; le pattern
robuste est trois états (frais / bientôt / dû-retard), jamais deux.

## Options considérées

- **Ruban des 7 prochains jours** (direction 04, retenue) — bande maison entière :
  colonne « En retard », aujourd'hui surligné, un jour par colonne, puces colorées
  par pièce, colonne « Plus tard → » compressée. Optimise la planification à deux.
- **Jauge d'usure du cycle à la Tody** — la barre de fraîcheur encode la fraction du
  cycle écoulée. Élégant mais change la sémantique de la barre existante.
- **Mini-liste sur les cartes / buckets dans le panneau / météo des corvées** —
  restent locales à une pièce, aucune vue transversale de la semaine.

Placement du ruban : Aujourd'hui seulement, Pièces seulement, ou les deux ; en
pleine largeur ou en carte verticale dans la colonne latérale.

## Décision

Le **ruban des 7 prochains jours** devient le composant canonique des tâches à
venir :

- Sur **Aujourd'hui**, pleine largeur sous le héros ; il **remplace la carte
  « Cette semaine »** et en absorbe les événements externes (faits en italique
  doré + icône, non cliquables). Le bouton de gestion des flux externes migre dans
  son en-tête.
- Sur **Pièces**, le même ruban au-dessus de la grille, justifié par un
  comportement propre : choisir une pièce **estompe les autres entrées à 40 %**
  (événements externes compris — ils n'appartiennent à aucune pièce).
- Anatomie : colonnes = En retard (teinte orange douce) · aujourd'hui (surligné
  jaune) · 6 jours suivants (week-end légèrement teinté, jour vide = un point) ·
  « Plus tard → » (échéances des jours 7 à 30, plafonnée à 6 lignes puis « n de
  plus »). Max 3 puces par jour puis « + n autres » qui **déplie le jour sur
  place**. Une puce de tâche ouvre l'éditeur de sa tâche.
- **Règle d'horizon : un cycle d'avance, plafonné à 30 jours** — soit
  `échéance ≤ aujourd'hui + 30` côté client. La matérialisation à la complétion
  garantit qu'une occurrence en attente est à au plus un cycle de son échéance ;
  le plafond couvre les ponctuelles (« un mois avant »), les annuelles et les
  fenêtres saisonnières lointaines. **Aucun changement d'API** (donc pas de parité
  MCP à toucher) : `en-attente` + `evenements-externes` suffisent.
- **Couleur de pièce** : attribuée côté client, déterministe par `ordre` de zone,
  depuis une palette fixe de 6 teintes dérivées du langage chaleureuse — pas de
  colonne couleur en base.

## Conséquences

- La carte « Cette semaine » disparaît (code retiré) ; la colonne latérale
  d'Aujourd'hui garde Météo, Bilan, Comptes à rebours.
- Les tâches sans échéance n'apparaissent jamais dans le ruban.
- La légende des couleurs de pièces est affichée sous le ruban ; au-delà de
  6 zones, les teintes se répètent (assumé pour un foyer).
- Le ruban ne montre que l'en-attente : les complétées du jour restent dans la
  liste du jour (rangée verte), pas dans le ruban.

## Confirmation

- `grep "Ruban" web/src/components/Ruban.tsx` existe.
- `grep "CetteSemaine" web/src/pages/Aujourdhui.tsx` n'existe plus.
- `grep "Ruban" web/src/pages/Pieces.tsx` existe (focus par pièce).
- Tests `ruban` dans `web/src/lib/` ou `web/src/components/` (groupement retard /
  aujourd'hui / 7 jours / plus-tard ≤ 30 j, débordement « + n autres »).
