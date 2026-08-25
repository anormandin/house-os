---
type: decision
status: accepted
date: 2026-08-25
feature: "[[Tâches]]"
tags: []
---

# Invariants d'occurrence en base

## Contexte

L'audit de couverture du 2026-08-25 a montré que deux complétions simultanées de la
même occurrence (double clic, deux appareils) passaient toutes deux la garde
applicative `Statut == EnAttente` : deux entrées de journal, deux occurrences
suivantes matérialisées, puis des 500 différés partout où le code suppose l'unicité
(`Single()` sur l'annulation, l'édition, les notes). Aucun verrou applicatif ne peut
fermer cette course sans point de sérialisation.

## Options considérées

- **Verrou applicatif** (jeton de concurrence EF / verrou pessimiste) : complexité
  répartie sur chaque écriture, et Sqlite (harnais de tests) ne le reproduit pas.
- **Index uniques partiels en base** : l'invariant vit là où il est vrai, une seule
  écriture gagne, l'autre reçoit une violation convertie en 409. Testable sur les
  deux moteurs (syntaxe de filtre commune Postgres/Sqlite).

## Décision

Deux index uniques portent les invariants du moteur :

1. `IX_Occurrences_TacheId_EnAttente` — **une seule occurrence en attente par
   tâche** (`Occurrences(TacheId) WHERE Statut = 'EnAttente'`).
2. `IX_Journal_OccurrenceId` — **une seule complétion par occurrence** (ferme la
   même course sur une ponctuelle, où aucune occurrence suivante ne s'insère).

Les violations sont attrapées dans `CompleterAsync`/`PasserAsync` et traduites en
conflit (409 « déjà complétée/traitée ») après purge du change tracker.

## Conséquences

- Migrations `UneSeuleOccurrenceEnAttenteParTache` et `UneCompletionParOccurrence`
  (données prod et dev vérifiées sans doublon avant création).
- Toute logique future qui matérialise des occurrences doit passer par la complétion
  ou tolérer le 409 — jamais insérer une deuxième en-attente.
- L'import de données historiques devra respecter les deux unicités.

## Confirmation

- `rg "IX_Occurrences_TacheId_EnAttente" server/HouseOs.Api/Infrastructure` — l'index
  partiel existe (contexte + migration).
- `rg "HasIndex\(x => x.OccurrenceId\).IsUnique" server/HouseOs.Api/Infrastructure/HouseOsDbContext.cs`
  — l'unicité du journal existe.
- Test `DeuxCompletionsParalleles_NeCreentQuUneSeuleSuivante` dans
  `server/HouseOs.Tests/Integration/TachesApiTests.cs`.
