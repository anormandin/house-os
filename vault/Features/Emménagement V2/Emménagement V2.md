---
type: feature
status: implemented
last-verified: 2026-09-30
verified-against: 6d1fda0
tags: []
---

# Emménagement V2

## Intention

Le 2026-10-06, les 88 tâches ponctuelles du déménagement disparaissent et House OS
doit gagner sa place sur la récurrence seule. Au 2026-09-28, la prod compte 100
tâches dont 12 récurrentes (3 avec fenêtre saisonnière), 10 équipements, un module
[[Budget]] jamais alimenté (0 enveloppe, 0 transaction) et deux restes de la phase 2
jamais entamés (Hydro-Québec, consommables). La revue de l'état de l'app et de la
[[Banque D'idées]] (session du 2026-09-28) a retenu la direction « la maison qui
s'entretient elle-même » plutôt que la phase 3 matériel (le lab déménage avec la
maison) ou l'approfondissement de la voix de la maison (retouchée le jour même).

Emménagement V2 est un **parapluie** : trois volets qui touchent chacun une feature
existante, un seul plan ([[Plan 2026-09-28 Emménagement V2]]). Chaque volet met à
jour la spec de la feature qu'il touche ; cette note porte l'intention commune, les
décisions et le plan. Une fois exécuté, cette note devient un pointeur vers les specs
touchées et son Recap.

## Comportement

As-built (livré le 2026-09-29, commit `c4fdf0e` ; voir [[Recap Emménagement V2]]).

### Volet 1 — la catégorie d'équipement

- Quand un utilisateur crée ou modifie un équipement, il peut lui donner une
  catégorie parmi la liste fermée
  ([[D-2026-09-28 Catégorie D'équipement En Liste Fermée]]) ; null = pas encore
  classé. REST et MCP refusent une valeur hors liste (400). Les listes (bureau,
  téléphone, `lister_equipements`) l'affichent ; le bureau peut filtrer par catégorie.

### Volet 2 — les packs d'entretien

- Le fichier `packs-entretien.qc.json`
  ([[D-2026-09-28 Packs D'entretien En Fichier De Données]]) contient un **programme
  de la maison** et un **pack par catégorie**.
  Chaque item : clé, titre, description, récurrence complète (mode + paramètres +
  fenêtre en mois-jour, [[D-2026-09-28 Fenêtres Des Packs En Mois-Jour Absolus]]),
  stratégie d'assignation. Lu au démarrage ; un item invalide est écarté et journalisé,
  jamais fatal.
- Quand un utilisateur demande les propositions pour un équipement
  (`GET /api/entretien/propositions?equipementId=`), le système renvoie les items du
  pack de sa catégorie, chacun marqué **déjà présent** si une tâche liée à cet
  équipement porte le même titre (sans accents ni casse). Un équipement sans catégorie
  ou de catégorie `Autre` renvoie une liste vide avec la raison.
- Quand un utilisateur demande le programme de la maison
  (`GET /api/entretien/propositions`), même contrat : déjà présent = même titre sur
  n'importe quelle tâche.
- Quand un utilisateur **adopte** des items (`POST /api/entretien/adopter`, clés +
  équipement optionnel), le système crée les tâches en une transaction (tout ou rien),
  liées à l'équipement et à sa zone, avec la première occurrence matérialisée par le
  moteur habituel. Un seul refus fait échouer le lot, rien n'est créé : clé inconnue
  ou hors du pack visé, liste vide, équipement pas classé ou `Autre` → 400 ; équipement
  introuvable → 404 ; titre déjà présent → 409 (le message nomme le titre et la clé).
  Réponse 201 : les tâches créées (id, titre, première échéance). Un lot = un seul
  événement [[Synchro]] « tâches créées ».
- UI bureau : sur la fiche d'un équipement classé, un bouton « Proposer les
  entretiens » ouvre le panneau des propositions (cases à cocher, déjà présents cochés
  et grisés, résumé lisible de la récurrence et de la fenêtre) et « Créer N tâches ».
  Sur la page Tâches, un bouton « Programme de la maison » ouvre le même panneau pour
  le programme. Téléphone : rien ([[D-2026-09-19 Portée De La Vue Téléphone]]).
- MCP : `proposer_entretiens(equipementId?)` et `adopter_entretiens(cles,
  equipementId?)`, même contrat ([[Serveur MCP]]).

### Volet 3 — semer la maison

- Le skill `inventorier-maison` ([[D-2026-09-28 Semis De La Maison Par Skill MCP]])
  lit un rapport d'inspection (PDF local), en tire un tableau d'équipements (nom,
  catégorie, zone existante, marque/modèle si lisibles, notes), attend la confirmation,
  crée par `gerer_equipement`, puis enchaîne sur `proposer_entretiens` /
  `adopter_entretiens` pour chaque équipement classé et pour le programme de la
  maison. Jamais d'id inventé.

### Volet 4 — le budget prend vie

- **Encore à faire** (étape 4 du plan) : le lecteur CSV AccWeb
  (`server/HouseOs.Api/Features/Budget/FournisseurTransactions.cs`) est confronté au
  premier vrai export Desjardins d'Alain et corrigé s'il le faut ; le fichier réel
  (anonymisé) devient un exemple de test. En attendant, le compte est ancré à sa valeur du
  2026-09-29 (pas de chiffres dans le vault).
- Puis, gestes de données en prod (pas de code) : l'ancrage est fait (2026-09-29) ;
  restent les enveloppes `Taxes` (municipales et scolaires de Sainte-Catherine,
  échéancier réel) et `Equipement` (toiture, chauffe-eau du garage — le semis l'a
  désigné, là où le plan disait thermopompe), et la tâche de virement mensuel liée.

### Dette fermée par le plan

- Liste des tâches : mesurée, puis `AsSingleQuery()` — et non `AsSplitQuery()`, le
  produit étant borné par l'invariant « une occurrence en attente par tâche »
  ([[Tâches#Dette connue]]).
- Cadence de nuit de l'écran : vérifiée dans la nuit du 2026-09-29, plafond par défaut
  porté à 4 h ([[Affichage E-ink]]).
- Décision « La pile » de la [[Vue Téléphone]] mintée rétroactivement (`#backfill`).
- Feuille de route d'[[Architecture]] remise au présent.

## Hors périmètre

- **Hydro-Québec `evenements-pointe`** : le foyer n'est inscrit à aucune offre de
  pointe (ni crédit hivernal `CPC-D` ni Flex D `TPC-DPC`, as of 2026-09). Le jeu de
  données ouvert est sondé et documenté (Opendatasoft, sans clé, champs `offre`,
  `datedebut`, `datefin`, `secteurclient`) ; reprendre comme troisième source de
  [[Flux Externes]] le jour où l'inscription se fait.
- Phase 3 matériel (hub MQTT, NFC, capteurs) : après la remise en route du lab dans
  la nouvelle maison.
- Consommables : toujours pas de besoin prouvé.
- Échéance de document → rappel ou tâche de renouvellement : pas de ce lot.
- Étiquettes QR, composants imbriqués d'équipement, compteurs d'usage, ancre des
  fenêtres aux normales climatiques : idées de la banque, pas de ce lot.
- Import d'inventaire dans l'app (sans Claude Code) : à décider si la demande vient.
- Sync bancaire SimpleFIN : décision dédiée avant toute activation
  ([[D-2026-08-26 Import Manuel D'abord Sync Ensuite]]).

## Décisions

- [[D-2026-09-28 Catégorie D'équipement En Liste Fermée]] — colonne nullable, enum de
  dix valeurs, migration.
- [[D-2026-09-28 Packs D'entretien En Fichier De Données]] — JSON versionné, proposer
  / adopter, déjà présent = même titre, parité MCP.
- [[D-2026-09-28 Fenêtres Des Packs En Mois-Jour Absolus]] — écrites pour la zone 4 ;
  l'ancre aux normales est une évolution du format, pas du moteur.
- [[D-2026-09-28 Semis De La Maison Par Skill MCP]] — `inventorier-maison`, zéro code
  serveur.
- [[D-2026-09-20 Banque Du Hasard En Fichier De Données]] — le précédent du fichier
  de données livré + chemin de config.

## Ancres de code

- `server/HouseOs.Api/Domaine/Equipement.cs` — `CategorieEquipement`
- `server/HouseOs.Api/Features/Entretien/packs-entretien.qc.json` — les packs livrés (zone 4)
- `server/HouseOs.Api/Features/Entretien/PacksEntretien.cs` — modèle, options, lecture au démarrage
- `server/HouseOs.Api/Features/Entretien/OperationsEntretien.cs` — proposer / adopter (REST + MCP)
- `server/HouseOs.Api/Features/Entretien/EntretienEndpoints.cs` — `GET /api/entretien/propositions`, `POST /api/entretien/adopter`
- `server/HouseOs.Api/Features/Mcp/OutilsEntretien.cs` — `proposer_entretiens`, `adopter_entretiens`
- `server/HouseOs.Api/Infrastructure/Texte.cs` — le comparateur de titres sans accents
- `web/src/components/PropositionsEntretien.tsx` — le panneau (fiche d'équipement et page Tâches)
- `web/src/lib/categories-equipement.ts` — libellés des catégories
- `.claude/skills/inventorier-maison/SKILL.md` — le semis (étape 3)

## Sources

- [[Banque D'idées]] — « Semer depuis les données publiques », « Programme annuel
  dérivé du climat + terrain », « Gabarits d'entretien par type d'équipement ».
- `docs/research/2026-08-23-donnees-externes-meteo.md` — Hydro-Québec
  `evenements-pointe` (écarté, voir Hors périmètre).
- Revue de l'état de l'app du 2026-09-28 (session Claude Code) : chiffres de prod
  cités dans l'intention.

## Historique

- [[Plan 2026-09-28 Emménagement V2]] — le plan, approuvé le 2026-09-28.
- [[Recap Emménagement V2]] — fermé le 2026-09-30, étape 4 (budget) encore ouverte.
- 2026-09-28 — spec ouverte après la revue d'état ; grill en deux tours (semis, catégorie,
  fenêtres, Hydro, packs, budget, extras) ; Hydro écarté faute d'inscription.
