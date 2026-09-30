---
type: reference
last-verified: 2026-09-30
verified-against: 6d1fda0
tags: []
---

# Fonds De Tiroir Ciel Et Climat

Sous-note de [[Fonds De Tiroir]] : les deux familles qui se calculent du dehors, sans
rien lire du foyer — le ciel (éphémérides locales) et le climat (normales tirées de
l'archive, [[D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo]]). Le
contrat du fait, le score et les sources sont dans [[Fonds De Tiroir]] ; les autres
familles, dans [[Fonds De Tiroir Maison Calendrier Ville Et Hasard]].

## Le ciel, en détail (as of 2026-09-20)

| Clé | Rareté | Ne sort que si |
|---|---|---|
| `ciel.jour` | tous les jours | — (nuit ou jour polaire : il le dit autrement, 60×/an) |
| `ciel.derive` | tous les jours | la dérive atteint la minute (nulle autour des solstices) |
| `ciel.lune` | 25×/an | pleine ou nouvelle lune, une seule journée |
| `ciel.saison` | 4×/an | le jour même, **dans le fuseau du foyer** |
| `ciel.saison-approche` | 40×/an | dix jours avant |
| `ciel.equilibre` | 2×/an | la durée du jour franchit douze heures — ce n'est **pas** l'équinoxe : la réfraction décale la bascule de quelques jours |
| `ciel.changement-heure` | 28×/an | quatorze jours avant **et le jour même**, si le fuseau en a un |
| `ciel.noirceur` | tous les jours | une tâche ouverte est dans une zone extérieure |

> [!warning] La bascule d'heure a lieu au petit matin.
> Le jour du changement, l'horloge porte déjà le nouveau décalage à midi. Chercher la
> prochaine bascule « à partir d'aujourd'hui » la rend donc **invisible le seul jour où
> elle compte** — le fait doit comparer à partir de la veille, et parler au passé ce
> jour-là. Trouvé en revue de code, 2026-09-20.

## Le climat, en détail (as of 2026-09-20)

Cinq items, sur une dizaine d'années de l'archive Open-Meteo (réanalyse ERA5) tirées
une fois l'an pour les coordonnées du `.env`
([[D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo]]).

| Clé | Rareté | Pertinence | Ne sort que si |
|---|---|---|---|
| `climat.gel` | 38×/an | 2 · **3** dans la semaine | la date normale du premier gel est à moins d'un mois devant, ou à moins d'une semaine derrière |
| `climat.neige` | 38×/an | 2 · **3** dans la semaine | idem, pour la première neige |
| `climat.douceur` | 29×/an | 1,5 · **2** dans la semaine | idem, pour la dernière journée à vingt degrés — fenêtre plus courte : trois semaines avant, ce n'est encore qu'une statistique |
| `climat.mois` | 61×/an | 1,5 les sept premiers jours, sinon 1 | on est **dans** le mois le plus sec ou le plus arrosé de l'année — et l'écart entre les deux vaut au moins un quart, sans quoi on classerait la longueur des mois |
| `climat.an-dernier` | tous les jours | 1 · **1,5** au-delà de huit degrés d'écart | l'archive couvre la même date, un an plus tôt |

La fenêtre **est** la rareté : « un mois avant, une semaine après » fait trente-huit
jours de parution possible, et c'est ce compte-là qu'on écrit — pas une envie.

> [!warning] Le gel qu'on annonce est celui du **sol**, pas celui de l'abri.
> Relevé au premier tirage réel : à zéro degré, la médiane des neuf saisons tombait au
> **27 octobre**, trois semaines après le gel que tout le monde connaît ici. Une maille
> de neuf kilomètres à deux mètres du sol ne voit ni le rayonnement nocturne d'un jardin
> ni l'air froid qui s'y accumule — c'est la raison pour laquelle les avertissements de
> gel s'émettent partout à deux ou quatre degrés annoncés. Seuil à **3 °C** : médiane au
> **3 octobre**, ce que disent les normales publiées de la station. Ce n'est pas un
> ajustement québécois, c'est un écart vrai partout.

> [!note] La saison, et non l'année civile.
> Un premier gel du 3 janvier et un du 5 octobre appartiennent au même hiver ; une
> moyenne par année civile les mélangerait en une date de juin qui n'existe nulle part.
> Les saisons se comptent depuis le **mois qui suit le plus chaud**, déduit de l'archive
> et non supposé — le dépôt est public et l'hémisphère sud a son été en janvier.
> La **date** se prend à la médiane (une année aberrante ne doit pas la déplacer),
> l'**écart** à la moyenne (cette même année doit se voir dans l'imprécision annoncée).

> [!note] Une normale qui n'en est pas ne sort pas.
> Moins de trois saisons complètes, un événement qui n'arrive pas dans 60 % des saisons
> (une neige décennale), ou une douceur qui ne s'arrête jamais — la normale tomberait
> alors au bord de la fenêtre de recherche, ce qui est le signe des tropiques. Dans les
> trois cas la valeur est absente et le fait se tait, exactement comme le lever du
> soleil au-delà du cercle polaire.

> [!warning] « L'an dernier » ne peut pas venir des tables de prévisions.
> `previsions_quotidiennes` est **remplacée à chaque heure** (`past_days=1`) et
> `releves_meteo` ne garde sept jours de brut : la maison n'a aucune mémoire du temps
> qu'il a fait. La journée d'il y a un an vient donc de l'archive, qui est **gardée en
> table** plutôt que jetée après le calcul. Les tables de [[Météo]] servent l'autre
> moitié du fait : le **maximum d'aujourd'hui**, qui transforme une température en
> comparaison. Sans lui, le fait change de phrase au lieu de se taire.

> [!warning] Le rattrapage d'une semaine doit franchir le Nouvel An.
> Une normale au 28 décembre, lue le 2 janvier, vient de passer depuis cinq jours — en
> plein dans la fenêtre de rattrapage — mais son occurrence de l'**année courante** est
> à presque douze mois. Chercher la date « cette année, sinon l'an prochain » faisait
> donc disparaître le fait précisément dans la fenêtre pour laquelle le rattrapage
> existe. C'est le même défaut que la bascule d'heure du ciel, à l'autre bout de
> l'année. Trouvé en revue de code, étape 5.

> [!warning] Un tirage maigre ne remplace pas un bon.
> Le danger n'est pas la panne, qui se voit et se réessaie : c'est le 200 maigre. Une
> réponse tronquée remplacerait dix ans d'archive par trois mois **et** poserait un
> horodatage tout neuf, qui interdit de réessayer avant 360 jours — une année de
> silence sur une ligne d'*information*. Un tirage doit couvrir 90 % de la fenêtre
> demandée et ne pas rendre moins de saisons complètes que ce qui est en base, sinon il
> est écarté et on repasse dans quelques heures. Trouvé en revue de code, étape 5.

> [!note] Les textes du climat portent leur chiffre à la fin, et le mur coupe.
> Le widget s'arrête à deux lignes : « Sur 9 saisons, la première gelée au sol s'est
> présentée à… » perdait précisément l'imprécision que la décision exige de porter. Les
> cinq textes sont écrits court et un test les borne à **soixante signes** — une règle
> d'écriture comme les trente signes de la valeur, ni pixel ni colonne. Trouvé au rendu
> de l'étape 5.

## Ancres de code

- `server/HouseOs.Api/Domaine/Ephemerides/` — `Soleil.cs`, `Lune.cs`, `Saisons.cs`,
  `ChangementHeure.cs`, assemblés par `Ciel.cs`.
- `server/HouseOs.Api/Features/FondsDeTiroir/FaitsDuCiel.cs`,
  `server/HouseOs.Api/Features/FondsDeTiroir/FaitsDuClimat.cs` — les faits.
- `server/HouseOs.Api/Domaine/Meteo/CalculDesNormales.cs` — les statistiques, pures.
- `server/HouseOs.Tests/Features/FondsDeTiroir/` — les tests des deux familles.
