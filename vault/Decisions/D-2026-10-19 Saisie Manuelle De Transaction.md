---
type: decision
status: accepted
date: 2026-10-19
feature: "[[Budget]]"
tags: []
---

# Saisie Manuelle De Transaction

## Contexte

Le compte réel du fonds de prévoyance est un compte d'épargne : AccèsD n'offre pour
lui ni conciliation ni téléchargement d'opérations (constaté 2026-10-19). Le seul
chemin d'entrée des transactions était l'import de fichier
([[D-2026-08-26 Import Manuel D'abord Sync Ensuite]]) : pour faire entrer trois lignes
lues à l'écran, il a fallu écrire un CSV au format AccWeb à la main et le téléverser
depuis un navigateur connecté. Le solde courant de l'app est faux tant que les lignes
n'entrent pas, et l'invariant du budget repose sur lui.

## Options considérées

- **Garder le détour CSV à la main** — aucun code, mais un geste bancal à chaque
  ligne, hors MCP (un agent ne téléverse pas), et un format de fichier à respecter
  pour trois champs.
- **Ré-ancrer le compte à chaque relevé** — simple, mais efface le rapprochement :
  les sorties ne se lient plus aux enveloppes, l'historique « où est passé l'argent »
  disparaît de l'app.
- **Saisie manuelle d'une ligne (date, montant signé, description), REST + MCP,
  même pipeline que l'import** — la ligne devient une transaction `Nouvelle` ordinaire,
  à lier comme une importée ; mêmes gardes (ancrage, dédup).

## Décision

**Une ligne de relevé se saisit à la main, par l'API et par MCP (`gerer_budget`
`saisir_transaction`), et entre dans l'inbox de rapprochement exactement comme une
ligne importée** (demandé par Alain, 2026-10-19). Gardes : date entre l'ancrage et
aujourd'hui, montant non nul, description requise. Clé de dédup = celle d'un import
sans numéro de séquence ; un doublon exact est **refusé** (rejeu, double clic) sauf
`forcer`, qui prend le rang d'occurrence libre suivant, comme deux doublons
intra-fichier.

## Conséquences

- L'import de fichier reste le chemin principal pour un compte qui exporte ; la
  saisie couvre les comptes qui n'exportent pas et la ligne isolée.
- Un agent MCP peut faire entrer une ligne lue sur une capture d'écran, puis la lier.
- Le rapport d'import ne change pas ; la saisie répond 201 avec la transaction.
- Pas de modification ni de suppression d'une ligne saisie : comme une importée,
  elle s'ignore (et se restaure).

## Confirmation

- `grep -n "saisir_transaction" server/HouseOs.Api/Features/Mcp/OutilsMaison.cs`
  retourne l'action.
- `grep -n "SaisirAsync" server/HouseOs.Api/Features/Budget/ImportTransactionsEndpoints.cs`
  retourne l'opération partagée REST/MCP.
- Tests : `SaisieManuelle_RefuseLeDoublonExact_SaufForcer`
  (`server/HouseOs.Tests/Integration/ImportTransactionsApiTests.cs`) et
  `Saisir_transaction_via_mcp_cree_une_nouvelle_et_refuse_le_doublon_sauf_forcer`
  (`server/HouseOs.Tests/Features/Mcp/OutilsMaisonTests.cs`).
