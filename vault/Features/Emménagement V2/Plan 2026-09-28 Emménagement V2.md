---
type: plan
status: approved
date: 2026-09-28
feature: "[[Emménagement V2]]"
---

# Plan 2026-09-28 Emménagement V2

## But

Faire de House OS la maison qui s'entretient elle-même une fois les tâches du
déménagement disparues : catégoriser les équipements, proposer et adopter des packs
d'entretien de zone 4, semer la nouvelle maison depuis le rapport d'inspection, faire
vivre le budget sur le vrai compte, et fermer la dette connue. Le code se fait avant
et après le 2026-10-06 ; les gestes de données (semis, adoption, enveloppes) se font
dans la nouvelle maison.

## Étapes

### Étape 1 — la catégorie d'équipement (T3, migration)

- [x] Enum `CategorieEquipement` (dix valeurs) et propriété nullable `Categorie` sur
      `server/HouseOs.Api/Domaine/Equipement.cs` ; mapping en texte (comme les autres
      enums du schéma), migration EF `AjouterCategorieEquipement` générée, jamais écrite
      à la main.
- [x] `EquipementRequete`, `EquipementResumeDto`, `EquipementDetailDto`
      (`server/HouseOs.Api/Features/Equipements/EquipementsEndpoints.cs`) et
      `ValiderAsync` : valeur hors liste → 400 « Catégorie inconnue » via `ParseurEnum`.
- [x] MCP `gerer_equipement` / `lister_equipements` / `obtenir_equipement`
      (`server/HouseOs.Api/Features/Mcp/OutilsMaison.cs`) : même champ, même
      validation, description qui liste les valeurs.
- [x] Web : `EquipementDonnees`/`EquipementDetail` dans `web/src/lib/api.ts` ; sélecteur
      dans la fiche (`web/src/pages/Equipements.tsx`), pastille dans la liste et filtre
      par catégorie ; pastille seule sur `web/src/pages/telephone/EquipementsTelephone.tsx`.
      Libellés français en dur (registre partagé `web/src/lib/categories-equipement.ts`).
- [x] Tests : `EquipementsTests.Une_categorie_inconnue_est_refusee` (REST + MCP),
      aller-retour de la catégorie en intégration, test de composant sur le sélecteur.
- [x] Vault : [[Équipements]] (comportement, ancres), [[Serveur MCP]] (liste d'outils),
      `docs/configuration.md` si une clé apparaît.
- [x] 2026-09-29 — étape 1 codée et verte (30 tests équipements/MCP, 7 tests web) ; le
      test s'appelle `CreerAvecCategorieInconnue_Repond400` côté REST et
      `Gerer_equipement_refuse_une_categorie_inconnue` côté MCP, le sélecteur est aussi
      dans la fiche téléphone (un PUT sans la catégorie la vide : la fiche téléphone
      doit la porter). Vault des specs touchées : fait à la fermeture de l'étape 2, qui
      touche les mêmes notes.

### Étape 2 — la tranche Entretien (packs, proposer, adopter)

- [x] Fichier `server/HouseOs.Api/Features/Entretien/packs-entretien.qc.json` : programme
      de la maison (gouttières, robinets extérieurs, coupe-froid, détecteurs de fumée
      et CO, extincteur, calfeutrage, drain et bassin de captation, déneigement de la
      toiture, filtre de la hotte…) et un pack par catégorie (Chauffage : filtres,
      inspection annuelle, ramonage ; EauChaude : vidange, anode, soupape ; Plomberie ;
      Electricite ; Toiture ; Exterieur ; PetitsMoteurs : souffleuse, tondeuse,
      génératrice ; Electromenager ; Vehicule : pneus, entretiens). Fenêtres en mois-jour
      pour la zone 4. `Vehicule` inclut le rendez-vous pneus déjà en prod comme
      exemple de titre.
- [x] `PacksEntretien` (modèle + lecture au démarrage sur le patron de
      `server/HouseOs.Api/Features/FondsDeTiroir/LectureDeLaBanque.cs`), option
      `Entretien:Fichier` (`.env` : `ENTRETIEN_FICHIER`), item invalide écarté et
      journalisé.
- [x] `OperationsEntretien` : proposer (équipement ou maison, marquage « déjà présent »
      par titre sans accents ni casse — réutiliser le comparateur existant côté serveur
      s'il y en a un, sinon en créer un dans `Infrastructure/`), adopter (transaction,
      création par `OperationsTaches` pour que la matérialisation et les invariants
      restent ceux du moteur, 409 sur clé inconnue ou déjà présente).
- [x] Endpoints `GET /api/entretien/propositions[?equipementId=]` et
      `POST /api/entretien/adopter` dans `Features/Entretien/EntretienEndpoints.cs`,
      auth cookie comme le reste.
- [x] MCP `proposer_entretiens` et `adopter_entretiens` (`Features/Mcp/OutilsEntretien.cs`),
      paramètres optionnels avec `= null`.
- [x] Web : `web/src/components/PropositionsEntretien.tsx` (panneau modal partagé),
      bouton « Proposer les entretiens » sur la fiche d'un équipement classé, bouton
      « Programme de la maison » dans l'en-tête de `web/src/pages/Taches.tsx` ;
      invalidation des requêtes tâches/occurrences après adoption ; toast « N tâches
      créées ».
- [x] 2026-09-29 — étape 2 codée et verte : 21 tests backend (packs, opérations, MCP,
      intégration), 3 tests du panneau. Écarts : pas de comparateur serveur existant, d'où
      `Infrastructure/Texte.cs` ; le `Vehicule` du plan cite « pneus » sans reprendre le
      titre exact de prod ; la première proposition de la fiche est repliée sur une seule
      ligne (le bouton, pas un panneau ouvert) pour ne pas alourdir la fiche.
- [x] Tests : `PacksEntretienTests.Chaque_item_du_pack_livre_est_une_recurrence_valide`,
      proposer marque le déjà présent (équipement et maison), adopter est tout ou
      rien, outils MCP en parité, composant du panneau (cases, déjà présent grisé,
      création).
- [x] Vault : [[Tâches]] (programme de la maison), [[Équipements]] (propositions),
      [[Serveur MCP]], `docs/configuration.md` (`ENTRETIEN_FICHIER`), README
      (fonctionnalités).

### Étape 3 — le skill inventorier-maison

- [x] `.claude/skills/inventorier-maison/SKILL.md` sur le contrat de
      `.claude/skills/planifier-taches/SKILL.md` : PDF local → tableau (nom,
      catégorie, zone existante via `lister_zones`, marque/modèle, notes, année
      probable) → confirmation → `gerer_equipement` en lot → `proposer_entretiens` /
      `adopter_entretiens` par équipement puis programme de la maison → vérification
      par `lister_equipements` et `lister_taches`.
- [ ] Essai à blanc sur la base de dev (copie de prod) avec le rapport d'inspection du
      17 rue de la Colline, avant tout geste en prod.
- [x] 2026-09-29 — le skill est écrit et chargé ; l'essai à blanc attend Alain au clavier
      (le skill exige ses confirmations, un essai sans lui ne prouverait rien). Les outils
      qu'il enchaîne sont vérifiés par curl sur le dev : `proposer_entretiens` marque « déjà
      présent » les gouttières de prod, un équipement non classé reçoit la raison, le
      classement par PUT ouvre le pack Chauffage (8 items).
- [x] Vault : [[Architecture]] (« Interface agent », trois skills), [[Équipements]].
- [x] 2026-09-29 — semis réel en prod avec Alain, depuis le rapport d'inspection (90 pages lues
      par `pdftotext`) : 10 équipements classés, 11 créés, pièce « Garage », 12 tâches d'Alain,
      3 retouchées, 19 items de packs adoptés (sans le test DDFT). Les premières occurrences
      d'automne tombant avant le 6 octobre ont été repoussées au 2026-10-10 : une annuelle
      manquée glisse d'un an. Constats pour le Recap : (1) les packs par catégorie sont
      grossiers — chaque équipement de plomberie se voit proposer fosse septique, puits et
      adoucisseur ; (2) un nouvel outil MCP n'apparaît dans une session Claude Code qu'au
      redémarrage de la session ; (3) Alain corrige beaucoup les notes du rapport (évaluations
      jugées inutiles, fenêtres et chauffe-eau déjà remplacés) : le rapport est une source, pas
      une vérité.

### Étape 4 — le budget sur le vrai compte

- [x] 2026-09-29 — pas de CSV disponible ; Alain a fourni le relevé en capture. Le compte
      est ré-ancré par MCP à sa valeur du jour (transactions antérieures = historique de
      la banque, écartées d'un futur import par `Date < DateAncrage`). La confrontation du
      lecteur CSV reste due au premier vrai export.
- [ ] Alain fournit un export CSV AccWeb du compte du fonds de prévoyance (chemin
      local) ; le lecteur est exercé dessus ; écart → correctif dans
      `server/HouseOs.Api/Features/Budget/FournisseurTransactions.cs` + fichier
      d'exemple anonymisé dans les tests.
- [ ] En prod, avec les chiffres d'Alain : ancrage du compte, enveloppes `Taxes`
      (municipales, scolaires, échéanciers réels), `Equipement` (toiture, thermopompe),
      tâche « Virer X $ au fonds » (fixe, jour du mois) liée par `TacheVirementId`.
      Par MCP (`gerer_budget`) ou dans l'UI, au choix d'Alain.
- [ ] Vault : [[Budget]] (lecteur CSV confronté au réel, date), Recap.
- [x] 2026-09-30 — corrections relevées par `/vault sync`, sans réécrire les lignes
      d'origine : (1) à l'étape 3, « fenêtres et chauffe-eau déjà remplacés » est inexact —
      ce sont les fenêtres et le réservoir de propane ; le chauffe-eau du garage reste à
      remplacer (Recap et prod font foi) ; (2) ci-dessus, « ancrage du compte » est fait
      depuis le 2026-09-29 (première ligne de l'étape) — restent les enveloppes et la tâche
      de virement.

### Étape 5 — la dette

- [x] `AsSplitQuery()` sur `ListerTachesAsync`
      (`server/HouseOs.Api/Features/Taches/OperationsTaches.cs`) : mesurer avant/après sur
      la base de dev (lignes SQL renvoyées, durée), appliquer, vérifier que l'avertissement
      EF disparaît des logs ; [[Tâches]] : section « Dette connue » fermée.
- [x] 2026-09-29 — correctif inversé après mesure : `AsSingleQuery()` et non
      `AsSplitQuery()` (66 lignes contre 131 ; le produit est borné par l'invariant
      « une occurrence en attente par tâche »). Le diagnostic de la dette était faux.
- [x] Une nuit d'observation de l'écran avec `ECRAN_PLAFOND_SECONDES=14400` sur le LXC
      (override compose, pas de rebuild) : lire `dernierContact` au matin ; verdict et
      valeur retenue consignés dans [[Affichage E-ink]] et `docs/configuration.md`.
      Revenir à 3600 si le firmware n'honore pas.
- [x] Minter `D-2026-09-19 Direction La Pile Pour Le Téléphone` (`#backfill`) depuis
      les maquettes et [[Vue Téléphone]] ; lier depuis la spec et le Recap.
- [x] [[Architecture]] : feuille de route au présent (phases 1–2 livrées, phase 2
      close hors Hydro et consommables, e-ink/journal/lettre/téléphone livrés, V2 en
      cours, phase 3 après le lab), pointeur E1003 « reçu et enrôlé 2026-09-04 ».

### Étape 6 — release et clôture

- [x] Release prod (push, `git pull && docker compose up -d --build`, prune du cache),
      200 sur `/api/sante`, migration appliquée, nouveau bundle.
- [x] 2026-09-29 08:27 — commit `c4fdf0e` en prod : santé 200, bundle `index-DxlqIQnV`,
      58 items de packs lus au démarrage, `Affichage__PlafondSecondes=14400` dans le
      conteneur (`.env` du LXC : `ECRAN_PLAFOND_SECONDES=14400`). Cache de build pruné
      (9 Go). Observation de nuit lancée : lire `dernierContact` du B73199 le 2026-09-30
      au matin — des trous de ~4 h entre 22 h et 5 h 30 = le firmware honore le délai ;
      des contacts toutes les heures = il plafonne lui-même, remettre 3600.
- [x] Recap [[Recap Emménagement V2]] ; specs touchées à l'as-built ; Home.md (feature
      `implemented`). Le plan reste `approved` tant que l'étape 4 est ouverte.
- [x] 2026-09-30 — verdict e-ink : le firmware honore 14400 s (22 h 09 → 2 h 08 → 5 h 28) ;
      le défaut livré passe à 14400 (code, test, compose, docs, spec).

## Vérification

- `dotnet test server/HouseOs.sln` et `npm test` verts ; CI verte.
- Parcours bureau : classer « Fournaise centrale avec thermopompe » en Chauffage →
  « Proposer les entretiens » → adopter deux items → ils apparaissent dans Tâches avec
  la bonne fenêtre et l'équipement lié ; re-proposer les montre grisés.
- Parcours MCP : `proposer_entretiens` sans équipement liste le programme ;
  `adopter_entretiens` avec une clé déjà adoptée répond 409.
- Skill : essai à blanc en dev, puis semis réel en prod après le 2026-10-06.
- Budget : import du vrai CSV sans transaction écartée ni doublon ; invariant du solde
  respecté après création des enveloppes.
