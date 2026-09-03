---
type: reference
last-verified: 2026-08-23
verified-against: 5f9f080
tags: [iot]
---

# Affichage Mural Et E-ink

La seconde vue de House OS ([[D-2026-08-23 Interface Desktop Et Écran E-ink]]) :
un écran e-ink mural, design distinct du desktop.

## Grammaire TRMNL (la référence e-ink)

- Écrans 800×480, 1-bit ; composants : Title / Value / Label / Description /
  Divider / Item / Table / Progress ; layouts en colonnes sans config.
- **Une énorme Valeur + une Étiquette par région** — un mot/chiffre domine chaque
  zone. Le débordement est l'ennemi : troncature systématique.
- Polices pixel verrouillées à des tailles fixes (rien d'anti-aliasé).
- Conséquence pour House OS : si la vue Aujourd'hui se réduit à des paires
  Valeur/Étiquette + listes d'Items, le rendu e-ink est presque gratuit.

## Lisibilité à distance (règles d'affichage dynamique)

- Hauteur de texte minimale ≈ distance (pieds) × 0,007 po ; confortable ×0,010-0,014.
- À 2-3 m : ~20-30 pt minimum pour le secondaire, beaucoup plus pour la ligne
  principale ; sans-serif, contraste fort, 3-5 lignes courtes max par zone.
- **Un layout desktop agrandi ne marche PAS au mur** : la vue murale est un palier
  de densité propre (primaire ×2, secondaire élagué).

## Layouts muraux qui marchent

- MagicMirror : régions ancrées aux bords, **centre volontairement vide** — l'info
  en périphérie (calm tech littéral).
- DAKboard : photos au tiers gauche, calendrier aux deux tiers droits.
- Skylight/Hearth : code couleur par personne comme repère principal ; l'écran doit
  passer un test *esthétique* (« est-ce qu'on l'encadrerait ? »).
- HA muraux : thèmes sombres (un écran lumineux dans un salon le soir est hostile).

## Contraintes techniques e-ink (rapport v2)

- **Pipeline éprouvé** (MagInkCal/MagInkDash) : le serveur rend une page HTML à la
  résolution exacte de l'écran (800×480 ou 1200×825) → capture headless Chrome →
  conversion 1-bit → poussée vers l'écran. La vue e-ink = une simple route de l'app.
- **Noir sur blanc pur d'abord** ; les gris passent par du tramage (boueux) — texte
  noir plein, rien sous ~16 px en gris, ni ombres ni dégradés. Zones inversées
  (blanc sur noir) = la seule « couleur » ; avec parcimonie (ghosting).
- **Rafraîchissement** : plein ~1-2 s avec flash ; les partiels accumulent du
  ghosting (un plein aux 5-10 partiels). Cadence saine : remplacement complet aux
  15-60 min ou sur changement. Pas d'animation — une page imprimée qui se réimprime.
- **Typo** : graisses regular à bold seulement (les fines se brisent en 1-bit) ;
  2-3 tailles fortes + graisse, une famille ; chiffres tabulaires ; lissage désactivé
  à la capture.
- **Grilles** : 2-4 zones rectangulaires aux filets 1-2 px (les bordures sont
  gratuites en e-ink) ; par zone : Étiquette petites-caps + Valeur énorme, ou liste
  ≤5 items tronqués. **Élaguer, pas rapetisser.**

## Esquisse House OS e-ink (phase 3)

Spec vivante : [[Affichage E-ink]] (matériel choisi : reTerminal E1003, 2026-09-03).

- Traitement retenu (si direction A) : ⅔ gauche = liste du jour (en-tête-date, ≤6
  items, « Ce soir » après un filet), ⅓ droit = zones Étiquette/Valeur empilées
  (météo, prochaine échéance, collecte) ; une seule bande inversée (l'en-tête).
- Contenu : date + météo, « À faire aujourd'hui », collecte à venir, titre d'humeur.
- Voir `docs/research/2026-08-23-affichages-iot-hardware.md` pour le matériel
  (Inkplate 10 / TRMNL X).
- Relevé du marché 2026-09-03 avec prix livrés, dispo et API par appareil :
  `docs/research/2026-09-03-ecrans-eink-candidats.md`. Point clé : le protocole
  TRMNL BYOS (`/api/setup`, `/api/display`, `/api/log`) comme API unique côté
  House OS, compatible TRMNL, Seeed reTerminal, Kindle et Kobo.
