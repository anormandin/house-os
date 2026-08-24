---
type: decision
status: accepted
date: 2026-08-24
feature: "[[Serveur MCP]]"
tags: [securite]
---

# Clé API Partagée Et AgirComme

## Contexte

Le endpoint `/mcp` doit être authentifié : l'app n'avait que le cookie de session
(`houseos_session`), impraticable pour un client machine. Il faut aussi savoir **au
nom de qui** une action MCP est enregistrée (complétions → statistiques d'équité de
la stratégie MoinsLAFait ; créations → CreePar ; flux iCal personnel).

## Options considérées

- **Clé API par utilisateur** (pattern JetonIcal) — la clé porte l'identité ; plus de
  clés à gérer pour un foyer de deux.
- **Clé partagée unique + paramètre `agirComme` par outil** — une seule clé ; identité
  déclarative par appel, adaptée à un poste partagé où l'un parle pour l'autre.
- **Réutiliser le login cookie** — mot de passe en clair dans la config du client et
  gestion de cookie jar ; rejeté.

## Décision

Clé partagée unique (config `Mcp:Cle`, header `Authorization: Bearer`), choisie par
Alain (2026-08-24). Scheme d'authentification ASP.NET dédié `CleApi` + policy
`McpCle` sur `/mcp` (remplace proprement la FallbackPolicy cookie). Comparaison à
temps constant ; clé non configurée = tout refusé (fail closed). Le paramètre
`agirComme` (`alain` | `ariane`) est **requis, sans défaut**, sur tout outil qui
enregistre une identité (`creer_taches`, `completer_occurrence`, `mon_flux_ical`).

## Conséquences

- L'identité est déclarative : le skill `planifier-taches` impose de demander qui est
  au clavier plutôt que deviner.
- En prod, la clé vient du `.env` du serveur (`HOUSEOS_MCP_KEY`, `openssl rand -hex 32`) ;
  docker-compose refuse de démarrer sans (`Mcp__Cle: ${HOUSEOS_MCP_KEY:?}`).
  Exposition réseau limitée au Tailscale (posture existante, HTTP sans TLS).
- Ouvre la voie aux « clés API pour devices IoT » de la feuille de route (phase 3) —
  probablement en clés par device à ce moment-là.

## Confirmation

- Tests `CleApiTests` et `AgirCommeTests` (server/HouseOs.Tests/Features/Mcp/) passent.
- `grep "AddPolicy(McpEndpoints.PolicyCleApi" server/HouseOs.Api/Program.cs` retourne
  la policy.
- `grep -c "agirComme" server/HouseOs.Api/Features/Mcp/OutilsTaches.cs` ≥ 2 (paramètres
  requis sur creer_taches et completer_occurrence).
