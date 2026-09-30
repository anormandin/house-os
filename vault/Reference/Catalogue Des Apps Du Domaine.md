---
type: reference
last-verified: 2026-09-30
verified-against: 6d1fda0
tags: []
---

# Catalogue Des Apps Du Domaine

Sous-note de [[Banque D'idées]] : les apps du balayage de marché du 2026-08-26, par
catégorie, avec leur score de proximité avec House OS (même barème que la note mère),
leur modèle d'affaires et l'idée à en retenir. Le détail de chaque app — URL, prix, cas
d'usage, features — est dans le rapport brut :
`docs/research/2026-08-26-balayage-apps-domaine.md`.

## Corvées et récurrence

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

## Inventaire et actifs

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

## Entretien de la maison

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

## Garde-manger, épicerie et recettes

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

## Organiseurs de foyer

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

## Documents et garanties

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

## Affichages muraux et e-ink

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

## Projets, réno et déménagement

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

## Véhicules et petits moteurs (actifs du foyer)

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

## Jardin, plantes et extérieur

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

## Énergie et Hydro-Québec

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

## Autres auto-hébergés

| App | Score | Modèle | À retenir |
|---|---|---|---|
| [Wallos](https://github.com/ellite/Wallos) | 5 | gratuit, OSS | Abonnements/renouvellements du foyer avec rappels ; logos auto |
| [Monica](https://www.monicahq.com/) | 3 | open core | Rappels attachés à une personne ; timeline mixte journal+événements |
| [Actual Budget](https://actualbudget.org/) | — | gratuit, OSS (ajout hors balayage) | Enveloppes/fonds de prévoyance ; sync bancaire SimpleFIN (~15 $US/an, banques CA dont Desjardins) |
| [Firefly III](https://www.firefly-iii.org/) | — | gratuit, OSS (ajout hors balayage) | Finances personnelles API-first ; Data Importer (CSV, GoCardless, SaltEdge) |
