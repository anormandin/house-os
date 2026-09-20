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

- [ ] `server/HouseOs.Api/Domaine/Ephemerides/` : lever, coucher, midi solaire, durée du
      jour, dérive quotidienne, phase lunaire, équinoxes et solstices, changement
      d'heure. Formules NOAA écrites à la main, aucun NuGet.
- [ ] Tests contre des valeurs connues pour Québec **et** un second point (hémisphère
      sud ou haute latitude) : le dépôt est public, le calcul ne doit pas supposer le
      Québec ([[Distribution]]).
- [ ] `server/HouseOs.Api/Features/FondsDeTiroir/` : le fait (clé stable, famille,
      étiquette, valeur courte, texte long, composantes de score) et le moteur de score
      `rareté × fraîcheur × pertinence`. La fraîcheur lit un historique **vide** à cette
      étape — elle se branche à l'étape 7.
- [ ] Les faits du ciel, avec leur rareté ; la pertinence du jour tient compte des
      tâches du jour (« il fera noir à 18 h 25 » quand la journée est physique).
- [ ] `ComposerDonneesEcran` appelle le fonds de tiroir et remplit le budget de widgets
      du rang.

**Vérification** — tests du domaine sans base ; tests du score sur des cas fabriqués
(l'équinoxe bat une démarche d'adresse, la durée du jour ne bat rien) ; `apercu.png` un
jour vide montre le tableau du ciel et un jour chargé la demi-phrase.

### 3 — La maison et le calendrier

Que des lectures de tables existantes — le journal de complétion, les [[Équipements]],
les [[Comptes À Rebours]], les [[Documents]]. Aucune migration.

- [ ] Faits « la maison » : série en cours et record, N séances depuis, plus vieil
      équipement et son prochain entretien, zone la plus négligée, coût de l'année,
      anniversaires. « Ce jour-là l'an dernier » est écrit mais **ne donnera rien avant
      septembre 2027** — c'est un argument pour le bâtir tôt, pas tard.
- [ ] Faits « le calendrier » : compte à rebours actif, « ça s'en vient » (7–30 j),
      travaux de la saison (les fenêtres saisonnières du moteur, exposées comme donnée
      lisible), garantie ou document qui expire.
- [ ] Chaque fait rend une valeur courte **et** un texte long : le journal choisit selon
      le rang.

**Vérification** — `dotnet test` avec des fixtures de journal de complétion ; sur la
prod par [[Serveur MCP]] (`bilan_taches`, `lister_equipements`), vérifier que les
chiffres sortis correspondent au réel.

### 4 — Le hasard

- [ ] Banque locale de dictons météo québécois et table de fêtes et journées nationales,
      **en données de configuration, pas en dur** — un foyer ailleurs remplace le
      fichier ([[Distribution]]).
- [ ] Rareté « bouche-trou » : ces faits ne sortent que lorsqu'il ne reste rien d'autre.

**Vérification** — un jour de prod sans aucune tâche et sans événement remplit quand
même les sept widgets du rang 0.

### 5 — Les normales climatiques

[[D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo]]. Première migration du
chantier.

- [ ] Ingestion annuelle de l'archive Open-Meteo (ERA5) pour
      `METEO_LATITUDE`/`METEO_LONGITUDE`, sur ~10 ans, sur le patron de [[Météo]] — un
      `BackgroundService`, une table normalisée, migration EF générée
      ([[D-2026-08-23 Pas De N8n Dans Le Cœur]]).
- [ ] La clé de la table **porte les coordonnées utilisées** : changer le `.env`
      invalide et recalcule. Critique — le déménagement du 2026-10-06 change les
      coordonnées.
- [ ] Statistiques en C# testable : premier gel, première neige, dernière journée à 20°,
      mois le plus sec ou le plus pluvieux.
- [ ] Faits « le climat », y compris « il a fait X° ce jour-là l'an dernier » depuis les
      tables météo existantes.
- [ ] `docs/configuration.md` : rien de neuf à saisir, mais documenter que les normales
      suivent les coordonnées.

**Vérification** — `dotnet test` ; un appel réel à l'archive en dev, les normales
matérialisées relues et comparées à la connaissance du coin (premier gel début octobre à
Québec) ; couper le réseau et vérifier que l'édition sort quand même, sans le widget.

### 6 — La ville

[[D-2026-09-20 Sources Municipales Séparées Par Solidité]]. **Rien de municipal n'entre
dans le dépôt.**

- [ ] Hors dépôt : convertir le PDF 2026 des collectes en ICS et l'héberger. Le secteur
      se lit dans l'index des rues du PDF — « de la Colline » est au **secteur Sud**,
      jeudi. Documenter la recette dans `CLAUDE.local.md`.
- [ ] Abonner House OS à cet ICS comme [[Flux Externes]] de type `Collecte` : **zéro
      code**, le chemin existe déjà et `ProchaineCollecte` le lit déjà.
- [ ] Créer la tâche récurrente annuelle « régénérer le calendrier de collectes »
      ([[D-2026-09-20 Calendrier De Collectes Régénéré À La Main]]), échéance en début
      d'année civile.
- [ ] `FluxExterne.Url` nullable + marque de source ; migration EF
      ([[D-2026-09-20 Flux Externe Poussé]]).
- [ ] **Le rafraîchissement ICS doit ignorer les flux poussés** — sinon la passe de 6 h
      les vide. C'est le piège principal de l'étape : un test le couvre explicitement
      (`FluxExternesRafraichissement.cs`).
- [ ] Endpoint authentifié de poussée (remplacement en transaction, mêmes bornes de
      longueur que l'ICS), hors cookie de session ; UI de gestion sans champ URL, avec
      l'horodatage de dernière réception.
- [ ] **Parité MCP** ([[Serveur MCP]]) : les outils de gestion de flux suivent dans la
      même tranche.
- [ ] Hors dépôt : le gratteur SCJC (événements) qui pousse une fois par jour,
      User-Agent identifiable, `If-Modified-Since`.
- [ ] Faits « la ville », avec la règle : un flux poussé périmé **cesse de sortir** au
      lieu de mentir.

**Vérification** — `dotnet test` (dont le test de non-vidage) ; pousser deux fois le
même flux et vérifier le remplacement ; débrancher le gratteur une semaine et vérifier
que le journal sort sans widget « ville » ; `grep -rni "villescjc\|pdftotext" server/ web/`
ne retourne rien.

### 7 — L'éditorialiste

[[D-2026-09-20 Une Édition Par Jour Matérialisée]], [[D-2026-09-20 Édition Écrite Par Opus]].

- [ ] Entité d'édition sous `Domaine/` + migration EF : date, rang, textes rendus,
      **clés de widgets publiées** en JSONB, rubriques, source (LLM ou gabarit),
      horodatage.
- [ ] `BackgroundService` au créneau du matin, sur le patron de `HumeurService.cs` :
      rattrapage au démarrage, plancher d'une minute, aucun appel dans le chemin de
      requête.
- [ ] Prompt de l'édition, distinct de celui de [[Titre D'humeur]] : surtitre,
      manchette, chapeau, deux paragraphes, rubriques. **Aucun fait inventé** ; les
      chiffres et les titres viennent tous de l'état fourni.
- [ ] Parse défensif et validation stricte de la sortie, sur le patron de
      `PolissageLlm.Extraire` : tout écart → repli en gabarit.
- [ ] Mémoire des sept derniers jours : les sept dernières éditions alimentent la
      pénalité de fraîcheur du score **et** le prompt anti-radotage.
- [ ] Réglage de modèle propre à l'édition (`claude-opus-5` par défaut) ;
      [[Titre D'humeur]] garde Haiku. `docs/configuration.md` mis à jour.
- [ ] `ComposerDonneesEcran` sert l'édition matérialisée ; la liste, la météo, les
      cochées, l'heure et la pile restent recalculées à chaque rendu.
- [ ] **Réévaluation du plancher à chaque rendu** : compte à rebours à zéro, retard qui
      passe trois jours ou échéance ferme entrante → réédition.

**Vérification** — test « un second rendu dans la même journée n'appelle pas le LLM »
(compteur sur un client fictif) ; test du repli sans clé API ; test de réédition
déclenchée par le plancher ; sept jours d'éditions en dev relues à la suite pour juger
le radotage.

### 8 — Le rang 10 et plus

[[D-2026-09-20 Regroupement Sans Catégorie De Tâche]].

- [ ] Regroupement par `Tache.ZoneId` puis `EquipementId` ; le paquet sans zone est
      nommé par l'éditorialiste (champ de rubriques de l'édition).
- [ ] Validation : toute tâche non affectée retombe dans « Le reste » ; toute tâche
      **inventée** par le modèle est rejetée — on ne montre que des occurrences réelles.
- [ ] Repli en liste plate groupée par zone quand l'API ne répond pas.
- [ ] Sommaire en bande inversée, trois colonnes, widgets en bande de pied.

**Vérification** — rendu de la journée du **2026-10-20** (14 tâches, la plus chargée de
toute la prod) ; test du repli sans LLM ; test « tâche inventée rejetée ».

### 9 — Calibration au mur

- [ ] `apercu.png` comparé au panneau réel : tailles à 2–3 m, lisibilité de la lettrine
      et des filets en 1-bit, aucun débordement sur une date longue (« dimanche
      20 septembre » fait 1046 px à 104 px).
- [ ] Vérifier que la densité n'a pas coûté de pile : `dernierContact` et tension dans
      `lister_appareils_affichage` sur deux semaines, comparées aux 3,79 V du
      2026-09-20.
- [ ] Recap et mise à jour des specs à l'as-built (`status: implemented`, freshness).

## Vérification (globale)

- `npm test` dans `web/` — aucun test affaibli ni contourné. Base : 189 avant l'étape 1,
  **200 après** (as of 2026-09-20).
- `dotnet test` — les trois couches ([[D-2026-08-25 Stratégie De Tests Trois Couches]]).
  Base : **663 verts** après l'étape 1 (as of 2026-09-20).
- Aperçu : `GET /api/affichage/apercu.png?largeur=1872&hauteur=1404` (cookie de session).
  Les données de dev portent depuis l'étape 1 une quinzaine de tâches de test créées pour
  voir les rangs chargés — jetables, à recréer ou à ignorer selon le besoin.
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
