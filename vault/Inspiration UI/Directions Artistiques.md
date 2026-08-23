---
type: reference
last-verified: 2026-08-23
verified-against: 5f9f080
tags: []
---

# Directions Artistiques

Trois directions candidates issues de la recherche (rapport
`docs/research/2026-08-23-ui-dashboards-directions.md`), adaptées à la cible
desktop + e-ink. Des mockups de chacune existent pour trancher avec Ariane.

## A — « Papier d'encre » (lignée Things 3)

- **Humeur** : un bel agenda papier vivant ; calme, littéraire, Muji.
- **Typo** : une sans humaniste (corps) + serif discrète pour la date-en-tête
  (ex. Inter/Public Sans + Lora). Hiérarchie par graisse, 3 tailles max.
- **Couleur** : papier Lotus (#faf7ed / encre #545464) comme identité première ;
  Wave la nuit. Couleur = éclats seulement (bleu aujourd'hui, jaune « ce soir »,
  rouge doux si vrai retard). Pas de fonds de cartes colorés — filets fins et blanc.
- **Densité** : aérée ; sections Aujourd'hui / Ce soir / Cette semaine ; rangées nues
  qui s'ouvrent sur place.
- **Signature** : la section « Ce soir » ; points de fraîcheur (à la Tody) pour les
  corvées ; date-en-tête française soignée.
- Traduit sur e-ink presque 1:1 — meilleure pérennité. Faisable en retirant du
  chrome shadcn plus qu'en ajoutant.

## B — « Tableau de bord Kanagawa » (lignée HA/Mushroom/TRMNL)

- **Humeur** : panneau de contrôle élégant — nuit d'encre sumi, accents lanterne.
- **Typo** : sans géométrique-humaniste (Figtree/Manrope), chiffres tabulaires.
  Grammaire par panneau : une grande Valeur, une petite Étiquette.
- **Couleur** : Wave (#1f1f28) identité première ; un accent Kanagawa par domaine
  (tâches bleu #7e9cd8, entretien aqua #7aa89f, épicerie vert #98bb6c, alertes
  orange #ffa066) en teinte d'icône/bord — jamais en aplat. Pastilles par personne.
- **Densité** : moyenne-haute ; rangée-résumé « état de la maison » en haut, puis
  grille de cartes.
- **Signature** : bandeau-résumé ; tuiles-valeurs ; ruban « prochaine chose ».
- Le meilleur pour l'écran mural (sombre, lisible de loin) ; le plus naturel en
  shadcn. Risque : « une app à gérer » plutôt qu'un outil de maison.

## C — « Cuisine chaleureuse » (lignée Hearth)

- **Humeur** : la porte de frigo de bon goût — chaud, arrondi, un brin ludique ;
  logiciel-décor.
- **Typo** : famille arrondie/douce (Nunito Sans, ou Fraunces en titres).
  Rayons généreux, puces-pilules, icônes amicales.
- **Couleur** : crème Lotus réchauffée en identité ; accents plus généreux (vert
  doux « fait ✓ », jaune « ce soir », avatars pastel).
- **Densité** : basse-moyenne ; rangées épaisses ; panneau-héros « aujourd'hui »
  avec titre d'humeur (« Tout est beau 🌿 » / « 2 choses avant dodo »).
- **Signature** : le titre d'humeur ; micro-célébration en fin de journée ; vue
  d'équilibre (« cette semaine : toi 6 · moi 5 ») — équilibre, pas classement.
- Le plus fort attrait émotionnel pour un outil de couple ; le plus dépendant du
  goût d'exécution ; l'e-ink perd sa chaleur (la couleur est son identité).

## Traductions e-ink par direction (rapport v2)

- **A → quasi 1:1** : desktop et mur sont le même objet en deux matières — le
  desktop est « l'agenda papier à l'écran », l'e-ink « la même page, imprimée dans
  la cuisine ».
- **B → re-matérialisation en grille** : la grammaire Valeur/Étiquette est native de
  l'e-paper, mais l'identité (la couleur) ne survit pas — frère, pas jumeau.
- **C → le titre d'humeur survit** (Fraunces tient bien en 1-bit), la chaleur non.

## Verdict (2026-08-23)

La recherche recommandait A comme base (meilleure traduction e-ink) ; **le couple a
choisi C — Cuisine chaleureuse** sur les maquettes
([[D-2026-08-23 Direction Artistique Cuisine Chaleureuse]]). Cinq variations de C
explorées ensuite sur le canvas (épurée, soirée, par pièces, tableau, illustrée)
pour fixer l'exécution. **Verdict final (2026-08-23) : le look de C5 — Illustrée**
(grand Fraunces, illustration au trait, accents en bord de carte) **comme langage
visuel, avec la vue « par pièces » de C3 comme vue de premier plan**. Les comptes à
rebours sont généralisés en feature ([[Comptes À Rebours]]). À retenir de la recommandation d'origine : le bandeau-résumé
de B reste greffable, et l'e-ink portera la chaleur par le ton et les formes, pas la
couleur. Ordre de construction inchangé : modèle « journée » partagé → gabarits
desktop → route `/eink` à 800×480 capturée par headless Chrome.
