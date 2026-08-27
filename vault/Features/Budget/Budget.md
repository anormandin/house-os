---
type: feature
status: implemented
last-verified: 2026-08-26
verified-against: 7eba036
tags: []
---

# Budget

## Intention

Le plan d'Alain (2026-08-26) : un **compte bancaire réel dédié au fonds de
prévoyance de la maison** — il y dépose de l'argent chaque mois, y puise pour
les **taxes** et pour les **équipements** à réparer ou remplacer (ex. repeindre
la toiture métallique), et veut aussi des **projets** vers lesquels mettre de
l'argent (rénover une pièce). House OS partitionne ce compte en enveloppes,
calcule les provisions, et fait entrer les transactions du compte. Genèse et
patrons du marché : [[Banque D'idées]] (section « Argent de la maison »).

## Comportement

### Compte et invariant

- Un seul compte (`CompteBudget`) : nom, institution, solde initial ancré à une
  date. Solde courant = solde initial + Σ transactions importées postérieures —
  jamais saisi après l'ancrage.
- L'ancrage (solde initial + date) reste **éditable** — c'est un point de
  départ, le solde courant se redérive. À l'import, une transaction datée avant
  l'ancrage est **écartée** et comptée dans le rapport d'import. Sans compte,
  la page Budget affiche le formulaire d'ancrage.
- Le compte porte un lien optionnel `TacheVirementId` vers la tâche récurrente
  de virement mensuel (sélecteur dans la page) ; sans lien, aucune suggestion
  de complétion.
- **Invariant** : solde courant = Σ soldes d'enveloppes + « Non affecté ».
  Quand Non affecté devient négatif (sur-allocation), la page Budget affiche un
  avertissement permanent ([[D-2026-08-26 Compte Unique Et Enveloppes Virtuelles]]).

### Enveloppes

- Une enveloppe : nom, type (`Equipement` | `Taxes` | `Projet` | `Reserve`),
  montant cible optionnel, date cible optionnelle, lien optionnel vers une
  tâche OU un équipement, échéancier de versements optionnel (type `Taxes`,
  liste {date, montant} en JSONB), statut (`Active` | `Fermee`).
- Quand une enveloppe est liée à une tâche récurrente, sa date cible est
  **dérivée** de la prochaine occurrence de la tâche
  ([[D-2026-08-26 Cibles D'enveloppe Dérivées Des Tâches]]).
- Solde d'enveloppe = Σ de ses mouvements
  ([[D-2026-08-26 Mouvements D'enveloppe En Journal]]). Un mouvement : date,
  montant signé, type (`Provision` | `Retrait` | `Ajustement` | `Transfert`),
  liens optionnels vers une transaction bancaire et une entrée de journal de
  complétion, note. Un transfert entre enveloppes = deux mouvements opposés.
- Fermer une enveloppe exige un solde à zéro (transférer d'abord le reste).
  Une enveloppe ne se **supprime** jamais — le journal de mouvements est de
  l'historique.
- Un solde d'enveloppe **négatif est permis** (lier un retrait plus gros que le
  solde) : affiché en rouge, le Non affecté encaisse la différence, l'invariant
  tient. La fermeture, elle, exige toujours zéro.
- Si la tâche liée est supprimée ou n'a pas d'occurrence en attente,
  l'enveloppe reste valide mais sans date dérivée → pas de provision ; la page
  l'indique.

### Provisions (calcul pur, à la lecture — `MoteurProvision`)

- Enveloppe à cible et date : provision mensuelle =
  (cible − solde) ÷ mois restants, plancher 0, arrondie au dollar.
- **Mois restants** = nombre de 1ᵉʳˢ du mois entre aujourd'hui (exclu) et la
  date cible (incluse), plancher 1 — la provision s'aligne sur les occasions de
  virement. Cible échue ou dans le mois courant → provision = tout le manque
  d'un coup, enveloppe affichée « en retard ».
- Enveloppe `Taxes` : provision lissée sur l'échéancier — chaque versement à
  venir contribue (montant − part couverte) ÷ mois restants avant sa date. La
  **part couverte** répartit le solde de l'enveloppe sur les versements en
  ordre chronologique (le prochain d'abord, l'excédent au suivant).
- L'échéancier de taxes est **statique** : les versements passés sont ignorés
  du calcul ; quand tous sont passés, l'enveloppe affiche « échéancier à
  renouveler » et la mise à jour est manuelle (nouveau compte de taxes).
- Enveloppe `Reserve` ou sans cible : pas de provision calculée.
- La page Budget affiche le **virement mensuel suggéré** = Σ des provisions
  courantes. Le virement lui-même est une tâche récurrente ordinaire créée par
  l'utilisateur (mode fixe, jour du mois) ; quand une transaction entrante
  proche du montant suggéré apparaît, l'inbox de rapprochement propose de
  compléter cette tâche — suggestion, jamais automatique (v1). « Proche » =
  ±10 % du virement suggéré ; la tâche visée est celle de `TacheVirementId`.

### Transactions et rapprochement

- Import manuel de fichiers CSV/OFX exportés de la banque, **web seulement** ;
  formats visés : OFX standard (FITID) et CSV AccWeb Desjardins, testés sur
  fichiers d'exemple ; déduplication par FITID (OFX) ou hash (date, montant,
  description) — le réimport du même fichier est sans effet
  ([[D-2026-08-26 Import Manuel D'abord Sync Ensuite]]). Le parsing passe par
  l'abstraction `IFournisseurTransactions` (SimpleFIN plus tard, par config).
- Une transaction importée est `Nouvelle` jusqu'à être `Liee` (rapprochée) ou
  `Ignoree`. L'inbox de rapprochement liste les nouvelles ; lier un **retrait**
  crée le mouvement d'enveloppe correspondant (mono-enveloppe) et,
  optionnellement, le lie à une entrée de journal de complétion existante.
- Lier un **dépôt** ouvre une ventilation multi-enveloppes pré-remplie par les
  provisions suggérées ; la confirmation crée N mouvements `Provision`
  pointant la même transaction, le reste demeure en Non affecté
  ([[D-2026-08-26 Ventilation Du Dépôt Multi-Enveloppes]]).
- Suggestion de rapprochement v1 : heuristique sur l'historique (enveloppe la
  plus souvent liée à un marchand semblable). Pas de table de règles en v1 ;
  le serveur MCP permet déjà des suggestions LLM par-dessus.

### API, MCP, UI

- Tranche verticale `Features/Budget/` : CRUD enveloppes et mouvements,
  gestion du compte, résumé (soldes, provisions, non affecté), import de
  fichier, inbox et actions de rapprochement. Auth requise partout (401 sinon).
- **Parité MCP** ([[Serveur MCP]]) : `gerer_budget` (enveloppes,
  mouvements, lier/ignorer une transaction) et `bilan_budget` (soldes,
  provisions, virement suggéré, transactions à rapprocher). L'upload de
  fichiers reste web-only, comme pour les documents.
- Page **Budget** (desktop, français) : vue par défaut « Flux raffiné » (E1) —
  bandeau chemin de l'argent (entrée mensuelle → compte partitionné en barre
  empilée → sorties prévues), rythme mensuel des provisions, grille
  d'enveloppes avec jauges et liens, inbox de rapprochement ; commutateur
  segmenté vers la vue « Flux » (diagramme de flux tracé d'E2), dernier mode
  mémorisé ([[D-2026-08-26 Page Budget Flux Raffiné Et Bascule Flux Tracé]]).
  La fiche d'un équipement lié montre son enveloppe (solde, provision).
  Maquettes de référence : artefact « Maquettes Budget » (2 rondes,
  E1 retenue + E2 en bascule).

## Hors périmètre

- Plusieurs comptes bancaires (schéma prêt, UI non).
- Sync bancaire automatique (SimpleFIN) — phase ultérieure, décision dédiée
  avant activation (coût + identifiants).
- Catégorisation générale des dépenses du foyer — House OS n'est pas un YNAB :
  seul le compte du fonds de prévoyance entre.
- Complétion automatique de la tâche de virement (suggestion seulement).
- Module Projet complet (dépendances, jalons) : l'enveloppe `Projet` suffit.
- Autre devise que CAD.

## Décisions

- [[D-2026-08-26 Compte Unique Et Enveloppes Virtuelles]] — un compte réel,
  enveloppes virtuelles, invariant non-affecté.
- [[D-2026-08-26 Cibles D'enveloppe Dérivées Des Tâches]] — cible sur
  l'enveloppe, échéance dérivée de la tâche liée, provision calculée.
- [[D-2026-08-26 Mouvements D'enveloppe En Journal]] — solde = Σ mouvements,
  jamais une colonne mutée.
- [[D-2026-08-26 Import Manuel D'abord Sync Ensuite]] — CSV/OFX v1 derrière
  `IFournisseurTransactions`.
- [[D-2026-08-26 Ventilation Du Dépôt Multi-Enveloppes]] — un dépôt se ventile
  en N mouvements `Provision` liés à la même transaction.
- [[D-2026-08-26 Page Budget Flux Raffiné Et Bascule Flux Tracé]] — E1 par
  défaut, bascule vers le flux tracé d'E2, mode mémorisé.

## Ancres de code

- `server/HouseOs.Api/Domaine/` — `CompteBudget.cs`, `Enveloppe.cs`,
  `MouvementEnveloppe.cs`, `TransactionBancaire.cs`, `MoteurProvision.cs`
- `server/HouseOs.Api/Features/Budget/` — `BudgetEndpoints.cs` (résumé,
  compte, enveloppes, mouvements, transferts, liaison),
  `ImportTransactionsEndpoints.cs`, `FournisseurTransactions.cs` (OFX + CSV),
  `SuggestionRapprochement.cs`
- `server/HouseOs.Api/Features/Mcp/OutilsMaison.cs` — `gerer_budget`,
  `bilan_budget`
- `server/HouseOs.Api/Infrastructure/Migrations/*AjouterBudget*`
- `web/src/pages/Budget.tsx` · `web/src/lib/budget-vues.ts` ·
  `web/src/lib/api.ts` (types + méthodes budget)
- Tests : `server/HouseOs.Tests/Domaine/MoteurProvisionTests.cs`,
  `server/HouseOs.Tests/Features/Budget/`,
  `server/HouseOs.Tests/Integration/BudgetApiTests.cs`,
  `web/src/pages/Budget.test.tsx`

## Sources

- Conversation de design 2026-08-26 (plan d'Alain, transcrit en Intention).
- [[Banque D'idées]] — patrons du marché (HomeManager, Actual/YNAB, Billdr).

## Historique

- [[Plan 2026-08-26 Budget V1]]
- [[Recap Budget]]
