---
type: plan
status: executed
date: 2026-08-24
feature: "[[Serveur MCP]]"
---

# Plan 2026-08-24 Serveur MCP V1

Décisions cadres : [[D-2026-08-24 Serveur MCP Intégré Au Backend]],
[[D-2026-08-24 Clé API Partagée Et AgirComme]]. Portée choisie par Alain : toute la
surface API (sauf upload de fichiers), skills usage + dev.

- [x] **Refactor logique partagée Taches** — extraire de TachesEndpoints.cs vers
  `Features/Taches/OperationsTaches.cs` : ConvertirRecurrence/ConvertirStrategie
  (erreurs en chaînes, plus d'IResult), PreparerTache (sans SaveChanges, pour le lot
  tout-ou-rien), ModifierTacheAsync, CompleterAsync (ResultatCompletion),
  ListerOccurrencesAsync, ChoisirProchainAssigne, RecalculerEcheance ; endpoints REST
  = minces adaptateurs. Tests domaine inchangés.
- [x] Package `ModelContextProtocol.AspNetCore` 2.2.0 (figé).
- [x] **Auth clé API** — `Features/Mcp/AuthentificationCleApi.cs` (scheme CleApi,
  FixedTimeEquals, fail closed) ; policy `McpCle` dans Program.cs ; clés
  `Mcp:Cle` (vide en prod par défaut, `dev-cle-mcp-houseos` en dev) ;
  `Mcp__Cle: ${HOUSEOS_MCP_KEY:?}` dans docker-compose.
- [x] **Wiring MCP** — `Features/Mcp/McpEndpoints.cs` : AddMcpServer + transport HTTP
  stateless + WithTools explicites ; MapMcp("/mcp").RequireAuthorization avant le
  fallback SPA.
- [x] **12 outils** — OutilsTaches (lister_utilisateurs, creer_taches lot
  tout-ou-rien, gerer_tache, lister_occurrences, completer_occurrence), OutilsMaison
  (lister_zones, gerer_zone, lister_equipements, obtenir_equipement, gerer_equipement,
  gerer_comptes_a_rebours), OutilsIcal (mon_flux_ical) ; AgirComme.cs ; Conversions.cs
  (dates YYYY-MM-DD).
- [x] Correctif découvert en smoke test : les paramètres optionnels d'outils doivent
  porter une valeur par défaut C# (`= null`), sinon le SDK les exige.
- [x] **Tests** — ConversionRecurrenceTests, AgirCommeTests, CleApiTests
  (server/HouseOs.Tests/Features/) ; 65 tests verts.
- [x] **Branchement Claude Code** — `.mcp.json` racine (URL et clé surchargeables par
  env) ; vérifié : `claude mcp list` voit `house-os` (approbation interactive à faire
  par Alain).
- [x] **Skills** — `.claude/skills/planifier-taches/SKILL.md` et
  `.claude/skills/demarrer/SKILL.md`.
- [x] **Vérification bout-en-bout** (curl) — 401 sans clé ; tools/list = 12 outils ;
  creer_taches (création + lot invalide refusé au complet, erreurs par index) ;
  lister_occurrences ; completer_occurrence (+ refus de double complétion) ;
  gerer_tache supprimer ; DateOnly sérialisée `YYYY-MM-DD`.
- [x] Vault — décisions, spec, plan, recap, Architecture.
