---
type: recap
date: 2026-08-24
feature: "[[Serveur MCP]]"
plan: "[[Plan 2026-08-24 Serveur MCP V1]]"
---

# Recap Serveur MCP

Construit tel que planifié, en une session : endpoint `/mcp` intégré au backend (SDK
C# officiel 2.2.0, transport HTTP stateless), clé API partagée (scheme CleApi, fail
closed) + `agirComme` requis sur les outils qui écrivent une identité, 12 outils
couvrant toute la surface API (sauf upload de fichiers), `.mcp.json` committé, skills
`planifier-taches` et `demarrer`. Le refactor `OperationsTaches` partage la logique
tâches entre REST et MCP sans toucher au domaine (tests existants inchangés) ;
`ListerOccurrencesAsync` a aussi été extrait (non prévu au plan, évitait 40 lignes
dupliquées) et le détail équipement partagé via `ChargerDetailAsync`.

Écart notable : les paramètres optionnels des outils MCP doivent porter `= null` en
C#, sinon le SDK (AIFunctionFactory) les traite comme requis — découvert au smoke
test, corrigé partout.

Vérifié par curl bout-en-bout (auth 401/200, lot tout-ou-rien, complétion + refus de
doublon, dates `YYYY-MM-DD`) et 65 tests verts. Reste à faire par Alain : approuver le
serveur `house-os` au prochain lancement interactif de `claude`, et en prod définir
`HOUSEOS_MCP_KEY` (+ `HOUSEOS_MCP_URL` côté client).
