---
type: decision
status: accepted
date: 2026-09-20
feature: "[[Fonds De Tiroir]]"
tags: []
---

# D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo

## Contexte

La famille « Le climat » du fonds de tiroir ([[Éditorialiste De L'Écran]]) tient sur des
**normales** : premier gel attendu, première neige, dernière journée à 20°. Elle sort
trois fois sur six dans les maquettes — c'est la voix la plus forte du journal en
octobre.

Les maquettes parlent de « quatre constantes par région ». Dans un dépôt public
([[Distribution]]), une constante québécoise dans le code est exactement ce qui est
interdit : le foyer est décrit par `METEO_LATITUDE`/`METEO_LONGITUDE` dans le `.env`, et
rien d'autre.

## Options considérées

- **Constantes de configuration** — zéro réseau, générique par construction. Mais
  personne ne connaît par cœur la date normale du premier gel chez lui : en pratique la
  famille reste vide chez presque tout le monde, y compris ici.
- **Depuis les tables météo de la maison** — aucune dépendance neuve, s'améliore tout
  seul, ne dit rien d'utile avant 2028.
- **Famille « Le climat » reportée** — le chantier rétrécit d'une ingestion complète,
  mais le fonds de tiroir perd une de ses six familles dès le départ.
- **Calculées depuis l'archive Open-Meteo** (retenue).

## Décision

Un tirage **annuel** de l'API archive d'Open-Meteo (réanalyse ERA5) pour la latitude et
la longitude du `.env`, sur une dizaine d'années, dont on déduit les normales et qu'on
**matérialise en base**.

Le projet parle déjà à Open-Meteo ([[D-2026-08-24 Météo Open-Meteo]], [[Météo]]) : même
fournisseur, même client, aucune clé, aucune dépendance neuve. C'est la seule option qui
marche **dès le premier jour et pour n'importe quel foyer** qui installe House OS.

Choix d'Alain, en grillage.

## Conséquences

- Une ingestion de plus, sur le patron de [[Météo]] : un `BackgroundService`, une table
  normalisée, des statistiques calculées en C# testable
  ([[D-2026-08-23 Pas De N8n Dans Le Cœur]]).
- ERA5 est une grille (~9 km) : les normales sont **régionales**, pas celles d'une
  station officielle. Assumé — le journal dit « attendu vers le 8 octobre », jamais une
  date exacte, et les textes doivent porter cette imprécision.
- Un seul appel réseau par an : la panne d'Open-Meteo un jour donné n'a **aucun** effet
  sur le journal, contrairement aux prévisions.
- Déménagement prévu le 2026-10-06 : changer `METEO_LATITUDE`/`METEO_LONGITUDE` doit
  **invalider et recalculer** les normales, sinon le journal annoncera le gel de la rue
  Fraser au 17 rue de la Colline.
- Les normales sont des faits comme les autres : une normale manquante doit faire taire
  le widget, jamais casser l'édition.

## Confirmation

Une table de normales existe avec sa migration EF, sa clé porte la latitude et la
longitude utilisées au calcul, et un test vérifie qu'un changement de coordonnées dans
la configuration déclenche un recalcul plutôt que de servir les anciennes valeurs.
