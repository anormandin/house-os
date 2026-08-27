---
type: plan
status: executed
date: 2026-08-26
feature: "[[Budget]]"
---

# Plan 2026-08-26 Budget V1

## But

Implémenter la v1 du module Budget : compte fonds de prévoyance, enveloppes
avec provisions calculées, import CSV/OFX et rapprochement — tel que spécifié
dans [[Budget]] et ses quatre décisions.

## Étapes

- [x] **Domaine** — créer `Domaine/CompteBudget.cs`, `Domaine/Enveloppe.cs`
      (types `Equipement|Taxes|Projet|Reserve`, statut, liens TacheId/EquipementId
      exclusifs, échéancier JSONB), `Domaine/MouvementEnveloppe.cs` (types
      `Provision|Retrait|Ajustement|Transfert`, liens optionnels transaction +
      entrée de journal), `Domaine/TransactionBancaire.cs` (statut
      `Nouvelle|Liee|Ignoree`, IdExterne/hash de dédup).
- [x] **MoteurProvision** — `Domaine/MoteurProvision.cs`, calculs purs :
      provision à cible/date, taxes lissées sur échéancier, cible dérivée de la
      prochaine occurrence d'une tâche liée, non-affecté. Tests unitaires
      (`HouseOs.Tests`) : tâche liée, taxes, enveloppe libre, cible atteinte,
      sur-allocation, bords de mois (DST/fin de mois).
- [x] **Migration** — DbSets dans `Infrastructure/HouseOsDbContext.cs` +
      `dotnet ef migrations add AjouterBudget`. Jamais de modification directe
      du schéma. Index unique sur la dédup de transaction.
- [x] **Endpoints** — `Features/Budget/BudgetEndpoints.cs` : compte
      (lire/ancrer), CRUD enveloppes (fermeture exige solde 0), mouvements
      (dont transfert = deux mouvements atomiques), résumé (soldes, provisions,
      virement suggéré, non affecté). 401 sans session ; validations 400 avant
      écriture.
- [x] **Import** — `Features/Budget/ImportTransactionsEndpoints.cs` : upload
      CSV/OFX web-only via `IFournisseurTransactions` (implémentation fichier
      v1) ; dédup FITID/hash ; réimport sans effet (testé).
- [x] **Rapprochement** — endpoints lier (crée le mouvement, lien journal
      optionnel, propose la complétion de la tâche de virement si montant
      proche) / ignorer ; heuristique de suggestion par historique de marchand,
      testée.
- [x] **MCP** — `Features/Mcp/OutilsMaison.cs` : `gerer_budget` et
      `bilan_budget`, parité avec l'API (params optionnels avec `= null` —
      exigence du SDK), upload exclu.
- [x] **Web** — `web/src/lib/api.ts` + `web/src/pages/Budget.tsx` : en-tête
      solde + invariant (bandeau si non-affecté négatif), enveloppes avec
      jauges et provisions, inbox de rapprochement (lier/ignorer), création
      d'enveloppe liée à une tâche ou un équipement. Fiche Équipement : bloc
      enveloppe si liée. Chaînes en français, dates OQLF.
- [x] **Tests d'intégration** — `HouseOs.Tests/Integration/BudgetApiTests.cs`
      (WebApplicationFactory + Testcontainers) : invariant, import + dédup au
      réimport, lier une transaction (mouvement + statut), transfert atomique,
      fermeture refusée si solde ≠ 0, garde 401.
- [x] **Tests frontend** — Vitest + Testing Library : page Budget (jauges,
      bandeau d'invariant), flux de rapprochement.
- [x] **Vault** — cocher les étapes, écrire `Recap Budget`, passer [[Budget]] à
      `implemented`, restamper les sceaux, valider les liens.

## Étapes ajoutées (grilling + maquettes, 2026-08-26)

- [x] **Grilling pré-implémentation** — 7 forks résolus (ventilation du dépôt,
      TacheVirementId, mois restants, échéancier statique, ancrage éditable,
      Desjardins CSV+OFX, solde négatif permis) ; spec amendée, décision
      [[D-2026-08-26 Ventilation Du Dépôt Multi-Enveloppes]] mintée.
- [x] **Maquettes** — 2 rondes (A–E puis E1–E4), artefact « Maquettes Budget » ;
      direction retenue : E1 + bascule vers le flux tracé d'E2
      ([[D-2026-08-26 Page Budget Flux Raffiné Et Bascule Flux Tracé]]).
- [x] **Ventilation du dépôt** — endpoint de liaison : dépôt = liste
      {enveloppeId, montant} (Σ ≤ transaction) → N mouvements `Provision` liés
      à la même transaction ; retrait = mono-enveloppe ; testé (intégration).
- [x] **TacheVirementId** — lien optionnel sur `CompteBudget`, sélecteur dans
      la page ; suggestion de complétion à ±10 % du virement suggéré.
- [x] **Vue Flux (E2)** — commutateur segmenté « Flux raffiné | Flux tracé »
      dans Budget.tsx, dernier mode mémorisé (localStorage protégé, patron de
      la page Tâches) ; diagramme SVG rendu client depuis le même résumé.

## Vérification

- `dotnet test` (unitaires MoteurProvision + intégration Budget) au vert.
- `npm test` (web) au vert.
- Parcours manuel via le skill `demarrer` : ancrer le compte, créer l'enveloppe
  « Repeindre la toiture » liée à une tâche décennale, vérifier la provision ;
  importer un OFX d'essai deux fois (aucun doublon) ; lier une transaction ;
  vérifier l'invariant et le bandeau de sur-allocation.
- `python3 ~/.claude/skills/vault/scripts/validate-vault.py vault` : CLEAN.
