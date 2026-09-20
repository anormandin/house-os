---
type: feature
status: draft
last-verified: 2026-09-20
verified-against: eb62d0e
tags: [iot]
---

# Fonds De Tiroir

## Intention

Les **petites choses vraies** que la maison sait d'elle-même et du monde autour, mises à
plat, datées et classées, pour que quelque chose d'autre les publie. Le jour où la
maison ne demande rien, il doit rester sept faits à dire ; le jour où elle en demande
quatorze, il doit rester de quoi remplir une bande de pied.

Matériau d'origine : [[Éditorialiste De L'Écran]] (trois tours de maquettes,
2026-09-20). Premier consommateur : [[Journal De La Maison]]. Second consommateur prévu :
la lettre du matin (courriel sortant), qui aura sa propre feature.

> [!warning] Rien de ceci n'est implémenté (as of 2026-09-20).
> Cette note décrit un contrat à construire, pas l'état du code. Le plan d'exécution est
> [[Plan 2026-09-20 Journal Éditorial]].

## Comportement

### Le contrat

Le fonds de tiroir rend une **liste de faits ordonnés par score**. Un fait porte :

- une **clé stable** (c'est elle qui permet la pénalité de fraîcheur d'un jour à l'autre) ;
- sa **famille** (le ciel, le climat, la maison, le calendrier, la ville, le hasard) ;
- une **étiquette** et une **valeur** courtes, et un **texte long** — le consommateur
  choisit ce qu'il a la place de montrer. Les widgets rétrécissent avant de disparaître ;
- ses **composantes de score** : rareté, fraîcheur, pertinence du jour.

Il ne connaît **ni pixel, ni colonne, ni 1-bit**
([[D-2026-09-20 Fonds De Tiroir Séparé Du Journal]]) : le budget de widgets, les rangs
et la lettrine appartiennent au journal.

### Comment un fait gagne sa place

`score = rareté × fraîcheur × pertinence du jour`

- **rareté** — combien de fois par année l'item peut paraître (l'équinoxe, 2 fois ; la
  durée du jour, tous les jours).
- **fraîcheur** — pénalité s'il est sorti récemment. C'est ce qui crée la surprise
  quotidienne ; elle se lit dans les clés publiées par les éditions précédentes
  ([[D-2026-09-20 Une Édition Par Jour Matérialisée]]).
- **pertinence du jour** — est-ce que le fait change quelque chose à aujourd'hui.
  « Le soleil se couche à 18 h 25 » est de la décoration un mardi ordinaire et une
  consigne le jour du déménagement.

Un fait dont une source manque **ne sort pas** ; il ne casse jamais la composition.

### Les six familles

| Famille | Ce qu'elle donne | Dépendance |
|---|---|---|
| **Le ciel** | lever, coucher, durée du jour, dérive quotidienne, phase lunaire, équinoxes et solstices, changement d'heure | calcul local depuis `METEO_LATITUDE`/`METEO_LONGITUDE` — aucune |
| **Le climat** | premier gel, première neige, dernière journée à 20°, « il a fait X° ce jour-là l'an dernier » | normales matérialisées + tables de [[Météo]] |
| **La maison** | ce jour-là l'an dernier, série en cours et record, N séances depuis, plus vieil équipement, zone la plus négligée, coût de l'année | journal de complétion — **ne donne rien la première année** |
| **Le calendrier** | compte à rebours, ça s'en vient (7–30 j), travaux de la saison, garantie qui expire | [[Comptes À Rebours]], occurrences, fenêtres saisonnières, [[Documents]] |
| **La ville** | prochaine collecte, collecte spéciale, événement municipal | [[Flux Externes]] — ICS pour les collectes, flux poussé pour les événements |
| **Le hasard** | dicton météo québécois, fête ou journée nationale | banque locale |

Détail par item, avec source et rareté : la table du fonds de tiroir dans
`design/maquettes/une-editorialiste.html`.

### Les sources

- **Éphémérides** : formules NOAA écrites à la main dans `Domaine/`, aucune dépendance
  NuGet. Pures, testables contre des valeurs connues, aucun appel réseau.
- **Normales climatiques** : un tirage annuel de l'archive Open-Meteo (ERA5) pour les
  coordonnées du `.env`, matérialisé
  ([[D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo]]).
- **La maison** : lectures du journal de complétion, des [[Équipements]] et des
  zones ([[D-2026-08-23 Zones Plates]]) — rien de neuf en base.
- **La ville** : rien de municipal dans le code
  ([[D-2026-09-20 Sources Municipales Séparées Par Solidité]]). Les collectes entrent
  par un ICS régénéré à la main une fois l'an
  ([[D-2026-09-20 Calendrier De Collectes Régénéré À La Main]]) ; les événements par un
  flux externe poussé ([[D-2026-09-20 Flux Externe Poussé]]).

## Hors périmètre

- Toute mise en forme : grille, colonnes, budget, lettrine, 1-bit — c'est
  [[Journal De La Maison]].
- Les **actualités** municipales (8 par an, sans date sur la page de liste) — reportées.
- Une **catégorie ou étiquette** sur la tâche
  ([[D-2026-09-20 Regroupement Sans Catégorie De Tâche]]).
- Un gratteur, un analyseur PDF ou un client municipal dans le dépôt.
- La lettre du matin et l'envoi de courriel sortant — feature à part, plus tard.

## Décisions

- [[D-2026-09-20 Fonds De Tiroir Séparé Du Journal]] — le fonds produit des faits sans
  mise en forme ; le journal les publie.
- [[D-2026-09-20 Sources Municipales Séparées Par Solidité]] — collectes par ICS hors
  dépôt, événements par flux poussé, rien de SCJC dans le code.
- [[D-2026-09-20 Calendrier De Collectes Régénéré À La Main]] — geste annuel, gardé par
  une tâche récurrente ; la panne est visible, jamais silencieuse.
- [[D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo]] — normales calculées
  pour les coordonnées du `.env`, générique pour tout foyer.
- [[D-2026-09-20 Flux Externe Poussé]] — `FluxExterne` gagne une source poussée plutôt
  qu'une table de faits séparée.
- [[D-2026-08-23 Pas De N8n Dans Le Cœur]] — les règles sont des classes C# testables.

## Ancres de code

À créer (aucune n'existe as of 2026-09-20) :

- `server/HouseOs.Api/Domaine/Ephemerides/` — soleil, lune, équinoxes : pur calcul.
- `server/HouseOs.Api/Features/FondsDeTiroir/` — les six familles, le score, la
  composition. Aucune référence à l'affichage.
- `server/HouseOs.Api/Features/Meteo/` — l'ingestion des normales rejoint l'existant.
- `server/HouseOs.Api/Features/FluxExternes/` — la source poussée.

## Sources

- [[Éditorialiste De L'Écran]] — la direction retenue, la table du fonds de tiroir, les
  sources municipales vérifiées au curl.
- `design/maquettes/une-editorialiste.html` — le fonds de tiroir au complet, par item.

## Historique

- [[Plan 2026-09-20 Journal Éditorial]]
