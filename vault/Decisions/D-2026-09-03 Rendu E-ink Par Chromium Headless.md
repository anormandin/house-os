---
type: decision
status: accepted
date: 2026-09-03
feature: "[[Affichage E-ink]]"
tags: [iot]
---

# Rendu E-ink Par Chromium Headless

## Contexte

L'appareil affiche un PNG à sa résolution (1872×1404 pour l'E1003, 16 niveaux de
gris possibles). Il faut le produire dans le conteneur House OS. Le critère
retenu par l'utilisateur est la **qualité de l'image** ; la taille de l'image
Docker n'est pas une contrainte.

## Options considérées

- **Chromium headless (Playwright pour .NET) capturant une route React `/ecran`**
  — moteur de texte Skia/FreeType/HarfBuzz (chasse, crénage, accents français
  irréprochables à toute taille), mise en page CSS (grid, `line-clamp`,
  `text-wrap: balance`, chiffres tabulaires), icônes Lucide en SVG comme le
  desktop, conception avec les mêmes outils et un aperçu gratuit dans le
  navigateur. Coût : ~400 Mo dans l'image, un navigateur persistant (~150 Mo
  de RAM), polices installées dans l'image pour un rendu identique partout,
  animations et lissage à neutraliser avant le seuillage.
- **Dessin C# ImageSharp.Drawing** — déjà une dépendance, rendu déterministe et
  testable sans navigateur, image Docker inchangée. Coût : mise en page en
  coordonnées, icônes à dessiner à la main, moteur de texte (SixLabors.Fonts)
  moins fin en hinting ; le résultat a tendance à « faire maison ». Rejeté sur
  le critère qualité.
- **HTML → SVG → Skia** — les défauts du dessin manuel sans les avantages du
  navigateur. Rejeté.

## Décision

**Chromium headless via Playwright pour .NET**, capturant la route React
`/ecran` à la taille demandée ; ImageSharp fait ensuite le seuillage 1-bit (ou
la quantification en gris si le firmware le sert) et l'encodage PNG. Choix de
l'utilisateur (2026-09-03) : « go for chromium ».

## Conséquences

- Nouvelle dépendance `Microsoft.Playwright` ; Chromium et ses bibliothèques
  installés dans l'image Docker (`Dockerfile`), navigateur lancé une fois et
  réutilisé (`IHostedService`).
- Le rendu devient une page de l'app : `/ecran` est conçue en Tailwind avec la
  grammaire e-ink, prévisualisable par n'importe qui de connecté.
- Les données de l'écran passent par un endpoint unique
  (`GET /api/affichage/donnees`) accessible au cookie de session **ou** à un
  jeton interne de rendu que le serveur donne à son propre navigateur.
- Polices auto-hébergées dans `web/public/fonts/` (OFL) : aucun appel réseau au
  rendu, même rendu en dev et en prod.
- Le rendu n'est plus une fonction pure : les tests vérifient la composition
  des données et le seuillage, et un test d'intégration optionnel (marqué)
  capture réellement.

## Confirmation

`server/HouseOs.Api/HouseOs.Api.csproj` référence `Microsoft.Playwright` ; le
`Dockerfile` installe Chromium ; `web/src/pages/Ecran.tsx` existe.
