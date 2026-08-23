---
type: reference
last-verified: 2026-08-23
verified-against: 5f9f080
tags: []
---

# Apps Similaires

Patterns et anti-patterns tirés des apps du domaine (rapport complet :
`docs/research/2026-08-23-ui-apps-similaires.md`). Le constat central : les apps de
corvées sont mobile-first — on leur vole leurs *concepts* ; les patterns
d'*interaction* desktop viennent des apps de productivité haut de gamme (Things 3
Mac, Todoist web, Linear).

## Concepts à voler

- **Gradient d'urgence continu (Tody)** — la trouvaille la plus aimée du domaine :
  pas de dates pour l'entretien récurrent, une barre « fraîcheur » vert→jaune→rouge
  qui se vide à la complétion. Rejoint le principe « pas de théâtre d'urgence » de
  [[Inspiration UI]]. Le retard est un gradient, jamais un badge rouge culpabilisant.
- **Pièces en vue maîtresse-détail (Tody)** — LA vue signature desktop : chaque
  pièce avec son état d'un coup d'œil ; cliquer = ses tâches dans le panneau voisin.
- **Budget d'effort (Sweepy)** — effort 1-3 par tâche, budget quotidien par
  personne, plan auto-rempli par urgence. L'équité = répartition pondérée + retour
  discret (« cette semaine : toi 6 · moi 5 »), jamais points/classements — même le
  compétitif pour couples (Nipto) s'épuise en 2 semaines.
- **Rotation automatique (Donetick)** — assigner à qui en a fait le moins ; modes
  fixe / rotation / « libre, premier arrivé ».
- **Routines qui se réarment (Skylight/HomeRoutines)** — configurées une fois, se
  réinitialisent seules ; « Focus Zone » (une pièce en vedette par semaine) ;
  mode « une seule chose » contre la fatigue décisionnelle.
- **Quick-add en langage naturel français (Todoist)** — « poubelles tous les mardis
  soir » parsé et surligné en direct. Une app sur mesure n'a qu'à parser *nos*
  formulations.
- **Clavier à la Linear** — `Q` ajout, `⌘K` palette, `E` compléter, `?` aide ;
  indices affichés partout : l'UI s'enseigne elle-même.
- **Commentaires sur l'item (TimeTree)** — la coordination du couple vit sur la
  tâche, pas dans un messenger séparé. Peu coûteux dans un panneau de détail.
- **Fil d'activité (FamilyWall)** — « Alain a fait X il y a 2 h » en colonne
  latérale discrète, jamais en écran d'accueil.

## Anti-patterns documentés (comment ces apps meurent)

- **Présentation « panneau d'admin »** (Grocy, Donetick) — la mort spécifique des
  outils auto-hébergés : la puissance sans présentation opinionnée transforme la
  maison en ERP. C'est exactement l'écart que House OS doit combler.
- **Charge mentale numérisée, pas partagée** (MIT Tech Review) : si un seul
  partenaire saisit/assigne/configure, l'app a échoué. Le *système* doit porter le
  souvenir (récurrence, rotation, auto-planification) ; permissions symétriques.
  ~70 % d'abandon en 100 jours dans la catégorie.
- **Bombardement de notifications** (Flatastic : bientôt/dû/en retard) — le gradient
  à l'écran remplace le harcèlement ; au plus un digest quotidien calme.
- **Création de tâche en plusieurs étapes** (Maple) ; flux modaux ; changement de
  route pour ce qu'un panneau peut faire.
- **Vues agrégées sans regroupement visuel** (Apple Reminders Aujourd'hui).
- **Souris seulement** — une web app sans flux clavier fait « site », pas « outil ».

## Deux natures de contenu

Entretien récurrent et tâches ponctuelles : fusionnés dans Aujourd'hui *avec des
traitements visuels distincts* (gradient de fraîcheur vs date), gérés séparément.
Alimente la spec [[Tâches]] (V1 : le moteur les distingue déjà par le mode).

Liens : [[Inspiration UI]] · [[Directions Artistiques]] · [[Patterns Vue Aujourd'hui]].
