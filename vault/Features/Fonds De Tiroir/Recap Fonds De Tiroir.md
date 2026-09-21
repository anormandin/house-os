---
type: recap
date: 2026-09-21
feature: "[[Fonds De Tiroir]]"
plan: "[[Plan 2026-09-20 Journal Éditorial]]"
---

# Recap Fonds De Tiroir

Bâti aux étapes 2 à 6 du plan commun (2026-09-20 → 2026-09-21) : le contrat du fait
(clé stable, famille, étiquette, valeur, texte long, trois composantes de score gardées
séparées), le moteur de score, et les six familles — le ciel (7 items, formules NOAA
écrites à la main), la maison (8), le calendrier (4), le hasard (2), le climat (5,
première migration du chantier), la ville (3, seconde migration). Premier
consommateur : [[Journal De La Maison]].

## Ce qui a dévié du plan, et pourquoi

- **Les normales viennent de l'archive Open-Meteo aux coordonnées du `.env`**, pas de
  constantes québécoises
  ([[D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo]]) : générique pour
  tout foyer ; le gel annoncé est celui du sol, compté par saison, à la médiane.
- **Rien de municipal dans le dépôt**
  ([[D-2026-09-20 Sources Municipales Séparées Par Solidité]]) : les collectes arrivent
  par ICS (PDF converti à la main, publié sur R2), les événements par un flux poussé
  avec sa propre clé ([[D-2026-09-20 Flux Externe Poussé]]).
- **La banque du hasard est un fichier de données** remplaçable, fêtes mobiles
  comprises ([[D-2026-09-20 Banque Du Hasard En Fichier De Données]]).
- **La rareté est un compte de jours**, pas une envie ; **la fraîcheur lit les clés
  publiées par les sept dernières éditions** depuis l'étape 7 — le fonds, lui, ne sait
  toujours pas qu'une édition existe.
- **La dérive du jour se dit à la semaine** : un texte long qui redit l'étiquette et la
  valeur est refusé par un test.
- **La famille « la maison » est muette la première année** (journal de complétion
  vide) ; le rang 0 tient sans elle, vérifié sur un jour de prod sans rien.
