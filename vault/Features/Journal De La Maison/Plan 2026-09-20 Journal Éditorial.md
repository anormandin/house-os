---
type: plan
status: approved
date: 2026-09-20
feature: "[[Journal De La Maison]]"
---

# Plan 2026-09-20 Journal Éditorial

## But

Transformer la vue `/ecran` — aujourd'hui la liste + zones de [[Affichage E-ink]] — en
la broadsheet à densité variable de [[Éditorialiste De L'Écran]], alimentée par
[[Fonds De Tiroir]] et écrite par un éditorialiste LLM.

Plan commun aux deux features : les étapes 2 à 6 construisent [[Fonds De Tiroir]], les
étapes 1, 7 et 8 construisent [[Journal De La Maison]].

## Séquencement

La bascule se fait **par remplacement direct, étape par étape** (choix d'Alain en
grillage, 2026-09-20) : pas de paramètre `?vue=`, pas de seconde route. Conséquence
assumée — **chaque étape doit laisser le mur dans un état livrable**, jamais un
intermédiaire. Deux garde-fous :

- l'aperçu existe déjà : `GET /api/affichage/apercu.png?largeur=1872&hauteur=1404`
  (cookie de session) rend le PNG exact que l'appareil recevrait, seuillage compris ;
- la voie de retour est `git revert` d'une étape, pas une variable de configuration.

L'ordre va du **plus visible et moins risqué** (la grille, avec les données qu'on a
déjà) au **plus profond** (l'éditorialiste, qui a besoin que les gabarits soient déjà
bons pour servir de repli).

## Étapes

### 1 — La broadsheet, avec les données d'aujourd'hui

Aucune donnée neuve : on remet en page ce que `ComposerDonneesEcran` sert déjà. Le
[[Titre D'humeur]] tient lieu de manchette jusqu'à l'étape 7.

- [x] Choix de rang comme fonction pure dans `web/src/lib/ecran-vues.ts` : entrée =
      tâches dues, plancher déclenché, budget ; sortie = grille du corps et nombre de
      widgets. Tests unitaires sur les six rangs du tableau de bascule.
- [x] Plancher (retard > 3 jours, compte à rebours à zéro, échéance ferme) comme
      fonction pure, testée séparément du rang.
- [x] Réécriture de `web/src/pages/Ecran.tsx` : bloc-titre, manchette avec lettrine,
      trois colonnes aux filets, encadré de compte à rebours, pied. Classes partagées
      entre les rangs — la densité change, pas la grammaire.
- [x] Le `Rangee` actuel devient la liste du jour dans la colonne du milieu ; météo,
      collecte et compte à rebours passent en widgets de la colonne de droite.
- [x] `ComposerDonneesEcran.cs` : lever le plafond `MaxLignes = 10` (mesuré à ~27 avant
      saturation), et exposer ce qu'il faut au plancher (retard en jours, échéance
      ferme). Tests de composition mis à jour.
- [x] **Garde anti-débordement** (le bug du 2026-09-20 se jouait à 50 px) : mesurer la
      hauteur de contenu au rendu et journaliser un avertissement Serilog quand elle
      dépasse la hauteur disponible — visible dans Seq ([[Observabilité]]).

**Vérification** — `npm test` dans `web/` (189 verts au départ) ; `dotnet test` ;
`apercu.png` à 1872×1404 sur une journée à 0, 1, 3 et 14 tâches (données de dev) ;
aucun avertissement de débordement dans le log.

#### Étapes correctives de l'étape 1 (ajoutées au rendu, 2026-09-20)

Ce que les six aperçus ont révélé et qui n'était pas prévu :

- [x] **`scrollHeight` ne voit pas le débordement.** Dans une grille en
      `overflow-hidden`, un bloc trop haut déborde sans agrandir la boîte : la garde
      ne détectait rien alors que le pied était recouvert. Remplacée par la mesure du
      plus bas des éléments (`debordementPx`, `Ecran.tsx`), et les colonnes clippent
      désormais au lieu de peindre par-dessus le pied.
- [x] **La manchette n'a pas de longueur bornée.** Tant que l'éditorialiste n'écrit
      pas, elle peut être un titre de tâche de 60 caractères : à 116 px il mangeait
      les deux tiers du mur. `manchetteDuJour` choisit la taille sur la longueur
      autant que sur le rang, et retire la lettrine passé 55 caractères.
- [x] **Le rang « événement » effaçait la journée.** Il forçait une seule colonne de
      liste : neuf tâches dues devenaient une. Il hérite maintenant des colonnes et du
      chapeau de la charge — le plancher impose la manchette, pas l'oubli du reste.
- [x] **Le sommaire perdait le compte à rebours.** À trois colonnes de liste, l'aparté
      disparaît et « 16 dodos » avec lui, le jour le plus chargé de l'année. Le compte
      à rebours descend maintenant en tête de la bande de pied.
- [x] **Deux tâches coupées en silence au sommaire.** Le plafond serveur (27) est celui
      de la donnée, pas celui du papier : `capaciteListe` calcule ce que la colonne
      peut montrer (le chapeau coûte une rangée) et le reste est annoncé.
- [x] **« + N autres » comptait des tâches faites.** Une journée à quatre choses
      annonçait « + 17 autres » parce que dix-sept étaient cochées. `resteAAnnoncer`
      ne compte que ce qui reste à faire.
- [x] **Bug pré-existant : le cercle d'assigné était dessiné vide.** Une tâche que
      personne ne porte affichait une pastille muette qui coûtait ~60 px de titre.
      Supprimée quand il n'y a ni assigné ni complétion.
- [x] Compression de rangée conditionnelle (`rangeeSerree`) : on ne tronque plus des
      titres pour de la place qu'on n'utilise pas.
- [x] **Le bloc-titre n'était pas celui des maquettes** (relevé par Alain au rendu,
      2026-09-20). Trois écarts, dont deux de fond. La forme : les **oreilles** (une
      seule ligne à gauche au lieu de trois — édition, lieu, numéro), le **nom** en
      bas-de-casse à 88 px et espacé au lieu de capitales à 126 px resserrées, la
      **dateline** en 60 px sans centre. Le fond : le **lieu de publication** est propre
      au foyer et n'existait nulle part (`Affichage:Lieu` / `MAISON_LIEU`, vide par
      défaut — [[Distribution]]) ; et l'**état du jour** (« Rien au programme »)
      appartient à la dateline, pas au surtitre de la manchette. Dans les six maquettes,
      le surtitre du corps est une ligne **éditoriale** (« Le condo est vendu depuis le
      1er septembre ») : le mettre au même endroit que le compte du jour effaçait la
      place que l'étape 7 doit remplir. `etatDuJour` (dateline) et `surtitreManchette`
      (plancher seulement) sont maintenant deux choses distinctes.
- [x] **Numéro d'édition** : une par jour depuis la première entrée du journal de
      complétion (`ComposerDonneesEcran.NumeroEdition`), nul sur une installation neuve.
      À l'étape 7 le compte d'éditions matérialisées le remplace sans rien changer au
      rendu. Choix d'Alain, 2026-09-20.
- [x] **La météo du bloc-titre se dit en mots** (`meteoEnMots`, `libelleMeteo`) au lieu
      d'une icône de 52 px : à trois mètres et en 1-bit, un mot se lit plus vite qu'un
      pictogramme. Le maximum ne sort que s'il reste à venir.

> [!warning] Trouvé à l'étape 1, non résolu : « échéance ferme ».
> Le plancher devait couvrir trois cas. Deux sont calculables (retard > 3 jours,
> compte à rebours à zéro). Le troisième — **l'échéance ferme** (notaire, livraison
> payée, date légale) — **n'a aucune représentation dans le modèle** : `Tache` et
> `Occurrence` ne distinguent pas une échéance négociable d'une date imposée du
> dehors. Livré sans, et à trancher par une décision avant l'étape 7, où
> l'éditorialiste devra savoir ce qu'il n'a pas le droit de reléguer.

> [!note] Tranché le 2026-09-20, avant l'étape 2.
> [[D-2026-09-20 Échéance Ferme Explicite Sur La Tâche]] : un booléen `EcheanceFerme`
> sur `Tache`, coché à la main, porté jusqu'au DTO de l'écran — aucune déduction. Bâti
> à l'**étape 7**, dans la même migration que l'entité d'édition. Le plancher tourne
> donc sur deux cas jusque-là, et c'est voulu.

**Rendu vérifié** (aperçus à 1872×1404, `apercu.png`) : rang « événement » (9 dues,
plancher à 20 jours de retard), rang « chronique » (0 due, 9 faites), rang « sommaire »
(14 dues + 9 faites, « + 2 autres » annoncées, widgets en bande de pied), rang
« resserré » (4 dues). Aucun avertissement de débordement dans le log sur les six
tirages. `npm test` 200 verts (189 au départ), `dotnet test` 663 verts.

**Livré en prod le 2026-09-20** (commit `8d7b8cf`, LXC 105) : `/api/sante` 200, nouveau
bundle, et surtout le **tirage de l'appareil réel confirmé** — `GET /api/display` 200 en
594 ms, **zéro avertissement de débordement** sur les vraies données de prod. C'est la
seule vérification que l'aperçu de dev ne pouvait pas donner : la prod a ses vraies
longueurs de titres. Le seul `WRN` du log est une dette EF Core sans rapport, consignée
dans [[Tâches]] (« Dette connue »).

### 2 — Le ciel : éphémérides et contrat de widget

Première famille du fonds de tiroir, et celle qui fait naître le contrat. Pur calcul,
aucune dépendance réseau, aucun schéma.

- [x] `server/HouseOs.Api/Domaine/Ephemerides/` : lever, coucher, midi solaire, durée du
      jour, dérive quotidienne, phase lunaire, équinoxes et solstices, changement
      d'heure. Formules NOAA écrites à la main, aucun NuGet. Quatre fichiers purs —
      `Soleil.cs`, `Lune.cs`, `Saisons.cs`, `ChangementHeure.cs` — assemblés par
      `Ciel.cs`. Le changement d'heure se lit dans **tzdata** (`TimeZoneInfo`) et non
      dans une règle écrite à la main : un foyer à Phoenix n'en a pas, et c'est une
      absence normale.
- [x] Tests contre des valeurs connues pour Québec **et** un second point : trois points
      en fait — Québec, **Hobart** (hémisphère sud) et **Longyearbyen** (au-delà du
      cercle polaire, où le lever et le coucher n'existent pas certains jours)
      ([[Distribution]]).
- [x] `server/HouseOs.Api/Features/FondsDeTiroir/` : le fait (clé stable, famille,
      étiquette, valeur courte, texte long, composantes de score) et le moteur de score
      `rareté × fraîcheur × pertinence` (`Tiroir.cs`). La fraîcheur lit un historique
      **vide** à cette étape — elle se branche à l'étape 7.
- [x] Les faits du ciel, avec leur rareté (celles de la table des maquettes) ; la
      pertinence du jour tient compte des tâches du jour. « Il fera noir à 18 h 25 » ne
      sort que si une occurrence ouverte est dans une zone **extérieure** — le seul
      signal « journée physique » que le modèle porte vraiment, puisqu'il n'y a pas de
      catégorie sur la tâche ([[D-2026-09-20 Regroupement Sans Catégorie De Tâche]]).
- [x] `ComposerDonneesEcran` appelle le fonds de tiroir et remplit le budget de widgets
      du rang. Le ciel a trois densités (`formeDuCiel`, `web/src/lib/ecran-vues.ts`) :
      tableau, phrase, demi-phrase.

**Vérification** — tests du domaine sans base ; tests du score sur des cas fabriqués
(l'équinoxe bat une démarche d'adresse, la durée du jour ne bat rien) ; `apercu.png` un
jour vide montre le tableau du ciel et un jour chargé la demi-phrase.

#### Ce que le rendu a corrigé, à l'étape 2

- [x] **La « valeur courte » n'était pas courte.** « Équinoxe de septembre dans 2 jours »
      se faisait couper au milieu dans une colonne de widget. Règle tirée de l'encadré
      du compte à rebours : **l'étiquette porte le sujet, la valeur porte le chiffre**
      (« Équinoxe de septembre » / « Dans 2 jours »). Un test balaie une année entière
      et refuse toute valeur de plus de trente signes.
- [x] **Le tableau coupait ses rangées.** Corrigé à la source plutôt qu'au CSS :
      `rangeesDuCiel` n'admet dans le tableau que les faits dont l'étiquette et la
      valeur tiennent ensemble sur une ligne (34 signes, mesurés au rendu) ; les autres
      gardent leur forme de widget empilé, où ils ont deux lignes. **Élaguer, pas
      rapetisser**, appliqué au fonds de tiroir.
- [x] **Le budget de widgets restait inutilisé.** Le ciel ne rendait qu'un seul widget
      quel que soit le rang ; il remplit maintenant ce qui reste du budget, le premier
      fait avec son texte long et les suivants avec leur seule valeur.
- [x] **Bug trouvé à la sonde : le fuseau du serveur au lieu de celui du foyer.**
      `EvenementSaisonnier.Instant.LocalDateTime` prenait le fuseau de la machine. Un
      conteneur en UTC aurait affiché « équinoxe le 23 septembre » toute la journée du
      22 au Québec. Corrigé en `TimeZoneInfo.ConvertTime` sur le fuseau du `.env`, avec
      un test dans les deux hémisphères.
- [x] **Bug trouvé à la sonde : « L'solstice de décembre ».** L'article était collé
      d'office. Il suit maintenant le mot, et un test le garde.

#### Ce que la revue de code a corrigé, à l'étape 2

- [x] **Bug : le changement d'heure disparaissait le jour même.** La bascule a lieu au
      petit matin, donc à midi le jour J l'horloge porte déjà le nouveau décalage :
      `ChangementHeure.Prochain` comparait à partir d'aujourd'hui et ne voyait rien,
      puis filait sur la bascule suivante (133 jours), hors fenêtre. L'écran passait
      d'un « dans 1 jour » la veille à **plus rien du tout** le jour même. La
      comparaison part maintenant de la veille, et le fait parle au passé
      (« C'était cette nuit »).
- [x] **Le texte long redisait l'étiquette et la valeur.** « LE JOUR RACCOURCIT /
      3 min par jour / Le jour raccourcit d'environ 3 minutes par jour » — trois lignes
      de mur pour une idée, et c'était le **cas courant**, pas un cas limite. Les huit
      textes longs ont été réécrits pour ajouter quelque chose (la dérive dit désormais
      l'écart **sur une semaine**, qui est le chiffre qu'on sent vraiment), et un test
      balaie l'année en refusant tout texte qui contient l'étiquette ou la valeur.
- [x] **Deux colonnes voisines coiffées « DEHORS ».** Le verdict météo du journal et le
      fait `ciel.noirceur` portaient le même titre, et sortent le même jour par
      construction. Le fait s'appelle maintenant « La noirceur ».
- [x] **Défaut pré-existant de l'étape 1 : `corpsVide` était inatteignable.**
      `repartitionColonnes` rendait trois colonnes de liste même sans rien à y mettre,
      si bien qu'une installation neuve (aucune tâche, aucune météo, aucun fait) peignait
      un « Aujourd'hui · 0 à faire » sur trois colonnes blanches au lieu de la manchette
      pleine page. Corrigé dans la fonction, pas dans la page.
- [x] **Le clamp du widget ne s'applique plus qu'au texte.** Le tableau du ciel
      échappait au `line-clamp-2` par un détail de `-webkit-box` — vrai, mais pas une
      chose sur laquelle parier au mur.
- [x] `ContexteDuJour.TachesOuvertes` était déclaré et jamais lu : retiré. La pertinence
      du jour se décide sur les zones extérieures, pas sur le compte.

**Rendu vérifié** (aperçus à 1872×1404) : jour vide (0 due, 23 faites) → **tableau du
ciel** à trois rangées, rien de coupé ; jour à 4 dues → le ciel en **phrase** plus deux
valeurs courtes ; jour à 10 dues → rang « sommaire », le ciel en **bande de pied** ;
plancher (compte à rebours à zéro) → un seul widget, et le ciel **disparaît** — ce qui
est la règle, pas un défaut. **Aucun avertissement de débordement** sur les six tirages
(quatre avant la revue, deux après). `npm test` 211 verts (200 au départ),
`dotnet test` 698 verts (663 au départ).

> [!note] La demi-phrase ne se voit pas encore en pratique.
> Au rang « événement » le budget est d'un widget, et ce widget est pris par ce qui
> **engage la journée** (un événement du calendrier, la collecte, le verdict du dehors)
> avant le ciel. La demi-phrase n'apparaît donc que les jours où le ciel est le seul
> candidat — elle est couverte par un test unitaire (`formeDuCiel`), pas encore par un
> aperçu. Les familles des étapes 3 à 6 rendront le cas courant.

### 3 — La maison et le calendrier

Que des lectures de tables existantes — le journal de complétion, les [[Équipements]],
les [[Comptes À Rebours]], les [[Documents]]. Aucune migration.

- [x] Faits « la maison » (`FaitsDeLaMaison.cs`) : série en cours et record, N séances
      depuis, plus vieil équipement et son prochain entretien, zone la plus négligée,
      coût de l'année, anniversaires. « Ce jour-là l'an dernier » est écrit mais **ne
      donnera rien avant septembre 2027** — c'est un argument pour le bâtir tôt, pas
      tard.
- [x] Faits « le calendrier » (`FaitsDuCalendrier.cs`) : compte à rebours actif, « ça
      s'en vient » (7–30 j), travaux de la saison (les fenêtres saisonnières du moteur,
      exposées comme donnée lisible par `SpecRecurrence.FenetreAutour`), garantie ou
      document qui expire.
- [x] Chaque fait rend une valeur courte **et** un texte long : le journal choisit selon
      le rang.

**Vérification** — `dotnet test` avec des fixtures de journal de complétion ; sur la
prod par [[Serveur MCP]] (`bilan_taches`, `lister_equipements`), vérifier que les
chiffres sortis correspondent au réel.

#### Ce que l'étape 3 a dû trancher en chemin

- [x] **Une famille sans source se tait, et c'est la même règle pour les trois.**
      `ContexteDuJour` portait des coordonnées obligatoires ; la maison et le calendrier
      n'en ont pas besoin, et un foyer sans `METEO_LATITUDE` aurait perdu son journal de
      complétion avec son ciel. Chaque famille a maintenant son matériau facultatif
      (`PointDObservation`, `EtatDeLaMaison`, `EtatDuCalendrier`) et rend une liste vide
      quand il manque — le pendant, à l'échelle de la famille, de la règle « un fait dont
      la source manque ne sort pas ».
- [x] **Les noms saisis par le foyer vivent dans le texte long, pas dans l'étiquette.**
      L'invariant « le texte ne redit jamais l'étiquette » se compare sans distinction de
      casse : une pièce nommée « Aucune » le ferait mentir. Les faits de la maison
      portent donc une étiquette fixe et une valeur chiffrée, et le nom de la pièce, de
      la tâche ou de l'équipement passe au texte. Seul le compte à rebours garde son
      titre en étiquette — c'est son sujet, et « DANS 16 JOURS » tout seul ne dit rien.
- [x] **Le compte à rebours serait sorti deux fois.** Le journal dessine déjà son
      encadré ; le fonds, lui, ne sait pas qu'un encadré existe et ne doit pas le savoir
      (la lettre du matin n'en aura pas). C'est le **consommateur** qui écarte le
      doublon : `CLES_DEJA_AU_JOURNAL` dans `web/src/lib/ecran-vues.ts`, testé.
- [x] **La série se compte jusqu'à hier quand la journée n'a encore rien donné.** Sinon
      l'écran annonçait « série rompue » chaque matin et « 12 jours » chaque soir — un
      journal qui se contredit tout seul entre deux réveils.
- [x] **Une zone sans tâche est vide, pas négligée.** Quatre des cinq pièces de la prod
      n'ont aucune tâche : les coiffer « jamais rien » serait un reproche adressé à
      personne. Le compte de tâches par zone sert exactement à faire cette nuance.
- [x] **`widgetsDuCiel` ne savait rendre qu'une famille.** Le journal parcourt maintenant
      le fonds **dans l'ordre du score** (`widgetsDuFonds`) : chaque famille donne des
      widgets empilés, et le ciel se replie en un bloc à trois densités à la place de son
      meilleur fait. Le score sert enfin à quelque chose — le journal ne retrie rien, il
      coupe à la fin.
- [x] **Bug : la jointure interne perdait les complétions des tâches effacées.** Le
      journal de complétion **survit à la suppression d'une tâche** — le
      `HouseOsDbContext` le dit en toutes lettres (« pas de FK vers Tache/Occurrence,
      les ids restent comme références historiques »). Une jointure interne les écartait
      **en silence** : une série de six jours retombait à zéro parce qu'une tâche avait
      été effacée, et le mur affichait un chiffre faux sans que rien ne le signale.
      Corrigé en jointure à gauche ; une tâche disparue compte encore dans la série, le
      coût et « l'an dernier », mais ne peut plus être **nommée** (« 27 séances de
      quoi ? »). Couvert par `LecturesDuFondsTests`, qui teste les lectures sur Sqlite —
      les faits, eux, se testent toujours sans base.
- [x] **Bug trouvé au premier rendu réel : EF refuse de projeter une entité possédée.**
      `Select(t => new { t.Titre, t.Recurrence })` sur une requête suivie fait tomber
      `/api/affichage/donnees` en 500 — la spec de récurrence est possédée par la tâche.
      Aucun test unitaire ne pouvait le voir (le fonds se teste sans base). Corrigé par
      `AsNoTracking`, qui est de toute façon juste : la composition ne modifie rien.

> [!note] Laissé en suspens à l'étape 3, à trancher par Alain.
> Le texte long d'un fait qui **cite un titre saisi par le foyer** se fait tronquer au
> mur quand le titre est long : « La première le 28 septembre : Homelab — plan de
> migratio… ». C'est le clamp à deux lignes du widget, le même que le verdict météo
> subit depuis l'étape 1 — donc un comportement déjà livré, pas une régression de
> l'étape 3. Mais c'est « rapetisser » là où la règle du chantier est « élaguer » :
> le fait pourrait couper le titre lui-même à une longueur lisible, ou ne pas le citer
> du tout. Touche `calendrier.ca-s-en-vient`, `maison.seances`, `maison.an-dernier` et
> `calendrier.saison`.

#### Ce que la revue de code a corrigé, à l'étape 3

- [x] **La rareté était une envie, pas un compte de jours.** `maison.doyen`,
      `maison.piece-oubliee`, `maison.cout` et `maison.seances` sont vrais **tous les
      jours** une fois leur condition remplie — le doyen a toujours seize ans. Cotés
      « douze fois par an », ils prenaient la tête du journal **tous les matins de
      l'année**, puisque la fraîcheur se remet à neuf au bout de sept jours : exactement
      le radotage que le score existe pour empêcher. La rareté se compte en **jours de
      parution possibles**, comme pour le ciel, et c'est la **pertinence** qui dit ce
      qu'un fait change à aujourd'hui — barème écrit dans les deux familles :
      1 = décoration, 1,5 = ça éclaire la journée, 2 = ça suggère un geste, 3 = ça engage
      la journée. Un test le garde (« un fait vrai tous les jours ne prend pas la place
      d'un fait rare »).
- [x] **Le bloc du ciel prenait la place de toute sa famille.** Il se dépliait en
      plusieurs widgets à la position de son meilleur fait : un jour d'équinoxe au rang
      « resserré », la bande devenait `[équinoxe, dérive, durée du jour, record]` et la
      **durée du jour** — le fait le plus banal du fonds, celui dont un test dit qu'il
      ne bat rien — passait devant une garantie qui expire soixante fois mieux classée,
      que la troncature du budget coupait ensuite. Le bloc prend maintenant **une seule
      place** et les faits du ciel restés dehors retournent au classement
      (`placesDuFonds`, testé).
- [x] **« Ce jour-là l'an dernier » citait un titre au hasard.** La liste venait d'un
      `ToListAsync` sans `OrderBy` : Postgres rend les lignes comme il veut, et le fait
      disait « C'était Boîtes » à un rendu et « C'était Tondre » au suivant. C'est
      précisément ce que `ContexteDuJour` interdit (« figé pour la journée entière »), et
      ça aurait fait mentir l'édition matérialisée de l'étape 7. Tri explicite.
- [x] **Bug pré-existant : le maximum de la météo au trait d'union.** `signe()` existe
      pour qu'un « −1 » ne se lise pas comme une coupure de mot à trois mètres, mais il
      n'était appliqué qu'à la température courante : une journée de janvier écrivait
      « −18 °C · max -9 », le vrai moins et le trait d'union sur la même ligne. Au
      Québec ce n'est pas un cas limite, c'est l'hiver.

**Rendu vérifié** (aperçus à 1872×1404, `apercu.png`, données de dev) : rang
« chronique » (0 due) → « La pièce oubliée » et « Ça s'en vient » à côté du tableau du
ciel, l'encadré de compte à rebours **non doublé** ; rang « sommaire » (12 dues) → les
deux familles en bande de pied ; rang « événement » (plancher, retard de 20 jours) → un
seul widget, et les faits du fonds disparaissent comme la règle le veut. **Aucun
avertissement de débordement** sur les trois tirages, ni sur les deux repris **après
la revue** — où l'ordre du fonds est visiblement meilleur : l'équinoxe ouvre la bande,
la pièce oubliée suit, et les faits quotidiens du ciel ferment la marche.
`npm test` 216 verts (211 au départ), `dotnet test` 729 verts (698 au départ).

**Chiffres recoupés sur la prod** ([[Serveur MCP]], 2026-09-20) : `bilan_taches` donne
46 complétions sur quatre semaines, dont **12 pour « Boites! »** depuis le 28 août — ce
que `maison.seances` annoncerait. La série est à **zéro** (rien le 19 ni le 20), donc ni
série ni record : correct. `lister_equipements` + `obtenir_equipement` : un seul
équipement daté (vélo, 2026-07-26, moins d'un an), donc **pas de doyen** — « une maison
neuve n'a pas de doyen ». Deux zones seulement portent des tâches, et la Cuisine n'a
jamais rien eu de coché : `maison.piece-oubliee` sortirait « Jamais rien ». Aucune tâche
de prod ne porte de fenêtre saisonnière et le seul document daté expire le
2027-09-01 : `calendrier.saison` et `calendrier.expiration` se taisent, comme prévu.

### 4 — Le hasard

- [x] Banque locale de dictons météo québécois et table de fêtes et journées nationales,
      **en données de configuration, pas en dur** — un foyer ailleurs remplace le
      fichier ([[Distribution]]).
- [x] Rareté « bouche-trou » : ces faits ne sortent que lorsqu'il ne reste rien d'autre.

**Vérification** — un jour de prod sans aucune tâche et sans événement remplit quand
même les sept widgets du rang 0.

#### Ce que l'étape 4 a tranché en chemin

- [x] **Où vit la banque** : [[D-2026-09-20 Banque Du Hasard En Fichier De Données]] —
      un fichier JSON livré avec l'app (`banque-du-hasard.qc.json`, 24 dictons et
      20 fêtes), remplaçable par `HASARD_FICHIER` / `Hasard:Fichier`, lu **une fois au
      démarrage**. Ni table (l'étape est sans migration), ni section d'`appsettings.json`
      (c'est du contenu, pas de la configuration technique). Un chemin réglé mais
      illisible fait **taire** la famille plutôt que de retomber sur la banque du
      Québec. `docs/configuration.md`, `.env.example` et le compose sont à jour.
- [x] **La moitié des jours fériés du Québec sont mobiles.** Pâques et ses deux congés,
      les Patriotes, le Travail, l'Action de grâce : une liste de dates fixes aurait été
      fausse quatre jours par an. Une fête déclare quand elle tombe sous quatre formes
      déclaratives (date fixe, n-ième jour de semaine du mois, dernier jour de semaine
      avant une date, décalage depuis Pâques) — la donnée reste de la donnée, la règle
      reste du C# testé ([[D-2026-08-23 Pas De N8n Dans Le Cœur]]). Comput grégorien
      écrit à la main, vérifié contre quatre dimanches de Pâques publiés.
- [x] **« Bouche-trou » est une pertinence, pas une rareté.** Un dicton peut paraître
      tous les jours : sa rareté est celle d'un fait quotidien, et la rareté ne se
      négocie pas (c'est la correction de l'étape 3). Ce qui le met en queue, c'est un
      cran **sous** la décoration — `Pertinence.BoucheTrou` = 0,5 — sans quoi il aurait
      été à égalité avec `ciel.jour`, dont un test dit qu'il est dernier. L'échelle
      entière (0,5 · 1 · 1,5 · 2 · 3) passe du commentaire au code (`Pertinence`,
      `FaitDeTiroir.cs`) et remplace les nombres nus des trois familles.
- [x] **La rareté de la fête se compte dans la banque.** « Combien de jours par année le
      fait peut paraître » est exactement le nombre d'entrées : un foyer qui n'inscrit
      que ses huit jours chômés obtient un fait deux fois plus rare que celui qui en
      inscrit vingt. Aucun nombre deviné.
- [x] **Le dicton est le seul fait dont la matière vit dans le texte long.** Un proverbe
      n'a pas de chiffre et ne tient pas dans trente signes. Relevé au rendu : la
      hiérarchie du widget s'en trouve inversée (le mois en gros, le proverbe en petit).
      Vivable, et c'est le seul découpage qui ne coupe pas le proverbe — à revoir avec
      l'éditorialiste à l'étape 7, pas avant.
- [x] **Tranché : la troncature laissée en suspens à l'étape 3.** C'est le **titre** qu'on
      élague, pas la phrase (`Mots.TitreCourt`, 32 signes, coupé à un mot entier) :
      la phrase garde ainsi sa fin et sa ponctuation. Appliqué aux huit faits qui citent
      un nom saisi par le foyer, et au nom d'une fête, qui vient d'un fichier donc d'un
      inconnu. Corollaire : « … . » ne s'écrit pas, et le corriger dans chaque famille
      serait une règle qu'une famille future oublierait — `FaitDeTiroir` normalise son
      texte long une fois pour toutes.

#### Ce que le rendu a corrigé, à l'étape 4

- [x] **Bug : le budget de widgets n'avait pas de capacité en face** — et c'est la
      famille neuve qui l'a révélé. Un widget de plus sur une journée vide et le mur
      débordait de **90 px**, le dicton coupé en deux sous le pied. Le budget du rang
      (7 au rang « chronique ») dit ce que le journal veut ; rien ne disait ce que la
      colonne tient. `capaciteWidgets` (`web/src/lib/ecran-vues.ts`) est le pendant exact
      de `capaciteListe` : deux widgets par colonne d'aparté, et l'encadré du compte à
      rebours en coûte un — donc **cinq**, jamais sept, au rang 0 avec un compte à
      rebours. La garde de l'étape 1 a fait son travail : elle a signalé le débordement
      au premier tirage.

#### Ce que la revue de code a corrigé, à l'étape 4

- [x] **Bug pré-existant de l'étape 3 : deux seuils qui se croisent faisaient un trou.**
      `maison.serie` se tait quand la série **est** le record ; `maison.record` ne parle
      qu'à partir de cinq jours. Une série de trois ou quatre jours qui est aussi le
      record — **la première série d'une maison neuve**, le moment exact que ce fait
      existe pour raconter — ne sortait donc **ni** en série **ni** en record. Le mur
      restait muet jusqu'à ce qu'une série plus longue existe déjà. La série ne se tait
      maintenant que lorsque le record parle vraiment, et son texte change quand elle est
      le meilleur résultat à ce jour, au lieu de citer un record égal au chiffre affiché.
      Le seuil `RecordMinimal` portait par ailleurs un commentaire qui parlait d'une
      semaine pour une valeur de cinq jours.
- [x] **La banque était lue à la première requête, pas au démarrage** — alors que le
      commentaire et `docs/configuration.md` promettaient le contraire. Un `HASARD_FICHIER`
      fautif ne se voyait donc pas dans le log de redémarrage, là où l'opérateur le
      cherche : l'avertissement attendait le prochain tirage du mur. Le singleton est
      résolu explicitement après `builder.Build()` ; vérifié avec un chemin inexistant,
      l'avertissement sort avant le « Now listening ».

> [!note] Signalé par la revue, laissé tel quel : `corpsVide` sur une journée tout
> entière cochée. `repartitionColonnes` rend un corps vide quand la liste n'a aucune
> colonne **et** qu'il n'y a aucun widget ; au rang « chronique » la liste n'a jamais de
> colonne, si bien qu'une journée à zéro due, N faites et aucun fait montre la manchette
> pleine page plutôt que la liste des cochées. C'est le comportement livré depuis
> l'étape 2, pas une régression, et l'étape 4 le rend **moins** atteignable, pas plus :
> la famille « le hasard » donne un dicton tous les jours, donc il y a toujours au moins
> un widget dès que la banque existe. Le cas ne survit qu'à une installation sans
> coordonnées **et** sans banque. À trancher par Alain, comme choix éditorial (montrer
> les cochées, ou la manchette), pas comme correctif.

**Rendu vérifié** (aperçus à 1872×1404, `apercu.png`, données de dev — 0 due, 33 faites,
aucun événement) : rang « chronique » → l'encadré, la pièce oubliée, le verdict du
dehors, « ça s'en vient », le tableau du ciel et **le dicton** en queue de colonne,
trois colonnes pleines jusqu'au pied. Un second tirage avec une banque de remplacement
passée par `Hasard__Fichier` (la clé vérifiée de bout en bout, y compris le
journal de démarrage) : la **fête du jour** prend sa place et c'est le **dicton** qui
tombe — le bouche-trou est bien le premier à partir. **Aucun avertissement de
débordement** après la borne de capacité. `dotnet test` 753 verts (729 au départ),
`npm test` 217 verts (216 au départ).

> [!note] La « vérification » de cette étape était fausse, et c'est le rendu qui l'a dit.
> Les **sept** widgets du rang 0 ne sont pas atteignables : trois colonnes d'aparté en
> tiennent six, moins un pour l'encadré du compte à rebours. La journée vide est bien
> pleine — sept faits servis, mis en page en cinq widgets plus l'encadré, plus le verdict
> météo, sans un trou ni un débordement — mais le nombre écrit dans le plan ne
> correspondait à rien de mesurable. Consigné dans [[Journal De La Maison]].

### 5 — Les normales climatiques

[[D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo]]. Première migration du
chantier.

- [x] Ingestion annuelle de l'archive Open-Meteo (ERA5) pour
      `METEO_LATITUDE`/`METEO_LONGITUDE`, sur ~10 ans, sur le patron de [[Météo]] — un
      `BackgroundService`, une table normalisée, migration EF générée
      ([[D-2026-08-23 Pas De N8n Dans Le Cœur]]).
- [x] La clé de la table **porte les coordonnées utilisées** : changer le `.env`
      invalide et recalcule. Critique — le déménagement du 2026-10-06 change les
      coordonnées.
- [x] Statistiques en C# testable : premier gel, première neige, dernière journée à 20°,
      mois le plus sec ou le plus pluvieux.
- [x] Faits « le climat », y compris « il a fait X° ce jour-là l'an dernier » — depuis
      l'archive, pas depuis les tables de prévisions (voir ci-dessous).
- [x] `docs/configuration.md` : rien de neuf à saisir, mais documenter que les normales
      suivent les coordonnées.

**Vérification** — `dotnet test` ; un appel réel à l'archive en dev, les normales
matérialisées relues et comparées à la connaissance du coin (premier gel début octobre à
Québec) ; couper le réseau et vérifier que l'édition sort quand même, sans le widget.

#### Ce que l'étape 5 a tranché en chemin

- [x] **« L'an dernier » ne pouvait pas venir des tables météo existantes** — l'étape le
      disait, et c'était faux. `previsions_quotidiennes` est **remplacée à chaque heure**
      (`past_days=1`, `ExecuteDelete`) et `releves_meteo` ne garde sept jours de brut :
      la maison n'a aucune mémoire du temps qu'il a fait. La journée d'il y a un an vient
      donc de l'archive ERA5, qu'on tire de toute façon ; les tables existantes servent
      l'autre moitié du fait, le **maximum d'aujourd'hui**, qui en fait une comparaison.
      Conséquence : l'archive est **gardée en table** (3 652 lignes) au lieu d'être jetée
      après le calcul — ce qui permet aussi de rejouer une statistique sans rappeler le
      réseau, comme le payload brut de l'ingestion horaire.
- [x] **Le tirage se déclenche à l'âge, pas à une date fixe.** Une date au calendrier
      serait ratée chaque année où la machine est éteinte ce jour-là, et surtout elle
      ferait attendre le déménagement jusqu'au prochain anniversaire. Le worker vérifie
      au démarrage puis toutes les six heures (une lecture d'une ligne) et ne tire que si
      les normales manquent, portent une autre clé, ou ont plus de 360 jours. **360 et
      non 365** : l'archive s'arrête trois jours avant aujourd'hui, et une fenêtre pile
      d'un an ouvrirait chaque année un trou d'une semaine dans « l'an dernier », juste
      avant le tirage suivant.
- [x] **Le gel qu'on annonce est celui du sol, pas celui de l'abri.** C'est le rendu réel
      qui l'a dit : à zéro degré, la médiane des neuf saisons tombait au **27 octobre**,
      trois semaines après le gel que tout le monde connaît ici. Une maille de neuf
      kilomètres à deux mètres du sol ne voit ni le rayonnement nocturne d'un jardin ni
      l'air froid qui s'y accumule — et c'est la raison pour laquelle les avertissements
      de gel s'émettent partout à deux ou quatre degrés annoncés. Seuil à **3 °C** :
      médiane au **3 octobre**, ce que disent les normales publiées de la station. Le
      seuil n'est pas un ajustement québécois, c'est un écart vrai partout.
- [x] **La saison, et non l'année civile.** Un premier gel du 3 janvier et un du
      5 octobre appartiennent au même hiver ; une moyenne par année civile les
      mélangerait en une date de juin qui n'existe nulle part. Les saisons se comptent
      depuis le **mois qui suit le plus chaud**, déduit de l'archive — le dépôt est
      public et l'hémisphère sud a son été en janvier. Un test le vérifie en Tasmanie.
- [x] **La date se prend à la médiane, l'écart à la moyenne.** Une année à gel très
      tardif ne doit pas déplacer la date — c'est l'intérêt de la médiane — mais elle
      doit se voir dans l'imprécision annoncée. Un écart médian l'aurait effacée, et le
      journal aurait promis « à un jour près » un climat qui varie de six semaines.
- [x] **Une normale qui n'en est pas ne sort pas** : moins de trois saisons, un événement
      qui n'arrive pas dans 60 % des saisons, ou une douceur qui ne s'arrête jamais (la
      normale tomberait au bord de la fenêtre de recherche — les tropiques). Même règle
      que le lever du soleil au-delà du cercle polaire.

#### Ce que le rendu a corrigé, à l'étape 5

- [x] **Le chiffre du texte tombait hors du widget.** Le widget coupe à deux lignes
      (`line-clamp-2`) : « Sur 9 saisons, la première gelée au sol s'est présentée à… »
      perdait précisément le « à 9 jours près », c'est-à-dire l'imprécision que la
      décision **exige** de porter. Les cinq textes du climat sont réécrits court, et un
      test les borne à soixante signes — une règle d'écriture comme les trente signes de
      la valeur, ni pixel ni colonne.
- [x] **Le test d'intégration portait le nom de la rue du foyer.** `RueDeLaColline` et
      les coordonnées exactes de la nouvelle maison dans un dépôt public
      ([[Distribution]]) : renommés en « avant / après le déménagement », sur deux lieux
      quelconques.

#### Ce que la revue de code a corrigé, à l'étape 5

- [x] **Le 200 maigre effaçait dix ans et se figeait un an.** Le seul garde-fou était
      « zéro journée ». Une réponse tronquée, une fenêtre rabotée par l'API ou un
      `Meteo:NormalesAnnees` baissé par erreur aurait remplacé l'archive par trois mois
      **et** posé un horodatage tout neuf, qui interdit de réessayer avant 360 jours :
      une année de silence pour « le climat » et « l'an dernier », sur une ligne
      d'*information*, alors que `docs/configuration.md` promet le contraire. Un tirage
      doit maintenant couvrir 90 % de la fenêtre demandée **et** ne pas rendre moins de
      saisons complètes que ce qui est déjà en base, sinon il est écarté avec un
      avertissement et on repasse dans six heures (`OperationsNormales.PourquoiRefuser`,
      pur et testé).
- [x] **Le rattrapage d'une semaine ne franchissait pas le Nouvel An.** `ProchaineDate`
      ne regardait que l'année courante et la suivante : une normale au 28 décembre, lue
      le 2 janvier, venait de passer depuis cinq jours — en plein dans la fenêtre — mais
      son occurrence de l'année courante était à presque douze mois, et le fait
      disparaissait. Les trois années sont maintenant regardées, l'an dernier en premier.
- [x] **Une série JSON nulle tuait le tirage.** Open-Meteo rend `null`, et non un tableau
      vide, quand une série entière manque : `GetArrayLength` lève dessus. Le même trou
      existait **depuis un an dans l'ingestion horaire** (`OpenMeteoNormalisation`), plus
      un `GetProperty("time")` qui remontait une `KeyNotFoundException` là où un test
      promettait « une erreur claire ». Corrigé des deux côtés.
- [x] **Le mois le plus sec pouvait n'être que le mois le plus court.** Sans écart
      minimal, douze mois identiques désignaient quand même un gagnant : février bat
      janvier de trois jours de pluie, soit 11 % — un artefact du calendrier publié
      comme une statistique du climat. Il faut maintenant **un quart d'écart**. Et un
      mois n'est « entier » que si **toutes ses journées** sont là (un compte de jours,
      pas un encadrement de dates) : la lecture de l'archive écarte les journées sans
      température, et un trou de dix jours au milieu d'un juillet le faisait passer pour
      un mois ordinaire. Le test qui prétendait couvrir le premier cas affirmait
      exactement le contraire de son nom.
- [x] **Le worker lisait la date de la machine, pas celle du foyer.** Un conteneur en
      UTC est déjà au lendemain chaque soir après vingt heures locales, et mangeait en
      silence la marge que `NormalesAgeMaxJours = 360` suppose. Le fuseau du foyer
      devient une méthode de `MeteoOptions`, que [[Affichage E-ink]] partage désormais
      au lieu d'en garder une copie privée.
- [x] Copie contre code : le texte de la neige promettait « un centimètre » pour un seuil
      d'un demi (le seuil passe à un — la date ne bouge pas), et le commentaire des
      normales parlait encore de « la première nuit sous zéro ».

**Rendu vérifié** (aperçu 1872×1404, données de dev, 2026-09-20) : appel réel à
l'archive, **3 652 journées, 9 saisons complètes** en 1,8 s — premier gel **3 octobre
± 9 j**, première neige **7 novembre ± 8 j**, dernière douceur 13 octobre, mois le plus
sec **février** (75 mm), le plus arrosé **juillet** (155 mm) : tout se recoupe avec les
normales publiées de la région. Au mur, `climat.gel` sort **en tête du classement**,
devant l'équinoxe — « LE PREMIER GEL / Vers le 3 octobre / La gelée au sol, à 9 jours
près sur 9 saisons », sans troncature et **sans avertissement de débordement**. Tables
vidées à chaud (l'état d'un premier démarrage sans réseau) : l'édition sort avec sept
faits, sans widget climat, sans erreur. Tirage refait après la revue : mêmes chiffres,
le garde-fou laisse passer un tirage complet. `dotnet test` **809** verts (764 au
départ), `npm test` **217** verts.

### 6 — La ville

[[D-2026-09-20 Sources Municipales Séparées Par Solidité]]. **Rien de municipal n'entre
dans le dépôt.**

- [x] Hors dépôt : convertir le PDF 2026 des collectes en ICS et l'héberger. Le secteur
      se lit dans l'index des rues du PDF — « de la Colline » est au **secteur Sud**,
      jeudi. Documenter la recette dans `CLAUDE.local.md`.
- [x] Abonner House OS à cet ICS comme [[Flux Externes]] de type `Collecte` : **zéro
      code**, le chemin existe déjà et le mur lit déjà la prochaine collecte.
- [x] Créer la tâche récurrente annuelle « régénérer le calendrier de collectes »
      ([[D-2026-09-20 Calendrier De Collectes Régénéré À La Main]]), échéance en début
      d'année civile.
- [x] `FluxExterne.Url` nullable + marque de source ; migration EF
      ([[D-2026-09-20 Flux Externe Poussé]]).
- [x] **Le rafraîchissement ICS doit ignorer les flux poussés** — sinon la passe de 6 h
      les vide. C'est le piège principal de l'étape : un test le couvre explicitement
      (`FluxExternesRafraichissement.cs`).
- [x] Endpoint authentifié de poussée (remplacement en transaction, mêmes bornes de
      longueur que l'ICS), hors cookie de session ; UI de gestion sans champ URL, avec
      l'horodatage de dernière réception.
- [x] **Parité MCP** ([[Serveur MCP]]) : les outils de gestion de flux suivent dans la
      même tranche.
- [x] Hors dépôt : le gratteur SCJC (événements) qui pousse une fois par jour,
      User-Agent identifiable, `If-Modified-Since`.
- [x] Faits « la ville », avec la règle : un flux poussé périmé **cesse de sortir** au
      lieu de mentir.

#### Ce que l'étape 6 a tranché en chemin

- [x] **L'ICS des collectes est hébergé sur R2, public** (choix d'Alain, 2026-09-21).
      La garde SSRF refuse tout hôte qui résout vers le réseau interne : le LXC était
      éliminé d'office, et un fichier derrière NPM aurait fait sortir puis rentrer la
      requête par l'IP WAN (hairpin NAT), qui marche jusqu'au jour où elle ne marche
      plus. Une IP publique Cloudflare passe la garde sans rien devoir au lab.
- [x] **La poussée a sa propre clé**, `HOUSEOS_POUSSEE_CLE`, et non celle du MCP
      (choix d'Alain, 2026-09-21). La décision citait le MCP comme *précédent* d'un
      appel machine, pas comme clé à partager : le gratteur vit hors de la maison, et
      pousser des événements n'a pas à ouvrir les tâches, le budget et les documents.
      Le schéma d'authentification est partagé (`OptionsCleApi`), le secret ne l'est pas.
- [x] **Le type `Municipal` plutôt que « poussé = municipal ».** La famille « la ville »
      avait besoin de savoir quels flux la concernent ; le faire dépendre de la *source*
      aurait rangé un calendrier scolaire poussé dans les nouvelles de la ville. C'est
      le **type** qui trie — une valeur d'enum de plus, aucune migration (les enums sont
      stockés en chaîne).
- [x] **La fenêtre de péremption vaut pour les deux sources, à sept jours.** La décision
      ne parlait que des flux poussés, mais un abonnement ICS qui échoue depuis un mois
      ment exactement de la même façon. Une seule règle, sur le même horodatage
      (« dernier remplacement »), plus facile à expliquer qu'à distinguer.
- [x] **La collecte spéciale se reconnaît à ce qu'elle ne revient pas** — un titre qui
      ne paraît qu'une fois dans la fenêtre du flux, là où le bac hebdomadaire y revient
      huit fois. C'était ça ou une connaissance municipale dans le dépôt, que la
      décision interdit.
- [x] **`ville.collecte` rejoint `CLES_DEJA_AU_JOURNAL`** : le journal garde une place
      fixe à la collecte — sortir le bac est le geste du soir, il ne doit pas dépendre
      d'un classement — et le fonds produit le fait quand même, pour la lettre du matin.
      Même traitement que le compte à rebours, déjà prévu par [[Fonds De Tiroir]]. La
      revue de code a montré ce que ce partage exigeait de plus (ci-dessous).

#### Ce que le rendu a corrigé, à l'étape 6

- [x] **Le jour de la semaine s'affichait en abrégé au mur.** « jeu » sous la collecte —
      trois lettres qui se lisent d'abord comme un jeu. Le défaut a disparu avec sa
      cause : le widget ne fabrique plus sa propre formulation, il prend celle du fait
      (« Dans 3 jours »). `quandCeJour` n'avait plus d'appelant et a été retiré ;
      `jourCourt` reste pour les listes denses de l'app.
- [x] **Les titres du calendrier de collectes étaient trop longs.** « Matières
      recyclables et Matières organiques » devenait « Matières recyclables et… » dans
      la phrase du fait, qui élague à trente-deux signes. Le convertisseur (hors dépôt)
      écrit maintenant court — « Recyclage et compost » — et met la majuscule à la
      première matière seulement, le titre étant cité dans une phrase.

#### Ce que le PDF a appris au convertisseur (hors dépôt)

- [x] **La lecture est géométrique, pas textuelle** : les colonnes de jours se
      reconstruisent depuis l'entête « D L M M J V S » de chaque mois, et un code tombe
      dans la colonne dont la cellule contient son centre. Les collectes d'espèce
      (encombrants) sont posées **dans** la case du jour, les hebdomadaires sur la ligne
      en dessous : le même découpage en colonnes traite les deux.
- [x] **Les notes de bas de case mangeaient deux semaines.** « AU 29 ET 30 DÉC. »
      imprimé par-dessus la grille : ses chiffres ouvraient une fausse ligne de semaine,
      et les vraies collectes du 22 janvier et du 10 décembre tombaient à côté. Les
      numéros de jour se trient par **hauteur de caractère** (13 points contre 7).
- [x] **Le calendrier valide la géométrie** : dans la colonne d'un jeudi, il ne peut y
      avoir qu'un jeudi. Ça écarte les cases d'un mois voisin (« 29 DEC. » en tête de
      janvier) sans rien savoir d'elles, et ça ferait tout refuser si la grille était
      lue décalée d'une colonne — le risque que
      [[D-2026-09-20 Calendrier De Collectes Régénéré À La Main]] nomme explicitement.
- [x] **La ville déplace les collectes des congés, et l'écrit.** Exiger un jour de
      semaine unique refusait un calendrier juste (secteur Nord : deux collectes
      déplacées en décembre). Le contrôle tolère un dixième d'exceptions, les **nomme**
      à l'écran, et refuse au-delà — ce qu'on attrape, c'est une grille lue de travers,
      qui déplacerait tout.
- [x] **Le slug de l'URL d'un événement ment** : la « séance du conseil » du 6 octobre
      porte l'URL `...-2026-10-05`, la date de publication. C'est la date **affichée**
      qui fait foi. Trouvé en lisant la vraie page, pas en la supposant.

#### Ce que la revue de code a corrigé, à l'étape 6

- [x] **Le widget de la collecte court-circuitait la seule règle de la famille.** Le
      journal le dessinait depuis `DonneesEcran.ProchaineCollecte` — une colonne qui ne
      juge ni la fraîcheur du flux ni la distance — pendant que l'étape ajoutait
      `ville.collecte` à `CLES_DEJA_AU_JOURNAL` au motif que « le journal le dessine
      déjà ». Un calendrier de collectes mort depuis trente jours continuait donc de
      s'afficher, et une collecte à quarante-cinq jours aussi, alors que le fait, lui,
      se taisait dans les deux cas. Le widget garde sa place fixe mais **prend les mots
      du fait** ; la colonne disparaît du contrat de l'écran.
- [x] **Le bandeau du jour publiait la ville une seconde fois.** `EvenementsDuJour`
      prenait tous les types : le jour d'une séance du conseil, le mur l'annonçait en
      « Aujourd'hui, au calendrier » **et** en « En ville » — et l'annonçait encore avec
      un gratteur mort. Le bandeau laisse maintenant `Collecte` et `Municipal` à leur
      famille. C'est exactement le doublon que `CLES_DEJA_AU_JOURNAL` existe pour
      éviter, par une porte que personne ne regardait.
- [x] **`Enum.TryParse` acceptait « 5 ».** `POST /api/flux-externes {"source":"5"}`
      rendait 201 et créait un flux **mort-vivant** : la passe de six heures l'ignore
      (ce n'est pas `Ics`), la poussée le refuse (ce n'est pas `Poussee`), et l'UI le
      liste comme un calendrier ordinaire. Le MCP, lui, s'en gardait depuis toujours
      (`Conversions.ParserEnum`). La règle déménage dans `Infrastructure/ParseurEnum.cs`
      et sert les deux — REST y gagne aussi la tolérance à la casse. Le même trou était
      **ouvert depuis un an** sur le type de flux et sur trois enums de la récurrence
      (`OperationsTaches` : mode, type fixe, stratégie d'assignation) : corrigé partout.
- [x] **Changer la source d'un flux répondait à côté.** Un client qui relit puis
      réécrit une fiche renvoie l'URL courante : en demandant `Poussee`, il s'entendait
      répondre « un calendrier poussé n'a pas d'URL » — vrai, mais sans rapport avec ce
      qu'il avait demandé. La garde de source passe **avant** la validation de l'URL.
- [x] **Le MCP acceptait une URL sur un flux poussé et la jetait en silence**, là où
      REST la refuse. Un « accepté mais ignoré » de plus, et une parité de moins :
      l'outil lève maintenant le même message.

**Vérification** — `dotnet test` (dont le test de non-vidage) ; pousser deux fois le
même flux et vérifier le remplacement ; débrancher le gratteur une semaine et vérifier
que le journal sort sans widget « ville » ; `grep -rni "villescjc\|pdftotext" server/ web/`
ne retourne rien.

**Vérifié** (dev, 2026-09-21) : `dotnet test` **852** verts (809 au départ), `npm test`
**222** (217 au départ ; un test est parti avec `quandCeJour`). Le grep de la décision ne rend rien dans le dépôt. L'ICS réel (54 jours
de collecte, tirés du PDF 2026) hébergé sur R2 public, ingéré par le vrai chemin — garde
SSRF et Ical.Net comprises, 10 événements dans la fenêtre. Le gratteur réel a poussé les
**6 événements** de la page municipale par l'endpoint, deux fois de suite, sans doublon.
Au mur (aperçu 1872×1404, **sans avertissement de débordement**) : « DANS LA VILLE /
Recyclage et compost / jeudi » et le widget du fonds « EN VILLE / Dans 6 jours / Les
journées de la culture, le 27 septembre, à 12 h 00 ». En simulant le 5 octobre — deux
semaines après la dernière réception réelle — la famille entière se tait : la règle de
péremption, vue de bout en bout. Le 26 septembre, les trois faits sortent, dont
« Une collecte spéciale / Dans 10 jours / Encombrants, le 6 octobre ».

### 7 — L'éditorialiste

[[D-2026-09-20 Une Édition Par Jour Matérialisée]], [[D-2026-09-20 Édition Écrite Par Opus]],
[[D-2026-09-20 Échéance Ferme Explicite Sur La Tâche]].

- [x] Entité d'édition sous `Domaine/` + migration EF : date, rang, textes rendus,
      **clés de widgets publiées** en JSONB, rubriques, source (LLM ou gabarit),
      horodatage. (`Domaine/Editorial/Edition.cs`, migration
      `AjouterEditionEtEcheanceFerme` — la troisième du chantier, mutualisée avec le
      booléen `Tache.EcheanceFerme` comme la décision le demandait.)
- [x] `BackgroundService` au créneau du matin, sur le patron de `HumeurService.cs` :
      rattrapage au démarrage, plancher d'une minute, aucun appel dans le chemin de
      requête. (`Features/Editorial/EditorialisteService.cs`, réveillé aussi par
      `SignalDeReedition`.)
- [x] Prompt de l'édition, distinct de celui de [[Titre D'humeur]] : surtitre,
      manchette, chapeau, deux paragraphes, rubriques. **Aucun fait inventé** ; les
      chiffres et les titres viennent tous de l'état fourni.
      (`Features/Editorial/RedactionLlm.cs`.)
- [x] Parse défensif et validation stricte de la sortie, sur le patron de
      `PolissageLlm.Extraire` : tout écart → repli en gabarit. Un titre de tâche
      inventé dans une rubrique est écarté, jamais affiché.
- [x] Mémoire des sept derniers jours : les sept dernières éditions alimentent la
      pénalité de fraîcheur du score **et** le prompt anti-radotage.
      (`Features/Editorial/MemoireDesEditions.cs`, une seule lecture pour les deux.)
- [x] Réglage de modèle propre à l'édition (`claude-opus-5` par défaut) ;
      [[Titre D'humeur]] garde Haiku. `docs/configuration.md` mis à jour
      (`Edition:Modele`).
- [x] `ComposerDonneesEcran` sert l'édition matérialisée ; la liste, la météo, les
      cochées, l'heure et la pile restent recalculées à chaque rendu.
- [x] **Réévaluation du plancher à chaque rendu** : compte à rebours à zéro, retard qui
      passe trois jours ou échéance ferme entrante → réédition.
- [x] `Tache.EcheanceFerme` : entité, `OccurrenceDto`, `TacheDto`, `TacheResumeDto`,
      `LigneEcranDto`, `plancher()` avec la raison `'ferme'`, `TacheEditeur` (chargé et
      renvoyé), `gerer_tache` et `creer_taches` en parité MCP.
- [x] Parité MCP : `regenerer_journal_mural` réécrit l'édition (Opus) avant la phrase
      et l'image, et gagne `date` (une autre journée, pour les essais).

#### Ce que l'étape 7 a tranché en chemin

- [x] **La réédition par plancher se fait en deux temps** (proposé, à confirmer par
      Alain : [[D-2026-09-21 Réédition En Deux Temps]]). Le rendu qui voit un plancher
      changé pose **tout de suite un gabarit** (la manchette du plancher, sa raison en
      surtitre), lève un drapeau sur l'édition et réveille le service de fond, qui
      rappelle Opus **hors du chemin de requête**. Le mur ne ment jamais, et l'appel
      LLM reste un appel de fond. Même mécanique pour une édition manquante — un
      redémarrage avant le créneau, un premier rendu après la migration.
- [x] **La clé API est celle du titre d'humeur** (`ANTHROPIC_API_KEY`) ; seul le
      **modèle** a son réglage propre (`Edition:Modele`). La décision demandait deux
      modèles et deux prompts, pas deux comptes Anthropic : le courriel entrant partage
      déjà cette clé, et une seconde variable aurait été une seconde chose à poser au
      déménagement.
- [x] **Le rang est figé dans l'édition, la grille reste vivante.** L'édition consigne
      le rang pour lequel la prose a été écrite ; la page calcule sa grille sur le
      compte vivant, comme avant. Sinon une tâche ajoutée à neuf heures sur une
      journée écrite « chronique » n'aurait eu **nulle part où aller** — la liste est
      vivante par décision, il lui faut sa colonne.
- [x] **Les widgets figés sont un ordre, pas une liste fermée.** `DonneesEcran.Faits`
      sert d'abord les clés publiées par l'édition, dans son ordre, puis le reste au
      score du moment. La place fixe de la collecte et l'encadré du compte à rebours
      (`CLES_DEJA_AU_JOURNAL`) sont publiés **hors budget** — le pendant serveur est
      `GenerationEdition.ClesDejaAuJournal`.
- [x] **Une horloge d'essai sur un autre jour ne matérialise rien.** Un aperçu de Noël
      en septembre aurait écrit une édition de Noël avec les faits de septembre, et le
      service de fond l'aurait trouvée « déjà écrite » le jour venu. L'édition ne se
      persiste que pour la journée vraie ; pour une autre date, c'est `date` sur la
      régénération à la main, qui est un geste explicite.
- [x] **Le numéro d'édition ne change pas de source.** Le compter en éditions
      matérialisées, comme la spec l'annonçait, aurait fait repartir le mur à « N° 1 »
      le jour du release. Il reste le compte de jours depuis la première entrée du
      journal de complétion.
- [x] **La chronique a sa colonne, pas la pleine largeur.** Les deux paragraphes
      prennent la première colonne du corps aux rangs « chronique » et « manchette »
      (les maquettes du 8 novembre et du 20 septembre), jamais sous la manchette : le
      bloc-titre et la manchette gardent leur hauteur, et c'est la colonne qui absorbe
      le texte. Le gabarit n'a pas de corps, donc pas de colonne : sans clé API, le mur
      est exactement celui de l'étape 6.
- [x] **Le premier rendu a coupé la chronique au milieu d'une ligne**, sans que la
      garde de débordement le voie : la colonne cache (`overflow-hidden`), le cadre ne
      déborde pas. Deux paragraphes de 260 signes à 34 px ne tenaient pas dans une
      colonne d'un tiers. Premier essai : la colonne à **1,55 fr** comme les maquettes
      — mais les colonnes d'aparté rétrécies faisaient clipper leurs étiquettes
      (« LA PROCHAINE… »), des étiquettes que le fonds écrit pour un tiers de page.
      Retenu : la chronique garde **un tiers**, le corps passe à 32 px, la borne à
      **200 signes** (sept lignes de ~32 signes, mesurées) et le clamp à sept lignes
      fait le filet. Le fonds ne bouge pas.
- [x] **L'aperçu d'un autre jour comptait les dodos depuis l'horloge du navigateur** :
      le 27 affichait « 15 dodos » pour le 6 octobre. `libelleDodos` compte depuis la
      date composée par le serveur. Le chemin de l'appareil n'était pas touché (l'heure
      vraie et la date composée sont les mêmes), seul l'essai mentait.
- [x] **Le service de fond vise le jour civil, pas « la veille avant l'heure du
      matin ».** Calqué sur le titre d'humeur, le rattrapage de trois heures du matin
      aurait écrit l'édition d'hier avec les tâches, le plancher et la météo
      d'aujourd'hui — la phrase du soir a un créneau de la veille à couvrir, l'édition
      n'en a pas. Trouvé en revue de code. Dans le même mouvement, l'éditorialiste lit
      ses sources **à la date demandée** (le matin de ce jour-là) quand ce n'est pas
      aujourd'hui.
- [x] **Une édition écrite pour un autre jour garde son drapeau.** L'essai du 27
      écrit le 21 aurait été servi tel quel le 27 — « aucune occurrence due » avec cinq
      tâches dues. Le drapeau reste levé, et le 27 au matin l'éditorialiste réécrit
      avec les faits du 27. Trouvé en revue de code.
- [x] **Deux premiers rendus en même temps ne font plus un 500.** L'appareil et un
      navigateur qui ouvrent la journée à la même seconde tentaient deux insertions
      sur l'index unique de la date ; le perdant relit et sert l'édition du gagnant.
      Un aperçu sans persistance lit **sans suivi** : l'édition composée pour l'essai ne
      reste pas modifiée dans le contexte. Trouvé en revue de code.
- [x] **`gerer_tache` en modification conserve l'échéance ferme omise**, comme il
      conserve l'échéance et les documents : un agent qui change l'assigné ne décoche
      pas le notaire en silence. Le PUT REST, lui, remplace tout — l'éditeur charge le
      champ. Trouvé en revue de code.
- [x] **La régénération à la main écrit la phrase avant l'édition** : sans clé API, le
      gabarit prend la phrase en manchette, et l'écrire après laissait l'ancienne au
      mur. Trouvé en revue de code.
- [x] **`DATE_DEMENAGEMENT` a quitté le code.** La phrase de repli du client portait
      la date du foyer en dur, dans un dépôt public ([[Distribution]]) ; elle compte
      maintenant les dodos du **prochain compte à rebours** de l'API — et sur l'écran,
      depuis la date composée par le serveur. Trouvé en revue de code, hors du
      périmètre de l'étape mais dans la règle du dépôt.
- [x] **Le rattrapage du démarrage et une régénération à la main se sont télescopés
      au release** : deux appels Opus pour le 21, une seule ligne possible sur l'index
      unique de la date, et la régénération à la main a rendu une exception
      (`23505`). Le même garde-fou que le rendu : le perdant relit la ligne du gagnant,
      et y pose son texte s'il devait remplacer. Vu en prod, corrigé dans la foulée
      (`fix` du 2026-09-21).
- [x] **Sept surtitres datés par le jour de semaine** : « Le dernier lundi… », « Le
      premier mercredi… », « Le samedi où… ». La mémoire des sept jours était bien
      dans le prompt, mais « ne reprends pas la formule » ne suffisait pas ; le prompt
      nomme maintenant le patron à éviter et demande un autre angle.

**Vérification** — test « un second rendu dans la même journée n'appelle pas le LLM »
(compteur sur un client fictif) ; test du repli sans clé API ; test de réédition
déclenchée par le plancher ; sept jours d'éditions en dev relues à la suite pour juger
le radotage.

**Vérifié** (dev, 2026-09-21) : `dotnet test` **901** verts (852 au départ : le
plancher, le rang, le gabarit, le parse strict, la génération sur Sqlite — dont « un
second rendu dans la même journée n'appelle pas le modèle », le repli sans clé par le
vrai rédacteur Anthropic, la réédition déclenchée par une échéance ferme et par un
compte à rebours, la mémoire des sept jours — le créneau du service, le signal, la
parité MCP de l'échéance ferme), `npm test` **224** (222 au départ : la chronique et
le troisième cas du plancher). Migration `AjouterEditionEtEcheanceFerme` appliquée au
démarrage sur le Postgres de dev, les trois colonnes jsonb écrites et relues. Au
démarrage, l'éditorialiste a rattrapé l'édition du jour avec Opus (rang chronique,
9 clés publiées) ; puis **sept éditions à la suite** (21 → 27 septembre) par
`regenerer_journal_mural` avec `date`, toutes écrites par le modèle, chacune avec la
mémoire des précédentes. Relues d'un trait : les surtitres ne se ressemblent plus
(« L'été s'en va demain soir, la maison ne demande rien », « Le camion et les
encombrants, le même 6 octobre », « Six degrés de plus qu'à pareille date l'an
dernier », « Onze heures cinquante-cinq de clarté pour emballer ») ; ce qui revient
d'un jour à l'autre, c'est la matière — les deux mêmes tâches en retard, le gel du
3 octobre, les trois minutes de clarté — et pas la formule. Un fait inventé : aucun
relevé ; chaque chiffre des sept éditions se retrouve dans la matière. Aperçus
1872×1404 des 21, 22 et 27 : chronique + deux apartés, chronique + liste + aparté,
plancher « en retard » en inversé — **aucun avertissement de débordement**, plus
aucune ligne coupée. Vu aussi en passant : deux appels refusés par l'API Anthropic
(`overloaded_error`, après les réessais du SDK) → gabarit, marqué tel quel, le mur
sort quand même — le repli fait son travail. **Laissé ouvert** : une édition tombée
en gabarit au créneau du matin le reste toute la journée ; réessayer une fois plus
tard dans la journée serait un choix à faire (coût : un appel), pas un défaut.

### 8 — Le rang 10 et plus

[[D-2026-09-20 Regroupement Sans Catégorie De Tâche]].

- [x] Regroupement par `Tache.ZoneId` puis `EquipementId` ; le paquet sans zone est
      nommé par l'éditorialiste (champ de rubriques de l'édition).
      (`Domaine/Editorial/Regroupement.cs` ; la matière porte la zone et l'équipement
      **en noms**, `TachePourEdition.Zone/Equipement` ; les noms sont lus une fois dans
      `LireLesSourcesAsync` — les zones et les équipements ne se lisent plus deux fois.)
- [x] Validation : toute tâche non affectée retombe dans « Le reste » ; toute tâche
      **inventée** par le modèle est rejetée — on ne montre que des occurrences réelles.
      (`RedactionLlm.Extraire` : seuls les titres **à nommer** — sans zone ni
      équipement — sont acceptés ; une rubrique nommée « Le reste » par le modèle est
      ignorée ; `Regroupement.Regrouper` refait le filtre côté rendu.)
- [x] Repli en liste plate groupée par zone quand l'API ne répond pas.
      (Le gabarit n'a pas de rubriques nommées : zone, équipement, « Le reste » ; une
      seule rubrique se lit en liste plate, sans titre pour rien.)
- [x] Sommaire en bande inversée, trois colonnes, widgets en bande de pied.
      (`EditionEcranDto.Rubriques`, `web/src/lib/ecran-vues.ts` : `groupesDeLaListe`,
      `colonnesDuSommaire`, `compteDeLaBande`, `tailleDeLaBande` ;
      `Ecran.tsx` : `BandeDuSommaire`, `ColonneDuSommaire`, `Annonce`.)

#### Ce que l'étape 8 a tranché en chemin

- [x] **[[D-2026-09-21 Réédition En Deux Temps]] est acceptée, amendée d'un second
      essai.** Les deux questions ouvertes de l'étape 7 ont la même réponse : un
      gabarit écrit alors qu'une clé est là, c'est un modèle qui n'a pas répondu
      (`overloaded_error` à 5 h 31, vu deux fois sur neuf appels le 2026-09-21) ; le
      service repasse **une fois**, une heure plus tard (`EditorialisteService.
      DelaiDeReessai`, `GenerationEdition.ReessayerAsync`), et plus jamais dans la
      journée. En mémoire, une fois par date ; un redémarrage redonne un essai. Sans
      clé (`IRedacteurEdition.PeutEcrire` faux), le gabarit est le fonctionnement
      normal et rien n'est réessayé.
- [x] **Le service vise le jour civil — pour de vrai.** La revue de l'étape 7 l'avait
      consigné, mais `DateDEdition` portait encore « la veille avant l'heure du
      matin » : le signal d'un rendu de nuit (le mur se réveille ~8 fois la nuit)
      tombait dans le vide jusqu'à 5 h 31. Posé, avec un test.
- [x] **Les rubriques servies sont vivantes, les noms sont figés.** L'édition ne porte
      que les rubriques nommées par l'éditorialiste (titres réels, tâches à nommer) ;
      `EditionEcranDto.Rubriques` est recalculé à chaque rendu sur les lignes servies :
      la zone ou l'équipement de la tâche range d'office (dans l'ordre d'arrivée, les
      retards d'abord), les rubriques nommées prennent ce qui reste, « Le reste » ferme
      la marche, et une ligne **faite** suit son titre — cochée à neuf heures, elle
      reste barrée sous « Gouvernements ». Servi dès **dix tâches dues**
      (`RangDuJour.SeuilDuSommaire`), plancher ou pas : un plancher change la
      manchette, pas la longueur de la liste. Le modèle, lui, n'écrit de rubriques
      qu'au rang « sommaire » (réglé à l'étape 7) ; au rang « événement » sur dix
      tâches, c'est le repli par zone.
- [x] **Les colonnes du sommaire se remplissent à la main, pas en `column-count`.**
      Un en-tête de rubrique n'est jamais orphelin en bas de colonne, un groupe coupé
      reprend sous un rappel de son nom, et une rubrique **entière par colonne** quand
      la place le permet (sinon au fil des rangées — jamais une ligne écartée pour un
      blanc). Un en-tête compte **une rangée serrée** (mesuré : ~65 px pour ~88 px) ;
      les faites cèdent leur place les premières ; ce qui ne tient pas est annoncé.
- [x] **« + N autres » est une rangée de la liste, pas une ligne sous le bloc.** Trouvé
      au rendu d'un jeudi d'essai à 36 tâches : posée sous le bloc, l'annonce lui
      volait ~50 px, et en colonnes CSS la septième rangée ne disparaissait pas vers
      le bas mais dans une **quatrième colonne, cachée à droite** — trois lignes
      perdues, et la garde ne voyait rien. C'était le 3 px du premier rendu de
      l'étape (28 lignes, plancher). Défaut de l'étape 1, corrigé sur les deux chemins
      (`listePlate`, la réserve de `colonnesDuSommaire`), comme le serveur le fait
      déjà (« la mention occupe la dernière place »).
- [x] **La garde de débordement voit ce que les colonnes cachent.** `debordementPx`
      mesure chaque élément contre le cadre **et** contre chaque ancêtre en
      `overflow-hidden`, vers le bas et vers la droite. Un clamp de texte n'est pas un
      débordement (il ne cache que du texte). C'est la garde que l'étape 7 réclamait.
- [x] **La bande inversée porte la manchette de l'éditorialiste**, pas « Journée
      chargée » : Opus en écrit une au rang « sommaire » (« Une adresse, et tout le
      monde à prévenir ») et personne ne la lisait. À droite, le compte **par personne**
      (« 3 Alain · 3 Ariane · 8 pour la maison »), comme dans la maquette — la dateline
      a déjà le total. La taille de la manchette suit la place qui reste à côté du
      compte (76 → 60 → 46 px, mesuré : ~0,39 em par signe de titraille, ~0,64 em de
      capitales espacées) : une manchette pliée sur deux lignes doublait la bande aux
      dépens de la liste, vu au rendu.
- [x] **Le prompt ne demande que le reste.** Les tâches avec `zone` ou `equipement`
      sont annoncées comme déjà rangées ; trois ou quatre rubriques de deux à cinq
      tâches, un nom court (30 signes), pas de « Le reste ». Sur la vraie journée du
      20 octobre, Opus a rendu **Gouvernements / Argent / Santé et assurances / Au
      travail** + « Le reste » (Amazon), 14 titres exacts, aucun inventé.
- [x] **Parité MCP** : `regenerer_journal_mural` rend `rubriques` (le compte de
      l'édition). Les rubriques servies sont un contrat de la page, web seulement.
- [x] Laissé tel quel, et noté : au sommaire les titres longs sont **tronqués avec des
      points de suspension** sur une rangée serrée (« Élections Canada — registre… »).
      C'est la règle « élaguer, pas rapetisser » de l'étape 1, visible et assumée ; la
      capacité (7 rangées par colonne) ne laisse pas deux lignes par titre.

#### Ce que la revue de code a corrigé, à l'étape 8

- [x] **Le modèle nomme dès dix tâches, plancher ou pas.** Le parse et le prompt
      gardaient les rubriques au seul rang « sommaire » ; or un plancher sur douze
      tâches fait un rang « événement », et la vraie journée du 20 octobre en aura
      sûrement un (les démarches d'octobre en retard). Le mur aurait montré son
      sommaire sans une seule rubrique nommée, le jour même pour lequel l'étape existe.
      Gate et prompt testent `JourneeChargee(TachesDues.Count)`.
- [x] **L'annonce a sa rangée même quand c'est le serveur qui a élagué.**
      `colonnesDuSommaire` ne réservait la place de « + N autres » que pour ce qu'il
      écartait lui-même ; 30 tâches dont 12 faites → 26 lignes servies, 21 unités
      justes, et l'annonce devenait une huitième rangée coupée en silence. Il reçoit
      les élaguées du serveur et compte tout.
- [x] **Le compte par personne se fait au serveur**, sur toutes les ouvertes
      (`DonneesEcran.Porteurs`) : les lignes servies sont plafonnées à 26, et la bande
      aurait dit « 12 Alain · 12 Ariane · 2 pour la maison » pour 36 tâches.
- [x] **Une édition écrite avant son créneau garde le drapeau** (`avantLeCreneau`) :
      avec le jour civil, un rattrapage à minuit dix aurait figé l'édition sur les
      faits de la nuit, et le créneau de 5 h 31 n'aurait rien réécrit — ni la ville
      poussée à 5 h 17, ni la météo du matin.
- [x] **Le second essai est borné et posé après coup.** La marque « fait » se pose
      après l'essai (une base indisponible pendant l'essai ne le consomme plus), un
      second échec **n'écrit rien** (le gabarit garde son heure), et l'essai ne vaut
      que dans la fenêtre d'une heure qui suit son moment — un redémarrage à midi ne
      rappelle pas le modèle pour le gabarit de 5 h 31.
- [x] **La bande cache ce qui dépasse, et la garde le voit** : `overflow-hidden` sur la
      bande, et `debordementPx` mesure aussi contre le bord droit du cadre. Une
      manchette en capitales plus large que l'estimation se verrait au journal, plus
      au mur. Les marges de l'estimation ont été élargies (0,46 em / 0,66 em).
- [x] **« le garage » et « Le garage » sont la même rubrique** : la fusion d'un nom de
      zone avec une rubrique nommée ignore la casse, comme « Le reste ».
- [x] La règle « à nommer » vit à un seul endroit (`TacheAGrouper.ANommer`) ;
      `TachePourEdition` s'y réfère.

**Vérification** — rendu de la journée du **2026-10-20** (14 tâches, la plus chargée de
toute la prod) ; test du repli sans LLM ; test « tâche inventée rejetée ».

**Vérifié** (dev, 2026-09-21) : tests
`RegroupementTests`, `Sans_llm_le_sommaire_retombe_sur_la_liste_groupee_par_zone`,
`La_journee_chargee_est_servie_en_rubriques_et_une_tache_inventee_est_rejetee`,
`Extraire_NeLaissePasLeModeleDeplacerUneTacheDejaRangee`, le second essai (deux
tests), le jour civil, la zone dans la matière ; côté web, le regroupement, le
remplissage des colonnes (entier, au fil, orphelins, annonce), le compte de la bande,
la taille de la bande, la liste plate. Rendus 1872×1404 par le chemin de l'appareil
(les tâches de dev sont une copie de la prod ; les 34 en retard ont été supprimées
pour que le 20 octobre soit la vraie journée à 14) : **2026-10-20** — 14 tâches, édition
Opus en 397 ms, 4 rubriques nommées + « Le reste », trois colonnes, bande de pied à
trois widgets, **0 px de débordement** ; **2026-10-22** — 36 tâches sans rubrique
(liste plate, 7 + 7 + 6 rangées + « + 16 autres »), 0 px ; **2026-10-24** — plancher
« en retard » sur 36 tâches, 0 px. Avant le correctif de l'annonce, le même jeudi
montrait 18 rangées pour 21 promises, et le premier rendu (plancher, 28 lignes)
rapportait 3 px.

### 9 — Calibration au mur

- [ ] `apercu.png` comparé au panneau réel : tailles à 2–3 m, lisibilité de la lettrine
      et des filets en 1-bit, aucun débordement sur une date longue (« dimanche
      20 septembre » fait 1046 px à 104 px).
- [ ] Vérifier que la densité n'a pas coûté de pile : `dernierContact` et tension dans
      `lister_appareils_affichage` sur deux semaines, comparées aux 3,79 V du
      2026-09-20.
      Relevés (B73199, reterminal_e1003, firmware 1.8.10) : **2026-09-20 matin 3,79 V** ·
      **2026-09-20 22 h 08 — 3,77 V, 52 %, RSSI −57 dBm**, après les étapes 1 à 5. Le
      prochain point utile est autour du 2026-10-04.
- [ ] Recap et mise à jour des specs à l'as-built (`status: implemented`, freshness).

## Vérification (globale)

- `npm test` dans `web/` — aucun test affaibli ni contourné. Base : 189 avant l'étape 1,
  217 après l'étape 4, 222 après l'étape 6 (un test est parti avec `quandCeJour`, dont
  le dernier appelant a disparu), 224 après l'étape 7, **233 après l'étape 8**
  (as of 2026-09-21).
- `dotnet test` — les trois couches ([[D-2026-08-25 Stratégie De Tests Trois Couches]]).
  Base : 753 verts après l'étape 4, 852 après l'étape 6, 901 après l'étape 7,
  **928 après l'étape 8** (as of 2026-09-21).
- Aperçu : `GET /api/affichage/apercu.png?largeur=1872&hauteur=1404` (cookie de session).
  Les données de dev portent depuis l'étape 1 une trentaine de tâches de test créées pour
  voir les rangs chargés (dix de plus à l'étape 2, toutes cochées à la fin) — jetables, à
  recréer ou à ignorer selon le besoin.
- **Où en est la prod** (as of 2026-09-21, 10 h) : le LXC 105 tourne l'**étape 8**
  (`86faebd`, puis `030fc2c` pour le vault). Rien de nouveau dans le `.env`, aucune
  migration. Vérifié au release : `/api/sante` 200, bundle `index-BzLh6msk.js` ;
  au démarrage l'éditorialiste a trouvé l'édition du 21 déjà écrite (aucun appel) ;
  régénération du mur par le MCP : 1872×1404, édition via Llm (rang manchette,
  0 rubrique — la journée n'est pas chargée), 32 125 o en 623 ms, aucun
  avertissement de débordement. Cache de build pruné avant (6,1 G rendus, 54 % → 27 %).
  Historique : au 2026-09-21 fin de journée la prod tournait
  l'étape 7 (`4352573` puis le `fix` de la course du release). Rien de nouveau
  dans le `.env` (`ANTHROPIC_API_KEY` y était, `Edition:Modele` a son défaut).
  Vérifié au release : `/api/sante` 200, bundle `index-CojqZ-s8.js`, migration
  `AjouterEditionEtEcheanceFerme` appliquée au démarrage, et dès le démarrage
  l'éditorialiste a écrit l'édition du 21 avec Opus (rang manchette, 7 clés
  publiées : « Treize séances d'affilée pour la même tâche / Quinze dodos, et des
  boîtes »). Régénération du mur par le MCP : 1872×1404, édition et phrase via Llm,
  image en 462 ms, aucun avertissement de débordement. Cache de build pruné avant
  (27 % du disque avant le build, 54 % après).
  Historique : au 2026-09-21 matin la prod tournait `e8e34ac`, soit les étapes 1 à 6. Posé à la main pour l'étape 6 : `HOUSEOS_POUSSEE_CLE` dans
  `/opt/house-os/.env` (la clé de la poussée, distincte de celle du MCP), les deux flux
  (« Collectes 2026 », ICS sur R2, 10 événements ; « Ville », poussé, 6 événements), la
  tâche annuelle « Régénérer le calendrier de collectes » (échéance 2027-01-12) et le
  cron du gratteur (`/etc/cron.d/houseos-scjc`, 5 h 17, avec
  `/opt/houseos-outils/{evenements.py,scjc.env}`). Vérifié au release : `/api/sante` 200,
  bundle `index-o6mXNDtJ.js`, migration `AjouterFluxExternePousse` appliquée (`Url`
  nullable, `Source` en `'Ics'` par défaut), premier tirage du gratteur en prod — 6
  événements reçus, retenus, aucun écarté — et régénération du mur par le MCP :
  1872×1404, 26 172 o en 657 ms, **aucun avertissement de débordement**. Cache de build
  prané : 9,0 Go rendus, le disque repasse de 54 % à 27 %.
  Historique : au 2026-09-20 la prod tournait `88deda1`, soit les étapes 1 à 5, plus la
  régénération à la demande du mur
  (`6e895ac`, qui avait manqué le train de l'étape 4). `MAISON_LIEU=17 rue de la Colline`
  a été posé dans `/opt/house-os/.env` à l'étape 4 (sauvegarde : `.env.avant-etape4`) ;
  `HASARD_FICHIER` reste absent, et c'est voulu — la banque québécoise livrée avec
  l'image est celle du foyer. **Rien de manuel à poser pour l'étape 5** : les normales
  suivent `METEO_LATITUDE`/`METEO_LONGITUDE`, déjà là.
  Vérifié au release : `/api/sante` 200, bundle `index-U9B5cAoO.js`, migration
  `AjouterNormalesClimatiques` appliquée au démarrage, banque lue (24 dictons,
  20 fêtes), et le **premier tirage réel de l'archive aux coordonnées de la prod**
  (46,8500 / −71,6200) — 3 652 journées, 9 saisons, **premier gel 3 octobre ± 10 j**,
  première neige 7 novembre ± 7 j, en 1,9 s. Les chiffres tiennent à travers les 35 km
  qui séparent la prod du dev. Régénération du mur par le MCP : 1872×1404, 25 570 o en
  662 ms, **aucun avertissement de débordement**.
  Historique de l'étape 4 : `b65eb2b`, bundle `index-tScc3tod.js`, `GET /api/display`
  200 en 751 ms.

> [!warning] Le release échoue « no space left on device » si le cache de build a grossi.
> **Confirmé au release de l'étape 5** : en une journée le cache était remonté à 6,1 G
> sur 10 G occupés. `docker builder prune -af` **avant** le build (54 % → 26 %, 8,8 G
> libres → 14 G), et la construction est passée sans incident. À faire systématiquement,
> pas seulement quand le disque a l'air plein — il avait l'air d'aller.
> Le 2026-09-20, `docker compose up -d --build` est tombé sur la couche Chromium alors
> que `df` annonçait 5,4 G libres : le **cache de build Docker** occupait 9,9 G des 14 G
> utilisés, et le disque ne manque qu'au pic d'extraction. `docker builder prune -af`
> (72 % → 26 %) puis rebuild. L'ancien conteneur continue de servir pendant l'échec —
> `/api/sante` reste à 200, il n'y a pas de panne à réparer dans l'urgence. Le disque
> avait déjà été agrandi 12 G → 20 G le matin même pour ce symptôme : c'était traiter
> la conséquence. Consigné aussi dans le dépôt d'infra (`unifi/CLAUDE.md`).
- Validateur du vault : `python3 ~/.claude/skills/vault/scripts/validate-vault.py vault`.
- Dépôt public : `grep -rni "villescjc\|jacques-cartier\|pdftotext\|colline" server/ web/`
  ne retourne rien ; tout ce qui est propre au foyer est dans le `.env` ou hors dépôt.
- Release par le chemin habituel, puis 200 sur `/api/sante` et nouveau bundle.

## Ce que ce plan ne fait pas

- La **lettre du matin** et l'envoi de courriel sortant — feature à part, plus tard.
- Les **actualités** municipales — reportées.
- Une **catégorie sur la tâche** — refusée explicitement.
- Le protocole de l'appareil, la cadence, la nuit, la grammaire 1-bit — inchangés.

## Risques connus

- **Pas de filet de bascule** : le remplacement direct signifie qu'une étape ratée se
  voit au mur. Mitigation — l'aperçu avant release, et `git revert` par étape.
- **Le débordement** est le mode de panne historique de cette vue (deux fois en un
  mois). La garde de l'étape 1 doit arriver avant la densité, pas après.
- **L'étape 6 peut vider les flux poussés** si le rafraîchissement ICS ne les filtre
  pas. Test dédié.
- **La famille « la maison » est muette la première année** : le rang 0 doit tenir sans
  elle jusqu'en septembre 2027.
