---
type: feature
status: implemented
last-verified: 2026-09-30
verified-against: 6d1fda0
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
[[Lettre Du Matin]] (courriel sortant, spec 2026-09-21).

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
- **fraîcheur** — pénalité pour **chaque** parution des sept derniers jours, qui
  remonte au carré de son âge : ~4 % au lendemain, et deux parutions se multiplient.
  C'est ce qui crée la surprise quotidienne ; elle se lit dans les clés publiées par
  les éditions précédentes ([[D-2026-09-20 Une Édition Par Jour Matérialisée]],
  [[D-2026-09-28 Fraîcheur Cumulée Des Faits]]).
- **pertinence du jour** — est-ce que le fait change quelque chose à aujourd'hui.
  « Le soleil se couche à 18 h 25 » est de la décoration un mardi ordinaire et une
  consigne le jour du déménagement. Une pertinence nulle fait **disparaître** le fait :
  c'est comme ça qu'un fait contextuel se tait les jours ordinaires.

La rareté se compte en **part de l'année** : l'inverse du nombre de **jours** où le fait
peut paraître (l'équinoxe, 1/4 ; la durée du jour, 1/365). La fraîcheur ne descend jamais
à zéro — le jour où un fait ressassé est la seule chose vraie qui reste, mieux vaut se
répéter qu'un trou dans le journal.

> [!warning] La fraîcheur doit pouvoir battre la rareté.
> Une fenêtre de trente-huit jours (le premier gel) vaut dix fois un fait quotidien.
> Avec l'ancienne pénalité — le seul dernier jour, linéaire, plancher à 0,15 — un fait
> sorti hier gardait encore 27 % de son score, et le gel, la douceur et la collecte
> spéciale sont sortis **huit matins sur huit** la semaine du 21 septembre 2026, pendant
> que le dicton, la durée du jour et « l'an dernier » ne paraissaient jamais. Rejoué sur
> la même semaine, la règle cumulée fait sortir les faits à fenêtre un jour sur deux et
> tourne le reste. Relevé à la lecture du mur, 2026-09-28.

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
| **La ville** ✅ | prochaine collecte, collecte spéciale, événement municipal | [[Flux Externes]] — ICS pour les collectes, flux poussé pour les événements |
| **Le hasard** ✅ | dicton de l'almanach, fête ou journée nationale | fichier de données remplaçable — aucune |

Détail par item, avec source et rareté : la table du fonds de tiroir dans
`design/maquettes/une-editorialiste.html`. ✅ = famille branchée.

Les règles propres à chaque famille — seuils, pièges trouvés en chemin, ce qui fait
taire un fait — vivent dans deux sous-notes :

- [[Fonds De Tiroir Ciel Et Climat]] — ce qui se calcule du dehors : éphémérides et
  normales climatiques.
- [[Fonds De Tiroir Maison Calendrier Ville Et Hasard]] — ce qui se lit dans les
  données du foyer, les flux de la ville et la banque du hasard.

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
- **La ville** ✅ : rien de municipal dans le code
  ([[D-2026-09-20 Sources Municipales Séparées Par Solidité]]). Les collectes entrent
  par un ICS régénéré à la main une fois l'an
  ([[D-2026-09-20 Calendrier De Collectes Régénéré À La Main]]) ; les événements par un
  flux externe poussé ([[D-2026-09-20 Flux Externe Poussé]]). Le convertisseur PDF → ICS
  et le gratteur vivent **hors du dépôt**, avec leur recette dans `CLAUDE.local.md`.
  Aucune table neuve : la famille lit les tables de [[Flux Externes]].

## Hors périmètre

- Toute mise en forme : grille, colonnes, budget, lettrine, 1-bit — c'est
  [[Journal De La Maison]].
- Les **actualités** municipales (8 par an, sans date sur la page de liste) — reportées.
- Une **catégorie ou étiquette** sur la tâche
  ([[D-2026-09-20 Regroupement Sans Catégorie De Tâche]]).
- Un gratteur, un analyseur PDF ou un client municipal dans le dépôt.
- La lettre du matin et l'envoi de courriel sortant — [[Lettre Du Matin]].

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
- [[D-2026-09-28 Fraîcheur Cumulée Des Faits]] — chaque parution de la semaine pèse,
  au carré de son âge ; la fraîcheur peut battre la rareté.
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
  `banque-du-hasard.qc.json`, `EtatDuClimat.cs` + `FaitsDuClimat.cs`,
  `EtatDeLaVille.cs` + `FaitsDeLaVille.cs`. Aucune référence à l'affichage.
- `FaitDeTiroir.cs` — `Rarete` et `Pertinence` : les deux barèmes du score, écrits dans
  le code et non seulement en commentaire, depuis l'étape 4.
- `server/HouseOs.Api/Domaine/SpecRecurrence.cs` — `FenetreAutour` : la fenêtre
  saisonnière lue comme deux dates plutôt que comme quatre nombres.
- Côté consommateur : `ComposerDonneesEcran.cs` (`ReglagesDuCiel`, `FaitEcranDto`,
  `LireLaMaisonAsync`, `LireLeCalendrierAsync`, `LireLeClimatAsync`) et `web/src/lib/ecran-vues.ts`
  (`formeDuCiel`, `rangeesDuCiel`, `faitsEnWidgets`, `faitAvecTexteLong`,
  `placesDuFonds`, `CLES_DEJA_AU_JOURNAL`) — c'est là, et nulle part ailleurs, que la densité et l'ordre des
  widgets se décident. **Le journal ne retrie jamais le fonds** : il replie le ciel en
  un bloc d'une seule place et coupe au budget du rang.

- `server/HouseOs.Api/Features/FluxExternes/` — la source poussée qui alimente « la
  ville » (migration `AjouterFluxExternePousse`), et `ComposerDonneesEcran.LireLaVilleAsync`
  qui en tire l'âge de chaque flux : le fonds ne lit pas l'horloge.

## Sources

- [[Éditorialiste De L'Écran]] — la direction retenue, la table du fonds de tiroir, les
  sources municipales vérifiées au curl.
- `design/maquettes/une-editorialiste.html` — le fonds de tiroir au complet, par item.

## Historique

- [[Plan 2026-09-20 Journal Éditorial]] · [[Recap Fonds De Tiroir]]
