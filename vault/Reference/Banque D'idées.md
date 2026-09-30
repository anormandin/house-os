---
type: reference
last-verified: 2026-09-30
verified-against: 6d1fda0
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

## Catalogue par catégorie

Les apps balayées, classées par catégorie avec leur score, leur modèle d'affaires et ce
qu'il faut en retenir : [[Catalogue Des Apps Du Domaine]].

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
