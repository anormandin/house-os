---
type: reference
last-verified: 2026-08-26
verified-against: d52fad1
tags: []
---

# Banque D'idées

Sac d'idées de features pour House OS, tiré d'un balayage du marché (2026-08-26) :
110 apps proches ou lointaines, catégorisées et notées 0-10 selon leur proximité
avec House OS. Rapport brut complet (par app : URL, prix, cas d'usage, features
détaillées) : `docs/research/2026-08-26-balayage-apps-domaine.md`. Complète
[[Apps Similaires]] (qui couvre les patterns/anti-patterns d'UI) — ici, ce sont
les **mécaniques et modules** à considérer, sans engagement de roadmap.

Barème : 9-10 = même domaine cœur · 6-8 = fort recouvrement sur un module ·
3-5 = adjacent · 0-2 = à peine lié. Pas de captures d'écran (recherche par
agents) — les URL font foi.

## Idées par thème

### Moteur de récurrence et tâches

- **Double déclencheur : date OU compteur d'usage, « premier atteint »** (LubeLogger,
  Simply Auto, MaintainX) — la norme de l'industrie véhicule/CMMS ; plan exact du
  mode « échéance par capteur » v3. LubeLogger matérialise même la prochaine
  échéance à la complétion, comme House OS.
- **Intervalle adaptatif** (Donetick) — suggérer un intervalle appris du journal de
  complétions réel plutôt que figé.
- **Multiplicateur saisonnier sur l'intervalle** (Planta) — arroser aux 4 jours
  l'été, aux 10 jours l'hiver : un facteur par saison sur le mode
  intervalle-depuis-complétion.
- **Routines qui se réarment** (Hearth Display, HomeRoutines) — checklist ordonnée
  liée à un moment (matin/soir), jamais « en retard », progression par personne :
  un objet distinct de la tâche à échéance.
- **« Focus zone » tournante** (HomeRoutines) — chaque semaine, une pièce vedette ;
  se marie aux Zones et à l'estompage déjà en place dans la vue Pièces.
- **Budget d'effort quotidien** (Sweepy) — effort 1-3 par tâche + budget par
  personne → la vue Aujourd'hui dose au lieu de tout lister ; filtre « je suis
  fatigué, montre les faciles ».
- **Équité pondérée par l'effort, pas le compte** (ChoreBuster) — stratégie
  « moins-d'effort-fourni » ; part de charge configurable (60/40) ; préférences
  aimées/détestées qui biaisent l'assignation.
- **Sous-tâches qui se réinitialisent à chaque occurrence** (Donetick).
- **Saisie rétroactive honnête** (Grocy) — compléter en précisant qui et quand
  (date passée), pas seulement « maintenant ».
- **« Reporter / déjà fait » qui recale l'horaire** (Planta) — cousin du rollover,
  sans dette de retard.
- **Procédures : checklist réutilisable attachée à une tâche récurrente**
  (MaintainX, AUTOsist) — chaque étape cochable à la complétion ; « inspection
  pré-hiver de la souffleuse » comme gabarit.
- **Métadonnées de guide sur la tâche** (Upkept, MoveAdvisor) — verdict
  DIY-vs-pro, instructions pas-à-pas, matériel requis (taille du filtre) portés
  par la définition de tâche.
- **Alternance une semaine sur deux** comme mode de première classe (Chorsee).
- **Rotation « aléatoire »** en 3e stratégie d'assignation (Donetick).

### Équipements et actifs

- **Composants imbriqués** (DumbAssets) — fournaise → filtre → format du filtre,
  chacun avec SA garantie, SES documents, SON entretien ; les consommables
  s'attachent à l'équipement qu'ils servent.
- **ID d'actif lisible + étiquettes QR imprimables** (Homebox, Shelf.nu) — #000-001
  collé sur l'objet/le bac ; le QR ouvre une page web mobile sans app. Utile dès
  le déménagement (boîtes).
- **Photo de plaque → fiche complète** (Dib, Centriq, HomeZada « Zada AI ») —
  LLM-vision lit marque/modèle/série et pré-remplit `gerer_equipement` ; recherche
  automatique du manuel officiel (Homer « AutoMagic », Recevity).
- **Veille des rappels (recalls) par marque/modèle/série** (HomeBinder, CARFAX) —
  un BackgroundService qui interroge les flux de rappels (Transport Canada/NHTSA)
  et crée une tâche quand un rappel sort.
- **Compteur d'usage de première classe** (LubeLogger) — odomètre/heures avancé
  par chaque entrée de journal ; unités agnostiques (km, heures, kWh, cycles)
  pour tracteur, génératrice, souffleuse.
- **Coût de possession par équipement** (LubeLogger, MaintainX, Drivvo) — le
  Journal porte déjà le coût ; il manque le roll-up par équipement, et le
  coût-par-heure/km comme métrique vedette.
- **Budget de remplacement** (HomeManager) — provision mensuelle calculée par
  gros item (toiture, thermopompe) depuis durée de vie et coût attendus.
- **Gabarits d'entretien par type d'équipement** (Fuelly, Centriq) — un pack de
  tâches recommandées avec intervalles à l'ajout d'une souffleuse/scie à
  chaîne/génératrice ; bibliothèque de fréquences par défaut (Sweepy).
- **Incident ≠ entretien planifié** (micasa, HomeLogger) — un bris ou une « tache
  au plafond » est une entité distincte (sévérité, photos) qui peut engendrer une
  tâche ; House OS n'a pas de maison pour ça.
- **La maison elle-même comme objet inventorié** (Under My Roof, Homer) — fiche de
  pièce : couleur/code de peinture, superficie, où acheté.

### Documents et garanties

- **Adresse courriel d'ingestion** (Papra, Maple, Ohai) — transférer une facture
  suffit à l'archiver ; ingesteur IMAP + LLM qui classe.
- **Enrichissement LLM à l'ingestion** (paperless-ai, Docspell) — dossier, facettes
  et équipement lié proposés automatiquement à l'upload, prompt contraint aux
  facettes existantes pour éviter l'explosion de taxonomie.
- **Date d'échéance de premier ordre sur un document** (Docspell, Tracktor) —
  assurance auto, immatriculation SAAQ, certificat : facette « expire le » qui
  alimente les comptes à rebours existants.
- **Garantie en un geste** (Warranty Tracker, Recevity) — photo du reçu → OCR
  extrait date d'achat et durée → rappels échelonnés 90/30/7 jours armés seuls ;
  fenêtre de retour distincte de la garantie.
- **Workflows planifiés sur dates** (Paperless-ngx) — « garantie expire dans
  30 jours » → action (tag, webhook, tâche).
- **Documents liés aux entrées de journal, pas juste à l'équipement**
  (LubeLogger) — la facture de la réparation vit sur la réparation.
- **Coffre « sensible »** (FamilyWall) — facette chiffrée/restreinte pour pièces
  d'identité et codes ; cousin du module « Secrets » de Homechart (wifi, NIP,
  cadenas — ce qu'un couple se retexte sans cesse).
- **« Exporter ma maison »** (Real Estate Ledger, HomyScan, Homer) — dossier PDF
  généré (équipements + documents + historique) pour assurance ou revente ;
  le cartable se transmet au prochain propriétaire.

### Consommables et épicerie (module futur)

- **Réapprovisionnement par stock minimum** (Grocy, Sortly) — sous le seuil →
  liste d'achats ; **une corvée consomme un produit** (pastille de
  lave-vaisselle) : le pont corvée ↔ stock.
- **Journal de stock matérialisé** (Grocy) — chaque achat/consommation est une
  écriture datée avec prix : coût unitaire et historique gratuits.
- **État « ouvert » distinct** qui raccourcit la péremption effective (Grocy).
- **Saisie deux champs** (BEEP, Pantry Check) — code-barres remplit nom+image
  (Open Food Facts), l'humain ne tape que la date.
- **Recettes classées par « Due Score »** (Grocy) / **par % d'ingrédients en
  stock** (KitchenPal) ; manquants → liste en un tap.
- **Liste triée par allées de SON épicerie** (Tandoor, AnyList, Bring!) ; sync
  temps réel à deux dans le magasin (KitchenOwl).
- **Dépenses d'épicerie rattachées au foyer** (KitchenOwl, Flatastic).

### Zones et terrain

- **Hiérarchie d'emplacements imbriquée** (Homebox : Maison → Pièce → Armoire →
  Boîte) — les Zones actuelles sont plates.
- **Carte de chaleur par pièce** (Tody) — pastille par zone selon les tâches dues.
- **Plan du terrain calibré sur image satellite** (Open Garden Planner,
  Magicplan) — une Zone extérieure porte un croquis cliquable ; pièces avec
  dimensions réelles (LiDAR).
- **Specs JSONB d'une zone extérieure** (Sunday) — analyse de sol (pH, texture)
  stockée et réutilisée par les règles.
- **Annoter une photo** (Gardenize) — où sont les bulbes, où est la valve d'hiver.

### Météo, saisons et jardin

- **Fenêtres saisonnières calculées, pas saisies** (Seedtime) — dates de gel du
  lieu (zone 4, Sainte-Catherine) → fenêtres de semis/plantation dérivées ;
  décaler une plantation décale en cascade semis → repiquage → récolte.
- **Skip-quand-il-a-plu** (GARDENA) — la pluie récente ou annoncée suspend
  l'occurrence d'arrosage : règle Open-Meteo pure, zéro matériel.
- **Alerte de gel → tâches auto** (GARDENA, Oply) — « rentrer les plantes »,
  « fermer la valve extérieure » : critique en zone 4.
- **Ancres astronomiques** (GARDENA, Grocy) — récurrence calée sur lever/coucher
  du soleil ; mode nuit dérivé du coucher.
- **Programme annuel dérivé du climat + terrain** (Sunday, HomeZada, HomeBeacon,
  Thumbtack) — interview de 5 min (type de maison, systèmes, climat) → calendrier
  d'entretien saisonnier pré-rempli, ajustable ensuite : LE wizard de démarrage
  qui manque à House OS.
- **Une plante = un équipement vivant** (Plant-it, HortusFox, Gardenize) —
  intervalle-depuis-dernier-événement par type de soin (arrosage ≠ fertilisation),
  journal photo multi-années par plate-bande, identification par photo
  (Pl@ntNet) ; le modèle Plante–Surface–Événement de Gardenize est un décalque
  d'Équipement–Zone–Journal.

### Énergie et Hydro-Québec

- **Événement de pointe en phases exploitables** (HydroQC) — pré-chauffe / ancre /
  pointe / retour, pas juste début-fin ; commencer par l'open data
  `evenements-pointe` sans login ; **pointes poussées dans le flux iCal existant**.
- **La pointe comme « défi » acceptable/refusable la veille** (Hilo) — avec bilan
  chiffré le lendemain et cumul saisonnier des crédits en $ ; modes d'intensité
  nommés (Modéré/Intrépide/Extrême) plutôt que des degrés.
- **La conso comme capteur d'événements, pas de kWh** (Sense, Emporia) — « la
  sécheuse a fini » → tâche vider le filtre ; « la pompe de puisard n'a pas tourné
  depuis X » → alerte ; compteur de cycles → entretien à l'usage réel.
- **Modèle de données en flux + stats long-terme downsamplées** (Home Assistant
  Énergie) — schéma de référence pour ne pas noyer Postgres sous le MQTT ;
  pipeline de normalisation AVANT stockage (emonCMS).
- **Ordonnancement par prix** (evcc) — « atteindre X avant T au meilleur coût » :
  décaler sécheuse/chauffe-eau hors pointe.

### Mur, e-ink et IoT (phase 3)

- **Terminal muet** (TRMNL, MagInkDash) — le serveur rend un BMP 1-bit
  (HTML→image avec la stack web existante), l'appareil à pile ne fait
  qu'afficher : le plan e-ink validé commercialement ; layouts en
  moitiés/quadrants, playlists d'écrans côté serveur.
- **Layouts par plage horaire + mode nuit noir** (DAKboard) — écran « matin » ≠
  « soirée » ; compte à rebours permanent vers une date (le déménagement !).
- **Cartes conditionnelles** (Home Assistant) — n'afficher que l'actionnable :
  « collecte demain » visible seulement la veille ; visibilité par utilisateur et
  par appareil.
- **Pages openHASP poussées en JSONL par MQTT** — « les 3 tâches de cette pièce »
  sur la plaque murale de la pièce, redéfinies à chaud.
- **Tablette pilotée par le serveur** (Fully Kiosk) — réveil par détection de
  mouvement, API REST/MQTT (luminosité, TTS : « sortir le bac bleu ce soir »).
- **Tuiles tapables à icônes** (Bring!) — réapprovisionner = taper des images,
  pas taper du texte : idéal tablette murale.
- **Vue TV en lecture seule** (Mango Display) — réutiliser la TV du salon, zéro
  matériel neuf.
- **Endpoint « widget » JSON stable** (Glance) — occurrences du jour consommables
  par Glance/TRMNL/MagicMirror sans écrire de client ; l'ICS existant alimente
  déjà tel quel un cadre e-paper du commerce (Invisible Calendar).

### IA et MCP

- **Magic Import** (Skylight, Ohai, Hearth) — photo d'un horaire papier, courriel
  transféré, PDF → événements/tâches extraits ; House OS a déjà le canal MCP.
- **Routines IA planifiées** (Maple) — « chaque dimanche, génère le plan de la
  semaine », « chaque soir 21 h, propose la réassignation des retards ».
- **Photo d'un problème → tâches correctives** (PictureThis, Oply) — diagnostic
  par photo (plante malade, rapport d'inspection PDF) → backlog structuré.
- **Chat avec sa maison** (Dib, Scanlily, micasa) — « où est le Nikon ? »,
  « c'est quoi le filtre de la fournaise ? » : démo MCP naturelle.
- **Quick-add en langage naturel** (Donetick, Pistachio) — « poubelles chaque
  lundi 18 h » parsé ; House OS n'a qu'à parser NOS formulations.

### Vie de couple (sans gamification imposée)

- **Points « merci » offerts manuellement** (Nipto) — reconnaître une tâche
  hors-liste : mécanique de reconnaissance quasi gratuite sur le journal.
- **Fil de discussion sur l'occurrence** (TimeTree) — la coordination vit sur la
  tâche, pas dans un messenger.
- **Répartition objectivée, discrète** (Nipto, Sweepy) — « cette semaine :
  toi 6 · moi 5 » depuis le journal ; jamais de classement imposé (deux lectures
  au choix : compétition ou objectif personnel).
- **Capture inbox à deux vitesses** (Hammond, Encircle) — l'un photographie
  (reçu, boîte, plaque), l'autre structure plus tard : découpler capture et
  saisie.
- **Le foyer comme unité** (OneHaus, AnyList) — un abonnement/une config pour la
  maisonnée ; rôles éditeur/lecteur suffisent (HomyScan).

### Argent de la maison (idée d'Alain, 2026-08-26)

> [!note] Devenue une feature : voir [[Budget]] (spec approuvée + plan + 4 décisions).

Le plan d'Alain : **un compte bancaire réel dédié au fonds de prévoyance** —
dépôt mensuel, retraits pour les taxes et pour les équipements à réparer ou
remplacer (ex. repeindre la toiture métallique), plus des **projets** vers
lesquels mettre de l'argent (rénover une pièce). Modèle qui en découle :

- **Un compte réel, des enveloppes virtuelles** — invariant de rapprochement :
  solde du compte synchronisé = somme des enveloppes (équipements, taxes,
  projets) ; tout écart est visible.
- **Contribution dérivée du moteur de récurrence** — « repeindre la toiture :
  tous les ~10 ans, ~4 500 $ » → l'enveloppe connaît sa cible et son échéance
  depuis la tâche récurrente elle-même, la provision mensuelle se calcule
  seule ; pareil pour tout gros entretien cyclique.
- **Taxes = enveloppes à échéancier connu** — municipales (versements datés) et
  scolaires : cible et dates certaines, provision lissée sur l'année.
- **Projets = enveloppes libres** — cible définie par le projet (rénover une
  pièce), on y met de l'argent, les dépenses du projet s'y rattachent.
- **Le virement mensuel comme tâche House OS** — « virer X $ au fonds » le
  1ᵉʳ du mois, X = somme des provisions courantes ; complétée quand la
  transaction synchronisée apparaît.

Idées du marché à l'appui :

- **Le Journal comme grand livre** — chaque complétion porte déjà un coût ; les
  roll-ups par équipement/projet/catégorie sont la fondation naturelle d'un
  module budget (voir coût de possession, plus haut).
- **Fonds de prévoyance par équipement** (HomeManager, HomeZada) — provision
  mensuelle calculée : (coût de remplacement − épargné) ÷ mois de vie utile
  restants, par gros item (toiture, thermopompe, chauffe-eau) ; coût de
  remplacement distinct du prix d'achat.
- **Budget de projet : estimé / engagé / réel** (HomeZada, Billdr, micasa) —
  soumissions comparées, « change orders » comme delta tracé contre le budget
  d'origine plutôt qu'édition silencieuse, paiements par jalons.
- **Dépenses rattachées aux entités du foyer** (Homechart, KitchenOwl,
  Flatastic) — le budget vit dans le même graphe que tâches/équipements/projets,
  pas dans une app à part.
- **Renouvellements et abonnements avec rappels** (Wallos, OneHaus) — déjà noté
  côté documents/contrats ; c'est aussi une ligne budgétaire récurrente.
- **Compte dédié synchronisé** (hors balayage : Actual Budget, Firefly III) —
  un vrai compte « maison » dont les transactions entrent seules :
  **SimpleFIN Bridge** (~15 $US/an) couvre les banques canadiennes, dont
  Desjardins, avec un rafraîchissement quotidien ; GoCardless côté UE. Pour
  House OS : un BackgroundService d'ingestion de plus (même patron que
  météo/HQ), tables normalisées, puis **rapprochement transaction ↔ maison**
  (règles par marchand + suggestion LLM via MCP : « Canadian Tire 84,12 $ →
  lier au journal “Huile à souffleuse” ? »).
- **Benchmarks de coûts** (Houzz Real Cost Finder, Thumbtack price index) —
  comparer le coût réel du journal à la norme régionale.

### Démarrage à froid et déménagement

- **Gabarit à rebours depuis une date** (MoveAdvisor, Moved) — la date du
  déménagement instancie un plan complet en offsets relatifs (T-8 sem. :
  réserver ; T-2 : transferts) ; version québécoise : Hydro, RAMQ, SAAQ,
  Postes Canada.
- **Boîtes QR + dictée vocale + photo avant de sceller** (BoxBuddy, Homebox) —
  « dans quelle boîte est le tire-bouchon ? » pendant le déballage.
- **Semer depuis les données publiques** (Dwellin, HomeBinder) — année de
  construction → âges probables des systèmes ; rapport d'inspection → liste
  d'équipements et backlog initial.
- **Import CSV assumé** (Homebox, Hammond, Homer) — y compris « secourir les
  données des concurrents morts ».

## Catalogue par catégorie (score ≥ proximité House OS)

### Corvées et récurrence

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [Donetick](https://github.com/donetick/donetick) | 10 | open core (AGPL + cloud payant) | Le concurrent direct : NFC, « Things » capteurs, intervalle adaptatif |
| [Grocy — corvées](https://grocy.info/) | 9 | gratuit, OSS | Corvée qui consomme un produit ; saisie rétroactive |
| [Tody](https://todyapp.com/) | 7 | achat unique + abo sync | Jauge de saleté continue ; « optimal day » sans culpabilité |
| [Sweepy](https://sweepy.com/) | 7 | freemium ~20 $US/an | Budget d'effort quotidien par personne |
| [ChoreBuster](https://www.chorebuster.net/) | 6 | abo 19,95 $US/an ou 29,95 $US à vie | Équité pondérée par difficulté ; préférences |
| [Nipto](https://nipto.app/) | 6 | freemium ~2 $US/mois | Points « merci » entre partenaires ; cycle hebdo |
| [Flatastic](https://www.flatastic-app.com/en/) | 6 | freemium ~18 €/an | Corvées + épicerie + dépenses en un foyer virtuel |
| [HomeRoutines](https://www.homeroutines.com/) | 5 | achat unique iOS | Focus zone tournante ; minuteur 15 min |
| [Vikunja](https://vikunja.io/) | 4 | open core | CalDAV bidirectionnel ; dépendances entre tâches |
| [Chorsee](https://chorsee.com/) | 2 | gratuit | Preuve photo par corvée (le reste : enfants, hors cible) |

### Inventaire et actifs

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [Homebox](https://homebox.software/) | 9 | gratuit, OSS | ID d'actifs + étiquettes QR ; emplacements imbriqués |
| [HomeZada](https://www.homezada.com/) | 9 | abo 65-99 $US/an | L'équivalent commercial complet de la vision ; calendrier auto par climat |
| [DumbAssets](https://github.com/DumbWareio/DumbAssets) | 8 | gratuit, OSS | Hiérarchie composants ; notifications Apprise (80+ canaux) |
| [Under My Roof](https://apps.apple.com/us/app/under-my-roof-home-inventory/id1524335878) | 8 | freemium 24,99 $US/an | Saisie vocale par pièce ; la maison comme objet |
| [Itemtopia](https://www.itemtopia.com/) | 7 | freemium ~39 $US/an | Rappel 30 j avant fin de garantie ; partage sélectif |
| [Scanlily](https://www.scanlily.com/) | 7 | abo + vente d'étiquettes QR | Recherche en langage naturel ; scan de boîte = contenu |
| [Shelf.nu](https://github.com/Shelf-nu/shelf.nu) | 6 | open core | QR sans app ; garde (qui l'a) ; réservation d'objets |
| [Nest Egg](https://nestegg.cloud/home-inventory/) | 6 | abo | Quantités/stock sur objets ; transfert entre emplacements |
| [HomyScan](https://homyscan.com/) | 6 | freemium | PDF assurance en un tap ; rôles éditeur/lecteur |
| [MyStuff2 Pro](https://www.maddysoft.com/mystuff/) | 6 | achat unique | Schémas de champs par catégorie (≈ JSONB à gabarits) |
| [Sortly](https://www.sortly.com/) | 5 | abo B2B 49 $US/mois+ | Navigation photo-first ; designer d'étiquettes |
| [Encircle](https://www.getencircle.com/) | 4 | B2B assurance | Photos horodatées GPS « preuve » ; consommateur abandonné (déc. 2025) |

### Entretien de la maison

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [micasa](https://micasa.dev/) | 8 | gratuit, OSS (TUI) | Soumissions comparées ; incidents ; LLM local sur la maison |
| [Oply](https://www.oply.app/) | 7 | abo + marketplace | Prédictif (climat+âge+historique) ; inspection PDF → tâches ; Home Score |
| [Homer](https://www.homer.co/) | 7 | freemium ~5 $US/mois | AutoMagic (manuel auto) ; timeline de la maison ; recettes de peinture |
| [Dwellin](https://dwellin.com/) | 6 | freemium 24,99 $US/an | Semé des données publiques (âges probables des systèmes) |
| [Upkept](https://www.consumerreports.org/home-maintenance-repairs/upkept-home-app-from-our-president-september-2021) | 6 | abo (moribond) | Verdict DIY-vs-pro par tâche ; tâche = mini-guide |
| [HomeManager](https://homemanager.io/) | 6 | freemium 80 $US/an | Budget de remplacement (provision mensuelle par gros item) |
| [HomeLogger](https://github.com/masoncfrancis/homelogger) | 6 | gratuit, OSS | Réparation ≠ entretien ; sauvegarde ZIP un fichier |
| [Thumbtack](https://www.thumbtack.com/) | 5 | leads pros | Guides saisonniers par maison ; index de prix par travaux |
| [MaintainX](https://www.getmaintainx.com/) | 5 | SaaS par siège | Le CMMS mûr : compteurs-seuils, procédures, pièces décrémentées |

### Garde-manger, épicerie et recettes

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [Grocy](https://grocy.info/) | 9 | gratuit, OSS | Stock min → liste ; Due Score ; état « ouvert » ; piles |
| [Mealie](https://mealie.io/) | 7 | gratuit, OSS | Import IA ; règles de planification ; webhooks planifiés |
| [KitchenOwl](https://kitchenowl.org/) | 7 | gratuit, OSS | Dépenses partagées ; sync temps réel en magasin |
| [Tandoor](https://tandoor.dev/) | 6 | open core (1,99-4,99 €/mois) | Tri par allées ; plan de repas → iCal |
| [Pantry Check](https://pantrycheck.com/) | 6 | freemium (cap 200 items) | Rappels de péremption matérialisés |
| [KitchenPal](https://kitchenpalapp.com/en/) | 6 | freemium + à vie 29,99 $US | Recettes par % d'ingrédients en stock |
| [AnyList](https://www.anylist.com/) | 5 | abo foyer 14,99 $US/an | Autocatégorisation ; suggestions depuis l'historique |
| [Out of Milk](https://outofmilk.com/) | 5 | freemium | Garde-manger ↔ liste en un tap ; les gens paient pour ça |
| [BEEP](https://www.beepscan.com/) | 5 | gratuit | Saisie deux champs (code-barres + date) |
| [My Pantry Tracker](https://mypantrytracker.com/) | 5 | sync payante seule | Web pour le lot, mobile pour le scan |
| [Bring!](https://www.getbring.com/en/home) | 4 | pubs détaillants | Tuiles à icônes zéro-frappe |

### Organiseurs de foyer

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [Homechart](https://homechart.app/) | 9 | freemium + à vie 149,99 $US | Le cousin idéologique : repas→liste, Secrets, wiki du foyer |
| [OneHaus](https://onehaus.app/) | 7 | abo 44,99 £/an | Corvées + actifs + abonnements/contrats avec rappels |
| [Cozi](https://www.cozi.com/) | 6 | freemium + pubs | Couleur par personne partout ; courriel-agenda du matin |
| [FamilyWall](https://www.familywall.com/) | 6 | freemium 44,99 $US/an | Coffre « Safe » ; géorepérage |
| [Maple](https://www.growmaple.com/) | 6 | mort (sunset 2026-12-31) | Courriel du foyer → tâches ; routines IA ; argument self-hosted |
| [Ohai.ai](https://www.ohai.ai/) | 5 | abo par sièges 9,99 $US+/mois | Assistante SMS-first ; digest quotidien proactif |
| [Pistachio](https://heypistachio.com/) | 5 | gratuit | « Pas un autre calendrier » — valide la stratégie flux iCal |
| [TimeTree](https://timetreeapp.com/intl/en) | 4 | freemium + pubs | Discussion et pièces jointes sur l'événement ; « Keep » sans date |

### Documents et garanties

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [Paperless-ngx](https://docs.paperless-ngx.com/) | 8 | gratuit, OSS | Consume folder ; classifieur appris ; workflows sur dates |
| [Recevity](https://recevity.com/) | 8 | freemium 29 $US/an | Reçu → fiche ; manuel auto ; fenêtre de retour ; recalls |
| [Dib](https://dib.io/) | 8 | freemium (bêta) | Photo de plaque → fiche ; chat avec sa maison |
| [Docspell](https://docspell.org/) | 7 | gratuit, OSS | Échéance extraite du texte ; multi-personnes « collective » |
| [paperless-ai](https://github.com/clusterzx/paperless-ai) | 7 | gratuit, OSS | LLM (Ollama local) qui classe à l'ingestion ; RAG |
| [Centriq](https://dib.io/blog/centriq-shutting-down-alternative) | 7 | mort (jan. 2026, données purgées) | Plaque → manuel/pièces/entretien ; leçon anti-cloud |
| [Papra](https://papra.app/) | 6 | freemium OSS | Adresse courriel d'ingestion ; règles déclaratives ; webhooks |
| [Warranty Tracker](https://www.warrantytracker.app/) | 5 | freemium | Garantie en un geste ; rappels 90/30/7 |

### Affichages muraux et e-ink

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [TRMNL](https://usetrmnl.com) | 7 | matériel 139 $US, zéro abo, BYOS | Terminal muet ; recipes/mashups/playlists |
| [DAKboard](https://dakboard.com) | 7 | freemium + matériel | Layouts par plage horaire ; mode nuit ; compte à rebours |
| [MagInkDash](https://github.com/speedyg0nz/MagInkDash) | 7 | gratuit, OSS DIY | Pipeline HTML→image→e-ink à pile |
| [Skylight](https://myskylight.com/calendar) | 7 | matériel 330-600 $US + abo 79 $US/an | Magic Import ; tap-pour-compléter au mur |
| [Hearth Display](https://hearthdisplay.com/) | 6 | matériel 699 $US + abo | Routines séquencées ; portrait assumé ; Helper par SMS |
| [MagicMirror²](https://magicmirror.builders) | 6 | gratuit, OSS | Bus de notifications inter-modules ; 1 000+ modules |
| [HA Dashboards](https://www.home-assistant.io/dashboards/) | 6 | gratuit, OSS | Cartes conditionnelles ; visibilité par user/appareil |
| [openHASP](https://www.openhasp.com/) | 6 | gratuit, OSS | Pages JSONL par MQTT sur écrans ~20 $ |
| [Invisible Calendar](https://www.invisible-computers.com/) | 5 | matériel 149 $US sans abo | Mange n'importe quel ICS — le flux House OS suffit |
| [Fully Kiosk](https://www.fully-kiosk.com/) | 5 | licence 7,90 $US/appareil | Réveil par mouvement ; API REST/MQTT ; TTS |
| [Mango Display](https://mangodisplay.com/) | 5 | freemium | La TV existante comme affichage, zéro matériel |
| [Homarr](https://homarr.dev) | 4 | gratuit, OSS | Widgets temps réel WebSockets ; calendrier agrégé |
| [Glance](https://github.com/glanceapp/glance) | 4 | gratuit, OSS | Widget custom-api : tout JSON devient un bloc |

### Projets, réno et déménagement

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [HomeBeacon](https://homebeacon.app/) | 7 | freemium (par propriété) | Interview 5 min → calendrier saisonnier ; matériel par tâche |
| [MoveAdvisor](https://moveadvisor.com/) | 6 | gratuit (leads déménageurs) | Plan à rebours depuis la date du déménagement |
| [HomeBinder](https://pages.homebinder.com/) | 6 | B2B2C (agents paient) | Inspection → backlog initial ; alertes de rappels |
| [BoxBuddy](https://boxbuddy.tech/) | 5 | abo 19,99 $US/an | Boîtes QR + dictée + photo avant scellage |
| [Real Estate Ledger](https://realestateledger.io/) | 5 | freemium | Classement 3 facettes auto ; « Property Guidebook » exportable |
| [Houzz](https://www.houzz.com/) | 4 | marketplace/pubs | Real Cost Finder ; ideabooks (inbox d'inspiration pré-projet) |
| [Magicplan](https://magicplan.app/) | 4 | freemium + crédits | Scan LiDAR → plan mesuré ; métrés → coûts |
| [Notion templates maison](https://damalu.gumroad.com/l/HouseMaintenanceTracker) | 4 | achat unique | Schéma révélé : Projets↔Tâches↔Entrepreneurs↔Matériaux↔Docs |
| [Billdr PRO](https://www.billdr.ai/) | 3 | B2B 180 $US+/mois | Change orders ; journal de chantier daté ; paiements par jalons |
| [Moved](https://moved.com/) | 3 | leads/B2B | Le volet administratif du déménagement comme checklist canonique |

### Véhicules et petits moteurs (actifs du foyer)

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [LubeLogger](https://github.com/hargata/lubelog) | 9 | gratuit, OSS (.NET!) | Double déclencheur date/odomètre ; docs sur le journal |
| [Hammond](https://github.com/akhilrex/hammond) | 7 | gratuit, OSS | Capture inbox photo-d'abord ; véhicules partagés à deux |
| [Tracktor](https://github.com/javedh-dev/tracktor) | 7 | gratuit, OSS | Documents à expiration (assurance, SAAQ) avec rappels |
| [Drivvo](https://www.drivvo.com/en/personal-use/) | 6 | freemium + pubs | Coût par km comme métrique vedette ; unités kWh |
| [Fuelly](https://apps.apple.com/us/app/fuelly-mpg-service-tracker/id295905460) | 6 | freemium + pubs | Gabarits de rappels par type de service |
| [Simply Auto](https://simplyauto.app/) | 6 | freemium + achat unique | Rapports automatiques courriel hebdo/mensuels |
| [AUTOsist](https://autosist.com/) | 5 | B2B par véhicule | Checklists d'inspection ; remorques/équipements traités pareil |
| [CARFAX Car Care](https://www.carfax.com/Service/) | 5 | gratuit (lead-gen) | Enrichissement par NIV ; veille de rappels en continu |

### Jardin, plantes et extérieur

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [Seedtime](https://seedtime.us/) | 8 | freemium + semences | Calendrier calculé des dates de gel ; cascades ; successions |
| [Plant-it](https://github.com/MDeLuise/plant-it) | 8 | gratuit, OSS | Intervalle-depuis-dernier-événement par type de soin |
| [Gardenize](https://gardenize.com/) | 7 | freemium 44 $US/an | Plante–Surface–Événement ; timeline multi-années ; dessin sur photo |
| [Planta](https://getplanta.com/) | 6 | freemium 35,99 $US/an | Intervalle modulé par saison/lumière/pot |
| [Sunday](https://www.getsunday.com/) | 6 | abo ~189 $US/saison + produits | Programme pelouse dérivé climat+sol ; fenêtres météo réelles |
| [HortusFox](https://github.com/danielbrendel/hortusfox-web) | 6 | gratuit, OSS | L'objet réclame une tâche (capteur manuel) ; Pl@ntNet |
| [GARDENA smart](https://www.gardena.com/int/c/discover/products/smart-system/smart-app) | 5 | matériel | Skip-pluie ; capteur de sol comme condition ; alertes gel |
| [Open Garden Planner](https://github.com/cofade/open-garden-planner) | 5 | gratuit, OSS | Plan du terrain calibré satellite ; Trefle/Perenual |
| [PictureThis](https://www.picturethisai.com/) | 4 | freemium 39,99 $US/an | Photo → fiche structurée ; diagnostic → traitement |

### Énergie et Hydro-Québec

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [HydroQC](https://hydroqc.ca/) | 8 | gratuit, OSS (QC!) | Pointes en phases ; open data sans login ; crédit projeté |
| [Hilo](https://www.hiloenergie.com/en-ca/) | 6 | filiale HQ, matériel | Défis acceptables ; modes d'intensité nommés ; bilan $ |
| [HA Énergie](https://www.home-assistant.io/docs/energy/) | 6 | gratuit, OSS | Modèle en flux ; stats downsamplées ; conso par équipement |
| [OpenEnergyMonitor](https://emoncms.org/) | 6 | OSS + matériel | Pipeline de normalisation avant stockage ; endpoint générique |
| [Emporia Vue 3](https://shop.emporiaenergy.com/products/emporia-vue-3) | 5 | matériel ~150 $US sans abo | Par circuit ~10 $/circuit ; reflashable ESPHome ; alertes de seuil |
| [Sense](https://sense.com/) | 5 | matériel 299 $US | Conso = événements d'appareils ; anomalies ; cycles |
| [evcc](https://evcc.io/en/) | 5 | OSS + jetons sponsors | Ordonnancement par prix ; financement hobby malin |
| [SolarAssistant](https://solar-assistant.io/) | 4 | licence unique ~40 $US | Modèle appliance (image SD) ; MQTT documenté |

### Autres auto-hébergés

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [Wallos](https://github.com/ellite/Wallos) | 5 | gratuit, OSS | Abonnements/renouvellements du foyer avec rappels ; logos auto |
| [Monica](https://www.monicahq.com/) | 3 | open core | Rappels attachés à une personne ; timeline mixte journal+événements |
| [Actual Budget](https://actualbudget.org/) | — | gratuit, OSS (ajout hors balayage) | Enveloppes/fonds de prévoyance ; sync bancaire SimpleFIN (~15 $US/an, banques CA dont Desjardins) |
| [Firefly III](https://www.firefly-iii.org/) | — | gratuit, OSS (ajout hors balayage) | Finances personnelles API-first ; Data Importer (CSV, GoCardless, SaltEdge) |

## Leçons de marché

- **Les SaaS de ce domaine meurent et emportent les données** : Centriq
  (jan. 2026, données purgées), Maple (sunset déc. 2026), Encircle consommateur
  (déc. 2025). C'est l'argument fondateur du choix auto-hébergé —
  [[D-2026-08-23 Plateforme Hobby Cœur Custom]] validée par le marché.
- **Le créneau tolère mal l'abonnement** : les modèles aimés sont l'achat unique
  (Tody, MyStuff2, Fully Kiosk, SolarAssistant), la licence à vie (Homechart
  149,99 $US, KitchenPal) et le matériel sans abo (TRMNL, Invisible, Emporia).
- **Le foyer est l'unité de facturation**, pas la personne (OneHaus, AnyList,
  Nipto) — cohérent avec les 2 comptes House OS.
- **Personne n'a le combo House OS** : récurrence riche + fenêtres saisonnières
  calculées + équipements/compteurs + documents + météo/HQ + MCP. Les plus
  proches (Donetick, Grocy, Homechart, HomeZada) en couvrent chacun une partie ;
  les fenêtres saisonnières dérivées du climat local restent le différenciateur
  annoncé — seul Seedtime (jardin) le fait, dans une niche.

Liens : [[Apps Similaires]] · [[Inspiration UI]] · [[Architecture]] · [[Glossaire]].
