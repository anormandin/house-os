---
type: decision
status: accepted
date: 2026-08-24
feature: "[[Serveur MCP]]"
tags: []
---

# Serveur MCP Intégré Au Backend

## Contexte

Alain veut piloter House OS depuis Claude Code (sa souscription Claude) : planifier un
lot de tâches en conversation (ex. le déménagement) puis le pousser dans l'app, lister
et compléter des occurrences, etc. Il faut choisir où vit le serveur MCP qui expose
ces opérations. Premier MCP d'Alain — l'intérêt d'apprentissage fait partie du but.

## Options considérées

- **Endpoint `/mcp` intégré au backend .NET** (SDK officiel C# `ModelContextProtocol.AspNetCore`) —
  réutilise DbContext, domaine et validations ; un seul process ; joignable en dev
  (localhost:5000) et en prod via Tailscale (port 8080). Coût : dépendance NuGet, un
  scheme d'auth de plus.
- **Serveur stdio séparé (TypeScript)** wrappant l'API REST — zéro changement backend,
  mais duplication des DTOs, gestion de cookie/clé côté client, un process de plus.
- **Pas de MCP : skill + curl** — le plus léger, mais fragile pour pousser un lot de
  40 tâches et sans schéma typé pour les outils.

## Décision

MCP intégré au backend, choisi par Alain (2026-08-24). Le MCP est une tranche
verticale de plus (`Features/Mcp/`), transport streamable HTTP **stateless**, outils
enregistrés explicitement (pas de découverte par assembly).

## Conséquences

- Le backend expose un deuxième protocole ; toute évolution d'API doit garder les
  outils MCP alignés (même logique partagée via OperationsTaches — pas de duplication).
- Le lot `creer_taches` est transactionnel côté serveur (tout-ou-rien), impossible à
  garantir aussi proprement depuis un wrapper REST externe.
- Claude Code se branche via `.mcp.json` committé (URL et clé surchargeables par env).

## Confirmation

- `grep -r "MapMcp" server/HouseOs.Api/Features/Mcp/McpEndpoints.cs` retourne le
  mapping `/mcp`.
- `grep "ModelContextProtocol.AspNetCore" server/HouseOs.Api/HouseOs.Api.csproj`
  retourne la référence de package.
- `.mcp.json` existe à la racine et déclare le serveur `house-os`.
