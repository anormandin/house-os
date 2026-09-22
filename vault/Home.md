---
type: index
---

# House OS — Vault

Source de vérité de House OS : specs de features, registres de décisions, notes de
référence et de domaine. Quand ce vault et le code se contredisent, l'un des deux a
tort — corriger dans la même session. Maintenu sous le contrat du skill global `vault`.

## À lire, toujours

- Ce fichier, en premier, dans toute session de design/planification/changement de contrat.
- [[Conventions]] — langue, nommage, tags, schémas du projet.

## À lire selon le contexte

| Si tu… | Lis |
|---|---|
| Touches une feature existante | `Features/<Feature>/<Feature>.md` et ses décisions liées |
| Démarres une nouvelle feature | Lance `/vault feature <nom>` |
| T'apprêtes à un choix T3 (schéma, architecture, sécurité, convention) | Le dossier `Decisions/` pour les précédents |
| Travailles sur la logique ou le vocabulaire du domaine | [[Glossaire]] |
| As besoin du portrait d'ensemble | [[Architecture]] |
| Reprends un plan en cours (`status` ≠ `executed`) | Sa note `Plan YYYY-MM-DD …` — et seulement dans ce cas |

> [!warning] Ne lis pas les notes `Plan …` par défaut
> Un plan est immuable une fois exécuté ; la vérité courante est dans la spec et le
> Recap de la feature. Ouvre un plan seulement pour le reprendre (aucun en cours as
> of 2026-09-21) ou pour l'archéologie d'un choix. Certains dépassent 80 Ko.

## Échelle de cérémonie (ce qu'une tâche doit au vault)

- **T1 — trivial** (typo, renommage, refactor sans changement de comportement) : rien.
- **T2 — changement de contrat** (comportement, API, modèle de données, flux UX) :
  mettre à jour les notes touchées + Recap dans la même session ; décision seulement
  si un vrai choix entre alternatives a été fait.
- **T3 — coûteux à renverser** (schéma, architecture, sécurité, convention
  transversale, contrat externe) : fichier de décision obligatoire + spec + note de plan.

## Contexte projet (as of 2026-08)

Déménagement le 2026-10-06 à Sainte-Catherine-de-la-Jacques-Cartier. V0
« Déménagement » visée mi-septembre (tâches ponctuelles), V1 autour de l'emménagement
(récurrence + équipements). Jamais d'enfants — aucune feature famille/points, jamais.
Recherche initiale : `docs/research/` (3 rapports, 2026-08-23).

## Map of content

### Décisions
![[Decisions.base]]

Liste en texte, groupée par feature (le `.base` ci-dessus ne se lit que dans Obsidian).
`(remplacée)` = `status: superseded` ; la remplaçante est dans la même ligne.
Une décision transversale gouverne plusieurs features.

- **Transversales** — [[D-2026-08-23 Plateforme Hobby Cœur Custom]] · [[D-2026-08-23 Monorepo]] · [[D-2026-08-23 Monolithe Modulaire Tranches Verticales]] · [[D-2026-08-23 PostgreSQL]] · [[D-2026-08-23 Frontend Vite React PWA]] · [[D-2026-08-25 Retrait Du Service Worker]] · [[D-2026-08-23 Direction Artistique Cuisine Chaleureuse]] · [[D-2026-08-23 Pas De N8n Dans Le Cœur]] · [[D-2026-08-23 Notifications Par Flux iCal]] · [[D-2026-08-23 Standard IoT MQTT Discovery]] · [[D-2026-08-23 Interface Desktop Et Écran E-ink]] (remplacée) → [[D-2026-09-19 Interface Téléphone Distincte]] · [[D-2026-08-23 Hébergement Maison Tailscale Docker]] (remplacée) → [[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]]
- **Tâches** — [[D-2026-08-23 Moteur De Récurrence Trois Modes]] · [[D-2026-08-23 Zones Plates]] · [[D-2026-08-23 Flux iCal Par Personne]] · [[D-2026-08-24 Annulation Et Passage D'occurrences]] (remplacée) → [[D-2026-08-28 Annulation Et Passage D'occurrences]] · [[D-2026-08-25 Invariants D'occurrence En Base]] · [[D-2026-08-25 Ruban Des 7 Prochains Jours]] · [[D-2026-08-25 Bilan Hebdo Du Ménage]] · [[D-2026-08-26 Documents Liés Aux Tâches]] · [[D-2026-08-26 Page Tâches Rythmes Et Année]] · [[D-2026-08-26 Vue Année Défilante]] · [[D-2026-08-28 Glissement Hors Fenêtre Des Intervalles]] · [[D-2026-08-28 Passer Conserve L'assigné]]
- **Équipements / Documents** — [[D-2026-08-23 Fichiers Sur Disque]] (remplacée) → [[D-2026-08-24 Document Unifié Sur Disque]] · [[D-2026-08-24 Catégories Et Échéance De Document]] · [[D-2026-08-25 Miniatures De Documents]] · [[D-2026-08-26 Dossier De Document]] · [[D-2026-08-26 Navigation Documents Par Facettes]] · [[D-2026-09-02 Boîte À Classer Des Documents]]
- **Courriel Entrant** — [[D-2026-09-02 Courriel Entrant Par Cloudflare Et R2]] · [[D-2026-09-02 Enrichissement LLM À L'ingestion]]
- **Budget** — [[D-2026-08-26 Compte Unique Et Enveloppes Virtuelles]] · [[D-2026-08-26 Mouvements D'enveloppe En Journal]] · [[D-2026-08-26 Cibles D'enveloppe Dérivées Des Tâches]] · [[D-2026-08-26 Ventilation Du Dépôt Multi-Enveloppes]] · [[D-2026-08-26 Import Manuel D'abord Sync Ensuite]] · [[D-2026-08-26 Page Budget Flux Raffiné Et Bascule Flux Tracé]]
- **Comptes À Rebours** — [[D-2026-08-23 Comptes À Rebours Au Flux iCal]] · [[D-2026-08-23 Gestion Des Comptes Dans Aujourdhui]] · [[D-2026-08-23 Icônes Maison Comptes À Rebours]] · [[D-2026-08-23 Célébration Puis Masquage Des Comptes]]
- **Titre D'humeur** — [[D-2026-08-24 Phrase Du Jour Haiku Matin Et Soir]] · [[D-2026-08-25 Phrase Du Jour Axée Tâches]]
- **Météo** — [[D-2026-08-24 Météo Open-Meteo]] · [[D-2026-08-24 Tables Météo Normalisées]] · [[D-2026-08-25 Code Météo Horaire Et Conditions Du Moment]]
- **Flux Externes** — [[D-2026-08-24 Tables Flux Externes]] · [[D-2026-08-24 Flux ICS Dans L'app Affichage Seul]] · [[D-2026-09-20 Flux Externe Poussé]]
- **Auth / Serveur MCP** — [[D-2026-08-23 Auth Simple Deux Comptes]] · [[D-2026-08-24 Serveur MCP Intégré Au Backend]] · [[D-2026-08-24 Clé API Partagée Et AgirComme]]
- **Synchro** — [[D-2026-08-28 Synchro Temps Réel Par SignalR]] · [[D-2026-08-28 Événements Par Intercepteur EF]] · [[D-2026-08-28 Toast Carte Posée]]
- **Observabilité** — [[D-2026-08-29 Journalisation Structurée Serilog Et Seq]] · [[D-2026-08-29 Collecteur Dans Son Propre LXC]]
- **Suite De Tests** — [[D-2026-08-25 Stratégie De Tests Trois Couches]]
- **Déploiement / Distribution** — [[D-2026-08-24 Prod LXC Proxmox NPM GitHub]] · [[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]] · [[D-2026-09-02 Dépôt Public AGPL Et Instance Générique]]
- **Vue Téléphone** — [[D-2026-09-19 Interface Téléphone Distincte]] · [[D-2026-09-19 Portée De La Vue Téléphone]]
- **Affichage E-ink** — [[D-2026-09-03 Protocole TRMNL BYOS Comme API D'affichage]] · [[D-2026-09-03 Registre Des Appareils D'affichage]] · [[D-2026-09-03 Rendu E-ink Par Chromium Headless]]
- **Fonds De Tiroir** — [[D-2026-09-20 Fonds De Tiroir Séparé Du Journal]] · [[D-2026-09-20 Banque Du Hasard En Fichier De Données]] · [[D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo]] · [[D-2026-09-20 Sources Municipales Séparées Par Solidité]] · [[D-2026-09-20 Calendrier De Collectes Régénéré À La Main]]
- **Lettre Du Matin** — [[D-2026-09-21 Courriel Sortant Par SMTP]] · [[D-2026-09-21 Une Lettre Au Foyer]] · [[D-2026-09-21 Adresse De Courriel Sur L'Utilisateur]] · [[D-2026-09-21 Lettre Écrite À Part Sur La Même Matière]] · [[D-2026-09-21 Lettre Matérialisée Et Rattrapée Le Jour Même]]
- **Journal De La Maison** — [[D-2026-09-20 Une Édition Par Jour Matérialisée]] · [[D-2026-09-20 Édition Écrite Par Opus]] · [[D-2026-09-20 Une Seule Mise En Page À Rangs]] · [[D-2026-09-20 Regroupement Sans Catégorie De Tâche]] · [[D-2026-09-20 Échéance Ferme Explicite Sur La Tâche]] · [[D-2026-09-21 Réédition En Deux Temps]] · [[D-2026-09-21 Matière Conservée Sur L'Édition]]

### Features
![[Features.base]]

Liste en texte (le `.base` ci-dessus ne se lit que dans Obsidian). Statut as of
2026-09-21 : toutes `implemented`. Quand un statut change, mettre la ligne à jour.

- [[Tâches]] — le cœur : tâches récurrentes/ponctuelles, occurrences, assignation, ruban 7 jours, vue année.
- [[Équipements]] — inventaire de la maison : marque, série, garantie, manuels, specs JSONB.
- [[Documents]] — le classeur : catégories, facettes, dossiers, boîte à classer, miniatures.
- [[Courriel Entrant]] — transfert d'un reçu à l'adresse maison → classeur, via Cloudflare Worker + R2, enrichi par LLM.
- [[Budget]] — compte unique + enveloppes virtuelles, mouvements en journal, cibles dérivées des tâches.
- [[Comptes À Rebours]] — « 44 dodos avant… » généralisé, icônes maison, célébration puis masquage.
- [[Titre D'humeur]] — la bannière-héros d'Aujourd'hui, phrase du jour axée tâches.
- [[Météo]] — Open-Meteo, tables normalisées, conditions du moment, règles « bonne journée pour… ».
- [[Flux Externes]] — abonnements ICS (collectes…) affichage seul + flux poussés par un gratteur.
- [[Auth]] — deux comptes seedés, cookie de session, clé API partagée + `agirComme`.
- [[Serveur MCP]] — `/mcp` intégré au backend, parité avec l'API REST.
- [[Synchro]] — temps réel par SignalR, événements par intercepteur EF, toast carte posée.
- [[Observabilité]] — Serilog structuré + Seq, `TraceId`, logs navigateur.
- [[Suite De Tests]] — stratégie trois couches (domaine, intégration, web).
- [[Déploiement]] — LXC Proxmox, Docker Compose, NPM, backups, flux iCal public via Funnel.
- [[Distribution]] — dépôt public AGPL : `.env` générique, installation, contribution, CI.
- [[Vue Téléphone]] — interface téléphone distincte (2026-09-19) et sa portée.
- [[Affichage E-ink]] — écran mural TRMNL BYOS, rendu Chromium headless, registre d'appareils.
- [[Fonds De Tiroir]] — les petits faits datés que la maison sait d'elle-même (normales climatiques, banque du hasard, sources municipales).
- [[Journal De La Maison]] — l'éditorialiste de l'écran : une édition par jour, écrite par Opus, mise en page à rangs.
- [[Lettre Du Matin]] — le courriel du matin : la maison écrit à ses habitants, même matière que le journal, SMTP (en prod depuis le 2026-09-21).

### Référence
- [[Architecture]] — stack, structure du monorepo, feuille de route par phases.
- [[Distribution]] — dépôt public : contrat d'installation (`.env`), contribution, CI.
- [[Éditorialiste De L'Écran]] — la direction retenue pour l'écran mural (2026-09-20) :
  fonds de tiroir, règle de bascule, sources municipales. Matériau ; bâti le
  2026-09-21 en [[Fonds De Tiroir]] + [[Journal De La Maison]].
- [[Ronde QA 2026-08]] — constats de la ronde QA d'août, ce qui a été corrigé ou non.
- [[Banque D'idées]] — sac d'idées de features tiré du balayage du marché
  (110 apps notées, catalogue par catégorie, leçons de monétisation).

### Inspiration UI
- [[Inspiration UI]] — index du dossier : directions artistiques, patterns, e-ink,
  typographie québécoise. Maquettes : `design/maquettes/` (canvas Artifact publié).

### Domaine
- [[Glossaire]] — vocabulaire du domaine (Tâche, Occurrence, Zone, Équipement…).
