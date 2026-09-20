---
type: decision
status: accepted
date: 2026-09-20
feature: "[[Fonds De Tiroir]]"
tags: []
---

# D-2026-09-20 Sources Municipales Séparées Par Solidité

## Contexte

La famille « La ville » du fonds de tiroir repose sur trois sources de
Sainte-Catherine-de-la-Jacques-Cartier, vérifiées au curl le 2026-09-20
([[Éditorialiste De L'Écran]]) : un **PDF annuel des collectes** (solide, avec l'index
des rues par secteur), une page **événements** et une page **actualités**, toutes deux
grattées d'un gabarit HTML sans RSS ni JSON-LD.

Le dépôt est public et sous AGPL ([[Distribution]],
[[D-2026-09-02 Dépôt Public AGPL Et Instance Générique]]) : rien de propre à un foyer ne va dans le code ni dans
les défauts. Et [[Flux Externes]] met déjà les « scrapers spécifiques par municipalité »
hors périmètre.

Le point qui tranche : ces trois sources n'ont pas la même **solidité ni le même rôle**.
Le PDF peut porter une vraie échéance (« jeudi, bac brun ») ; les deux pages grattées
sont des widgets qui ont le droit de manquer.

## Options considérées

- **Abstraction « source municipale » + SCJC livré dedans** — le rapprochement du secteur
  par l'index des rues profiterait à tout le monde. Mais on publierait un gratteur pour
  une ville de 7 000 habitants dans un dépôt AGPL, et ses sélecteurs CSS pourrissent à
  la première refonte du site — une mise à jour de House OS pour un changement de `div`.
- **Gratteur configurable** (URL + sélecteurs CSS + format de date saisis dans l'app) —
  générique par construction, mais c'est un mini-DSL à configurer à la main, et le PDF
  des collectes, la source la plus utile, ne s'exprime pas du tout comme ça.
- **Tout hors dépôt, sans rien recevoir** — le plus propre, mais la famille « La ville »
  se réduit aux collectes et on perd les événements.
- **Séparer par solidité** (retenue).

## Décision

On sépare selon ce que la source peut porter :

- **Les collectes** (échéance ferme) sont converties en **ICS hors dépôt**, une fois
  l'an, et consommées par [[Flux Externes]] tel quel, type `Collecte`. **Zéro code neuf,
  zéro SCJC dans le dépôt.** Cadence de régénération :
  [[D-2026-09-20 Calendrier De Collectes Régénéré À La Main]].
- **Les événements municipaux** (fragiles, peuvent manquer) entrent par une
  **ingestion générique de faits datés** — un flux externe « poussé »
  ([[D-2026-09-20 Flux Externe Poussé]]). Le gratteur SCJC vit **dehors**, se corrige
  sans redéployer House OS, et n'existe nulle part dans le dépôt.
- **Les actualités** attendent : 8 par an, et la date n'est que sur la page de
  l'article.

Choix d'Alain, en grillage.

## Conséquences

- House OS ne contient **aucun client `villescjc.com`**, aucun `pdftotext`, aucun
  sélecteur CSS municipal. Ce qu'il gagne est générique : recevoir des faits datés.
- Le gratteur et le convertisseur de PDF deviennent un outil personnel, hors du cycle de
  release ; leur existence est documentée dans `CLAUDE.local.md`, pas dans le dépôt.
- Un widget « La ville » qui manque n'est **jamais** une panne : le fonds de tiroir doit
  survivre à un flux poussé vide ou périmé, et le score doit simplement ne pas le sortir.
- La ligne « scrapers spécifiques par municipalité » du hors périmètre de
  [[Flux Externes]] **reste vraie**.
- Un foyer qui installe House OS ailleurs branche sa ville par un ICS ou en poussant ses
  propres faits — le contrat est public, l'adaptateur est à lui.

## Confirmation

```
grep -rni "villescjc\|jacques-cartier\|pdftotext\|c-event-card\|c-publication-card" server/ web/ infra/
```

doit ne rien retourner. Et `server/HouseOs.Api/Features/FluxExternes/` ne contient aucun
analyseur HTML ni PDF (pas de `HtmlAgilityPack`, `AngleSharp`, `PdfPig` dans
`server/HouseOs.Api/HouseOs.Api.csproj`).
