---
type: feature
status: implemented
last-verified: 2026-08-28
verified-against: e19a4fb
tags: []
---

# Serveur MCP

## Intention

Piloter House OS depuis Claude Code avec la souscription Claude d'Alain : planifier un
lot de tâches en conversation (cas vedette : le déménagement du 2026-10-06) puis le
pousser d'un coup dans l'app, consulter ce qui est dû, compléter, gérer zones,
équipements et comptes à rebours — sans passer par l'UI web.

## Comportement

- Le backend expose un endpoint MCP **`/mcp`** (streamable HTTP, stateless) via le SDK
  officiel C#. Quand la requête ne porte pas la clé API (`Authorization: Bearer`,
  config `Mcp:Cle`), le système répond 401 ; clé non configurée = tout est refusé.
- **19 outils**, noms snake_case français, erreurs en français actionnables
  (`McpException`) ; dates en chaînes `YYYY-MM-DD` ; enums en chaînes ; retours
  camelCase (mêmes formes que les DTO REST) :
  - `lister_utilisateurs`, `lister_zones`, `gerer_zone`
  - `lister_taches`, `creer_taches` (lot **tout-ou-rien** : une tâche invalide →
    rien n'est créé, erreurs par index), `gerer_tache` (obtenir/modifier/supprimer),
    `lister_occurrences` (filtres aujourdhui/avenir/en-attente/completees),
    `completer_occurrence` (matérialise la prochaine occurrence si récurrente),
    `gerer_occurrence` (annuler-completion/passer/reporter — voir
    [[D-2026-08-28 Annulation Et Passage D'occurrences]]),
    `bilan_taches` (complétions du ménage par semaine, heure du serveur — voir
    [[D-2026-08-25 Bilan Hebdo Du Ménage]])
  - `lister_equipements`, `obtenir_equipement`, `gerer_equipement`
  - `lister_documents`, `gerer_document` (métadonnées et suppression seulement —
    les octets passent par l'interface web ; voir [[Documents]])
  - `gerer_budget`, `bilan_budget` — parité avec le module [[Budget]] sur les
    cœurs partagés (ancrage, enveloppes, mouvements, rapprochement, dont
    `restaurer_transaction` et un `lier_transaction` à réclamation atomique —
    QA 2026-08-28)
  - `gerer_comptes_a_rebours`, `mon_flux_ical` (dont l'action `regenerer` —
    rotation du jeton — et l'`urlPublique` Funnel ; voir
    [[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]])
- Quand un outil enregistre une identité (`creer_taches`, `completer_occurrence`,
  `gerer_occurrence` action passer, `mon_flux_ical`), le paramètre **`agirComme`**
  (`alain` | `ariane`) est requis sans défaut — voir
  [[D-2026-08-24 Clé API Partagée Et AgirComme]].
- La validation référentielle est explicite : `zoneId`/`equipementId`/`assigneA`
  inconnus sont refusés avec un message qui pointe vers l'outil de listage.
- **Garde-fous fiche partielle** (durcissement 2026-08-25, le client est un LLM) :
  `gerer_tache modifier` refuse une fiche sans `recurrence` sur une tâche
  récurrente (obtenir d'abord), conserve l'échéance en attente quand `echeance`
  est omise (`'aucune'` pour l'effacer) ; `gerer_zone`/`gerer_comptes_a_rebours`
  traitent un nom/titre vide comme « ne pas toucher ». Le paramètre `action` est
  insensible à la casse et aux espaces (comme `agirComme`) ; un filtre de liste
  inconnu est refusé au lieu de tout retourner ; les enums refusent les valeurs
  numériques ; la liste d'icônes des messages d'erreur est dérivée de l'enum. Le
  tout-ou-rien de `creer_taches` purge le change tracker (un SaveChanges ultérieur
  du même scope ne flushe rien). Depuis la ronde QA 2026-08-28,
  `gerer_equipement`, `gerer_comptes_a_rebours` et `gerer_document` passent par
  les validations REST partagées (longueurs, garanties, bornes des specs) —
  plus aucun chemin MCP ne contourne un `ValiderAsync`.
- Claude Code se branche via `.mcp.json` (racine, committé) : URL
  `${HOUSEOS_MCP_URL:-http://localhost:5000/mcp}`, clé
  `${HOUSEOS_MCP_KEY:-<clé dev committée>}` ; en prod, les deux variables pointent
  vers le serveur maison (Tailscale, port 8080) et la clé du `.env`.
- Deux skills projet accompagnent le MCP : `.claude/skills/planifier-taches/`
  (workflow conversation → tableau récapitulatif → confirmation humaine → un seul
  `creer_taches`) et `.claude/skills/demarrer/` (démarrage dev + smoke tests MCP).

## Hors périmètre

- Téléversement/téléchargement de pièces jointes via MCP (binaire) — interface web
  seulement ; MCP liste les métadonnées et peut supprimer.
- Clés par utilisateur ou par device (reporté aux clés IoT de la phase 3).
- Élicitation/sampling MCP (le transport est stateless).

## Décisions

- [[D-2026-08-24 Serveur MCP Intégré Au Backend]] — endpoint dans l'app .NET plutôt
  qu'un serveur séparé ou du curl.
- [[D-2026-08-24 Clé API Partagée Et AgirComme]] — clé unique + identité déclarative
  par appel.

## Ancres de code

- `server/HouseOs.Api/Features/Mcp/McpEndpoints.cs` — wiring AddMcpServer/MapMcp + policy.
- `server/HouseOs.Api/Features/Mcp/AuthentificationCleApi.cs` — scheme CleApi.
- `server/HouseOs.Api/Features/Mcp/OutilsTaches.cs` — outils tâches (dont creer_taches).
- `server/HouseOs.Api/Features/Mcp/OutilsMaison.cs` — zones, équipements, comptes.
- `server/HouseOs.Api/Features/Mcp/OutilsIcal.cs` — mon_flux_ical.
- `server/HouseOs.Api/Features/Mcp/AgirComme.cs` — résolution d'identité.
- `server/HouseOs.Api/Features/Taches/OperationsTaches.cs` — logique partagée REST+MCP.
- `server/HouseOs.Tests/Integration/McpApiTests.cs` et
  `server/HouseOs.Tests/Features/Mcp/` — tests (401 sans clé, clé API, agirComme).
- `.mcp.json` — branchement Claude Code.

## Sources

—

## Historique

- [[Plan 2026-08-24 Serveur MCP V1]] · [[Recap Serveur MCP]]
