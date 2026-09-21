---
type: feature
status: building
last-verified: 2026-09-20
verified-against: f4747af
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

> [!note] Bâti : tout sauf « la ville » (as of 2026-09-20).
> L'étape 2 du [[Plan 2026-09-20 Journal Éditorial]] a livré le fait, le moteur de score
> et les sept items du ciel ; l'étape 3 les huit items de la maison et les quatre du
> calendrier ; l'étape 4 les deux du hasard ; l'étape 5 les cinq du climat, avec la
> **première migration** du chantier. Reste une famille : ville (étape 6). La
> **fraîcheur** lit un historique vide jusqu'à l'étape 7.

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

La rareté se compte en **part de l'année** : l'inverse du nombre de **jours** où le fait
peut paraître (l'équinoxe, 1/4 ; la durée du jour, 1/365). La fraîcheur ne descend jamais
à zéro — le jour où un fait ressassé est la seule chose vraie qui reste, mieux vaut se
répéter qu'un trou dans le journal.

> [!warning] La rareté est un compte de jours, pas une envie.
> Beaucoup de faits de la maison et du calendrier sont vrais **tous les jours** une fois
> leur condition remplie : le doyen a toujours seize ans, la pièce est toujours négligée.
> Les coter « quelques fois par an » parce qu'on ne voudrait les lire que rarement les
> met en tête du journal **tous les matins de l'année** — la fraîcheur se remet à neuf au
> bout de sept jours et ne retient rien de plus. Ces faits sont donc quotidiens, et c'est
> la **pertinence** qui porte leur poids, sur une échelle écrite : **0,5 = bouche-trou**,
> 1 = de la décoration, 1,5 = ça éclaire la journée, 2 = ça suggère un geste,
> 3 = ça engage la journée. Trouvé en revue de code, 2026-09-20. L'échelle vit dans le
> code depuis l'étape 4 (`Pertinence`, `FaitDeTiroir.cs`) : un barème qui n'existe qu'en
> commentaire n'est pas un barème.

> [!note] Le bouche-trou est un cran **sous** la décoration.
> Un dicton d'almanach peut paraître tous les jours — sa rareté est donc celle d'un fait
> quotidien, et la rareté ne se négocie pas. Ce qui le met en queue de classement, c'est
> sa pertinence : il ne change rien à aujourd'hui, et il doit passer **derrière le fait
> le plus banal du fonds**, sans quoi il ne serait plus un bouche-trou. D'où le cran à
> 0,5. Ajouté à l'étape 4.

**La journée est-elle physique ?** La pertinence de « il fera noir à 18 h 25 » se décide
sur les **zones extérieures** : au moins une occurrence ouverte du jour dans une zone de
type `Exterieur`. C'est le seul signal que le modèle porte vraiment — il n'y a pas de
catégorie sur la tâche et il n'y en aura pas
([[D-2026-09-20 Regroupement Sans Catégorie De Tâche]]).

Un fait dont une source manque **ne sort pas** ; il ne casse jamais la composition. Au
delà des cercles polaires il n'y a ni lever ni coucher certains jours, et beaucoup de
fuseaux n'ont pas de changement d'heure : ce sont des absences normales.

La même règle vaut **à l'échelle de la famille** : chaque famille porte son matériau
dans `ContexteDuJour`, et il est facultatif. Sans coordonnées le ciel se tait, sans
journal de complétion la maison se tait, sans calendrier le calendrier se tait — un
foyer qui n'a pas rempli `METEO_LATITUDE` garde tout le reste de son journal.

> [!warning] Les noms saisis par le foyer vivent dans le **texte long**.
> L'invariant « le texte ne redit jamais l'étiquette » se compare sans distinction de
> casse. Une pièce nommée « Aucune » ferait donc mentir un texte qui commence par
> « Aucune tâche n'y a été cochée… ». Les faits qui parlent d'une zone, d'une tâche ou
> d'un équipement portent une **étiquette fixe** et une **valeur chiffrée** ; le nom
> passe au texte. Seul `calendrier.compte-a-rebours` garde son titre en étiquette —
> c'est son sujet, et « DANS 16 JOURS » tout seul ne dit rien. Trouvé à l'étape 3.

### Les six familles

| Famille | Ce qu'elle donne | Dépendance |
|---|---|---|
| **Le ciel** ✅ | lever, coucher, durée du jour, dérive quotidienne, phase lunaire, équinoxes et solstices, changement d'heure, bascule jour/nuit, « il fera noir à » | calcul local depuis `METEO_LATITUDE`/`METEO_LONGITUDE` — aucune |
| **Le climat** ✅ | premier gel, première neige, dernière journée à 20°, mois le plus sec ou le plus arrosé, « il a fait X° ce jour-là l'an dernier » | archive ERA5 matérialisée + le maximum du jour des tables de [[Météo]] |
| **La maison** ✅ | ce jour-là l'an dernier, série en cours et record, N séances depuis, plus vieil équipement, zone la plus négligée, coût de l'année | journal de complétion — **ne donne rien la première année** |
| **Le calendrier** ✅ | compte à rebours, ça s'en vient (7–30 j), travaux de la saison, garantie qui expire | [[Comptes À Rebours]], occurrences, fenêtres saisonnières, [[Documents]] |
| **La ville** | prochaine collecte, collecte spéciale, événement municipal | [[Flux Externes]] — ICS pour les collectes, flux poussé pour les événements |
| **Le hasard** ✅ | dicton de l'almanach, fête ou journée nationale | fichier de données remplaçable — aucune |

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

### Le climat, en détail (as of 2026-09-20)

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

### La maison, en détail (as of 2026-09-20)

Les seuils sont éditoriaux, pas techniques : ils disent à partir de quand une chose
vraie devient une chose qu'on a envie de lire.

| Clé | Rareté | Pertinence | Ne sort que si |
|---|---|---|---|
| `maison.serie` | tous les jours | 1,5 | trois jours d'affilée au moins, et le fait `maison.record` ne parle pas (donc : série < record, **ou** série trop courte pour être un record) |
| `maison.record` | 6×/an | 2 | la série en cours **égale ou dépasse** le record, à partir de cinq jours |
| `maison.seances` | tous les jours | 1,5 · **3** aux dizaines | une tâche a été cochée dix fois ou plus |
| `maison.doyen` | tous les jours | 1,5 · **3** si l'entretien est dans la quinzaine | le plus vieil équipement daté a au moins un an |
| `maison.piece-oubliee` | tous les jours | 2 · **3** passé six mois | une zone **qui a des tâches** n'a rien eu de coché depuis soixante jours (ou jamais) |
| `maison.cout` | tous les jours | 1,5 | au moins un coût consigné depuis le 1er janvier |
| `maison.anniversaire` | 12×/an | 2 | un équipement ou un jalon du foyer a son mois-jour aujourd'hui, et au moins un an |
| `maison.an-dernier` | 180×/an | 1,5 | quelque chose a été coché un an jour pour jour avant aujourd'hui |

> [!warning] Deux seuils qui se croisent font un trou, et il faut les croiser exprès.
> La série se tait quand elle **est** le record (deux colonnes pour le même chiffre,
> c'est une colonne perdue) et le record ne parle qu'**à partir de cinq jours**. Pris
> séparément, les deux seuils laissaient muette une série de trois ou quatre jours qui
> est aussi le record — c'est-à-dire **la première série d'une maison neuve**, le moment
> précis que ce fait existe pour raconter. La série ne se tait donc que lorsque le record
> parle vraiment, et elle change de texte quand elle est le meilleur résultat à ce jour,
> plutôt que de citer un record égal au chiffre qu'elle affiche déjà. Trouvé en revue de
> code, étape 4.

> [!note] La série se compte **jusqu'à hier** quand la journée n'a rien donné.
> À six heures du matin rien n'est encore fait. Exiger une complétion du jour ferait
> annoncer « série rompue » chaque matin et « douze jours » chaque soir — un journal qui
> se contredit entre deux réveils. La série se casse à la fin de la journée, pas à son
> premier café.

### Le calendrier, en détail (as of 2026-09-20)

| Clé | Rareté | Pertinence | Ne sort que si |
|---|---|---|---|
| `calendrier.compte-a-rebours` | tous les jours | 1,5 · **3** la dernière semaine | la cible est devant et à moins de cent vingt jours |
| `calendrier.ca-s-en-vient` | tous les jours | 1,5 | **au moins deux** échéances ouvertes entre 7 et 30 jours — en deçà de sept, la liste du jour les montre déjà |
| `calendrier.saison` | 56×/an (fermeture), 28×/an (ouverture) | 2 · 1 | une fenêtre saisonnière se referme dans la quinzaine, sinon une qui s'est ouverte dans la semaine |
| `calendrier.expiration` | 60×/an | 1,5 · **3** dans la quinzaine | une garantie d'équipement ou l'échéance d'un document tombe dans les soixante jours |

> [!warning] Le compte à rebours sort **deux fois** si le consommateur n'y prend garde.
> [[Journal De La Maison]] dessine déjà son encadré, et le fonds ne sait pas qu'un
> encadré existe — c'est la décision, et la lettre du matin voudra le fait. C'est donc
> au consommateur d'écarter le doublon : `CLES_DEJA_AU_JOURNAL`
> (`web/src/lib/ecran-vues.ts`), testé. Trouvé à l'étape 3.

### Le hasard, en détail (as of 2026-09-20)

Deux items, et rien qui vienne d'un calcul ou d'une table : la matière est un **fichier
de données remplaçable** ([[D-2026-09-20 Banque Du Hasard En Fichier De Données]]).
L'app en livre une version québécoise ; `HASARD_FICHIER` la remplace.

| Clé | Rareté | Pertinence | Ne sort que si |
|---|---|---|---|
| `hasard.fete` | le nombre de fêtes **de la banque** (20 dans celle du Québec) | 2 si c'est un jour chômé, sinon 1,5 | une fête de la banque tombe aujourd'hui |
| `hasard.dicton` | tous les jours | **0,5** (bouche-trou) | la banque a au moins un dicton pour le mois |

La rareté de la fête **se compte dans la banque elle-même** : « combien de jours par
année le fait peut paraître » est exactement le nombre d'entrées. Un foyer qui n'inscrit
que ses huit jours chômés obtient un fait deux fois plus rare que celui qui en inscrit
vingt — et c'est exact, là où un nombre écrit en dur aurait menti pour l'un des deux.

Le dicton du jour est choisi par le **quantième**, dans la liste du mois : figé pour la
journée (comme tout `ContexteDuJour`), différent le lendemain. Rien d'aléatoire, malgré
le nom de la famille — un journal qui change de dicton entre deux réveils se contredirait
tout seul.

> [!note] Le dicton est le seul fait dont la matière vit dans le **texte long**.
> Un proverbe n'a pas de chiffre à mettre en valeur, et il ne tient pas dans les trente
> signes de la forme courte : c'est donc l'étiquette qui dit « Le dicton », la valeur qui
> dit le mois, et le texte qui porte le proverbe. Ça tombe bien — un bouche-trou ne
> paraît que lorsque le journal a de la place, donc lorsqu'il montre les textes longs.
> **Relevé au rendu de l'étape 4** : la hiérarchie typographique du widget s'en trouve
> inversée (le mois en gros, le proverbe en petit). C'est vivable et c'est le seul
> découpage qui ne coupe pas le proverbe ; à revoir avec l'éditorialiste (étape 7), pas
> avant.

> [!warning] Les fêtes mobiles sont la moitié des jours fériés du Québec.
> Pâques et ses deux congés, la Journée des patriotes, la fête du Travail, l'Action de
> grâce : une banque de dates fixes serait fausse quatre jours par an. Une fête déclare
> donc **quand elle tombe** sous l'une de quatre formes déclaratives — date fixe ;
> n-ième jour de semaine du mois (rang négatif = depuis la fin) ; dernier jour de semaine
> **avant** une date (« le lundi qui précède le 25 mai », strictement avant, même quand le
> 25 est lui-même un lundi) ; décalage en jours depuis Pâques. Le comput grégorien est
> écrit à la main, comme les éphémérides.

### Les sources

- **Éphémérides** : formules NOAA écrites à la main dans `Domaine/Ephemerides/`, aucune
  dépendance NuGet. Pures, testables, aucun appel réseau. L'ancrage extérieur des tests
  est l'**instant publié des équinoxes et solstices** (2024 et 2025, à un quart d'heure
  près) : il valide d'un coup la longitude du soleil, donc la déclinaison dont dépendent
  tous les levers et couchers. Le reste est vérifié sur des invariants de physique et
  sur trois points du globe. Le changement d'heure vient de **tzdata**
  (`TimeZoneInfo`), jamais d'une règle écrite à la main.
- **Normales climatiques** ✅ : un tirage de l'archive Open-Meteo (ERA5) pour les
  coordonnées du `.env`, matérialisé
  ([[D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo]]). Le tirage se
  déclenche **à l'âge et non à date fixe** — au démarrage puis toutes les six heures,
  le worker ne tire que si les normales manquent, portent une autre clé, ou ont plus de
  360 jours. Une date au calendrier serait ratée chaque année où la machine est éteinte
  ce jour-là, et ferait attendre le déménagement jusqu'au prochain anniversaire ; 360 et
  non 365 parce que l'archive s'arrête quelques jours avant aujourd'hui et qu'une
  fenêtre pile d'un an ouvrirait un trou d'une semaine dans « l'an dernier ».
  Les deux tables **portent les coordonnées du calcul** : changer le `.env` les invalide
  et fait tout repartir. Référence : `docs/configuration.md`.
- **La maison** et **le calendrier** ✅ : lectures du journal de complétion, des
  [[Équipements]], des zones ([[D-2026-08-23 Zones Plates]]), des
  [[Comptes À Rebours]], des occurrences et des [[Documents]] — **rien de neuf en
  base**, aucune migration. Les fenêtres saisonnières du moteur de récurrence
  ([[Tâches]]) sont exposées comme deux dates par `SpecRecurrence.FenetreAutour`.
- **Le hasard** ✅ : un fichier JSON de données, lu **une fois au démarrage**
  ([[D-2026-09-20 Banque Du Hasard En Fichier De Données]]). L'app en livre une version
  québécoise ; `HASARD_FICHIER` (`Hasard:Fichier`) la remplace, et un chemin réglé mais
  illisible fait **taire** la famille au lieu de retomber sur celle du Québec.
  Référence : `docs/configuration.md`.
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
- [[D-2026-09-20 Banque Du Hasard En Fichier De Données]] — dictons et fêtes en fichier
  remplaçable, quatre formes de date déclaratives, jamais de table en dur.
- [[D-2026-08-23 Pas De N8n Dans Le Cœur]] — les règles sont des classes C# testables.

## Ancres de code

Bâties :

- `server/HouseOs.Api/Domaine/Ephemerides/` — `Soleil.cs`, `Lune.cs`, `Saisons.cs`,
  `ChangementHeure.cs`, assemblés par `Ciel.cs`. Pur calcul, aucune base.
- `server/HouseOs.Api/Domaine/Meteo/` — `JourDeClimat.cs` et `NormalesClimatiques.cs`
  (les deux tables, migration `AjouterNormalesClimatiques`) et `CalculDesNormales.cs`
  (les statistiques, pur et sans base). Ingestion :
  `Features/Meteo/OpenMeteoArchive.cs` (seul endroit qui connaît la forme de l'API
  archive), `OperationsNormales.cs` (la clé et le remplacement) et
  `NormalesIngestionService.cs` (le worker).
- `server/HouseOs.Api/Features/FondsDeTiroir/` — `FaitDeTiroir.cs` (le contrat),
  `Tiroir.cs` (le score), `HistoriqueDeParution.cs` (la fraîcheur),
  `ContexteDuJour.cs` (le matériau de chaque famille), `Mots.cs` (les tournures
  partagées), `FaitsDuCiel.cs`, `EtatDeLaMaison.cs` + `FaitsDeLaMaison.cs`,
  `EtatDuCalendrier.cs` + `FaitsDuCalendrier.cs`, `BanqueDuHasard.cs` +
  `LectureDeLaBanque.cs` + `HasardOptions.cs` + `FaitsDuHasard.cs` et la banque livrée
  `banque-du-hasard.qc.json`, `EtatDuClimat.cs` + `FaitsDuClimat.cs`. Aucune référence
  à l'affichage.
- `FaitDeTiroir.cs` — `Rarete` et `Pertinence` : les deux barèmes du score, écrits dans
  le code et non seulement en commentaire, depuis l'étape 4.
- `server/HouseOs.Api/Domaine/SpecRecurrence.cs` — `FenetreAutour` : la fenêtre
  saisonnière lue comme deux dates plutôt que comme quatre nombres.
- Côté consommateur : `ComposerDonneesEcran.cs` (`ReglagesDuCiel`, `FaitEcranDto`,
  `LireLaMaisonAsync`, `LireLeCalendrierAsync`, `LireLeClimatAsync`) et `web/src/lib/ecran-vues.ts`
  (`formeDuCiel`, `rangeesDuCiel`, `faitsEnWidgets`, `faitAvecTexteLong`,
  `placesDuFonds`) — c'est là, et nulle part ailleurs, que la densité et l'ordre des
  widgets se décident. **Le journal ne retrie jamais le fonds** : il replie le ciel en
  un bloc d'une seule place et coupe au budget du rang.

À créer :

- `server/HouseOs.Api/Features/FluxExternes/` — la source poussée.

## Sources

- [[Éditorialiste De L'Écran]] — la direction retenue, la table du fonds de tiroir, les
  sources municipales vérifiées au curl.
- `design/maquettes/une-editorialiste.html` — le fonds de tiroir au complet, par item.

## Historique

- [[Plan 2026-09-20 Journal Éditorial]]
