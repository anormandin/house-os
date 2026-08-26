# Balayage du marché — apps du domaine House OS (2026-08-26)

Recherche web multi-agents (13 agents, 12 catégories : 9 planifiées + 3 combles
détectés par un critique de complétude — véhicules, jardin/plantes, énergie/Hydro-Québec).
110 applications retenues, dédupliquées, avec URL vérifiée, prix/monétisation,
cas d'usage, features remarquables et un score de proximité 0-10 avec House OS
(10 = même domaine cœur ; 6-8 = fort recouvrement sur un module ; 3-5 = adjacent).

Synthèse et catalogue distillés dans `vault/Reference/Banque D'idées.md`.
Détail brut par app ci-dessous, trié par score décroissant.

---

## Donetick — 10/10 [chores-recurrence + selfhosted-household]
URL: https://github.com/donetick/donetick
Application open source auto-hébergeable de gestion de tâches et corvées pour un foyer (« circles » partagés), avec appli mobile et instance hébergée officielle. Le concurrent direct le plus proche de HouseOS.
Prix: gratuit en auto-hébergement (AGPLv3) ; instance hébergée donetick.com avec plan gratuit + abonnement Plus payant (montant non confirmé) | Monétisation: open core : auto-hébergement gratuit, abonnement sur l'instance hébergée | OSS: True | Self-host: True
Pourquoi ce score: Même domaine cœur que HouseOS (corvées récurrentes multi-membres, auto-hébergé, NFC et déclencheurs capteurs déjà livrés).
Cas d'usage: Corvées récurrentes partagées en couple/coloc; Rotation automatique des assignations; Complétion par tag NFC; Déclenchement de tâches par capteur/API (« Things »)
Features à voler:
  - Bascule par tâche entre récurrence basée sur l'échéance (cadence fixe) et basée sur la date de complétion — exactement les deux modes HouseOS, mais choisis tâche par tâche dans l'UI
  - « Adaptive scheduling » : l'intervalle s'ajuste en apprenant de l'historique réel de complétions (HouseOS a le journal, il pourrait suggérer un intervalle)
  - « Things » : entités nombre/booléen/texte alimentées par API ; une tâche se complète ou se déclenche quand la valeur atteint un seuil — préfigure la phase capteurs de HouseOS
  - Écriture de tags NFC depuis l'app : scanner le tag complète la tâche (roadmap NFC HouseOS validée par un concurrent)
  - Sous-tâches qui se réinitialisent automatiquement à chaque nouvelle occurrence d'une tâche récurrente
  - Saisie en langage naturel (« sortir les poubelles chaque lundi 18 h ») — parfait candidat pour le MCP/LLM de HouseOS
  - Rotation d'assignation à 3 stratégies : moins-complété, aléatoire, tour de rôle (HouseOS n'a pas « aléatoire »)
  - Canaux de notification Telegram/Discord/Pushover en plus de l'app

## Grocy (module corvées) — 9/10 [chores-recurrence]
URL: https://grocy.info/
ERP domestique open source auto-hébergé ; son module « chores » gère les corvées récurrentes multi-utilisateurs à côté du garde-manger et de l'inventaire. Inspiration déjà citée de HouseOS.
Prix: gratuit, open source (MIT) | Monétisation: aucune (dons) | OSS: True | Self-host: True
Pourquoi ce score: Même domaine (corvées + inventaire du foyer, auto-hébergé) ; source des patterns rollover et moins-l'a-fait déjà repris.
Cas d'usage: Corvées récurrentes du foyer; Suivi de qui a fait quoi et quand; Lien corvée ↔ stock de consommables
Features à voler:
  - Une corvée peut consommer un produit d'inventaire à chaque exécution (ex. pastille de lave-vaisselle) — pont naturel vers le module Consommables de HouseOS
  - Mode « manually scheduled » : pas de récurrence, on planifie chaque occurrence à la main — utile pour les tâches irrégulières que HouseOS force en ponctuel
  - Mode « track date only » : journaliser une exécution sans heure précise (saisie a posteriori plus honnête)
  - Saisie rétroactive : compléter en précisant qui et à quelle date/heure réelle, pas seulement « maintenant »
  - Rollover d'échéance (jamais en retard, l'échéance glisse) — confirmé comme pattern éprouvé
  - Assignation « who least did it first » calculée sur le journal — même idée que HouseOS, valide l'approche

## Homebox (sysadminsmedia fork) — 9/10 [home-inventory]
URL: https://homebox.software/
Self-hosted, lightweight home inventory server (Go + SQLite, Docker) aimed at homelabbers cataloging household items, warranties and maintenance.
Prix: gratuit, open source (AGPL); Docker self-host | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Same core domain as HouseOS's equipment module — self-hosted household item/warranty/maintenance registry — and already named as inspiration.
Cas d'usage: catalog household items by location/label; warranty and receipt tracking; printable QR/asset-ID labels for bins; maintenance schedules on items
Features à voler:
  - Sequential human-friendly asset IDs (#000-001) auto-assigned to every item, searchable by typing # in the search bar — a stable physical-label handle separate from DB ids
  - Built-in label generator producing printable QR sheets from asset IDs, sized for common label paper; QR resolves to the item page
  - Maintenance log entries per item with date + cost, so total cost of ownership per equipment accumulates over time
  - Custom fields per item on top of a fixed core schema (brand, serial, purchase price/date, warranty expiry)
  - CSV import/export of the whole inventory — a low-friction on-ramp for initial mass entry
  - Idle footprint under 50 MB with embedded SQLite — deliberate 'runs on anything' positioning

## HomeZada — 9/10 [home-inventory + projects-renovation]
URL: https://www.homezada.com/
SaaS 'digital home management' suite for homeowners: inventory, maintenance calendar, projects/remodel budgets, home finances and documents in one place.
Prix: 65$US/an (Basic) / 99$US/an (Premium), essai gratuit, pas de palier gratuit permanent | Monétisation: subscription | OSS: False | Self-host: False
Pourquoi ce score: Closest commercial equivalent to the whole HouseOS vision — inventory + maintenance + projects + documents for one house, minus self-hosting.
Cas d'usage: insurance-grade home inventory; seasonal maintenance calendar; remodel project budgeting; home value & expense tracking; estate/resale documentation
Features à voler:
  - Auto-generates a starter maintenance calendar from the home's location, climate and declared systems (HVAC, deck, gutters…), then lets you tune cadence per task — a great onboarding pattern for HouseOS's recurrence engine
  - Completed maintenance tasks accept receipts, photos and documents, building a per-home cost-and-care history usable at resale
  - Inventory items carry replacement cost distinct from purchase price, aggregated per policy category for insurance-coverage gap checks
  - 'Zada AI' creates item records and value estimates from photos and diagnoses maintenance needs from a picture
  - Projects module ties budgets/quotes to the same equipment and documents graph as inventory

## Grocy — 9/10 [pantry-grocery + selfhosted-household]
URL: https://grocy.info/
Self-hosted 'ERP beyond your fridge': full pantry stock tracking with expiry dates, plus chores, equipment, batteries and tasks. The closest existing tool to HouseOS's whole domain and its stated inspiration.
Prix: gratuit, open source (self-hosted ; apps desktop/Android/iOS gratuites) | Monétisation: none (donations) | OSS: True | Self-host: True
Pourquoi ce score: Same core domain as HouseOS (household stock + chores + equipment, self-hosted), differing only in polish and UI philosophy.
Cas d'usage: pantry/stock inventory with expiry alerts; shopping list auto-filled from min-stock levels; recipe availability checking against stock; chore and battery-charge tracking; kitchen-terminal barcode workflows
Features à voler:
  - Min-stock replenishment: each product has a minimum stock amount; shopping list auto-suggests everything below minimum
  - 'Due Score' on recipes: ranks which dishes best consume ingredients about to expire
  - Materialized stock ledger: every purchase/consume/open is a journal entry with price, so cost-per-unit and consumption history come free
  - Product lookup via Open Food Facts barcode to prefill name/image when adding a new item
  - 'Opened' state distinct from 'in stock' — opening a product can shorten its effective expiry
  - One-hand three-second entry forms designed for a cheap wall/kitchen terminal (PWA), matching HouseOS's wall-tablet phase
  - Night mode auto-activated from sunset times — cute fit with HouseOS's weather ingestion
  - Custom user-defined fields and entities on any object (JSONB-style flexibility HouseOS already has for equipment)
  - Battery tracking = interval-since-last-charge recurrence applied to objects, a pattern generalizable to any consumable-driven task

## Homechart — 9/10 [family-organizers]
URL: https://homechart.app/
Hub de maisonnée auto-hébergeable (ou cloud) : calendriers, repas/recettes, corvées, budget, notes, inventaire, secrets et listes — le cousin idéologique le plus proche de HouseOS.
Prix: self-hosted ; cloud : gratuit (perso) / Foyer 4.99$US mois, 49.99$US an ou 149.99$US à vie | Monétisation: freemium + licence à vie, code sur GitHub (candiddev/homechart, source disponible) | OSS: True | Self-host: True
Pourquoi ce score: Même domaine cœur (gestion complète du foyer), même philosophie self-hosted/vie-privée, panier de modules quasi identique à la feuille de route HouseOS.
Cas d'usage: hub familial self-hosted en Docker; recettes → plan de repas → liste d'épicerie; budget du foyer; coffre à secrets partagé; wiki de notes de la maison
Features à voler:
  - Chaîne recettes → plan de repas → liste d'épicerie générée automatiquement (les ingrédients des repas planifiés tombent dans la liste) — gabarit pour le futur module consommables/repas
  - Module « Secrets » : mots de passe et codes du foyer (wifi, cadenas, NIP) chiffrés côté client dans le même hub — les infos de maison qu'un couple se retexte sans cesse
  - Licence à vie 149.99$US comme alternative à l'abonnement — modèle sympathique au self-hosted
  - Budget intégré au foyer plutôt qu'app séparée : dépenses rattachées aux mêmes entités (courses, projets)
  - Notes en wiki interne du foyer (procédures : fermer la piscine, hivernage) — recoupe le vault/documents de HouseOS

## LubeLogger — 9/10 [vehiclesmallenginemaintenancetrackerscarsashouseholdassets]
URL: https://github.com/hargata/lubelog
Self-hosted, web-based vehicle maintenance and fuel-mileage tracker (Docker image, .NET-based) for individuals keeping service records, reminders and costs on their own hardware. The reference app of the niche — Hacker News/Hackaday darling.
Prix: gratuit, open source (licence MIT); image Docker officielle | Monétisation: none (donations) | OSS: True | Self-host: True
Pourquoi ce score: Same core domain as HouseOS's equipment module extended to vehicles — self-hosted, Docker, maintenance-recurrence-centric, and the dual-trigger pattern HouseOS explicitly wants.
Cas d'usage: Track oil changes and routine maintenance per vehicle; Fuel fill-up and MPG/consumption logging; Recurring service reminders by date or odometer; Cost breakdown and tax-deduction reports per vehicle; Attach receipts/documents to each service record
Features à voler:
  - Dual-trigger reminders: due date OR future odometer reading OR 'whichever comes first' — urgency computed from server date vs max odometer across all record types; exact blueprint for HouseOS's usage-OR-time recurrence (sensor phase)
  - Recurring reminders refresh on completion: creating a Service/Repair/Upgrade record or marking a Plan done pushes the next due date/odometer out by the interval — same 'materialize next occurrence at completion' philosophy HouseOS already uses
  - Odometer as a first-class record type with auto-update from any record (fuel, service, repair): every logged event advances the usage counter — maps to HouseOS equipment hour-meters (snowblower, generator, tractor)
  - Per-vehicle cost analysis and tax-deduction report generated from the completion journal (cost per record already exists in HouseOS's journal — the missing piece is the roll-up report per equipment)
  - Receipts/documents attached per service record, not just per asset — HouseOS documents module could link documents to journal entries, not only to equipment

## DumbAssets (DumbWare) — 8/10 [home-inventory]
URL: https://github.com/DumbWareio/DumbAssets
'Stupid simple' self-hosted asset tracker for physical belongings, their components, warranties and routine maintenance; single Docker container.
Prix: gratuit, open source (GPL-3.0) | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Self-hosted, home-scale, exactly the equipment+warranty+maintenance triangle HouseOS covers, in a deliberately minimal package.
Cas d'usage: track appliances and their parts; warranty expiry alerts; routine maintenance reminders (filters, water-softener salt); store receipts/manuals/photos
Features à voler:
  - Component/sub-component hierarchy: an asset (furnace) owns components (filter, igniter) each with its OWN warranty, docs and maintenance schedule — HouseOS equipment could nest consumable parts this way
  - Sort/filter the whole asset list by warranty status and expiration date, turning the registry into an actionable 'what expires next' view
  - Notification on any asset modification — an audit-style event stream for the household
  - PIN auth with brute-force protection as the entire auth model — honest about the home-LAN threat model

## Under My Roof — 8/10 [home-inventory + documents-warranties]
URL: https://apps.apple.com/us/app/under-my-roof-home-inventory/id1524335878
Apple-only (iPhone/iPad/Mac) home inventory and maintenance app by Binary Formations, successor of the venerable 'Home Inventory' Mac app; data lives in the user's own iCloud.
Prix: gratuit (limite 10 articles) / 3.99$US mois / 24.99$US an | Monétisation: subscription (freemium) | OSS: False | Self-host: False
Pourquoi ce score: Household inventory plus maintenance history with costs — strong overlap with HouseOS equipment and documents modules, plus a data-ownership stance (your own iCloud).
Cas d'usage: room-by-room belongings catalog; warranty/receipt/manual storage; maintenance scheduling with repair history; record paint colors and home details
Features à voler:
  - Voice-driven Quick Entry Mode: walk a room dictating items hands-free — bulk-capture pattern worth copying for initial inventory
  - Repair/maintenance history per item records cost, parts used and photos, not just a completion date
  - Room records store paint colors, square footage and property-assessment details — the house itself is an inventoriable object, not just its contents
  - Unlimited user-defined custom fields, locations, categories, collections and color-coded tags
  - 3D room scanning and document/text capture as ingestion paths
  - Syncs through the user's private iCloud account — vendor never holds the data

## DumbAssets — 8/10 [home-maintenance + documents-warranties]
URL: https://github.com/DumbWareio/DumbAssets
'Stupid simple' self-hosted, open-source asset tracker for valuables, warranties, receipts and maintenance schedules; Docker-first, for homelab folks.
Prix: gratuit, open source (GPL) — Docker ou NodeJS | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Self-hosted equipment registry + warranty + maintenance notifications is exactly HouseOS's Équipements module, minus everything else.
Cas d'usage: asset/warranty registry; maintenance event reminders; receipt and photo storage
Features à voler:
  - Parent-child asset hierarchy: components and sub-components under an asset (furnace → filter → filter size), so consumables attach to the equipment they serve
  - Warranty expiration notifications with lead time, delivered through Apprise (one integration → 80+ notification channels: ntfy, Telegram, email...)
  - Maintenance events attached directly to an asset rather than living in a separate task silo
  - Deliberate anti-feature stance (no barcode/QR/mobile app) as a design philosophy — simplicity documented as a feature

## micasa — 8/10 [home-maintenance + selfhosted-household]
URL: https://micasa.dev/
Local-first modal TUI (pure Go, single SQLite file) tracking home projects, maintenance schedules, appliances, vendor quotes and incidents; for terminal-dwelling homeowners.
Prix: gratuit, open source | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Almost the same domain model as HouseOS (appliances, maintenance schedules + service logs, projects) built local-first by a hobbyist — richest source of module ideas.
Cas d'usage: recurring maintenance with service logs; renovation project pipeline; vendor quote comparison; incident logging; appliance/warranty registry
Features à voler:
  - Quotes module: collect and compare multiple vendor quotes per project before choosing — a natural HouseOS 'Projets' sub-feature
  - Incidents log: household issues recorded with severity, separate from planned maintenance — captures the 'water stain on ceiling' events HouseOS has no home for
  - Projects tracked as a pipeline from idea → quoted → in progress → done
  - On-device LLM chat over the house database — direct validation of HouseOS's MCP-server approach
  - Attachments (manuals, invoices) on any record type, all in one portable SQLite file

## HomeBox — 8/10 [selfhosted-household]
URL: https://homebox.software/en/
Inventaire domestique auto-hébergé (continuation par sysadminsmedia) : objets, emplacements imbriqués, garanties, étiquettes QR. Pensé pour l'utilisateur maison, pas l'entreprise.
Prix: gratuit, open source | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Recouvre fortement le registre d'équipements de HouseOS et une partie documents, inspiration déjà citée.
Cas d'usage: inventaire maison → pièce → tiroir; suivi des garanties et factures; étiquetage QR des boîtes de déménagement; journal d'entretien par objet
Features à voler:
  - Emplacements imbriqués illimités (Maison → Pièce → Armoire → Boîte) — les Zones de HouseOS sont plates, la hiérarchie vaudrait l'étude
  - Génération de QR codes par objet et par contenant : coller l'étiquette sur la boîte et scanner pour retrouver le contenu — parfait pour le déménagement du 6 octobre
  - Champs de garantie de première classe (durée, expiration) séparés des specs libres, avec pièces jointes reçu/facture par objet
  - Gabarits d'objets (templates) pour saisir vite des items semblables
  - Entrées d'entretien datées par objet (« piles vérifiées le … ») reliées à l'inventaire plutôt qu'à un module de tâches séparé — HouseOS peut relier Occurrence ↔ Équipement plus fort

## Paperless-ngx — 8/10 [documents-warranties + selfhosted-household]
URL: https://docs.paperless-ngx.com/
Le standard de facto du DMS auto-hébergé : scanne, OCRise, indexe et classe automatiquement les documents du foyer. Pour quiconque veut un bureau sans papier chez soi.
Prix: gratuit, open source (GPL-3.0) | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Recouvre exactement le module Documents de HouseOS (ingestion, OCR, facettes) en version mûrie, auto-hébergé comme HouseOS, mais sans lien équipement/tâches.
Cas d'usage: Archiver factures, contrats, relevés après scan; Retrouver n'importe quel papier par recherche plein texte; Classement automatique par correspondant/type/tags; Rappels d'échéance de documents via workflows planifiés
Features à voler:
  - Consume folder + ingestion par courriel : tout fichier déposé (scanner réseau, pièce jointe) est OCRisé et classé sans action manuelle
  - Matching automatique appris : correspondant, type de document et tags suggérés par un classifieur entraîné sur les corrections de l'utilisateur (auto, any/all/regex/fuzzy par terme)
  - Champs personnalisés typés (date, monnaie, URL, lien vers document) attachables par type de document — ex. champ 'fin de garantie' de type date
  - Workflows à déclencheur planifié : offset en jours avant/après une date (créée, ajoutée ou champ personnalisé) → action (tag, titre, webhook) — le pattern exact d'un rappel 'garantie expire dans 30 jours'
  - ASN (archive serial number) : numéro imprimé sur le papier physique pour relier l'original papier à sa version numérique
  - Suggestions et chat IA natifs (OpenAI ou Ollama local, RAG FAISS) arrivés en 2025-2026

## Recevity — 8/10 [documents-warranties]
URL: https://recevity.com/
« L'OS de tout ce que vous possédez » : transforme reçus transférés par courriel en fiches de possession structurées, avec manuel officiel retrouvé automatiquement et suivi de garantie.
Prix: gratuit (5 items) / 29$US an (Pro, illimité + manuels + rappels) / 49$US an (Famille, 5 membres) | Monétisation: freemium, abonnement annuel | OSS: False | Self-host: False
Pourquoi ce score: Fusionne exactement Documents + Équipements + garanties comme HouseOS veut le faire, avec l'automatisation en plus ; fermé et nuage seulement.
Cas d'usage: Transférer un reçu d'achat par courriel → fiche produit créée; Bibliothèque de manuels officiels constituée automatiquement; Alertes fenêtre de retour et fin de garantie; Vérification de rappels (recalls) produits
Features à voler:
  - Reçu → fiche structurée : extraction du produit, date d'achat, prix, durée de garantie depuis un simple reçu (courriel, PDF ou photo)
  - Recherche automatique du manuel officiel du modèle exact et attachement à la fiche — HouseOS pourrait le faire via son MCP/LLM sur les équipements
  - Détection des exigences d'enregistrement de garantie ('enregistrez sous 60 jours pour l'extension') avec fenêtres d'éligibilité surveillées
  - Suivi de la fenêtre de retour (return window) distincte de la garantie
  - Vérification périodique des rappels de sécurité par produit
  - Export complet des données garanti (anti lock-in) — argument né de la mort de Centriq

## Dib — 8/10 [documents-warranties]
URL: https://dib.io/
Successeur spirituel de Centriq (en bêta) : photographier un objet, l'IA l'identifie, extrait le numéro de série, retrouve le manuel et propose l'entretien. Couvre maison, véhicules, documents, plantes, animaux.
Prix: gratuit (items illimités, entretien, documents, véhicules) ; premium payant pour les extras IA (reconnaissance photo, chat IA, manuels automatiques), prix non publié (bêta) | Monétisation: freemium, IA en tier payant | OSS: False | Self-host: False
Pourquoi ce score: Domaine quasi identique (équipements + manuels + garanties + entretien) avec l'angle IA que HouseOS possède déjà via MCP ; fermé, nuage.
Cas d'usage: Photo de la plaque signalétique → fiche équipement complète; Recherche automatique de manuels; Suivi de garanties et d'entretien; Inventaire maison + véhicules + documents
Features à voler:
  - Identification d'item par photo : l'IA lit la plaque (marque/modèle/série) et remplit la fiche — parfait candidat pour gerer_equipement via un LLM-vision
  - « AI chat with your home » : poser des questions en langage naturel sur son propre inventaire
  - Copie hors-ligne téléchargeable des données comme argument de confiance
  - Scope élargi au-delà des électros : véhicules, plantes, animaux, projets — rappel que le registre d'actifs peut absorber plus que des appareils

## Seedtime — 8/10 [gardenplantcareseasonaloutdoorplanningapps]
URL: https://seedtime.us/
Web-app de planification de jardin potager qui génère un calendrier de semis/plantation/récolte à partir de la localisation et des dates de gel. Pour jardiniers et petites fermes qui veulent visualiser toute leur saison d'un coup.
Prix: Gratuit (1 calendrier, horaires/tâches/journal illimités) ; plans premium annuels (AI planning, calendriers illimités, layouts, inventaire) + 25 sachets de semences/an offerts aux membres annuels | Monétisation: freemium (abonnement annuel) + boutique de semences/matériel | OSS: False | Self-host: False
Pourquoi ce score: C'est exactement le moteur de récurrence de HouseOS appliqué au jardin : des fenêtres saisonnières calculées depuis la localisation et les dates de gel, avec tâches dérivées — le chaînon manquant entre les fenêtres mois-jour actuelles et un vrai calendrier de jardin zone 4.
Cas d'usage: Générer un calendrier de plantation annuel à partir des dates de gel locales; Planifier les semis intérieurs vs extérieurs et les repiquages; Plantations en succession pour étaler les récoltes; Listes de tâches hebdomadaires dérivées du calendrier; Journal de jardin et inventaire de semences
Features à voler:
  - Calendrier généré automatiquement depuis les dates de premier/dernier gel du lieu (zone de rusticité → fenêtres de semis) — mappe directement sur les fenêtres saisonnières de HouseOS, mais calculées au lieu d'être saisies à la main
  - Bandes de gel visibles en un coup d'œil sur le calendrier (zone de gel + zone de gel dur pour le jardinage d'hiver)
  - Barres de culture glissables : décaler une plantation décale en cascade semis → repiquage → récolte (récurrences dérivées les unes des autres)
  - Planification en succession : re-semer tous les N jours pour récolter en continu — un mode de récurrence borné par la fenêtre saisonnière
  - Checklist hebdomadaire imprimable générée depuis le plan annuel

## Plant-it — 8/10 [gardenplantcareseasonaloutdoorplanningapps]
URL: https://github.com/MDeLuise/plant-it
Compagnon de jardinage open source et auto-hébergé (Docker Compose, backend + app Android) pour suivre les soins des plantes : journal d'événements, photos, rappels. Pour self-hosters qui veulent leurs données de plantes chez eux.
Prix: gratuit, open source | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Auto-hébergé, Docker Compose, et son modèle de rappel « intervalle depuis le dernier événement » est littéralement le mode intervalle-depuis-complétion de HouseOS appliqué aux plantes — la preuve qu'une entité Plante n'est qu'une Tâche + un journal.
Cas d'usage: Journal d'événements par plante (arrosage, fertilisation, rempotage); Rappels du type « avertir si pas arrosé depuis 4 jours »; Vue calendrier des prochains rappels; Photothèque par plante; Base botanique via API Trefle ou plantes personnalisées
Features à voler:
  - Rappels par intervalle-depuis-dernier-événement, par plante ET par type d'événement (arrosage ≠ fertilisation) — chez HouseOS, une plante = un équipement avec plusieurs tâches à intervalle
  - Journal d'événements typés (arrosage/fertilisation/biostimulation) filtrable par plante et par type — même patron que le Journal de complétion
  - Notifications par courriel ou Gotify (push auto-hébergé) — piste pour le futur push HouseOS
  - Séparation base botanique (Trefle) vs instances possédées — patron catalogue/exemplaire réutilisable pour consommables et équipements

## HydroQC — 8/10 [homeenergyutilitymonitoringinclhydroqubecpeakevents]
URL: https://hydroqc.ca/
Projet communautaire québécois (lib Python + intégration Home Assistant hydroqc-ha) qui se connecte au compte client Hydro-Québec pour exposer consommation, facturation, pannes et surtout les événements de pointe hiver (Crédit hivernal / Flex D). Exactement le public de HouseOS : foyers québécois auto-hébergés.
Prix: gratuit, open source | Monétisation: aucune (projet communautaire, dons) | OSS: True | Self-host: True
Pourquoi ce score: Recouvre précisément l'item de feuille de route « Hydro-Québec evenements-pointe » avec le même contexte (Québec, auto-hébergé, Docker) — mais c'est un module d'ingestion, pas un OS maison complet.
Cas d'usage: Recevoir les événements de pointe HQ (ancre/pointe/pré-chauffe) comme capteurs et calendrier local; Suivre la consommation quotidienne et la période de facturation depuis le portail HQ; Automatiser le pré-chauffage et le délestage pendant les pointes pour maximiser les crédits; Détecter les pannes Hydro-Québec
Features à voler:
  - Modélise chaque événement de pointe en phases exploitables : pré-chauffe (ex. 3 h avant), ancre, pointe, retour à la normale — HouseOS devrait stocker l'événement avec ces fenêtres dérivées, pas juste début/fin
  - Synchronise les pointes dans un calendrier local — s'aligne parfaitement sur le flux iCal existant de HouseOS (les pointes deviennent des événements dans le flux des téléphones)
  - Sépare deux sources : l'open data public evenements-pointe (sans login, cf. hydropeak-ha) vs le scraping du compte client (consommation, facture $) — HouseOS peut commencer sans authentification
  - Capteurs « crédit hivernal projeté » : estimation en $ des crédits gagnés pendant la saison — chiffre motivant à afficher sur le tableau mural
  - Expose la période de facturation en cours (jours restants, kWh cumulés) pour des projections de coût par facture

## Tody — 7/10 [chores-recurrence]
URL: https://todyapp.com/
App mobile de ménage qui remplace le calendrier par un « niveau de saleté » : chaque tâche a un indicateur qui se remplit depuis la dernière exécution. Pour foyers qui veulent nettoyer selon le besoin réel.
Prix: iOS ≈ 6,99$US (achat unique) ; Android gratuit avec achats intégrés ; synchro multi-appareils via abonnement Tody Plus | Monétisation: achat unique + abonnement pour la synchro famille | OSS: False | Self-host: False
Pourquoi ce score: Recouvre le cœur corvées/récurrence de HouseOS avec un modèle mental original (besoin continu, pas échéance binaire).
Cas d'usage: Plan de ménage par pièce; Prioriser selon la saleté effective plutôt que des dates; Ménage partagé multi-appareils
Features à voler:
  - Indicateur continu de « saleté » : une jauge se remplit entre la dernière complétion et le point optimal puis le dépasse — bien plus parlant qu'un badge en-retard/pas-en-retard ; trivial à calculer avec le journal HouseOS
  - Agrégation par pièce : pastille blanche/orange/rouge par zone selon le nombre de tâches dues — carte de chaleur des Zones HouseOS
  - « Optimal day » : la fréquence définit un point idéal, pas une deadline dure ; faire la tâche en avance recale la jauge sans culpabiliser
  - Tâches « anytime » sans fréquence, tirées quand on a un creux
  - Visualiser l'effet du nettoyage (jauge qui retombe à zéro) comme récompense visuelle immédiate

## Sweepy — 7/10 [chores-recurrence]
URL: https://sweepy.com/
App mobile de planification du ménage qui génère chaque jour une liste de tâches calibrée sur l'effort que chaque membre veut fournir ce jour-là. Pour couples/familles qui veulent un plan quotidien tout fait.
Prix: gratuit (1 utilisateur) / Premium ≈ 3,99$US mois ou 19,99$US an (multi-utilisateurs, leaderboard) | Monétisation: freemium, abonnement | OSS: False | Self-host: False
Pourquoi ce score: Même noyau corvées récurrentes multi-membres, avec un planificateur par budget d'effort que HouseOS n'a pas.
Cas d'usage: Génération automatique du plan de ménage quotidien; Répartition équitable entre membres; Suivi de la propreté par pièce
Features à voler:
  - Niveau d'effort par tâche (facile/moyen/difficile) + budget d'effort par jour et par personne → l'app compose le programme quotidien qui rentre dans le budget ; idée forte pour une vue « Aujourd'hui » qui dose au lieu de tout lister
  - Filtrer les tâches dues par effort (« je suis fatigué, montre-moi les faciles »)
  - Score de propreté par pièce dérivé des tâches en retard de la pièce
  - Leaderboard familial pondéré par l'effort, pas par le nombre de tâches
  - L'app suggère une fréquence de départ par type de tâche (draps : 3 semaines) que l'utilisateur ajuste — bibliothèque de fréquences par défaut à voler

## Itemtopia — 7/10 [home-inventory]
URL: https://www.itemtopia.com/
Cross-platform home inventory app for tracking items, receipts, warranties, service records — plus people, pets and services — in one searchable household database.
Prix: gratuit (20 articles) / Premium ≈ 39$US an + 3.99$US an par utilisateur additionnel | Monétisation: subscription (freemium) | OSS: False | Self-host: False
Pourquoi ce score: Consumer household inventory with warranties and multi-user sharing — overlaps HouseOS equipment/documents but cloud-only and mobile-first.
Cas d'usage: household item catalog with receipts; warranty expiry reminders; share items/locations with spouse; QR/barcode labels on storage
Features à voler:
  - Automatic reminder fired 30 days BEFORE a warranty expires — a countdown-style derived task, exactly the kind of rule HouseOS's engine could generate from equipment warranty dates
  - In-app scanner where scanning an item's QR immediately offers search / move / update actions — the scan is a verb menu, not just a lookup
  - Selective sharing: share one item or one location with another person rather than the whole account
  - Service records attached to items (who serviced it, when) alongside receipts and documents

## Scanlily — 7/10 [home-inventory]
URL: https://www.scanlily.com/
AI-first inventory app (mobile + web) built around optional physical QR labels: photograph an item and AI catalogs it; scan a box to see inside without opening it.
Prix: gratuit (de base) / Pro 9$US mois ; articles illimités s'ils portent une étiquette QR Scanlily achetée | Monétisation: subscription + sale of physical QR labels | OSS: False | Self-host: False
Pourquoi ce score: Item/container/location inventory with heavy QR mechanics — directly relevant to HouseOS's NFC/QR tap plans, though cloud SaaS.
Cas d'usage: moving/storage box contents; AI-assisted item cataloging; natural-language 'where is my X' search; item valuation for insurance
Features à voler:
  - Ask-anything natural-language search over the inventory ('where's the Nikon I bought last week?') — a perfect MCP demo for HouseOS since the LLM layer already exists
  - Every item is tagged with room + container + GPS and can be shown on a map; scanning a box's QR lists its contents without unpacking
  - AI photo capture: snap an item and the product, brand and details are auto-identified and filled in
  - AI valuation that finds comparable products online and produces a defensible value with source links
  - Pricing tied to physical labels: unlimited items as long as they carry a purchased QR tag — monetizes the hardware anchor, not the software

## Oply — 7/10 [home-maintenance]
URL: https://www.oply.app/
AI-driven predictive home maintenance app that forecasts what the house will need and books vetted local pros; for homeowners who want maintenance done for them.
Prix: abonnement payant (montant non publié clairement); renouvellement auto | Monétisation: subscription + take rate on the pro-services marketplace | OSS: False | Self-host: False
Pourquoi ce score: Core domain matches (per-equipment schedules, service history, costs) but half the product is a contractor marketplace HouseOS will never be.
Cas d'usage: predictive maintenance reminders; booking contractors; service history and cost tracking; home condition scoring
Features à voler:
  - Predictions weight location/climate, appliance age, AND past service history — the interval adapts instead of being fixed
  - Upload a home inspection report (PDF) and the app extracts the top issues into actionable tasks automatically — great LLM/MCP use case for HouseOS
  - Real-time 'Home Score': a single condition index that degrades as maintenance is skipped and recovers when done
  - History Hub: unified ledger of repairs, costs, warranties and improvements queryable per equipment
  - Region-specific seasonal alerts (freeze warnings → winterize outdoor faucets)

## Homer — 7/10 [home-maintenance]
URL: https://www.homer.co/
Mobile-first home binder app (documents, inventory, expenses, recurring maintenance tasks) from Sweden; repeatedly featured by Apple.
Prix: gratuit / Premium ~4.99$US mois | Monétisation: freemium subscription | OSS: False | Self-host: False
Pourquoi ce score: Strong overlap on documents + inventory + recurring tasks, but mobile-first consumer cloud app with no recurrence depth.
Cas d'usage: document vault per home; recurring maintenance tasks; appliance manuals; home event timeline; expense tracking
Features à voler:
  - 'AutoMagic': type an appliance's brand/model and the app auto-fetches the user manual, support links and tips — HouseOS could do this via its MCP/LLM layer for the equipment registry
  - Home timeline: every completion, purchase, and document lands on one chronological feed of the house's life
  - Paint 'color recipes' stored per room — a concrete, borrowable zone attribute (paint code, finish, where bought)
  - CSV importer specifically built to rescue data from dead competitors (Centriq shutdown) — data portability as a feature
  - The whole home binder is designed to be handed over to the next owner at sale

## Mealie — 7/10 [pantry-grocery + selfhosted-household]
URL: https://mealie.io/
Self-hosted recipe manager and meal planner (FastAPI + Vue) for households comfortable with Docker; the most polished of the open-source recipe stack.
Prix: gratuit, open source (AGPL, Docker) | Monétisation: none (donations/sponsors) | OSS: True | Self-host: True
Pourquoi ce score: Strong overlap on the planned consumables/recipes module and on architecture taste (self-hosted, API-first, webhooks).
Cas d'usage: scrape recipes from any URL; weekly meal planning; shopping list generated from the meal plan; multi-household recipe sharing
Features à voler:
  - 'Import with AI': create a recipe from a URL, a photo of a handwritten card, or even a video's audio transcription — same LLM-assist pattern as HouseOS's MCP server
  - Planner Rules: constrain auto-suggested meals by tag/category per weekday or meal type (rule-driven planning, like HouseOS's 'good day to mow' rules)
  - Cookbooks = saved filtered views over categories/tags/tools — a cheap 'facet preset' idea reusable for HouseOS documents
  - Shopping list items carry store labels so the list sorts itself by aisle/store
  - Apprise-based notifiers + scheduled webhooks firing with meal-plan payloads — event-driven notification layer worth copying
  - 'Recipe Actions' with merge fields (${slug}, ${url}) to POST a recipe to arbitrary external endpoints
  - Groups/Households multi-tenancy: shared recipe pool but separate meal plans and shopping lists per household

## KitchenOwl — 7/10 [pantry-grocery + selfhosted-household]
URL: https://kitchenowl.org/
Self-hosted grocery list + recipe + meal plan app (Flutter + Flask) with true native mobile apps and real-time sync; the simplest of the self-hosted stack to run.
Prix: gratuit, open source (AGPL ; apps mobiles natives gratuites) | Monétisation: none (donations) | OSS: True | Self-host: True
Pourquoi ce score: Couple-oriented shared-household mechanics (lists, expenses, real-time sync) map directly onto HouseOS's two-user model.
Cas d'usage: shared real-time shopping list in-store; recipes feeding the list; weekly meal plan; household grocery expense tracking
Features à voler:
  - Expense tracking attached to shopping: log what a trip cost and split it between household members — a natural extension of HouseOS's completion journal (which already stores cost)
  - Real-time WebSocket sync of the list so two people shopping in different aisles never double-buy
  - Items carry icon + category so the list groups visually by store section
  - The app learns purchase habits and suggests recipes/items ('smart recommendations') from history
  - Native Flutter apps against a tiny Flask server — proof that a minimal API + rich client works for the in-store use case HouseOS's desktop-first web can't cover

## Skylight Calendar — 7/10 [dashboards-displays + family-organizers]
URL: https://myskylight.com/calendar
Calendrier mural tactile grand public (15″ à 27″) : agendas synchronisés, tableaux de corvées, planification des repas, listes — géré depuis une app mobile. Leader du marché « family command center ».
Prix: 15″ ≈ 329,99 $US, Calendar Max 27″ ≈ 599,99 $US ; abonnement Plus 79 $US/an (sync bidirectionnelle Google, corvées, repas) | Monétisation: matériel + abonnement | OSS: False | Self-host: False
Pourquoi ce score: Chevauchement fort (corvées + calendrier + repas sur écran mural) malgré l'orientation enfants/récompenses sans intérêt ici.
Cas d'usage: centre de commande familial dans la cuisine; tableau de corvées tactile; planification des repas de la semaine
Features à voler:
  - Magic Import : photographier un horaire papier ou transférer un courriel, l'IA en extrait les événements — idée directement transposable au MCP HouseOS (photo du calendrier des collectes → tâches)
  - Couleur par personne appliquée partout (événements, corvées, filtres) : identité visuelle par membre du foyer, cohérente sur mur et mobile
  - Panneau latéral repas : le plan de la semaine affiché en permanence à côté du calendrier, pas dans un autre écran
  - Vue « aujourd'hui » simplifiée pour l'écran mural vs vue mensuelle dense pour l'app : deux densités du même domaine
  - Corvées récurrentes cochables directement sur l'écran tactile mural (tap-pour-compléter sans téléphone)

## OneHaus — 7/10 [family-organizers]
URL: https://onehaus.app/
Organiseur de maisonnée britannique : tâches/corvées récurrentes assignables, calendrier du foyer, listes d'achats ET inventaire de la maison (électros, véhicules, animaux, documents, abonnements).
Prix: essai 30 jours / Pro 4.99£ mois ou 44.99£ an (un abonnement couvre tout le foyer) | Monétisation: abonnement | OSS: False | Self-host: False
Pourquoi ce score: Rare organiseur familial qui combine corvées récurrentes ET registre d'actifs/documents — c'est presque le périmètre exact de HouseOS en SaaS fermé.
Cas d'usage: corvées récurrentes assignées; inventaire des électros et véhicules avec documents; suivi des abonnements du foyer avec rappels d'échéance; listes d'achats partagées
Features à voler:
  - Inventaire du foyer élargi au-delà des électros : véhicules, animaux ET abonnements/contrats avec rappels de renouvellement — idée : entité « Contrat/Abonnement » avec compte à rebours d'échéance à côté d'Équipement
  - Fiches d'inventaire portant leurs documents (facture, garantie, manuel) — même couplage équipement↔documents que HouseOS, valide le design
  - Un seul abonnement pour toute la maisonnée (le foyer est l'unité de facturation, pas la personne)

## HomeLogger — 7/10 [selfhosted-household]
URL: https://github.com/masoncfrancis/homelogger
Traqueur d'entretien domiciliaire auto-hébergé (Go + React, MIT) qui centralise électroménagers, réparations et tâches d'entretien avec reçus et fichiers. Jeune projet en 0.x.
Prix: gratuit, open source (MIT) | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Chevauche exactement équipements + journal d'entretien de HouseOS, mais projet embryonnaire avec peu de mécaniques originales.
Cas d'usage: historique des réparations par appareil; reçus et photos attachés aux travaux; calendrier d'entretien de la maison
Features à voler:
  - Réparations comme entité distincte de l'entretien planifié : un incident (bris) n'est pas une occurrence de tâche — distinction que le Journal de HouseOS pourrait adopter
  - Sauvegarde/restauration en un ZIP (données + fichiers) — mécanisme d'export simple à offrir à côté du pg_dump
  - Mode démo avec données d'exemple intégré au produit

## Docspell — 7/10 [documents-warranties]
URL: https://docspell.org/
Organiseur de documents auto-hébergé axé sur le pipeline d'ingestion : l'humain ne classe rien, la machine propose tout. Pour foyers/petites équipes (« collectives »).
Prix: gratuit, open source (AGPL-3.0) | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Même domaine que le module Documents, avec le pipeline de métadonnées le plus poussé du segment, mais aucun concept d'équipement ni de maintenance.
Cas d'usage: Déverser scans et courriels bruts et laisser le système annoter; Dates d'échéance extraites du texte du document; Gestion multi-personnes d'une même boîte de documents
Features à voler:
  - Extraction de métadonnées candidates depuis le texte OCR : dates (dont date d'échéance du document), correspondants et montants détectés puis proposés à un clic de confirmation
  - Classifieur auto-entraîné périodiquement sur les documents déjà classés pour prédire tags/correspondant des suivants
  - Notion de 'due date' de premier ordre sur un document avec notification périodique des documents arrivant à échéance (courriel) — transposable en occurrences HouseOS
  - Adresses courriel d'ingestion par boîte + scanner de boîtes IMAP planifié
  - Multi-tenant par 'collective' : deux comptes (Alain/Ariane) partagent le même corpus avec identités distinctes

## paperless-ai — 7/10 [documents-warranties]
URL: https://github.com/clusterzx/paperless-ai
Compagnon IA pour Paperless-ngx : un LLM (OpenAI ou Ollama local) lit chaque nouveau document, le titre, le tague et le classe, et offre un chat sémantique sur le corpus.
Prix: gratuit, open source (MIT) | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Pas un produit autonome, mais la démonstration exacte de ce que le serveur MCP de HouseOS pourrait faire avec le module Documents : enrichissement LLM à l'ingestion.
Cas d'usage: Classification et titrage automatiques de tout document entrant via LLM; Chat « où est la facture de la thermopompe ? » sur ses propres papiers; Fonctionnement 100 % local avec Ollama
Features à voler:
  - Pipeline post-ingestion : à chaque nouveau document, appel LLM avec prompt contraint aux tags/correspondants existants pour éviter l'explosion de taxonomie
  - Mode 'chat with your documents' (RAG) sur le corpus personnel
  - Écosystème entier de variantes à piller : paperless-gpt (OCR par LLM-vision, meilleur que Tesseract sur manuscrit), PaperCortex (MCP server pour Claude sur les documents !), Paperless-LabelAI (extraction légère sur Raspberry Pi)
  - Idée directe pour HouseOS : l'outil MCP gerer_document pourrait proposer dossier/facettes/équipement lié automatiquement à l'upload

## Centriq (défunt, jan. 2026) — 7/10 [documents-warranties]
URL: https://dib.io/blog/centriq-shutting-down-alternative
L'app de référence « photographiez la plaque, on s'occupe du reste » pour propriétaires : manuels, pièces de rechange, rappels et guides d'entretien dérivés du modèle exact. Fermée le 31 janvier 2026, données supprimées.
Prix: était freemium ; service fermé, domaine racheté par un tiers | Monétisation: était freemium ; a échoué commercialement | OSS: False | Self-host: False
Pourquoi ce score: Domaine identique au duo Équipements+Documents ; sa mort (et la purge des données utilisateurs) est l'argument massue du choix auto-hébergé de HouseOS.
Cas d'usage: Photo de plaque signalétique → manuel + pièces + rappels; Base de connaissances d'entretien par appareil; Inventaire d'appareils pour l'assurance
Features à voler:
  - Le mécanisme fondateur du segment : de la seule photo de plaque, dériver manuel PDF, liste de pièces compatibles (filtres, courroies), avis de rappel et calendrier d'entretien recommandé
  - Tâches d'entretien pré-remplies par type d'appareil (ex. « remplacer le filtre du frigo tous les 6 mois ») rattachées à l'équipement — exactement le lien Équipement→Tâche récurrente de HouseOS
  - Leçon négative : dépendance totale au nuage d'un tiers = perte de toutes les données du foyer à la fermeture ; l'export CSV de dernière minute était insuffisant

## DAKboard — 7/10 [dashboards-displays]
URL: https://dakboard.com
Plateforme hébergée qui transforme n'importe quel écran (ou leur matériel Wall Display) en affichage mural personnalisable : calendrier, photos, météo, nouvelles, ~100 intégrations. Pour familles et bureaux voulant un écran toujours allumé sans bricolage.
Prix: gratuit (1 écran, layouts prédéfinis) / Premium ≈ 5-8 $US/mois ; matériel : CPU 149-179 $US, Wall Display Touch 22″ 599,95 $US, 27″ 799,95 $US | Monétisation: freemium + abonnement + matériel | OSS: False | Self-host: False
Pourquoi ce score: Même vision qu'une future vue murale HouseOS (calendrier + météo + tâches sur écran cuisine), mais SaaS fermé.
Cas d'usage: calendrier familial mural; cadre photo + agenda combiné; écran d'accueil cuisine avec météo et tâches
Features à voler:
  - Planification d'écrans par plage horaire : un layout « matin » (agenda, météo, trajets) bascule automatiquement vers un layout « soirée » ou un mode photo — HouseOS pourrait servir des vues murales différentes selon l'heure
  - Mode nuit : écran noir pur entre deux heures configurées pour ne pas éclairer la pièce
  - Boucles (playlists) d'écrans avec durée d'affichage par écran
  - Éditeur visuel de blocs en grille libre : chaque widget positionnable/redimensionnable, pas de layout imposé
  - Widget « compte à rebours » vers une date (déménagement !) affiché en permanence

## TRMNL — 7/10 [dashboards-displays]
URL: https://usetrmnl.com
Écran e-ink 7,5″ WiFi à pile (~3 mois d'autonomie) piloté par un serveur qui rend des écrans 1-bit ; 850+ plugins communautaires, firmware et serveur open source. Pour affichage passif « glanceable » sans écran lumineux.
Prix: appareil 139 $US (à vie, sans abonnement) ; TRMNL X 10,3″ 219 $US ; kit DIY 45 $US + licence BYOD 50 $US ; licence développeur plugins privés 20 $US (unique) | Monétisation: matériel + licences uniques (aucun abonnement) | OSS: True | Self-host: True
Pourquoi ce score: Architecture exacte prévue pour l'e-ink HouseOS phase 3 (rendu serveur, appareil passif) avec un écosystème de plugins mûr à copier.
Cas d'usage: agenda e-ink au mur; tableau de bord bureau sans distraction; affichage rotatif météo/tâches/citations
Features à voler:
  - Architecture « terminal muet » : le serveur rend un BMP 1-bit, l'appareil ne fait que le télécharger et l'afficher — c'est précisément le plan e-ink rendu-serveur de HouseOS, validé commercialement
  - Recipes : configurations de plugins partagées par la communauté, forkables en un clic (marketplace de layouts, pas de code)
  - Mashups : composer 2-4 plugins sur un même écran e-ink (moitiés, quadrants) via des layouts prédéfinis
  - Playlists d'écrans avec rotation et durée par écran, gérées côté serveur
  - Plugins privés en deux stratégies au choix : polling (le serveur va chercher) ou webhook (l'app pousse) — modèle simple pour brancher HouseOS
  - BYOS (Build Your Own Server) : serveur officiel auto-hébergeable, le business model survit au self-hosting

## MagInkDash / MagInkCal — 7/10 [dashboards-displays]
URL: https://github.com/speedyg0nz/MagInkDash
Projets DIY open source cultes : un serveur (Pi ou Docker) récupère Google Calendar/météo, rend l'écran en HTML puis en image, et la pousse vers un e-ink à pile (Inkplate 10 ou Waveshare 12,48″). Des forks actifs (mag-ink-dash-plus) modernisent le rendu en conteneur.
Prix: gratuit, open source (matériel DIY ~100-200 $US) | Monétisation: aucune | OSS: True | Self-host: True
Pourquoi ce score: Le prototype exact de l'e-ink HouseOS phase 3 : rendu serveur HTML→image, appareil passif à pile, code lisible.
Cas d'usage: calendrier e-ink mural sur batterie; dashboard e-paper « glanceable » sans fil
Features à voler:
  - Pipeline rendu serveur : HTML/CSS rendu en headless puis converti en image poussée à l'écran — HouseOS peut réutiliser sa stack web existante pour générer l'écran e-ink au lieu d'un rendu natif
  - Appareil à pile qui se réveille une fois par jour (ou par heure), télécharge l'image et se rendort : des mois d'autonomie car toute l'intelligence est côté serveur
  - E-ink tricolore utilisé pour surligner le jour courant et les urgences en rouge : hiérarchie visuelle avec 3 couleurs seulement
  - Fork family-e-ink-dashboard : rendu dans un conteneur Docker sur le serveur maison, l'Inkplate ne fait qu'afficher — colle au Compose de HouseOS
  - Layout agenda condensé pensé pour être lu en 3 secondes en passant devant

## HomeBeacon — 7/10 [projects-renovation]
URL: https://homebeacon.app/
A lightweight home-maintenance SaaS for owner-occupiers positioning itself as the simpler, free-to-start HomeZada alternative; one full home free.
Prix: gratuit (1 maison complète) / plans payants pour plusieurs propriétés | Monétisation: freemium subscription gated on number of properties | OSS: False | Self-host: False
Pourquoi ce score: Same core domain as HouseOS's task+equipment modules (maintenance, history, warranties), minus projects/finances.
Cas d'usage: seasonal maintenance checklists; equipment/appliance tracking with warranties and manuals; recurring task reminders; per-feature maintenance history
Features à voler:
  - 5-minute onboarding interview (home type, climate, features) auto-generates a personalized seasonal maintenance schedule — a 'seed my house' wizard HouseOS lacks
  - Maintenance history browsable two ways: by home and by feature/equipment — the per-feature timeline is a cheap, high-value view
  - Tasks carry material tracking and suggestions (which filter size, which caulk) so the supply is part of the task definition
  - Checklists parameterized by climate — maps directly onto HouseOS's seasonal-window differentiator

## Hammond — 7/10 [vehiclesmallenginemaintenancetrackerscarsashouseholdassets]
URL: https://github.com/akhilrex/hammond
Self-hosted vehicle and expense management system (Go + Vue, Docker), successor to Clarkson, built around multi-user households sharing multiple vehicles.
Prix: gratuit, open source (Docker) | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Self-hosted household-vehicles domain with the couple/shared-vehicle model HouseOS has, but expense-centric rather than maintenance-recurrence-centric.
Cas d'usage: Fuel and expense tracking for all household vehicles; Multiple users sharing the same vehicles; Per-vehicle and overall spending reports; Photo-of-receipt quick capture for later entry; Import history from Fuelly
Features à voler:
  - Quick Entry: snap a photo of the receipt/pump screen now, structure the data later — a 'capture inbox' pattern HouseOS could reuse for journal entries and documents (Ariane photographs, someone files later)
  - Vehicles shared across users with per-user attribution — matches the Alain/Ariane two-account model for shared equipment
  - Vehicle-level AND household-overall expense reporting from one ledger
  - Migration importer from a commercial app (Fuelly) as an adoption path — CSV import for journal/history is cheap goodwill

## Tracktor — 7/10 [vehiclesmallenginemaintenancetrackerscarsashouseholdassets]
URL: https://github.com/javedh-dev/tracktor
Self-hosted vehicle tracking management system covering fuel logs, service history, and regulatory/compliance documents (insurance, pollution certificates) with expiry reminders and a fleet dashboard.
Prix: gratuit, open source (Docker, scripts Proxmox LXC communautaires) | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Self-hosted vehicle maintenance plus a compliance-documents angle that overlaps both HouseOS equipment and documents modules.
Cas d'usage: Fuel logging with efficiency trends; Maintenance/service history per vehicle; Insurance and certificate renewal tracking with lapse reminders; Cross-fleet dashboard of what needs attention
Features à voler:
  - Compliance documents as dated entities with expiry (insurance, certificates) and reminders before they lapse — HouseOS documents/equipment could carry an 'expire le' facet feeding countdowns (assurance auto, immatriculation SAAQ, garanties)
  - Single 'what's going on across the fleet' dashboard aggregating fuel, service, and expiring paperwork per asset — a per-equipment health card idea
  - Runs happily on a Pi/home server; Proxmox community script exists — same deployment culture as HouseOS

## Gardenize — 7/10 [gardenplantcareseasonaloutdoorplanningapps]
URL: https://gardenize.com/
Journal de jardin photo-first (web + mobile) organisé en trois entités : Plantes, Surfaces (plates-bandes) et Événements. Pour jardiniers qui veulent documenter l'évolution de leur jardin année après année.
Prix: gratuit (1 photo par plante/surface/événement, sans export ni web) / Plus ~4,40$US mois ou 44$US an (4,99$US via stores) | Monétisation: freemium, abonnement | OSS: False | Self-host: False
Pourquoi ce score: Son triptyque Plante–Surface–Événement est un décalque du modèle Équipement–Zone–Journal de HouseOS appliqué au jardin, et son journal photo multi-années est la feature mémoire qui manque au Journal de complétion.
Cas d'usage: Journal photo chronologique par plante et par plate-bande; Annoter des photos (où sont plantés les bulbes, quoi tailler); Rappels ponctuels ou récurrents de soins; Base de 45 000 espèces + identification photo; Historique multi-années pour comparer les saisons
Features à voler:
  - Modèle Plantes ↔ Surfaces ↔ Événements où chaque événement relie une photo, une plante et un lieu — extension naturelle du Journal de complétion (photo déjà prévue) vers un vrai journal de plates-bandes
  - Outil de dessin sur photo pour marquer où sont les bulbes / quoi tailler — utile aussi pour équipements (où est la valve d'hiver)
  - Timeline « la même plate-bande à travers les années » — comparaison saison sur saison, précieuse pour apprendre un nouveau terrain comme celui de Sainte-Catherine
  - Fiches d'inspiration de soins par espèce attachables à ses propres plantes

## ChoreBuster — 6/10 [chores-recurrence]
URL: https://www.chorebuster.net/
Service web vétéran (119 000+ foyers) qui génère automatiquement un horaire de corvées équitable et l'envoie par courriel ou en version imprimable. Pour familles qui veulent déléguer la planification.
Prix: essai 4 semaines, puis 2,49$US mois / 19,95$US an / 29,95$US achat à vie | Monétisation: abonnement, avec option achat unique | OSS: False | Self-host: False
Pourquoi ce score: Cœur = rotation équitable de corvées récurrentes, mais web 1.0, sans zones/équipements/journal riche.
Cas d'usage: Génération d'un horaire équitable multi-personnes; Corvées pondérées par difficulté; Horaire imprimé/courriel hebdomadaire
Features à voler:
  - L'équité est calculée sur la somme des difficultés (« intensity ») des corvées, pas sur leur nombre — raffinement direct de la stratégie moins-l'a-fait de HouseOS : « moins-d'effort-fourni »
  - Part de charge configurable par personne (ex. 60/40 %) — utile quand un des deux a une semaine chargée
  - Préférences par personne (corvées aimées/détestées) qui biaisent l'assignation automatique
  - Livraison de l'horaire par courriel récurrent et version imprimable — canal basse-friction complémentaire au flux iCal de HouseOS

## Nipto — 6/10 [chores-recurrence]
URL: https://nipto.app/
App mobile française qui transforme le partage des corvées en compétition de points hebdomadaire entre partenaires ou colocs. Pensée d'abord pour les couples.
Prix: gratuit (5 groupes de tâches récurrentes) / Premium ≈ 1,99$US mois, un seul abonnement couvre tout le foyer | Monétisation: freemium, abonnement | OSS: False | Self-host: False
Pourquoi ce score: Corvées récurrentes pour couple (le public exact de HouseOS) mais centré gamification, sans zones/équipements/récurrence riche.
Cas d'usage: Rendre visible la répartition réelle des corvées dans un couple; Compétition ludique hebdomadaire; Objectifs personnels de participation
Features à voler:
  - Cycle hebdomadaire : points remis à zéro chaque dimanche soir, gagnant de la semaine qui réclame une récompense choisie — boucle courte et re-jouable
  - Points bonus offerts manuellement à l'autre comme « merci » (reconnaître une tâche hors-liste) — mécanique de reconnaissance dans un couple, quasi gratuite à implémenter sur le journal HouseOS
  - Deux modes au choix : compétition ou objectif personnel (même donnée, deux lectures) — évite la gamification imposée
  - Graphique de participation par personne qui objective la charge mentale — un simple agrégat du journal de complétion suffirait dans HouseOS
  - Un seul abonnement premium débloque tout le foyer

## Flatastic — 6/10 [chores-recurrence]
URL: https://www.flatastic-app.com/en/
App allemande tout-en-un pour colocs, couples et familles : corvées à points, liste d'épicerie partagée temps réel et partage de dépenses dans la même app.
Prix: gratuit / Premium 1,99€ mois ou 17,99€ an | Monétisation: freemium, abonnement | OSS: False | Self-host: False
Pourquoi ce score: Recouvre corvées récurrentes + épicerie (futur module consommables), mais peu profond sur la récurrence et non auto-hébergeable.
Cas d'usage: Corvées récurrentes avec points; Liste d'épicerie partagée; Partage et règlement des dépenses communes
Features à voler:
  - Regroupement corvées + épicerie + dépenses dans un seul foyer virtuel : valide l'ambition « OS de la maison » plutôt qu'app mono-fonction
  - Points par corvée pondérant l'importance (sortir les poubelles < grand ménage de la salle de bain)
  - Rappels « intelligents » : relance automatique de la personne en retard sur sa corvée, plutôt qu'une notification à tout le foyer
  - Liste d'épicerie temps réel avec photos sur les items — référence UX pour le futur module consommables

## Shelf.nu — 6/10 [home-inventory]
URL: https://github.com/Shelf-nu/shelf.nu
Open-source (AGPL) asset management and scheduling platform aimed at teams/equipment fleets — QR tracking, custody, bookings — self-hostable or SaaS.
Prix: gratuit, open source (self-host) ; SaaS : plan gratuit 1 utilisateur, Team 67$US mois / 370$US an | Monétisation: open-core SaaS subscription | OSS: True | Self-host: True
Pourquoi ce score: Team-oriented rather than household, but a mature open-source reference for QR-anchored asset records and location hierarchy.
Cas d'usage: equipment registry with QR tags; who-has-it custody tracking; booking/checkout of shared gear; hierarchical location tree
Features à voler:
  - QR codes scannable with any phone camera, no app: the code opens a mobile web page for the asset with check-in/out actions — HouseOS's wall-tablet/NFC flow could mimic this app-less pattern
  - Custody log: every asset knows who currently holds it and the full hand-over history
  - Booking calendar per asset preventing double-booking, with checkout/return dates
  - Hierarchical locations (building → floor → room → shelf) with GPS tagging
  - Flat price per workspace with unlimited assets — never per-item pricing

## Nest Egg — 6/10 [home-inventory]
URL: https://nestegg.cloud/home-inventory/
Barcode-centric home/small-business inventory app (iOS/Android/web) with quantity tracking, stock levels and cloud sync.
Prix: gratuit (essai) / 11.99$US an (150 articles) / Advanced 31.99$US mois (3 utilisateurs, 2500 articles) ; variante iOS à achat unique | Monétisation: subscription (plus one-time legacy app) | OSS: False | Self-host: False
Pourquoi ce score: Inventory-with-quantities bridges HouseOS's equipment registry and its planned consumables/pantry module.
Cas d'usage: barcode-scan cataloging of belongings; pantry/consumable quantity tracking; printable QR/barcode labels; multi-location stock transfers
Features à voler:
  - Barcode scan auto-populates product name, photo AND typical price from a product database — near-zero-typing item entry
  - Quantity/stock-level model on household items with low-stock awareness — the consumables pattern HouseOS has on its roadmap
  - Stock transfer operation moving quantities between locations as a first-class action
  - Customizable printable QR/barcode label templates, camera or Bluetooth scanner input

## HomyScan — 6/10 [home-inventory]
URL: https://homyscan.com/
Modern mobile-first home inventory app organized Space → Room → Box, aimed at insurance documentation and moving, with real-time family collaboration.
Prix: gratuit au départ ; paliers payants pour plus d'articles et de fonctions (montants non publiés clairement) | Monétisation: subscription (freemium) | OSS: False | Self-host: False
Pourquoi ce score: Clean take on the same box/room/warranty domain; useful mostly as UX inspiration for capture flow and reporting.
Cas d'usage: insurance-ready inventory PDFs; moving-box contents via QR; couple/roommate shared inventory; warranty and serial documentation
Features à voler:
  - Fixed three-level physical hierarchy Space → Room → Box with QR codes on boxes — matches a real house's mental model better than free-form folders
  - One-tap insurance-ready PDF export: photos, values, serials, grouped by room — a 'documents' output HouseOS could generate from equipment + documents data
  - Real-time collaboration with Editor/Viewer roles per space — the two-person household permission model, minimal and sufficient
  - Barcode scan auto-fills item details on capture

## MyStuff2 Pro — 6/10 [home-inventory]
URL: https://www.maddysoft.com/mystuff/
Veteran iOS/macOS personal inventory database by a solo developer; fully user-definable schema, one-time purchase, no cloud dependency required.
Prix: gratuit (15 articles, toutes fonctions) puis achat unique pour articles illimités ; extras Pro en achats intégrés — pas d'abonnement | Monétisation: one-time purchase + in-app purchases | OSS: False | Self-host: False
Pourquoi ce score: A database-first, schema-flexible inventory — closer to HouseOS's JSONB-specs philosophy than photo-first competitors, though Apple-only.
Cas d'usage: catalog collections (books, wine, electronics); user-defined category schemas; barcode product lookup; local/own-server data transfer
Features à voler:
  - Per-category custom field schemas: each user-defined category (wine vs. electronics) carries its own set of typed fields — a concrete design for HouseOS's JSONB equipment specs with per-type templates
  - Barcode scanning plus product-database search, autofill, quick-copy and bulk editing for fast entry
  - Data transfer via the user's OWN channels — local computer, FTP server, email, Dropbox — sync without a vendor cloud
  - One-time-purchase economics sustained since 2012, proof the niche tolerates no-subscription tools

## Dwellin — 6/10 [home-maintenance + projects-renovation]
URL: https://dwellin.com/
Home maintenance app that builds a customized upkeep schedule from public property data; aimed at ordinary homeowners who don't know what needs doing.
Prix: gratuit / Pro 2.99$US mois ou 24.99$US an | Monétisation: freemium subscription | OSS: False | Self-host: False
Pourquoi ce score: Same maintenance-schedule core but shallow, mobile-only, and dependent on US property-data sources.
Cas d'usage: auto-generated maintenance schedule; home inventory; appliance manuals; insurance export
Features à voler:
  - Seeds the schedule from public records: square footage, year built, and *probable system ages* inferred from the build year — smart cold-start when equipment install dates are unknown
  - Auto-updating 'home binder' — the exportable insurance/inventory document regenerates itself as data changes instead of being a manual export
  - Automatic refresh of appliance manuals when a model is identified

## Upkept — 6/10 [home-maintenance]
URL: https://www.consumerreports.org/home-maintenance-repairs/upkept-home-app-from-our-president-september-2021
Consumer Reports-backed home maintenance app that proposes a year-round plan with step-by-step DIY guidance; for homeowners deciding DIY vs pro.
Prix: essai 30 jours puis 4.99$US mois (statut actuel incertain — app de 2021) | Monétisation: subscription | OSS: False | Self-host: False
Pourquoi ce score: Directly the seasonal-checklist/maintenance-plan domain, but thin beyond scheduling and possibly moribund.
Cas d'usage: yearly maintenance plan; DIY step-by-step instructions; task reminders
Features à voler:
  - Each task carries an explicit DIY-vs-hire-a-pro verdict with reasoning — a useful metadata field for HouseOS tasks
  - Tasks embed step-by-step instructions and expert tips inline (task as a mini-guide, not just a title + due date)
  - Recommends the best time of year to do each DIY job — pairs naturally with HouseOS seasonal windows and weather rules

## HomeManager — 6/10 [home-maintenance]
URL: https://homemanager.io/
All-in-one homeowner app with monthly maintenance checklists, DIY how-to videos, and home financial reporting; web + mobile.
Prix: gratuit / PLUS 8$US mois ou 80$US an | Monétisation: freemium subscription | OSS: False | Self-host: False
Pourquoi ce score: Monthly-checklist and service-history core overlaps HouseOS, but consumer cloud app with education content as the main value-add.
Cas d'usage: monthly maintenance checklists; maintenance history for resale; replacement budgeting; home financial report
Features à voler:
  - 'Replacement budget': computes a monthly sinking-fund amount per big-ticket item (roof, HVAC, water heater) from expected lifespan and cost — concrete formula HouseOS could derive from its equipment registry
  - Maintenance history explicitly packaged as an artifact for insurance claims and home resale
  - Each monthly checklist item links to a how-to video / step-by-step guide

## Tandoor Recipes — 6/10 [pantry-grocery + selfhosted-household]
URL: https://tandoor.dev/
Feature-maximal self-hosted recipe manager (Django + PostgreSQL) with spaces, granular permissions and nutrition; also sold as a cheap hosted service.
Prix: self-host gratuit / hébergé : 0 € · 1,99 €/mois · 3,49 €/mois · 4,99 €/mois (Premium AI) | Monétisation: freemium hosted tiers on top of open source | OSS: True | Self-host: True
Pourquoi ce score: Recipe/meal-plan module overlap plus a PostgreSQL-first self-hosted architecture close to HouseOS's stack.
Cas d'usage: recipe import from almost any site; automatic meal planning; shopping list sorted by supermarket layout; shared cookbook spaces with permissions
Features à voler:
  - Shopping list auto-sorted 'according to your supermarket' — user-defined aisle order per store
  - Automatic nutrition calculation and price computation per recipe
  - Meal plan exports to calendar (iCal) — same notification channel HouseOS already uses for tasks
  - Spaces with per-recipe share links and 'secret recipe' visibility — fine-grained sharing model
  - Real-time multi-user checking of the shopping list in-store

## Pantry Check — 6/10 [pantry-grocery]
URL: https://pantrycheck.com/
Dedicated grocery/pantry inventory app centered on barcode scanning and best-before tracking to cut food waste.
Prix: gratuit jusqu'à 200 articles ; abonnements payants au-delà (montants non publiés) | Monétisation: freemium subscription (item-count cap) | OSS: False | Self-host: False
Pourquoi ce score: Pure play on the pantry/expiry module HouseOS has on its roadmap.
Cas d'usage: barcode-scan pantry inventory; expiration reminders; shopping list from what ran out; meal planning from stock
Features à voler:
  - Barcode scanner as the primary inventory-entry gesture, with a product database prefilling name/photo
  - Automatic expiration reminders scheduled from each item's best-before date — same materialized-due-date philosophy as HouseOS's recurrence engine
  - Item-count cap (200 free) as the paywall lever rather than features
  - Shopping list generated from consumed/expired inventory, closing the stock → list loop

## KitchenPal — 6/10 [pantry-grocery]
URL: https://kitchenpalapp.com/en/
Commercial pantry tracker + meal planner + shopping list that suggests recipes from what you actually have in stock.
Prix: gratuit / Premium 3,99 $US mois · 14,99 $US an · 29,99 $US à vie · 39,99 $US à vie famille | Monétisation: freemium subscription with lifetime option | OSS: False | Self-host: False
Pourquoi ce score: Covers the whole pantry→recipe→list loop HouseOS's consumables module would want.
Cas d'usage: pantry inventory with barcode; what-can-I-cook-with-what-I-have recipe matching; shopping list from missing ingredients; household sharing
Features à voler:
  - Recipe matching ranked by percentage of ingredients already in your pantry, listing exactly what's missing
  - Missing ingredients push to the shopping list in one tap
  - Lifetime and family-lifetime pricing tiers — the anti-subscription posture a self-hosted hobbyist audience loves
  - Dietary-profile filters applied across the household's suggestions

## Cozi Family Organizer — 6/10 [family-organizers]
URL: https://www.cozi.com/
Le vétéran des organiseurs familiaux : calendrier partagé, listes d'épicerie, planificateur de repas et to-dos pour toute la maisonnée. Grand public, très orienté familles avec enfants.
Prix: gratuit avec pubs / Cozi Gold 39$US an (sans pub, vue mois, suivi d'anniversaires) | Monétisation: freemium + publicité sur le palier gratuit | OSS: False | Self-host: False
Pourquoi ce score: Fort recouvrement sur les modules calendrier/listes/repas que HouseOS vise, mais SaaS grand public daté, sans entretien de maison ni inventaire.
Cas d'usage: calendrier familial partagé code-couleur; listes d'épicerie synchronisées; planification de repas avec boîte à recettes; courriel-agenda quotidien envoyé à toute la famille
Features à voler:
  - Une couleur par membre appliquée partout (calendrier, listes, tâches) — identité visuelle transversale, pas seulement dans le calendrier
  - Courriel-agenda automatique chaque matin résumant la journée de toute la maisonnée (équivalent : digest quotidien HouseOS par courriel ou flux)
  - Autocomplétion des articles d'épicerie depuis l'historique d'achats du foyer
  - Suivi d'anniversaires/dates récurrentes comme entité distincte des événements

## FamilyWall — 6/10 [family-organizers]
URL: https://www.familywall.com/
Hub familial tout-en-un : calendrier, repas, listes, messagerie, localisation et coffre-fort de documents. Couvre large sans aller profond sur chaque module.
Prix: gratuit (base) / Premium 4.99$US mois ou 44.99$US an | Monétisation: freemium abonnement | OSS: False | Self-host: False
Pourquoi ce score: Le concurrent le plus « tout-en-un » côté SaaS : recoupe documents, repas, listes et calendrier de HouseOS, mais rien sur l'entretien/équipements.
Cas d'usage: calendrier partagé; planificateur de repas avec import de recettes; coffre-fort de documents et pièces d'identité; partage de position avec alertes de zones; suivi de dépenses simple
Features à voler:
  - « Safe » : coffre chiffré pour documents sensibles (assurances, pièces d'identité, mots de passe du foyer) séparé des documents ordinaires — idée pour une facette « sensible » dans le module Documents
  - Géorepérage : alerte quand un membre arrive/quitte un lieu défini (maison, travail) — pertinent pour la phase capteurs/présence
  - Import de recettes par URL vers le planificateur de repas, qui alimente la liste d'épicerie
  - Partage de dépenses ponctuelles rattachées au foyer (embryon de journal de coûts)

## Hearth Display — 6/10 [dashboards-displays + family-organizers]
URL: https://hearthdisplay.com/
Écran familial tactile 27″ vertical à cadre de bois centré sur les « routines » (checklists quotidiennes par personne) avec app compagnon et assistant IA. Positionné haut de gamme famille.
Prix: 699 $US (souvent 599 $US en solde) + adhésion 86,40 $US/an ou 9 $US/mois (requise pour l'installation) | Monétisation: matériel + abonnement quasi obligatoire | OSS: False | Self-host: False
Pourquoi ce score: Même domaine mural corvées/calendrier ; le concept de routines séquencées est la pépite, le volet enfants/étoiles est hors sujet.
Cas d'usage: routines du matin/soir affichées au mur; calendrier partagé du foyer; to-dos assignés visibles de tous
Features à voler:
  - Routines : checklists ordonnées rattachées à un moment de la journée (matin, retour, soir) avec progression par personne — un chaînon manquant entre « tâche » et « habitude » que le moteur HouseOS pourrait modéliser
  - Hearth Helper par texto : envoyer un SMS ou une photo, l'IA crée les événements/to-dos — équivalent du MCP HouseOS mais par canal SMS
  - Orientation portrait assumée : un écran vertical montre mieux une journée chronologique qu'un paysage
  - Profils illimités y compris invités récurrents (gardienne, grands-parents) avec vue restreinte
  - Tap-pour-compléter sur le mur, sync immédiate vers l'app des autres membres

## Maple — 6/10 [family-organizers]
URL: https://www.growmaple.com/
« Assistant familial » tout-en-un avec IA : calendrier, repas, to-dos, corvées, notes et une adresse courriel familiale dont la boîte est convertie en tâches. Racheté par Wander, fermeture annoncée le 31 déc. 2026.
Prix: gratuit (généreux) / Maple+ 5$US mois ou 40$US an — service en fin de vie (sunset 2026-12-31) | Monétisation: freemium abonnement (avant rachat) | OSS: False | Self-host: False
Pourquoi ce score: Recouvrement fort sur tâches/repas/listes et surtout sur la couche IA d'ingestion, mais SaaS mourant — ses idées d'automatisation sont le vrai butin.
Cas d'usage: transformer les courriels d'admin/école en tâches; automatisations planifiées par IA; calendrier + repas + listes en un seul endroit
Features à voler:
  - Adresse courriel dédiée au foyer : tout ce qu'on y transfère (facture, avis d'école, confirmation) est parsé en événement/tâche — idée directe pour un ingesteur IMAP + LLM dans HouseOS
  - « Routines » = automatisations IA planifiées : « chaque dimanche, génère le plan de repas », « chaque soir 21h, réassigne les tâches en retard » — mariage naturel avec le serveur MCP de HouseOS
  - Sa mort (rachat + sunset) est l'argument massue du self-hosted : les données familiales dans un SaaS disparaissent avec lui

## HortusFox — 6/10 [selfhosted-household]
URL: https://github.com/danielbrendel/hortusfox-web
Système collaboratif auto-hébergé de gestion des plantes de la maison : catalogue par emplacement, alertes de soins, identification par photo. Pour foyers à pouces verts.
Prix: gratuit, open source | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Niche du vivant-de-la-maison : mêmes patterns que HouseOS (objets par zone, tâches de soin, journal) appliqués aux plantes.
Cas d'usage: cataloguer les plantes par pièce; alertes des plantes qui ont besoin de soins; identification d'espèce via Pl@ntNet; journal des actions par membre du foyer
Features à voler:
  - Système d'alerte « cette plante a besoin d'attention » dérivé de l'état de l'objet plutôt que d'un calendrier — un objet peut réclamer une tâche (pattern capteur→tâche version manuelle)
  - Plantes groupées par emplacement/pièce, calqué sur les Zones — confirme que la zone est la bonne colonne vertébrale
  - Identification d'espèce par photo via l'API Pl@ntNet — enrichissement d'une fiche par un service externe au moment de la création
  - Journal d'historique des actions par utilisateur sur chaque plante, l'équivalent du Journal de complétion appliqué à un objet vivant

## Papra — 6/10 [documents-warranties]
URL: https://papra.app/
Plateforme de gestion documentaire open source minimaliste, pensée archivage sans friction, avec ingestion par courriel et règles de tagging. Pour qui trouve Paperless-ngx trop lourd.
Prix: gratuit auto-hébergé, open source (AGPL-3.0) ; nuage géré freemium (résidence de données UE) | Monétisation: freemium (tier nuage géré) | OSS: True | Self-host: True
Pourquoi ce score: Chevauchement direct avec le module Documents mais volontairement plus pauvre (pas d'OCR ni d'enrichissement automatique), intéressant surtout pour son ergonomie et son API.
Cas d'usage: Archivage simple de documents du foyer; Tagging automatique par règles déclaratives; Intégration programmatique (API, webhooks, CLI, SDK)
Features à voler:
  - Adresse courriel dédiée par organisation (@papra.email) : transférer une facture reçue par courriel suffit à l'archiver — idée forte pour HouseOS
  - Règles de tagging déclaratives (conditions sur nom/contenu → tags) éditables dans l'UI, sans ML : simple, prévisible, débogable
  - Webhooks sur événements documentaires : brancher l'arrivée d'un document sur une action externe (ex. créer une tâche)
  - Corbeille avec rétention avant suppression définitive

## MagicMirror² — 6/10 [dashboards-displays]
URL: https://magicmirror.builders
Plateforme open source de miroir/écran intelligent modulaire (Electron sur Raspberry Pi) avec plus de 1 000 modules tiers. La référence DIY des affichages muraux depuis 2016.
Prix: gratuit, open source (MIT) | Monétisation: aucune (projet communautaire) | OSS: True | Self-host: True
Pourquoi ce score: Architecture de référence pour une vue murale extensible ; le domaine (affichage) recoupe la phase 3 e-ink/tablette de HouseOS.
Cas d'usage: miroir intelligent dans l'entrée; écran d'information mural DIY; affichage agenda + météo + transports
Features à voler:
  - Bus de notifications inter-modules : chaque module peut émettre/écouter des événements des autres (ex. le module présence masque les infos privées quand un invité est détecté) — patron intéressant pour des widgets HouseOS réactifs
  - Écosystème de 1 000+ modules avec dépôt central testé automatiquement (MagicMirror-3rd-Party-Modules) : la qualité vient d'un contrat de module minimal et stable
  - Régions de placement nommées (top_bar, middle_center…) plutôt que grille : un module déclare sa zone, le layout reste lisible
  - Module remote-control : masquer/afficher des modules et changer de page depuis le téléphone
  - Modules « recommandation d'habillement selon la météo » : exemple concret de règle météo → suggestion actionnable, exactement l'esprit « bonne journée pour tondre »

## Home Assistant Dashboards (Lovelace) — 6/10 [dashboards-displays]
URL: https://www.home-assistant.io/dashboards/
Le système de tableaux de bord de Home Assistant : vues en sections (grille), dizaines de cartes natives + écosystème HACS (Mushroom, layout-card, button-card). Standard de facto des tablettes murales domotiques.
Prix: gratuit, open source | Monétisation: aucune pour les dashboards (Nabu Casa 6,50 $US/mois pour le cloud, optionnel) | OSS: True | Self-host: True
Pourquoi ce score: Pas le domaine tâches/maison de HouseOS, mais le modèle de composition d'écrans muraux que la phase 3 devra égaler.
Cas d'usage: tablette murale de contrôle maison; dashboard par pièce; vue kiosque cuisine
Features à voler:
  - Cartes conditionnelles : une carte n'apparaît que si un état correspond (ex. carte « collecte demain » visible seulement la veille) — applicable aux occurrences HouseOS : n'afficher que ce qui est actionnable maintenant
  - Visibilité par utilisateur et par appareil : le même dashboard montre des sections différentes selon qui regarde et sur quel écran
  - Vue « sections » : grille responsive où les cartes se redimensionnent, avec en-têtes repliables par section
  - Badges compacts en haut de vue pour les états secondaires (portes, piles faibles) — dense sans encombrer
  - Mode kiosque (kiosk-mode HACS) qui masque chrome/navigation pour l'écran mural

## openHASP — 6/10 [dashboards-displays]
URL: https://www.openhasp.com/
Firmware open source (LVGL sur ESP32) qui transforme des écrans tactiles bon marché en plaques de contrôle murales pilotées par MQTT. Indépendant de Home Assistant mais avec intégration officielle.
Prix: gratuit, open source (MIT) ; matériel ESP32+écran ~15-60 $US | Monétisation: aucune | OSS: True | Self-host: True
Pourquoi ce score: Déjà nommé dans la feuille de route phase 3 de HouseOS ; son protocole pages-par-MQTT est le patron à suivre.
Cas d'usage: interrupteur mural intelligent avec écran; mini-panneau de contrôle par pièce; afficheur de température/état près d'une porte
Features à voler:
  - Pages d'UI décrites en objets JSONL poussés par MQTT : le serveur redéfinit l'écran à chaud sans reflasher — HouseOS pourrait pousser « les 3 tâches de cette pièce » sur la plaque de la pièce
  - Liaison bidirectionnelle générique : tout événement d'objet (tap, slider) publie sur MQTT, toute propriété d'objet est réglable par MQTT — aucune logique dans le firmware
  - 12 pages navigables par appareil, extensibles en config
  - Rétroéclairage piloté par capteur de mouvement via automatisation (écran éteint par défaut)
  - Tourne sur du matériel jetable à ~20 $ : multiplier les petits écrans par pièce plutôt qu'un seul grand

## MoveAdvisor — 6/10 [projects-renovation]
URL: https://moveadvisor.com/
Free end-to-end moving planner app: timeline checklist, room-by-room inventory, mover directory and quotes, for anyone planning a household move.
Prix: gratuit (monétisé par les références aux déménageurs) | Monétisation: free app; revenue from moving-company lead generation | OSS: False | Self-host: False
Pourquoi ce score: A move is exactly a HouseOS 'project' with deadline-driven tasks — and HouseOS's first real data is Alain's October move.
Cas d'usage: week-by-week moving preparation; room-by-room pre-move inventory; estimating shipment weight for mover quotes; finding and booking movers
Features à voler:
  - Deadline-anchored timeline: enter the move date and it auto-generates the full task plan week-by-week counting down (T-8 weeks: book movers; T-2: notify utilities) — a template of relative-offset tasks instantiated from one date
  - Room-by-room virtual inventory built during packing that then estimates total shipment weight — inventory captured once, reused for logistics
  - Checklist items carry embedded guidance (why/how), not just a title

## HomeBinder — 6/10 [projects-renovation]
URL: https://pages.homebinder.com/
Free digital 'home binder' typically gifted to buyers at closing by real-estate agents and home inspectors: documents, maintenance reminders, appliance records, projects.
Prix: gratuit pour les propriétaires (offert par les agents/inspecteurs, qui paient) | Monétisation: B2B2C: agents, inspectors and lenders pay to gift branded binders and stay in the client's app | OSS: False | Self-host: False
Pourquoi ce score: Same document+equipment+maintenance record core as HouseOS, with a distribution-driven twist rather than a product-depth one.
Cas d'usage: centralizing home documents after purchase; email maintenance reminders; appliance recall alerts; maintenance history as a resale asset
Features à voler:
  - Binder pre-seeded from the home inspection report: the inspector's findings become the initial equipment list and task backlog — a bulk-import model for cold-start
  - Appliance recall alerts: matches registered brand/model/serial against recall databases and notifies — a concrete, automatable value-add for HouseOS's equipment registry
  - Maintenance history framed explicitly as a transferable asset: the binder is handed to the next owner at sale, arguing that complete records raise home value

## Drivvo — 6/10 [vehiclesmallenginemaintenancetrackerscarsashouseholdassets]
URL: https://www.drivvo.com/en/personal-use/
Freemium mobile/web vehicle-management app for consumers: fuel, expenses, income, preventive-maintenance alerts, with graphical reports; also covers EVs and fleets up to 100 vehicles.
Prix: gratuit (fonctions essentielles) / Pro par abonnement (sauvegarde infonuagique + synchro multi-appareils); plans affaires multi-véhicules | Monétisation: freemium subscription + ads in free tier | OSS: False | Self-host: False
Pourquoi ce score: Strong overlap on the vehicle-costs module but closed, mobile-first SaaS with no self-hosting.
Cas d'usage: Log every fill-up, cost per km, price history; Categorized expenses (insurance, taxes, tolls, parking); Preventive maintenance alerts (oil change etc.); EV charge logging and mi/kWh consumption; Monthly cost reports per vehicle
Features à voler:
  - Cost-per-kilometre as the headline derived metric (not just totals) — HouseOS journal costs + odometer/hour readings could yield cost-per-hour for the tractor/generator
  - Fuel price history per station/fill-up giving trend charts — a pattern for any consumable price tracking (propane, gas for small engines) in the future consommables module
  - Expense categories spanning ownership costs (insurance, taxes, registration), not just maintenance — a per-equipment 'total cost of ownership' view
  - EV/hybrid treated as first-class with kWh units — reminder to keep HouseOS usage counters unit-agnostic (km, heures, kWh, cycles)

## Fuelly — 6/10 [vehiclesmallenginemaintenancetrackerscarsashouseholdassets]
URL: https://apps.apple.com/us/app/fuelly-mpg-service-tracker/id295905460
Long-running MPG and service tracker (formerly Gas Cubby) with a companion website; community fuel-economy data plus per-vehicle service reminders and attachments.
Prix: gratuit avec version premium par abonnement (prix non publié clairement) | Monétisation: freemium subscription + ads | OSS: False | Self-host: False
Pourquoi ce score: Overlaps the maintenance-reminder and logging module; closed consumer app, aging, community-data angle irrelevant to a two-person household.
Cas d'usage: Fuel-up logging and MPG trend charts; Service reminders with presets (oil change, tire rotation); Store vehicle identity data (VIN, plate); Attach photos/PDFs to records; Compare real-world fuel economy with other owners of the same model
Features à voler:
  - Preset service-reminder templates (oil change, tire rotation…) so a new vehicle starts with a sensible maintenance plan — HouseOS could ship task-template packs per equipment type (souffleuse, tracteur, génératrice, scie à chaîne) with recommended intervals
  - Reminders derived from the service history you enter — history-first, schedule-second, same direction as HouseOS's interval-since-completion mode
  - VIN/plate stored as identity fields on the asset — cheap columns that unlock recall lookup later

## Simply Auto — 6/10 [vehiclesmallenginemaintenancetrackerscarsashouseholdassets]
URL: https://simplyauto.app/
Mobile vehicle-management app (cars, motorcycles, trucks, small fleets) with fuel, service reminders by mileage or date, GPS trip logging for tax-deductible mileage, and multi-driver data sharing.
Prix: gratuit / Gold et Platinum ~9,99$US mois ; option Platinum achat unique 29,99$US | Monétisation: freemium subscription (plus one-time unlock option) | OSS: False | Self-host: False
Pourquoi ce score: Same reminders-by-mileage-or-date core, but closed, phone-first, and heavy on business-mileage features a household doesn't need.
Cas d'usage: Service reminders by mileage or date; Fill-up, service, and expense logging with multiple receipts per record; Business vs personal trip logs via GPS for tax deduction; Scheduled automated weekly/monthly email reports; CSV export/import
Features à voler:
  - Service reminders configurable by mileage OR date per service type — another confirmation the dual-trigger model is the industry norm worth building natively
  - Multiple receipts attached to a single record — HouseOS journal photo field could become a small collection
  - Scheduled automated summary reports emailed weekly/monthly — pattern for a HouseOS 'bilan de la maison' digest via the existing iCal/notification channel
  - One-time-purchase premium tier alongside subscription — irrelevant to HouseOS monetization but a note on how this niche prices

## Planta — 6/10 [gardenplantcareseasonaloutdoorplanningapps]
URL: https://getplanta.com/
App suédoise grand public de soins des plantes d'intérieur : horaire d'arrosage « intelligent » personnalisé par plante, lumière, saison et pot. Pour propriétaires de plantes d'intérieur qui les laissent mourir.
Prix: gratuit (plantes limitées, horaire d'arrosage de base) / premium 7,99$US mois, 17,99$US 3 mois, 35,99$US an | Monétisation: freemium, abonnement | OSS: False | Self-host: False
Pourquoi ce score: Même noyau (tâches récurrentes de soin par objet vivant) mais grand public, mobile, fermé — la valeur est dans ses raffinements d'horaire contextuels, pas dans son domaine.
Cas d'usage: Horaire d'arrosage/fertilisation/brumisation par plante; Identification de plante et de maladie par photo; Photomètre (capteur du téléphone) pour évaluer l'emplacement; Recommandations de plantes selon la pièce et sa lumière
Features à voler:
  - Intervalle d'arrosage modulé par le contexte : saison, lumière de la pièce, taille du pot — chez HouseOS, un multiplicateur saisonnier sur l'intervalle (arroser aux 4 jours l'été, aux 10 jours l'hiver) serait un différenciateur naturel du moteur
  - Chaque plante appartient à une pièce (« site ») avec profil de lumière — s'aligne sur les Zones existantes
  - Actions « reporter/j'ai déjà fait » sur un rappel qui recalent l'horaire au lieu d'empiler du retard — cousin du rollover
  - Gradation de difficulté des plantes et recommandations selon la pièce

## Sunday Lawn Care — 6/10 [gardenplantcareseasonaloutdoorplanningapps]
URL: https://www.getsunday.com/
Programme de pelouse DIY par abonnement : plan saisonnier personnalisé bâti sur imagerie satellite, données climatiques locales et analyse de sol, avec produits livrés au bon moment. Pour propriétaires qui veulent une belle pelouse sans compagnie d'entretien.
Prix: abonnement saisonnier à partir de ~109$US ; ~189$US/saison pour ~3 000 pi² (3 livraisons, test de sol inclus) | Monétisation: abonnement + vente de produits (le plan vend les intrants) | OSS: False | Self-host: False
Pourquoi ce score: Domaine étroit (pelouse) mais c'est le meilleur exemple vivant de « programme saisonnier généré depuis climat + terrain », le patron exact des règles météo « bonne journée pour tondre » planifiées.
Cas d'usage: Plan de fertilisation saisonnier personnalisé au terrain; Analyse de sol maison (pH, texture, nutriments); Rappels d'application synchronisés à la météo locale; Mesure de la surface de pelouse par satellite
Features à voler:
  - Programme annuel de pelouse dérivé de climat local + surface + analyse de sol — modèle pour des « programmes » HouseOS : gabarits de tâches saisonnières instanciés selon la propriété (pelouse nordique zone 4 : chaux au printemps, sursemis fin août, engrais d'automne)
  - Fenêtres d'application déclenchées par la météo réelle (température du sol, pluie annoncée) et non par des dates fixes — la règle météo qui ajuste l'échéance d'une occurrence
  - Test de sol comme donnée d'entrée structurée (pH, sable/argile/limon) stockée et réutilisée — candidat aux specs JSONB d'une Zone extérieure
  - Courbes de potentiel de croissance du gazon selon température/pluie — une « saison » calculée plutôt que déclarée

## Hilo (Hydro-Québec) — 6/10 [homeenergyutilitymonitoringinclhydroqubecpeakevents]
URL: https://www.hiloenergie.com/en-ca/
Service maison intelligente d'Hydro-Québec : thermostats et appareils connectés + application qui orchestre des « défis » pendant les pointes hivernales et verse des récompenses en argent. Pour les résidents québécois clients HQ.
Prix: app gratuite ; hub et installation offerts avec engagement aux défis ; rabais jusqu'à 20-30 % sur les appareils ; récompenses moyennes ~135 $/hiver | Monétisation: filiale d'Hydro-Québec : valeur de l'effacement de pointe + vente de matériel | OSS: False | Self-host: False
Pourquoi ce score: Domaine directement pertinent (pointes HQ, chauffage, récompenses) mais service fermé, cloud, anti-philosophie HouseOS — pure mine d'idées UX.
Cas d'usage: Réduire automatiquement le chauffage pendant les pointes et toucher ~100-135 $ de récompenses par hiver; Contrôler thermostats, prises et éclairage depuis une app; Choisir un niveau d'intensité de participation (Modéré / Intrépide / Extrême)
Features à voler:
  - Le concept de « défi » : un événement de pointe devient une invitation acceptable/refusable la veille, avec un résultat chiffré après coup — HouseOS pourrait présenter chaque pointe comme un défi à deux (Alain vs Ariane ?) avec bilan kWh/$ le lendemain
  - Modes d'intensité nommés (Modéré/Intrépide/Extrême) au lieu de degrés Celsius : excellente abstraction UX pour régler l'agressivité du pré-chauffage/délestage
  - Pré-chauffage automatique avant l'événement puis abaissement pendant — la stratégie de référence à répliquer en phase IoT avec thermostats Zigbee
  - Bilan saisonnier cumulatif des récompenses en argent : rendre l'économie visible et motivante
  - Notification la veille au soir (l'événement de demain matin) — le bon moment pour notifier, à reprendre dans le flux iCal/ntfy

## Home Assistant — tableau de bord Énergie — 6/10 [homeenergyutilitymonitoringinclhydroqubecpeakevents]
URL: https://www.home-assistant.io/docs/energy/
Module énergie intégré de Home Assistant : agrège réseau, solaire, batterie, gaz et eau, calcule les coûts par tarif et ventile la consommation par appareil. Pour les auto-hébergeurs domotique.
Prix: gratuit, open source (Apache 2.0) ; cloud Nabu Casa optionnel ~6,50 $US/mois | Monétisation: abonnement cloud optionnel (Nabu Casa) finançant la fondation | OSS: True | Self-host: True
Pourquoi ce score: Pas un outil de gestion de maison au sens HouseOS, mais LE modèle de référence du module énergie que HouseOS voudra en phase 3, dans la même philosophie locale-d'abord.
Cas d'usage: Vue unifiée conso réseau / production solaire / batterie / eau / gaz; Coût réel par jour/semaine/mois via tarifs statiques ou dynamiques; Ventilation par appareil individuel (prises mesurantes, capteurs kWh); Long-terme : statistiques horaires conservées indéfiniment
Features à voler:
  - Modèle de données en « flux » (réseau→maison, solaire→maison, maison→réseau) plutôt qu'en simples compteurs — schéma propre à copier pour les tables Postgres
  - Statistiques long-terme downsamplées (horaires) séparées des états bruts : pattern de rétention indispensable pour ne pas noyer Postgres avec du MQTT
  - Section « appareils individuels » : n'importe quel capteur kWh (prise Zigbee) devient une ligne du camembert — pont naturel vers le registre Équipements de HouseOS (conso rattachée à un équipement existant)
  - Tarifs configurables par entité ou prix statique → coût en $ partout, pas des kWh abstraits
  - S'appuie sur les prises/capteurs Zigbee2MQTT que HouseOS prévoit déjà — aucune dépendance matérielle nouvelle

## OpenEnergyMonitor / emonCMS — 6/10 [homeenergyutilitymonitoringinclhydroqubecpeakevents]
URL: https://emoncms.org/
Doyen des plateformes énergie open source (depuis 2009) : matériel ouvert (emonPi/emonTx, IoTaWatt compatible) + application web emonCMS de journalisation et visualisation de données énergie/température. Pour bricoleurs et auto-hébergeurs.
Prix: gratuit auto-hébergé (AGPL) ; emoncms.org hébergé à bas coût par crédits ; matériel emonPi/emonTx vendu en boutique | Monétisation: vente de matériel ouvert + hébergement emoncms.org | OSS: True | Self-host: True
Pourquoi ce score: Même ADN (auto-hébergé, ouvert, données locales) et le module énergie le plus mûr du marché, mais mono-domaine : rien du reste d'un OS maison.
Cas d'usage: Monitorer des circuits via transformateurs de courant et journaliser dans une base séries temporelles; Pipeline de traitement des entrées (facteurs d'échelle, cumul→delta kWh, logique custom) avant stockage; Tableaux de bord drag-and-drop avec jauges temps réel et historiques; Recevoir les données d'un IoTaWatt (14 circuits, tampon local sur SD)
Features à voler:
  - Pipeline d'input configurable AVANT stockage (échelle, cumul→delta, corrections) — pattern idéal pour le BackgroundService d'ingestion HouseOS : normaliser à l'entrée, stocker propre
  - Moteurs de feed à intervalle fixe très compacts (PHPFina) : des années de données par circuit en quelques Mo — leçon de frugalité pour les séries temporelles dans Postgres
  - Découplage matériel/logiciel : n'importe quelle source pouvant POSTer du JSON alimente emonCMS — HouseOS devrait offrir le même endpoint générique d'ingestion
  - Tampon local + rattrapage en masse quand le réseau revient (IoTaWatt→emonCMS) : robustesse à viser pour les capteurs derrière Tailscale
  - Apps dédiées « Ma consommation électrique » / « Mon solaire » : vues métier prêtes à l'emploi au-dessus des feeds bruts, plutôt qu'un builder générique seul

## HomeRoutines — 5/10 [chores-recurrence]
URL: https://www.homeroutines.com/
App iOS classique inspirée de la méthode FlyLady : routines à cocher (matin/soir/hebdo) et « zone de la semaine » qui fait tourner l'attention sur une partie de la maison. Pour adeptes de routines plutôt que de listes.
Prix: ≈ 4,99$US (achat unique iOS ; app ancienne, prix historique) | Monétisation: achat unique | OSS: False | Self-host: False
Pourquoi ce score: Domaine corvées/zones mais modèle routines-papier, mono-utilisateur, app vieillissante.
Cas d'usage: Routines quotidiennes à cocher qui se réinitialisent; Rotation hebdomadaire du focus de ménage par zone; Minuteur de sessions de 15 minutes
Features à voler:
  - « Focus zone » tournante : chaque semaine du mois, une zone de la maison devient LA zone à fond (semaine 1 : cuisine…) — se marie naturellement avec les Zones + fenêtres saisonnières de HouseOS
  - Routines-checklists qui se réinitialisent automatiquement (matin/soir/hebdo) avec étoiles cochées — un autre objet que la tâche à échéance : la routine sans dette, jamais « en retard »
  - Minuteur intégré de 15 minutes pour attaquer une zone sans engagement — micro-mécanique anti-procrastination

## Sortly — 5/10 [home-inventory + projects-renovation]
URL: https://www.sortly.com/
Visual inventory SaaS for small businesses (originally consumer): folder-based organization with photos, QR/barcode labels and stock alerts.
Prix: gratuit (très limité) / Advanced 49$US mois / Ultra 149$US mois ; étiquettes QR ~250$US/1000 | Monétisation: subscription + label supplies | OSS: False | Self-host: False
Pourquoi ce score: Now business-oriented and overkill for a couple's house, but its label and folder mechanics remain reference material.
Cas d'usage: business stock tracking; printable QR labels for bins; visual folder-based organization; low-stock alerts
Features à voler:
  - Photo-first folder hierarchy: every folder and item is represented by a large photo grid, so navigation is visual recognition, not reading
  - Label designer embedding item name, price, tags, custom fields, logo and photo, in sizes from 1"x1" micro to 8.5"x5.5" sheet
  - Min-quantity threshold per item triggering low-stock alerts — reusable for HouseOS consumables
  - Check-in/check-out with per-user activity history on each item

## Thumbtack (Home Care) — 5/10 [home-maintenance]
URL: https://www.thumbtack.com/
Pro-services marketplace whose app now includes a personalized 'home care plan' with seasonal upkeep guides; for homeowners who mostly hire out the work.
Prix: gratuit pour le proprio (les pros paient les leads) | Monétisation: lead-generation fees charged to service pros | OSS: False | Self-host: False
Pourquoi ce score: Marketplace at heart, but its data-driven seasonal guide mechanics overlap the checklist/weather-rules part of HouseOS.
Cas d'usage: seasonal upkeep checklists; hiring contractors; cost benchmarking for jobs
Features à voler:
  - Seasonal upkeep guides generated per home from its systems + location, ranked using outcome data from 80M past projects (e.g. 'Aerate Lawn' only if you have a lawn)
  - Home Care Price Index: published cost benchmarks per maintenance job — HouseOS journal could compare actual cost vs regional norm
  - Goal-based planning: pick goals (save energy, prep for winter) and the plan reprioritizes tasks accordingly

## MaintainX — 5/10 [home-maintenance]
URL: https://www.getmaintainx.com/
Industrial CMMS (work orders, preventive maintenance, asset management) that hobbyists sometimes bend to home use; its free tier makes that feasible.
Prix: gratuit (Basic, limité à 2 tâches récurrentes) / 20$US util./mois (Essential) / 65$US util./mois (Premium) | Monétisation: per-seat SaaS subscription, freemium entry | OSS: False | Self-host: False
Pourquoi ce score: Adjacent (industrial CMMS, per-seat pricing absurd for a couple) but its mechanics are the mature versions of HouseOS's planned features.
Cas d'usage: preventive maintenance scheduling; work orders with procedures; meter-based maintenance; parts inventory; time and cost tracking
Features à voler:
  - Meter-based maintenance: a preventive task triggers when a meter reading crosses a threshold, not on a calendar — exactly the model for HouseOS phase-3 sensor-triggered due dates
  - Procedures: reusable step-by-step checklists attached to a recurring task, each step checkable at completion time
  - Time + cost captured on every work order, rolling up per asset to a lifetime cost-of-ownership figure
  - Parts inventory decremented by work-order completion — the link HouseOS wants between consumables and tasks
  - Free 'requester' role: someone can report a problem without being a full user — model for a guest/incident-report entry point

## AnyList — 5/10 [pantry-grocery]
URL: https://www.anylist.com/
Polished commercial shared grocery list + recipe box for families; the mainstream benchmark for list UX.
Prix: gratuit / AnyList Complete : 9,99 $US an (individuel) ou 14,99 $US an (foyer) | Monétisation: freemium subscription | OSS: False | Self-host: False
Pourquoi ce score: Adjacent — pure list/recipe UX with no pantry stock or maintenance domain, but its interaction polish is the bar to beat.
Cas d'usage: instantly-synced shared lists; recipe web-clipping; meal-planning calendar feeding the list; voice add via Siri/Alexa
Features à voler:
  - Auto-categorization of typed items by store section with per-store custom category orders
  - Type-ahead suggestions drawn from your own purchase history first
  - Meal-plan calendar where tapping a planned recipe dumps its ingredients into the list
  - Household plan priced as a couple/family unit — the 'two accounts, one subscription' shape HouseOS embodies
  - Photos attachable to list items (premium) — 'buy exactly this one' disambiguation

## Out of Milk — 5/10 [pantry-grocery]
URL: https://outofmilk.com/
Veteran free grocery-list app whose distinctive trait is a separate Pantry list with auto-restock reminders.
Prix: gratuit / Pro 1,99 $US mois ou 9,99 $US an (sans pub + sync) / Premium 4,99 $US an (pantry + rappels de réapprovisionnement) | Monétisation: freemium + ads in free tier | OSS: False | Self-host: False
Pourquoi ce score: Adjacent list app, but its pantry-to-shopping-list loop and restock reminders are exactly the consumables mechanic HouseOS plans.
Cas d'usage: grocery list with barcode add; pantry list of what's at home; to-do list; auto-restock reminders (premium)
Features à voler:
  - Explicit Pantry list separate from the shopping list, with one-tap move between them ('ran out' → shopping list)
  - Auto-restock reminders: the app nudges you when a pantry staple is predicted to run low
  - Barcode scan to add items to either list
  - Monetization insight: users pay a few dollars/year specifically for pantry tracking — evidence the consumables module is the valued part

## BEEP — 5/10 [pantry-grocery]
URL: https://www.beepscan.com/
Minimal free expiry-date tracker: scan a barcode, enter the date, get reminded; used by homes, pharmacies, and small shops.
Prix: gratuit (iOS/Android) | Monétisation: free (team/business features as upsell) | OSS: False | Self-host: False
Pourquoi ce score: Single-mechanic overlap (expiry tracking) done with radical simplicity worth studying.
Cas d'usage: expiry-date reminders; barcode-prefilled item entry; location/category grouping; shared team inventory
Features à voler:
  - Two-field capture flow: barcode fills name+image, user only types the expiry date — minimal-friction entry HouseOS should imitate for consumables
  - Reminder lead time chosen per item (1 day / 1 week / 1 month before expiry)
  - Items grouped by physical location (fridge, freezer, basement) — maps directly onto HouseOS Zones
  - Team sharing of one inventory — the couple use case with zero account ceremony

## My Pantry Tracker — 5/10 [pantry-grocery]
URL: https://mypantrytracker.com/
Free mobile + web pantry inventory tracker with barcode scanning; optional paid cloud sync is the only charge.
Prix: gratuit ; option cloud/sync payante (petit abonnement 6 mois ou annuel) | Monétisation: free app + optional cloud-sync subscription | OSS: False | Self-host: False
Pourquoi ce score: Straightforward pantry-inventory overlap; interesting mainly as a sync-as-the-only-paywall model.
Cas d'usage: home food inventory; expiry tracking; barcode entry; web + mobile access to the same pantry
Features à voler:
  - Local-first and fully usable free; only multi-device cloud sync costs money — a clean separation HouseOS gets for free by self-hosting
  - Parallel web app and mobile app over one inventory (desktop for bulk edits, phone for scanning) — matches HouseOS's desktop-first + phone-for-capture split

## Ohai.ai — 5/10 [family-organizers]
URL: https://www.ohai.ai/
Assistante IA « O » pilotée par texto pour la charge mentale du foyer : gestion d'agenda, extraction d'événements depuis courriels/PDF/photos, rappels et coordination.
Prix: essai 14 jours / 9.99$US mois (1 pers.), 19.99$US (2), 29.99$US (groupe) | Monétisation: abonnement par sièges | OSS: False | Self-host: False
Pourquoi ce score: Adjacent-IA : pas de gestion de maison en soi, mais son interface conversationnelle-d'abord est exactement ce que le serveur MCP de HouseOS rend possible.
Cas d'usage: assistant par SMS sans app; scan de courriels et documents vers le calendrier; digest quotidien du foyer; commande d'épicerie automatisée (Instacart)
Features à voler:
  - Interface SMS-first : on texte l'assistante comme une vraie personne, zéro app à ouvrir — équivalent HouseOS : parler au MCP via Claude sans toucher l'UI
  - Ingestion multi-format (courriel transféré, PDF, photo d'un dépliant) → événements et tâches extraits
  - « Daily Summary » : digest proactif envoyé chaque matin plutôt qu'une app à consulter
  - Tarification par siège du foyer (1/2/groupe) — modèle intéressant à observer, pas à copier

## Pistachio — 5/10 [family-organizers]
URL: https://heypistachio.com/
Anti-Cozi minimaliste iOS : listes partagées, corvées avec rotation et notes du foyer, délibérément SANS calendrier — pour les foyers qui ont déjà Google/Apple Calendar.
Prix: gratuit, sans paywall (iOS seulement, Android à venir) | Monétisation: aucune visible (gratuit) | OSS: False | Self-host: False
Pourquoi ce score: Petit mais philosophiquement intéressant : il assume de compléter les calendriers existants au lieu de les remplacer, comme HouseOS avec son flux iCal sortant.
Cas d'usage: listes d'épicerie temps réel; rotation de corvées récurrentes; notes partagées du foyer; capture en langage naturel
Features à voler:
  - Positionnement « pas un autre calendrier » : s'appuyer sur les calendriers existants du couple et ne construire que ce qu'ils ne couvrent pas — valide la stratégie flux iCal de HouseOS plutôt qu'un module calendrier complet
  - Capture IA en langage naturel (« soccer mardi 17h, apporter les crampons ») qui range l'info au bon endroit (liste, tâche, note)
  - Rotation de corvées récurrentes comme mécanique de base même dans une app minimaliste — confirme que l'alternance est une attente du marché

## Wallos — 5/10 [selfhosted-household]
URL: https://github.com/ellite/Wallos
Traqueur personnel d'abonnements et paiements récurrents auto-hébergé (PHP + SQLite, un conteneur) : visualiser les dépenses récurrentes du foyer et ne rien rater.
Prix: gratuit, open source | Monétisation: none (dons) | OSS: True | Self-host: True
Pourquoi ce score: Adjacent : l'échéance récurrente d'un paiement est cousine de l'occurrence de tâche, et « renouvellements du foyer » est un module plausible de HouseOS.
Cas d'usage: inventaire des abonnements du foyer; alertes avant renouvellement; statistiques de dépenses récurrentes par catégorie; conversion multi-devises
Features à voler:
  - Notification X jours avant chaque renouvellement sur le canal du choix (courriel, Telegram, gotify, webhook…) — le pattern « compte à rebours notifié » que HouseOS a déjà, appliqué aux contrats/abonnements
  - Récupération automatique du logo du service sur le web à la création — micro-délice UX qui rend la liste lisible d'un coup d'œil
  - Toutes devises ramenées à la devise principale via l'API Fixer pour totaliser le coût mensuel réel du foyer

## Warranty Tracker — 5/10 [documents-warranties]
URL: https://www.warrantytracker.app/
App mobile mono-fonction : scanner le reçu, l'OCR extrait dates et durée de garantie, l'app rappelle avant expiration. Pour consommateurs qui perdent leurs reçus.
Prix: freemium (base gratuite, illimité payant ; prix exacts non publiés sur la page) | Monétisation: freemium | OSS: False | Self-host: False
Pourquoi ce score: Ne couvre qu'une tranche fine (reçu→garantie→rappel) du module Équipements, mais l'exécution mono-geste du flux est instructive.
Cas d'usage: Scan de reçu avec extraction OCR de la date d'achat; Rappels avant fin de garantie; Classement des achats par catégories
Features à voler:
  - Flux en un geste : photo du reçu → OCR pré-remplit date d'achat et durée → rappel armé automatiquement ; réduire à ça la saisie d'une garantie dans HouseOS
  - Échéancier de rappels échelonnés (90/30/7 jours avant expiration)
  - Compte à rebours « jours restants » affiché par item — s'accorde avec les comptes à rebours existants de HouseOS

## Invisible Calendar (Invisible Computers) — 5/10 [dashboards-displays]
URL: https://www.invisible-computers.com/
Calendrier e-paper 7,5″ dans un cadre de bois fini main, qui synchronise Google Calendar et tout flux ICS. Produit grand public : zéro configuration, zéro lumière, une seule fonction bien faite.
Prix: 149 $US, sans abonnement | Monétisation: vente unique de matériel | OSS: False | Self-host: False
Pourquoi ce score: Mono-fonction mais démontre le format cible : l'ICS que HouseOS émet déjà suffirait à alimenter un tel écran.
Cas d'usage: agenda du couple affiché dans la cuisine; calendrier e-paper de bureau
Features à voler:
  - S'abonne à n'importe quel lien ICS — le flux iCal existant de HouseOS pourrait alimenter cet appareil tel quel, preuve que l'ICS est la bonne interface d'export
  - Layout agenda « aujourd'hui + prochains jours » optimisé pour la lisibilité e-paper (hiérarchie typographique, pas de grille mensuelle illisible)
  - Cadre en bois : l'affichage assumé comme objet déco, pas comme gadget — pertinent pour l'acceptation conjugale d'un écran mural
  - Configuration entière via app mobile compagnon, l'appareil n'a aucune UI

## Fully Kiosk Browser — 5/10 [dashboards-displays]
URL: https://www.fully-kiosk.com/
Navigateur Android verrouillé pour tablettes murales : mode kiosque, réveil par détection de mouvement (caméra), administration à distance. Standard de facto pour afficher un dashboard web sur tablette au mur.
Prix: gratuit avec filigrane / licence Plus 7,90 $US par appareil (achat unique) | Monétisation: licence unique par appareil | OSS: False | Self-host: False
Pourquoi ce score: Déjà dans la feuille de route HouseOS (tablette murale Fully Kiosk) ; c'est la couche d'affichage, pas le domaine.
Cas d'usage: tablette murale affichant un dashboard web; écran cuisine qui se réveille à l'approche; borne verrouillée sur une seule app
Features à voler:
  - Réveil par détection de mouvement via la caméra frontale : l'écran s'allume quand quelqu'un s'approche, s'éteint sinon — l'UI web murale de HouseOS peut supposer ce comportement
  - API REST/MQTT d'administration : luminosité, capture d'écran, TTS, rechargement de page pilotables depuis le serveur — HouseOS pourrait faire parler la tablette (« sortir le bac bleu ce soir »)
  - Économiseur d'écran programmable (photos, dashboard atténué, noir) selon plage horaire
  - Injection JavaScript et masquage d'éléments pour adapter un site existant au mode kiosque sans le modifier
  - Intégration Home Assistant qui expose la tablette comme entité (capteurs batterie, luminosité, mouvement)

## Mango Display — 5/10 [dashboards-displays]
URL: https://mangodisplay.com/
App qui transforme un téléviseur (Samsung, LG, Android/Fire TV) ou une tablette existante en affichage familial : calendriers, corvées, photos, menus, widgets — sans acheter de matériel dédié.
Prix: gratuit (2 écrans, widgets de base) / Pro 5,99 $US/mois / Business 19,99 $US/mois | Monétisation: freemium par abonnement | OSS: False | Self-host: False
Pourquoi ce score: Même contenu mural (calendrier, corvées, repas) mais SaaS ; son intérêt est le déploiement sans matériel dédié.
Cas d'usage: réutiliser la TV du salon comme tableau familial; affichage cuisine sur vieille tablette; signalisation légère
Features à voler:
  - Cible les TV existantes via leurs app stores (Samsung/LG/Fire TV) : zéro matériel neuf — une « vue TV » en lecture seule de HouseOS coûterait peu et couvrirait le salon
  - Mix-and-match de widgets (calendrier, météo, menus, notes, santé) sur fonds photo
  - Deux écrans gratuits pour accrocher l'utilisateur avant l'abonnement
  - Mode signage avec rotation d'écrans pour le même moteur — un seul produit, deux marchés

## BoxBuddy — 5/10 [projects-renovation]
URL: https://boxbuddy.tech/
Obscure but sharply-scoped family moving app: QR-labeled boxes with voice-dictated contents and photos, built for the pack-move-unpack cycle.
Prix: 10 premières boîtes gratuites / 19,99$US an (individuel) / 29,99$US an (famille) | Monétisation: cheap annual subscription | OSS: False | Self-host: False
Pourquoi ce score: Narrow single-purpose tool, but its capture ergonomics for the box→contents→location problem are directly relevant to HouseOS's move and future NFC/QR plans.
Cas d'usage: tracking 60+ moving boxes; finding which box an item is in after the move; collaborative packing between spouses
Features à voler:
  - Voice-first capture: dictate box contents hands-free while packing — capture friction is the whole battle for inventory data
  - Photo of contents taken just before sealing the box, attached to the QR record — cheap 'proof of contents'
  - Keyword search across all box records answers 'which box is the corkscrew in' during unpacking
  - Offline-first scanning for basements/storage without signal (HouseOS is LAN/Tailscale — same concern for a garage tablet)
  - Family sharing: two people pack in parallel into one shared box registry

## Real Estate Ledger — 5/10 [projects-renovation]
URL: https://realestateledger.io/
Property document vault that AI-categorizes uploads by property/system/document-type and blockchain-fingerprints each file, producing a shareable 'Property Guidebook' for buyers and lenders.
Prix: gratuit (jusqu'à 10 propriétés, 5 Go) / palier Entreprise sur devis | Monétisation: freemium; enterprise tier for portfolios | OSS: False | Self-host: False
Pourquoi ce score: Documents-module overlap only, but its classification and export mechanics fit HouseOS's new documents facets work.
Cas d'usage: organizing home documents automatically; tamper-evident proof of maintenance/improvement records; handing a verified home dossier to a buyer or lender
Features à voler:
  - AI auto-files every uploaded document along three facets — property, system, and document type — exactly the faceted-navigation model HouseOS just adopted, but with classification automated at upload
  - Property Guidebook: one-click generated, shareable report assembling the home's documents and history into a buyer/lender-ready dossier — an 'export my house' artifact
  - Cryptographic fingerprint per file for tamper-evidence, selling record authenticity as the product

## AUTOsist — 5/10 [vehiclesmallenginemaintenancetrackerscarsashouseholdassets]
URL: https://autosist.com/
Cloud fleet-maintenance SaaS for businesses: preventive maintenance, digital inspection checklists, work orders, fuel, parts inventory and document management for vehicles, trailers and equipment.
Prix: à partir de ~59$US mois (jusqu'à 5 véhicules) ; formules ~7$US/véhicule/mois maintenance seule, jusqu'à 55$US/véhicule/mois avec GPS+caméras | Monétisation: B2B SaaS subscription per vehicle/asset | OSS: False | Self-host: False
Pourquoi ce score: Adjacent B2B fleet tooling; overkill commercially but its inspection/work-order model covers 'equipment' broadly, not just cars.
Cas d'usage: Preventive maintenance schedules across a fleet; Digital inspection checklists with custom items; Work orders and service history with receipts/documents; Parts inventory tracking; Push/email reminders for service and fuel
Features à voler:
  - Explicitly tracks non-vehicle equipment and trailers alongside vehicles in the same maintenance system — validates HouseOS treating snowblower/generator/chainsaw identically to cars under one equipment registry
  - Custom inspection checklists as a record type distinct from services — a HouseOS 'inspection saisonnière' (pré-hiver de la souffleuse, pré-été du tracteur) could be a checklist task template attached to equipment with pass/fail items in the journal
  - Work-order concept linking a detected problem to its scheduled fix — a lightweight 'problème signalé → tâche créée' flow between equipment and tasks
  - Parts inventory tied to maintenance (filters, belts, spark plugs) — natural bridge to the planned consommables module: a service consumes stock

## CARFAX Car Care — 5/10 [vehiclesmallenginemaintenancetrackerscarsashouseholdassets]
URL: https://www.carfax.com/Service/
Free consumer app from CARFAX that, given a VIN or plate, auto-pulls the vehicle's service history, sends open-recall alerts and manufacturer-schedule service reminders.
Prix: gratuit (produit d'appel pour les rapports CARFAX payants) | Monétisation: free app funding lead-gen/data for CARFAX's paid vehicle-history reports and shop network | OSS: False | Self-host: False
Pourquoi ce score: Adjacent data-service rather than a tracker, but it is the reference for the recall/VIN-lookup pattern HouseOS could copy via public APIs.
Cas d'usage: Open recall alerts for your VIN; Auto-populated service history from shop networks; Service reminders based on the vehicle's actual history; History-based vehicle valuation
Features à voler:
  - VIN-keyed enrichment: enter VIN once, get make/model/specs and history — HouseOS could enrich vehicle equipment from the free NHTSA vPIC VIN-decode API instead of manual data entry
  - Open-recall monitoring as an ongoing background check, not a one-time lookup — a HouseOS BackgroundService polling NHTSA/Transport Canada recall feeds per VIN, surfacing a task when a recall appears (fits the existing ingestion-service architecture)
  - Reminders derived from manufacturer maintenance schedules rather than user-defined intervals — a curated interval library per equipment model is a differentiator no self-hosted tool has

## GARDENA smart system — 5/10 [gardenplantcareseasonaloutdoorplanningapps]
URL: https://www.gardena.com/int/c/discover/products/smart-system/smart-app
App compagnon de l'écosystème d'irrigation et de tonte connectées Gardena : programmation d'arrosage ajustée par prévisions météo et capteur d'humidité du sol. Pour propriétaires équipés de leur matériel.
Prix: app gratuite ; matériel requis (smart Water Control ~100-150$US, smart Sensor, passerelle) | Monétisation: hardware | OSS: False | Self-host: False
Pourquoi ce score: Écosystème matériel fermé, mais ses règles météo→action (pluie annoncée = skip, gel = alerte, capteur sol = déclencheur) sont le cahier des charges exact de la phase 3 IoT et des règles Open-Meteo de HouseOS.
Cas d'usage: Programmer l'irrigation par zone de jardin; Sauter l'arrosage quand il a plu ou que le sol est humide; Alertes de gel avant les nuits froides; Piloter robot-tondeuse et vannes à distance
Features à voler:
  - Skip-quand-il-a-plu : la prévision ou la pluie récente suspend l'occurrence d'arrosage — règle Open-Meteo directement implémentable sur les tâches d'arrosage HouseOS, sans matériel
  - Capteur d'humidité du sol comme condition d'échéance (n'arroser que si sol sec) — prototype du « échéance déclenchée par capteur » v3
  - Alertes de gel automatiques avant les nuits froides — tâche « rentrer les plantes / fermer la valve extérieure » auto-déclenchée, critique en zone 4
  - Horaires recalés sur lever/coucher du soleil au fil de l'année — un type d'ancre temporelle astronomique pour les récurrences

## Open Garden Planner — 5/10 [gardenplantcareseasonaloutdoorplanningapps]
URL: https://github.com/cofade/open-garden-planner
Outil open source de plan de jardin façon CAD : dessin du terrain à précision métrique, calibration sur image satellite, métadonnées riches par plante. Pour jardiniers qui veulent un plan spatial exact de leurs plates-bandes.
Prix: gratuit, open source | Monétisation: none | OSS: True | Self-host: True
Pourquoi ce score: Purement spatial (dessin de plan), sans moteur de tâches, mais c'est la dimension carte-du-terrain qui manque aux Zones de HouseOS pour l'extérieur d'une propriété rurale.
Cas d'usage: Dessiner le plan du terrain et des plates-bandes à l'échelle; Calibrer le plan sur une image satellite de la propriété; Positionner chaque plante avec ses métadonnées; Exporter vers des formats standards
Features à voler:
  - Plan du terrain calibré sur imagerie satellite à précision métrique — une Zone extérieure HouseOS pourrait porter un croquis/plan cliquable de la propriété
  - Intégration de bases botaniques ouvertes (Trefle.io, Perenual) pour enrichir les fiches — sources de données gratuites à connaître pour un futur module plantes
  - Positions de plantes comme objets riches en métadonnées sur le plan — pont entre carte spatiale et registre d'entités

## Emporia Vue 3 — 5/10 [homeenergyutilitymonitoringinclhydroqubecpeakevents]
URL: https://shop.emporiaenergy.com/products/emporia-vue-3
Moniteur d'énergie à installer dans le panneau électrique : 2 pinces 200 A + 8 ou 16 pinces de circuit, app avec conso temps réel par circuit et coûts. Le meilleur rapport prix/circuit du marché grand public.
Prix: ~150 $US (2 pinces principales) + capteurs 8/16 circuits en option ; app gratuite, sans abonnement | Monétisation: vente de matériel (moniteurs, prises, chargeur VE, batteries), sans abonnement | OSS: False | Self-host: False
Pourquoi ce score: Matériel+cloud fermé, mais c'est LE capteur par-circuit abordable que la phase IoT de HouseOS finirait par brancher (surtout reflashé ESPHome), et son app définit le standard des vues par circuit.
Cas d'usage: Conso temps réel par circuit (chauffe-eau, thermopompe, sécheuse…); Projection de coût sur la période de facturation avec tarifs saisis; Gestion de charge (délester des circuits via relais/prises Emporia); Flash ESPHome communautaire pour un fonctionnement 100 % local vers Home Assistant/MQTT
Features à voler:
  - Granularité par circuit à ~10 $ le circuit : rend possible la règle « ce circuit = cet équipement » — mapper chaque pince sur un Équipement HouseOS (chauffe-eau, échangeur d'air…)
  - La voie ESPHome communautaire (reflash du Vue) colle exactement à la doctrine HouseOS « firmwares maison en ESPHome » : données par circuit en local, zéro cloud
  - Vue « toujours allumé » (charge fantôme) calculée automatiquement — bon indicateur santé-maison
  - Projection de la facture en cours à partir du tarif et du rythme de conso — à croiser avec la période de facturation HQ
  - Alertes de seuil par circuit (« la pompe de puisard n'a pas tourné depuis X ») : c'est littéralement le pont conso→tâche/alerte que HouseOS veut construire

## Sense — 5/10 [homeenergyutilitymonitoringinclhydroqubecpeakevents]
URL: https://sense.com/
Moniteur d'énergie (2 pinces au panneau) qui échantillonne à 1 MHz et identifie les appareils individuels par apprentissage automatique de leurs signatures électriques. Pour propriétaires curieux de « qui consomme quoi » sans pinces par circuit.
Prix: ~299 $US matériel ; app et détection incluses ; abonnement Sense+ optionnel (historique étendu) | Monétisation: vente de matériel + partenariats compteurs/services publics + abonnement optionnel | OSS: False | Self-host: False
Pourquoi ce score: Cloud fermé et ML propriétaire, mais son idée maîtresse — la consommation comme signal de comportement des appareils — est la plus transposable aux règles HouseOS.
Cas d'usage: Détection automatique d'appareils (frigo, CVC, chauffe-eau) sans câblage par circuit; Alertes appareil allumé/éteint (« la porte de garage s'est ouverte », « la pompe tourne encore »); Suivi temps réel du total maison et historique par appareil détecté
Features à voler:
  - Traiter la conso comme un capteur d'ÉVÉNEMENTS, pas de kWh : « la sécheuse a fini » peut déclencher une tâche HouseOS (vider le filtre), « le déshumidificateur tourne 2× plus longtemps » peut créer une occurrence « vérifier le filtre »
  - Notifications d'anomalie par appareil (durée de marche inhabituelle, appareil resté allumé) — le modèle exact du pont conso→corvée du mandat
  - Compteur de cycles par appareil : brancher l'entretien à l'usage réel (« nettoyer la thermopompe après N heures de marche ») plutôt qu'au calendrier — complément naturel du mode intervalle-depuis-complétion
  - Détection « toujours allumé » et comparaison à des maisons semblables pour contextualiser
  - Timeline des événements électriques de la journée : narration lisible de l'activité de la maison, jolie idée pour l'écran e-ink

## evcc — 5/10 [homeenergyutilitymonitoringinclhydroqubecpeakevents]
URL: https://evcc.io/en/
Gestionnaire d'énergie domestique open source (Go) auto-hébergé, centré recharge VE : pilote 620+ bornes selon le surplus solaire ou les tarifs dynamiques, intègre onduleurs, batteries, thermopompes et véhicules, 100 % local. Pour foyers avec VE/solaire.
Prix: gratuit, open source (MIT) ; jeton sponsor pour bornes commerciales : 4 $US/mois, 50 €/an ou 150 € à vie | Monétisation: jetons sponsor volontaires/requis selon la borne + dons | OSS: True | Self-host: True
Pourquoi ce score: Philosophie jumelle (local, sans cloud, Docker, MQTT) et gestion d'énergie de pointe, mais domaine étroit (VE/solaire) éloigné du cœur corvées/équipements.
Cas d'usage: Recharger le VE uniquement sur le surplus solaire; Planifier la recharge sur les heures à tarif bas (tarifs dynamiques); Superviser onduleur, batterie maison, thermopompe et compteur dans une seule UI; Publier tout l'état sur MQTT/API pour la domotique
Features à voler:
  - Ordonnancement par prix : « charge N kWh d'ici 7 h au moindre coût » — l'algorithme à transposer aux pointes HQ (décaler sécheuse/lave-vaisselle/chauffe-eau hors fenêtre de pointe)
  - Modèle de financement malin pour un projet-hobby MIT : cœur libre, jeton sponsor pour les intégrations de matériel commercial
  - Abstraction propre des « sites/loadpoints/tariffs » configurée en YAML : bonne référence de modélisation domaine pour un module énergie C#
  - UI temps réel des flux d'énergie (solaire→batterie→maison→réseau) sobre et lisible, pensée tablette murale
  - Plans de charge avec objectif et échéance (« 80 % pour demain 7 h ») : le pattern générique « atteindre X avant T au meilleur coût »

## Vikunja — 4/10 [chores-recurrence]
URL: https://vikunja.io/
Gestionnaire de tâches open source auto-hébergé (listes, kanban, Gantt), souvent cité comme alternative à Donetick/Todoist pour un foyer. Généraliste, pas spécifique maison.
Prix: gratuit, open source (AGPLv3) ; instance hébergée payante en abonnement | Monétisation: open core : auto-hébergement gratuit, cloud payant | OSS: True | Self-host: True
Pourquoi ce score: Todo générique auto-hébergé : recoupe la mécanique tâches mais rien du domaine maison (zones, équipements, saisons).
Cas d'usage: Tâches récurrentes partagées; Projets domestiques en kanban; Synchronisation CalDAV vers les téléphones
Features à voler:
  - CalDAV bidirectionnel (pas seulement un flux iCal en lecture) — piste d'évolution du flux HouseOS si l'édition depuis le téléphone devient désirée
  - Relations entre tâches (bloque/dépend de) — utile pour le futur module Projets (le déménagement est plein de dépendances)
  - Vues multiples sur les mêmes tâches (liste, kanban, Gantt) — le module Projets de HouseOS pourrait n'être qu'une vue kanban sur les tâches existantes
  - Filtres sauvegardés partageables comme pseudo-listes

## Encircle — 4/10 [home-inventory]
URL: https://www.getencircle.com/
Professional field-documentation platform for insurance restoration contractors and adjusters (contents inventory, packout, claims); its free consumer home-inventory product was discontinued December 2025.
Prix: 270$US mois (Small Shop) / 455$US mois (Medium) / 650$US mois (Large) ; produit consommateur gratuit abandonné (déc. 2025) | Monétisation: subscription (B2B) | OSS: False | Self-host: False
Pourquoi ce score: Now purely a B2B claims tool, but its evidence-grade capture mechanics are the gold standard for the insurance-documentation angle.
Cas d'usage: insurance claim contents inventory; room-by-room photo documentation; schedule-of-loss reports; packout tracking for restoration jobs
Features à voler:
  - Every photo stamped with date, time and GPS metadata kept as tamper-evident chain-of-custody — an 'evidence mode' HouseOS could apply to its completion-journal photos
  - Photo-first room-by-room sweep where cataloging details are back-filled later from the photos, decoupling capture from data entry
  - One-click generated deliverables: schedule of loss, photo report, inventory list — templated reports from the same underlying records
  - Its own exit from free consumer inventory signals the consumer market doesn't sustain SaaS here — an argument for self-hosted

## Bring! — 4/10 [pantry-grocery]
URL: https://www.getbring.com/en/home
Free visual shared shopping list (Swiss) built on tappable item tiles with icons; monetized by retailer offers and digital brochures.
Prix: gratuit (toutes les fonctions de base) ; monétisé par les détaillants | Monétisation: ads/retail partnerships (sponsored offers and brochures) | OSS: False | Self-host: False
Pourquoi ce score: List-only and ad-driven, but its zero-typing tile interaction model is a strong UI idea for a wall tablet.
Cas d'usage: visual tile-based shared list; one-tap re-add of frequent items; retailer offers/flyers added to the list; recipe inspiration to list
Features à voler:
  - Tile grid of your recurring items with icons — restocking is tapping pictures, not typing; ideal pattern for HouseOS's wall-tablet/e-ink phase
  - Smart search autocompletes and personalizes suggestions from household history
  - Custom category order matched to your supermarket's aisles
  - Per-item photo/description/quantity annotations on a shared list
  - Deep links from recipes and retailer flyers straight into the list — an 'external source adds items' ingestion pattern

## TimeTree — 4/10 [family-organizers]
URL: https://timetreeapp.com/intl/en
Calendrier partagé social : plusieurs calendriers par contexte de vie (couple, famille, hobby), très populaire chez les couples pour croiser leurs horaires.
Prix: gratuit avec pubs / Premium 4.49$US mois ou 44.99$US an (sans pub, pièces jointes, épinglage) | Monétisation: freemium + publicité | OSS: False | Self-host: False
Pourquoi ce score: Adjacent : uniquement du calendrier partagé, sans tâches riches ni maison, mais ses mécaniques de contexte par événement sont empruntables.
Cas d'usage: calendrier de couple superposé; calendriers multiples par groupe avec vue fusionnée; coordination d'événements avec discussion intégrée
Features à voler:
  - Fil de discussion et pièces jointes ATTACHÉS à chaque événement — le contexte vit sur l'occurrence, pas dans un chat séparé (idée : notes/fil sur une occurrence HouseOS)
  - « Keep » : liste d'items sans date qu'on promeut en événement quand la date se précise
  - Calendriers multiples avec bascule rapide + vue fusionnée filtrable (équivalent : facettes de vues par personne/zone)
  - Épinglage d'événements prioritaires en tête de vue

## Homarr — 4/10 [dashboards-displays]
URL: https://homarr.dev
Dashboard homelab auto-hébergé avec éditeur glisser-déposer, plus de 50 intégrations live (Proxmox, Pi-hole, *arr, Home Assistant) et widgets temps réel via WebSockets. Pour centraliser ses services auto-hébergés.
Prix: gratuit, open source | Monétisation: aucune (dons) | OSS: True | Self-host: True
Pourquoi ce score: Adjacent : dashboard de services homelab, pas de gestion de la maison ; mais patrons d'UI de widgets réutilisables.
Cas d'usage: page d'accueil du homelab; monitoring des services Docker; calendrier unifié des sorties médias
Features à voler:
  - Auto-découverte Docker : lit les labels des conteneurs et propose automatiquement les tuiles/intégrations correspondantes
  - Widgets poussés en temps réel par WebSockets (tRPC + Redis) plutôt que polling — pertinent pour un mur HouseOS toujours affiché
  - Calendrier unifié qui agrège plusieurs sources en une seule vue mensuelle (patron pour fusionner occurrences + ICS externes + météo)
  - Éditeur en grille glisser-déposer avec redimensionnement des tuiles, entièrement dans le navigateur
  - Recherche globale qui interroge toutes les intégrations connectées depuis une seule barre

## Glance — 4/10 [dashboards-displays]
URL: https://github.com/glanceapp/glance
Dashboard auto-hébergé ultra-léger (binaire Go unique) qui agrège flux RSS, subreddits, météo, marchés, monitoring de sites — entièrement configuré en YAML. Pour qui veut une page d'accueil dense et versionnable.
Prix: gratuit, open source (AGPL-3.0) | Monétisation: aucune (dons) | OSS: True | Self-host: True
Pourquoi ce score: Dashboard de flux généraliste, pas de domaine maison ; mais son widget custom-api est un modèle d'extensibilité à moindre coût.
Cas d'usage: page d'accueil personnelle de flux; monitoring léger de services; revue matinale (météo + nouvelles + agenda)
Features à voler:
  - Widget custom-api : n'importe quel endpoint JSON rendu via un template Go dans le YAML — HouseOS pourrait exposer un endpoint « widget » stable (occurrences du jour, météo-règles) consommable par Glance/TRMNL sans écrire de client
  - Toute la configuration en un fichier YAML versionnable en git, avec rechargement à chaud
  - Colonnes small/full avec passage automatique en onglets sur mobile
  - Cache par widget avec durée configurable pour ménager les APIs sources
  - Dépôt community-widgets : la communauté partage des blocs YAML copiables, extensibilité sans plugin binaire

## Houzz — 4/10 [projects-renovation]
URL: https://www.houzz.com/
Giant renovation inspiration + professional marketplace: 25M photos, ideabooks, pro finder; Houzz Pro is the contractor-side business software.
Prix: gratuit pour les propriétaires ; Houzz Pro (côté entrepreneurs) par abonnement, essai 30 jours | Monétisation: free consumer side funded by pro subscriptions, marketplace commissions and advertising | OSS: False | Self-host: False
Pourquoi ce score: Adjacent — inspiration/marketplace, not home operations — but its cost-data and ideabook mechanics are borrowable for a HouseOS projects module.
Cas d'usage: collecting renovation inspiration into ideabooks; finding local contractors/designers; estimating what a project should cost locally
Features à voler:
  - Real Cost Finder: crowd-sourced actual renovation costs from 100 000+ homeowners, filterable by project type and region — a 'what did this really cost people near me' benchmark
  - Ideabooks: clip photos/products into named collections attached to a future project — a pre-project 'inspiration inbox' state before a project has tasks or budget
  - Photos tagged with the products/materials in them, so inspiration links directly to a shopping list

## Magicplan — 4/10 [projects-renovation]
URL: https://magicplan.app/
Phone/LiDAR floor-plan scanning app that turns a walkthrough into a measured 2D/3D plan, with takeoff and cost-estimating on top; aimed at contractors but usable by homeowners.
Prix: gratuit (2 projets) / abonnements dès ~10$US mois / packs PRO 25-40$US par projet (min. 10 projets/mois) | Monétisation: freemium subscription plus per-project credit packs | OSS: False | Self-host: False
Pourquoi ce score: Adjacent capture tool, but a measured floor plan is a plausible future substrate for HouseOS zones and the wall-tablet UI.
Cas d'usage: scanning rooms into measured floor plans; furniture placement planning before a move; material takeoff and cost estimates from the plan
Features à voler:
  - LiDAR room scan → measured floor plan in minutes; rooms carry real dimensions, so zones become geometric objects instead of labels
  - Cost estimating derived from the plan's measured surfaces (paint by wall area, flooring by floor area) — quantity takeoff from geometry
  - Plan doubles as a placement sandbox: drag furniture/equipment onto the measured plan — useful for a move-in ('where does the couch go') and for pinning equipment to a location on a map of the house

## House Maintenance Tracker (Notion template) — 4/10 [projects-renovation]
URL: https://damalu.gumroad.com/l/HouseMaintenanceTracker
Representative of the widely-shared Gumroad/Notion home-renovation and maintenance templates DIYers actually use: linked databases for projects, tasks, contractors, materials and a maintenance log.
Prix: templates du genre : gratuit à ~90$US, achat unique (celui-ci dans la fourchette basse) | Monétisation: one-time template purchase on Gumroad | OSS: False | Self-host: False
Pourquoi ce score: Not a product but a revealed-preference signal: what people hand-build in Notion shows the minimum viable schema for home projects.
Cas d'usage: DIY home maintenance/renovation tracking in Notion; one workspace linking projects to contractors, materials and reference docs
Features à voler:
  - Maintenance log rows auto-compute the next due date via formula from last-done + interval — the community reinvents HouseOS's interval-since-completion mode in every template, confirming it is the natural model
  - The recurring template schema is always the same five linked tables: Projects ↔ Tasks ↔ Contractors ↔ Materials ↔ Reference docs — Contractors and Materials as first-class linked entities is what HouseOS's project module still lacks
  - Reference/inspiration material stored per project alongside tasks, merging the ideabook and the plan in one place

## PictureThis — 4/10 [gardenplantcareseasonaloutdoorplanningapps]
URL: https://www.picturethisai.com/
App d'identification de plantes par photo (400 000+ espèces, ~98 % de précision) avec diagnostic de maladies et fiches de soins. Pour quiconque veut savoir « c'est quoi cette plante et pourquoi elle jaunit ».
Prix: gratuit limité / premium ~39,99$US an (essai 7 jours) | Monétisation: freemium, abonnement | OSS: False | Self-host: False
Pourquoi ce score: Cœur = identification par IA, loin du domaine tâches/maison de HouseOS ; l'idée à retenir est le flux photo→fiche structurée, pas le produit.
Cas d'usage: Identifier une plante inconnue sur le nouveau terrain; Diagnostiquer une maladie par photo et obtenir un traitement; Vérifier la toxicité (animaux, humains); Attacher une fiche de soins à chaque plante identifiée
Features à voler:
  - Photo → fiche structurée (espèce, soins, toxicité) en une étape — via le serveur MCP, HouseOS pourrait faire pareil avec un LLM multimodal : photo d'une plante ou d'un équipement → entité pré-remplie, zéro saisie
  - Diagnostic de maladie par photo avec plan de traitement — patron « photo d'un problème → tâches correctives » généralisable (toiture, moisissure, équipement)
  - Avertissements de toxicité rattachés d'office à la fiche — métadonnées de sécurité automatiques

## SolarAssistant — 4/10 [homeenergyutilitymonitoringinclhydroqubecpeakevents]
URL: https://solar-assistant.io/
Logiciel commercial à bas prix qui transforme un Raspberry Pi branché sur l'onduleur en console de monitoring solaire locale (multi-onduleurs, BMS batterie), avec broker MQTT intégré. Pour installations solaires hors réseau/hybrides.
Prix: licence unique ~40 $US par installation (Pi non inclus), sans frais récurrents | Monétisation: vente de licence one-time | OSS: False | Self-host: True
Pourquoi ce score: Auto-hébergé et local comme HouseOS, mais fermé et strictement mono-domaine solaire — pertinent surtout comme modèle d'appliance et d'intégration MQTT.
Cas d'usage: Monitoring local temps réel d'onduleurs et batteries sans cloud constructeur; 10 ans d'historique stockés localement sur la carte SD; Pilotage/lecture via MQTT, WebSocket ou REST vers Home Assistant ou du custom
Features à voler:
  - Modèle « appliance » : image SD prête à flasher, zéro administration — inspiration pour distribuer un jour un HouseOS clé en main
  - Broker MQTT embarqué avec topics propres et documentés : le contrat d'intégration que le hub HouseOS phase 3 devrait offrir à ses propres capteurs
  - Rétention locale 10 ans assumée sur SD : preuve qu'un historique énergie long n'exige pas de cloud
  - Licence unique ~40 $ sans abonnement : prix d'ami viable pour du logiciel local — contre-exemple aux abonnements
  - Support direct des protocoles série des onduleurs (RS232/RS485) : lire le matériel à la source plutôt que via le cloud du fabricant

## Monica — 3/10 [selfhosted-household]
URL: https://www.monicahq.com/
CRM personnel open source : se souvenir des détails des gens de sa vie (anniversaires, conversations, cadeaux, rappels). Auto-hébergeable, avec version hébergée payante.
Prix: gratuit et open source en auto-hébergement ; version hébergée par abonnement payant (prix exact non confirmé dans mes sources) | Monétisation: subscription sur l'hébergé, open source sinon | OSS: True | Self-host: True
Pourquoi ce score: Domaine différent (relations, pas la maison), mais le pattern rappels-attachés-à-une-entité recoupe les mécaniques HouseOS.
Cas d'usage: rappels d'anniversaires et d'occasions; journal des interactions avec proches; idées cadeaux par personne
Features à voler:
  - Rappels récurrents attachés à une personne (anniversaire, « prendre des nouvelles tous les 3 mois ») — le mode intervalle-depuis-dernier-contact est l'exact jumeau d'intervalle-depuis-complétion
  - Journal libre daté mêlé aux événements structurés sur la ligne de temps d'une entité — modèle pour fusionner journal d'humeur et journal de complétion en une timeline de la maison

## Billdr PRO — 3/10 [projects-renovation]
URL: https://www.billdr.ai/
Montreal-born construction management software for residential general contractors (quoting, budgeting, scheduling, payments) with a client dashboard for the homeowner.
Prix: Starter 180$US mois / Premium 325$US mois / Titanium 580$US mois (facturation annuelle) | Monétisation: B2B subscription tiers by team size | OSS: False | Self-host: False
Pourquoi ce score: Contractor-side tool, wrong buyer — but its project-money mechanics are the most rigorous model to steal from for a HouseOS renovation project.
Cas d'usage: contractor quoting and invoicing; renovation project scheduling (Gantt); change-order tracking; homeowner visibility into an ongoing reno
Features à voler:
  - Change orders as first-class objects: scope changes create a tracked delta against the original budget rather than silently editing it — great model for reno budget drift
  - Daily logs: dated photo + observation entries per project, forming a chronological build diary (before/after tracking done as a stream, not a pair)
  - Milestone-based payment schedule tied to project phases — maps to 'release budget tranche when phase done'
  - Project-level profitability view: every expense, PO, and invoice rolls up to one live margin number per project

## Moved — 3/10 [projects-renovation]
URL: https://moved.com/
Moving concierge platform that automates the administrative side of a move — address changes, utility transfers, internet setup — largely white-labeled through property managers.
Prix: gratuit pour le consommateur (option premium) ; monétisé côté partenaires | Monétisation: free consumer service; revenue from service-provider referrals and B2B property-manager contracts | OSS: False | Self-host: False
Pourquoi ce score: Concierge marketplace, not software HouseOS would emulate — but the 'administrative side of moving' checklist domain is a reminder that a move project has non-physical tasks.
Cas d'usage: utility transfer coordination; address change automation; move-in service setup
Features à voler:
  - Treats a move as a canonical checklist of administrative events (forward mail, transfer hydro, cancel/re-subscribe internet) each with its own lead time — a curated task-template pack worth replicating for the Québec context (Hydro-Québec, RAMQ, SAAQ)
  - One coordinator view showing the status of every third-party service switch in a single progress board

## Chorsee — 2/10 [chores-recurrence]
URL: https://chorsee.com/
App iOS de corvées et d'allocation pour familles avec enfants : corvées colorées, preuve photo et suivi des récompenses. Public enfants — hors cible HouseOS.
Prix: gratuit (iOS) | Monétisation: gratuit (app indépendante, achats intégrés éventuels) | OSS: False | Self-host: False
Pourquoi ce score: Corvées récurrentes mais entièrement orienté enfants/allocation, un axe explicitement exclu de HouseOS.
Cas d'usage: Corvées d'enfants avec allocation; Preuve photo de complétion; Horaires alternés (une semaine sur deux)
Features à voler:
  - Preuve photo exigible par corvée, jointe à la complétion — transposable au journal HouseOS pour les gros entretiens (photo du filtre changé, du drain nettoyé) comme trace d'équipement
  - Horaires alternés (« alternating schedules » : une semaine sur deux) comme mode de récurrence de première classe

