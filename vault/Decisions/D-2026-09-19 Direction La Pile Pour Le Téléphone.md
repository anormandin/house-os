---
type: decision
status: accepted
date: 2026-09-19
feature: "[[Vue Téléphone]]"
tags: [backfill]
---

# Direction La Pile Pour Le Téléphone

> [!note] Décision rétro-datée (`#backfill`, écrite le 2026-09-29).
> Le choix a été fait le 2026-09-19 sur des maquettes (canevas Claude Design « House OS
> — UI téléphone »), livré le 2026-09-20, et le [[Recap Vue Téléphone]] a signalé qu'aucun
> fichier de décision ne le consignait. Cette note le fait, d'après la spec et le Recap.

## Contexte

[[D-2026-09-19 Interface Téléphone Distincte]] acte une interface téléphone à part, et
[[D-2026-09-19 Portée De La Vue Téléphone]] la borne à un compagnon. Restait la forme :
comment la journée se lit debout, à une main, sur 390 px.

## Options considérées

- **Le bureau en une colonne** — mêmes cartes, empilées. Rien à concevoir, mais le
  tableau de bord multi-colonnes devient un long défilement où la tâche du moment se
  cherche.
- **Barre d'onglets du bas + écrans dédiés** — la convention des apps natives, permise
  par la décision d'interface distincte. Mais une barre d'onglets ne porte pas de
  compteur lisible, et la question du téléphone est « qu'est-ce qui est en retard ? ».
- **« La pile »** (retenue) — l'accueil se lit une carte à la fois dans la zone du
  pouce ; un menu déroulant sous une barre du haut collante remplace la barre d'onglets
  et porte la pastille des retards ; les gestes cachés au survol deviennent une feuille
  d'actions du bas ; liste ⇄ détail se fait dans le même écran, sans nouvelle route.

## Décision

Alain et Ariane, 2026-09-19 : la direction « La pile ». Le menu déroulant plutôt que la
barre d'onglets parce qu'il peut porter la pastille « en retard » — c'est le sujet du
seul test de la coquille. La permission d'une barre d'onglets reste valide, simplement
non exercée.

## Conséquences

- Les routes restent communes aux deux vues ; seul le composant rendu change.
- Les vues d'analyse (Année, Flux) n'ont pas d'équivalent et leurs commutateurs
  disparaissent au téléphone.
- Revenir à une barre d'onglets un jour ne demande pas de nouvelle décision, seulement
  de garder la pastille quelque part.

## Confirmation

- `web/src/components/telephone/CoquilleTelephone.test.tsx` (la pastille des retards
  dans le menu) reste vert.
- `grep -rn "tabbar\|barre-onglets" web/src/components/telephone` ne trouve rien.
