---
type: reference
last-verified: 2026-08-23
verified-against: 5f9f080
tags: []
---

# Patterns Vue Aujourd'hui

Mécaniques concrètes des meilleures vues « aujourd'hui », à transposer au desktop.

## Things 3 (l'étalon-or du calme)

- Blanc dominant ; couleur en éclats seulement (étoile bleue Aujourd'hui, lune jaune
  du soir, date rouge d'échéance) — jamais de fonds colorés.
- **Une tâche est du texte nu jusqu'au clic** : case + titre. Ouverte, la rangée
  devient une carte (notes, checklist, échéance dedans) pendant que la liste
  s'estompe. Métadonnées invisibles tant qu'inutiles.
- Hiérarchie par graisse (une famille, peu de tailles).
- **Sections dans une même page** : « This Evening » sépare le jour du soir sans
  onglet supplémentaire → transposer en « Ce soir ».
- Jamais de badges hurlants ; le retard est discret.

## Fantastical (desktop!)

- **Double paradigme côte à côte** : mini-grille du mois + agenda déroulant — on
  sait toujours « où dans le mois » en lisant « quoi ensuite ». Excellent pattern
  desktop multi-colonnes.
- **Saisie en langage naturel** (« souper chez maman jeudi 18 h ») — le tueur de
  friction pour l'ajout. Candidat sérieux pour le quick-add House OS.
- Couleur par calendrier en points et barres fines, pas en blocs pleins.

## Structured

- Chronologie verticale du jour avec ligne « maintenant » — voler l'accent « prochaine
  chose », refuser l'obligation d'horodater chaque corvée.

## Home Assistant 2026

- 2026.1 : **cartes-résumé en haut** (roll-ups), favoris, puis pièces — résumé
  d'abord, détail à la demande, une seule page.
- 2026.2 : chrome plat et silencieux — les données au centre, pas l'app.

## Doctrine widget (Apple HIG)

- Un panneau = « un résumé compact d'un coup d'œil » : une idée forte, hiérarchie
  par graisse, plancher de lisibilité 17 pt, l'UI s'efface devant le contenu.
- **Test** : chaque panneau House OS doit tenir dans un widget moyen. Sinon : trop
  dense.

## Conventions desktop (calm/home software)

- **Sidebar gauche fixe et repliable** dès 5+ destinations (Things Mac, Notion,
  Linear) — le composant Sidebar de shadcn est exactement ce pattern.
- **Multi-panneaux** : liste + détail côte à côte ; le détail s'ouvre sur place ou en
  troisième panneau, jamais en mur modal.
- **Colonne de contenu ~60-75ch centrée** — une liste pleine largeur 1440 px fait
  « panneau d'admin », pas agenda.
- **Clavier** : palette ⌘K (cmdk, fourni par shadcn), raccourci quick-add global,
  navigation à touche unique. Le luxe : saisie naturelle (« ⌘K, acheter lait,
  Entrée »).
- Densité : calme ≠ dense ; une colonne confortable pour Aujourd'hui, la densité
  réservée à la vue semaine/planification.

## Application à House OS (desktop)

- Page d'accueil = Aujourd'hui : bandeau-résumé (titre d'humeur + météo un jour),
  colonne principale « À faire » sectionnée (En retard discret / Aujourd'hui /
  Ce soir), colonne latérale (semaine à venir, équilibre du couple).
- Quick-add omniprésent (barre + ⌘K) — viser la saisie naturelle à terme.
- Liens : [[Directions Artistiques]] · [[Affichage Mural Et E-ink]].
