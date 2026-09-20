---
type: feature
status: building
last-verified: 2026-09-20
verified-against: c8c11b9
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

> [!note] Bâti : le contrat et la famille « le ciel » (as of 2026-09-20).
> L'étape 2 du [[Plan 2026-09-20 Journal Éditorial]] a livré le fait, le moteur de score
> et les sept items du ciel. Les cinq autres familles sont encore un contrat à
> construire : climat (étape 5), maison et calendrier (étape 3), ville (étape 6),
> hasard (étape 4). La **fraîcheur** lit un historique vide jusqu'à l'étape 7.

## Comportement

### Le contrat

Le fonds de tiroir rend une **liste de faits ordonnés par score**. Un fait porte :

- une **clé stable** (c'est elle qui permet la pénalité de fraîcheur d'un jour à l'autre) ;
- sa **famille** (le ciel, le climat, la maison, le calendrier, la ville, le hasard) ;
- une **étiquette** et une **valeur** courtes, et un **texte long** — le consommateur
  choisit ce qu'il a la place de montrer. Les widgets rétrécissent avant de disparaître ;
- ses **composantes de score** : rareté, fraîcheur, pertinence du jour, gardées
  séparées plutôt que multipliées d'avance — quand un fait sort ou ne sort pas, on veut
  pouvoir dire lequel des trois a tranché.

La **valeur courte** obéit à une règle apprise au rendu : **l'étiquette porte le sujet,
la valeur porte le chiffre** (« Équinoxe de septembre » / « Dans 2 jours », comme
l'encadré du compte à rebours). Une valeur de quarante signes se fait couper dans une
colonne de widget ; un test balaie une année entière et refuse plus de trente signes.

Le **texte long doit ajouter quelque chose**. L'étiquette et la valeur disent déjà
l'essentiel ; « LE JOUR RACCOURCIT / 3 min par jour / Le jour raccourcit d'environ trois
minutes par jour » coûte trois lignes de mur pour une seule idée. Un second test refuse
tout texte long qui contient l'étiquette ou la valeur — c'est ce qui a fait passer la
dérive du jour à la **semaine** (« 23 minutes de moins qu'il y a une semaine »), qui est
l'écart qu'on sent vraiment.

Il ne connaît **ni pixel, ni colonne, ni 1-bit**
([[D-2026-09-20 Fonds De Tiroir Séparé Du Journal]]) : le budget de widgets, les rangs
et la lettrine appartiennent au journal.

### Comment un fait gagne sa place

`score = rareté × fraîcheur × pertinence du jour`

- **rareté** — combien de fois par année l'item peut paraître (l'équinoxe ou le
  solstice, 4 fois ; la durée du jour, tous les jours).
- **fraîcheur** — pénalité s'il est sorti récemment. C'est ce qui crée la surprise
  quotidienne ; elle se lit dans les clés publiées par les éditions précédentes
  ([[D-2026-09-20 Une Édition Par Jour Matérialisée]]).
- **pertinence du jour** — est-ce que le fait change quelque chose à aujourd'hui.
  « Le soleil se couche à 18 h 25 » est de la décoration un mardi ordinaire et une
  consigne le jour du déménagement. Une pertinence nulle fait **disparaître** le fait :
  c'est comme ça qu'un fait contextuel se tait les jours ordinaires.

La rareté se compte en **part de l'année** : l'inverse du nombre de parutions possibles
(l'équinoxe, 1/4 ; la durée du jour, 1/365). La fraîcheur ne descend jamais à zéro — le
jour où un fait ressassé est la seule chose vraie qui reste, mieux vaut se répéter qu'un
trou dans le journal.

**La journée est-elle physique ?** La pertinence de « il fera noir à 18 h 25 » se décide
sur les **zones extérieures** : au moins une occurrence ouverte du jour dans une zone de
type `Exterieur`. C'est le seul signal que le modèle porte vraiment — il n'y a pas de
catégorie sur la tâche et il n'y en aura pas
([[D-2026-09-20 Regroupement Sans Catégorie De Tâche]]).

Un fait dont une source manque **ne sort pas** ; il ne casse jamais la composition. Au
delà des cercles polaires il n'y a ni lever ni coucher certains jours, et beaucoup de
fuseaux n'ont pas de changement d'heure : ce sont des absences normales.

### Les six familles

| Famille | Ce qu'elle donne | Dépendance |
|---|---|---|
| **Le ciel** ✅ | lever, coucher, durée du jour, dérive quotidienne, phase lunaire, équinoxes et solstices, changement d'heure, bascule jour/nuit, « il fera noir à » | calcul local depuis `METEO_LATITUDE`/`METEO_LONGITUDE` — aucune |
| **Le climat** | premier gel, première neige, dernière journée à 20°, « il a fait X° ce jour-là l'an dernier » | normales matérialisées + tables de [[Météo]] |
| **La maison** | ce jour-là l'an dernier, série en cours et record, N séances depuis, plus vieil équipement, zone la plus négligée, coût de l'année | journal de complétion — **ne donne rien la première année** |
| **Le calendrier** | compte à rebours, ça s'en vient (7–30 j), travaux de la saison, garantie qui expire | [[Comptes À Rebours]], occurrences, fenêtres saisonnières, [[Documents]] |
| **La ville** | prochaine collecte, collecte spéciale, événement municipal | [[Flux Externes]] — ICS pour les collectes, flux poussé pour les événements |
| **Le hasard** | dicton météo québécois, fête ou journée nationale | banque locale |

Détail par item, avec source et rareté : la table du fonds de tiroir dans
`design/maquettes/une-editorialiste.html`. ✅ = famille branchée.

### Le ciel, en détail (as of 2026-09-20)

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

### Les sources

- **Éphémérides** : formules NOAA écrites à la main dans `Domaine/Ephemerides/`, aucune
  dépendance NuGet. Pures, testables, aucun appel réseau. L'ancrage extérieur des tests
  est l'**instant publié des équinoxes et solstices** (2024 et 2025, à un quart d'heure
  près) : il valide d'un coup la longitude du soleil, donc la déclinaison dont dépendent
  tous les levers et couchers. Le reste est vérifié sur des invariants de physique et
  sur trois points du globe. Le changement d'heure vient de **tzdata**
  (`TimeZoneInfo`), jamais d'une règle écrite à la main.
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

Bâties :

- `server/HouseOs.Api/Domaine/Ephemerides/` — `Soleil.cs`, `Lune.cs`, `Saisons.cs`,
  `ChangementHeure.cs`, assemblés par `Ciel.cs`. Pur calcul, aucune base.
- `server/HouseOs.Api/Features/FondsDeTiroir/` — `FaitDeTiroir.cs` (le contrat),
  `Tiroir.cs` (le score), `HistoriqueDeParution.cs` (la fraîcheur),
  `ContexteDuJour.cs`, `FaitsDuCiel.cs`. Aucune référence à l'affichage.
- Côté consommateur : `ComposerDonneesEcran.cs` (`ReglagesDuCiel`, `FaitEcranDto`) et
  `web/src/lib/ecran-vues.ts` (`formeDuCiel`, `rangeesDuCiel`) — c'est là, et nulle
  part ailleurs, que la densité se décide.

À créer :

- `server/HouseOs.Api/Features/Meteo/` — l'ingestion des normales rejoint l'existant.
- `server/HouseOs.Api/Features/FluxExternes/` — la source poussée.

## Sources

- [[Éditorialiste De L'Écran]] — la direction retenue, la table du fonds de tiroir, les
  sources municipales vérifiées au curl.
- `design/maquettes/une-editorialiste.html` — le fonds de tiroir au complet, par item.

## Historique

- [[Plan 2026-09-20 Journal Éditorial]]
