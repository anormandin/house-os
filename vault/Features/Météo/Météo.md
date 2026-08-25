---
type: feature
status: implemented
last-verified: 2026-08-25
verified-against: eb830ec
tags: []
---

# Météo

## Intention

Donner à House OS la conscience du dehors : savoir la météo des prochains jours à
Sainte-Catherine-de-la-Jacques-Cartier pour (1) nourrir des règles « bonne journée
pour… » (tondre, teindre le patio, laver les vitres…), (2) alimenter la bannière
d'Aujourd'hui et, plus tard, [[Titre D'humeur]] côté serveur et la vue e-ink.
Première brique d'ingestion de données externes de la phase 2.

## Comportement

- Un `BackgroundService` interroge Open-Meteo `/v1/forecast`
  ([[D-2026-08-24 Météo Open-Meteo]] — horaire ~7 jours, modèle canadien HRDPS,
  sans clé) toutes les heures pour les coordonnées configurées
  (`Meteo:Latitude`/`Longitude`, pointées sur la nouvelle maison) et normalise le
  résultat dans les tables locales ([[D-2026-08-24 Tables Météo Normalisées]]).
  La normalisation est défensive (durcie 2026-08-25) : réponses partielles ou
  tronquées tolérées (série plus courte que `time`, `sunrise`/`sunset` absents),
  corps sans `hourly`/`daily` → erreur claire, et **l'heure murale dupliquée de la
  nuit du retour à l'heure normale est dédoublonnée** — sans quoi l'index unique
  sur `Heure` ferait échouer (et geler) l'ingestion entière ; la cadence est
  bornée au plancher (une config nulle ne tue pas le service).
- Les règles « bonne journée pour… » sont des classes C# testables sur le modèle
  normalisé — elles ne voient jamais la forme de l'API. Règles v1 (choix
  utilisateur 2026-08-24) : **tonte**, **aération / fenêtres ouvertes**,
  **journée dehors / journée en dedans** (un verdict activités extérieures vs
  intérieures). Seuils dans le plan, ajustables par code + test. Depuis le
  2026-08-25, la série horaire inclut le code temps WMO et tout code ≥ 51
  (bruine incluse, même à 0 mm) compte comme précipitation
  ([[D-2026-08-25 Code Météo Horaire Et Conditions Du Moment]]).
- Un évaluateur `MeteoRemarquable` (statique, du même domaine mais hors
  `RegleJournee.Toutes` — il ne sort pas par `GET /api/meteo`) produit un
  signal « la météo sort de l'ordinaire » (orage, neige, pluie soutenue,
  chaleur/froid extrême, grand vent, journée exceptionnelle) — consommé
  uniquement par [[Titre D'humeur]], qui ne parle météo que sur ce signal.
- Quand des prévisions existent, Aujourd'hui affiche un bandeau météo compact et
  des pastilles sur verdict `Bon` d'une règle, plus une pastille « rester en
  dedans » quand « Être dehors » est défavorable (`Passable` reste muet) ; pas de
  page météo dédiée. La grosse icône et la température affichent les conditions
  de l'heure courante (objet `maintenant` de `GET /api/meteo`, dérivé de la ligne
  horaire, repli sur le code quotidien si la ligne manque) ; le code quotidien
  sert aux jours suivants.
- L'UI lit uniquement les tables locales via `GET /api/meteo` ; Open-Meteo hors
  ligne → dernières données connues, jamais d'appel API dans le chemin de requête.

## Hors périmètre

- Pollen (Google Pollen exige un compte GCP facturable) et prix de l'essence
  (aucune source gratuite viable) — exclus de la v1 par la recherche.
- Alertes ECCC / AQHI — complément possible plus tard, pas dans cette tranche.
- Multi-lieux : une seule localisation configurée (choix utilisateur 2026-08-24).
- Règles teinture patio et lavage de vitres — écartées de la v1 par l'utilisateur ;
  faciles à ajouter comme nouvelles classes de règles.
- Toute dépendance à n8n/Node-RED ou à Home Assistant.

## Décisions

- [[D-2026-08-24 Météo Open-Meteo]] — Open-Meteo `/v1/forecast` (HRDPS, sans clé)
  comme source unique de la v1.
- [[D-2026-08-24 Tables Météo Normalisées]] — worker horaire → tables normalisées
  + brut archivé ; règles évaluées à la lecture.
- [[D-2026-08-25 Code Météo Horaire Et Conditions Du Moment]] — code WMO horaire,
  règles sensibles à la bruine, conditions du moment dérivées de l'heure courante.
- [[D-2026-08-23 Pas De N8n Dans Le Cœur]] — ingestion et règles en C# typé.

## Ancres de code

- `server/HouseOs.Api/Domaine/Meteo/` — modèle normalisé + règles (`RegleTonte`,
  `RegleAeration`, `RegleJourneeDehors`, `MeteoRemarquable`, seuils en constantes).
- `server/HouseOs.Api/Features/Meteo/` — options, normalisation Open-Meteo,
  worker d'ingestion, endpoint `GET /api/meteo`.
- `server/HouseOs.Tests/Domaine/ReglesJourneeTests.cs`,
  `server/HouseOs.Tests/Domaine/MeteoRemarquableTests.cs` et
  `server/HouseOs.Tests/Features/Meteo/OpenMeteoNormalisationTests.cs` — tests.
- `web/src/components/MeteoCarte.tsx` — carte « Dehors » d'Aujourd'hui
  (bandeau + pastilles).

## Sources

- `docs/research/2026-08-23-donnees-externes-meteo.md` — rapport de recherche
  (comparatif d'APIs, patterns d'ingestion, exemples de règles).

## Historique

- [[Plan 2026-08-24 Météo V1]] · [[Recap Météo]]
- [[Plan 2026-08-25 Météo Du Moment Et Phrase Axée Tâches]]
