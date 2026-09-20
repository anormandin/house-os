---
type: plan
status: draft
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

- [ ] Choix de rang comme fonction pure dans `web/src/lib/ecran-vues.ts` : entrée =
      tâches dues, plancher déclenché, budget ; sortie = grille du corps et nombre de
      widgets. Tests unitaires sur les six rangs du tableau de bascule.
- [ ] Plancher (retard > 3 jours, compte à rebours à zéro, échéance ferme) comme
      fonction pure, testée séparément du rang.
- [ ] Réécriture de `web/src/pages/Ecran.tsx` : bloc-titre, manchette avec lettrine,
      trois colonnes aux filets, encadré de compte à rebours, pied. Classes partagées
      entre les rangs — la densité change, pas la grammaire.
- [ ] Le `Rangee` actuel devient la liste du jour dans la colonne du milieu ; météo,
      collecte et compte à rebours passent en widgets de la colonne de droite.
- [ ] `ComposerDonneesEcran.cs` : lever le plafond `MaxLignes = 10` (mesuré à ~27 avant
      saturation), et exposer ce qu'il faut au plancher (retard en jours, échéance
      ferme). Tests de composition mis à jour.
- [ ] **Garde anti-débordement** (le bug du 2026-09-20 se jouait à 50 px) : mesurer la
      hauteur de contenu au rendu et journaliser un avertissement Serilog quand elle
      dépasse la hauteur disponible — visible dans Seq ([[Observabilité]]).

**Vérification** — `npm test` dans `web/` (189 verts au départ) ; `dotnet test` ;
`apercu.png` à 1872×1404 sur une journée à 0, 1, 3 et 14 tâches (données de dev) ;
aucun avertissement de débordement dans le log.

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

- `npm test` dans `web/` — 189 verts au départ, aucun test affaibli ni contourné.
- `dotnet test` — les trois couches ([[D-2026-08-25 Stratégie De Tests Trois Couches]]).
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
