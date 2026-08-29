---
type: decision
status: accepted
date: 2026-08-29
feature: "[[Observabilité]]"
tags: []
---

# D-2026-08-29 Journalisation Structurée Serilog Et Seq

## Contexte

Un 503 intermittent frappe la complétion d'occurrence, en dev et en prod : l'écriture
commit (recliquer donne 409) mais la réponse n'arrive jamais. Trois sessions d'enquête
ont écarté le refactor du toast, ControlCenter sur le port 5000, un backend périmé et un
redémarrage de conteneur — sans conclure, parce qu'**il n'existait aucune trace à lire**.

`appsettings.json` mettait `"Microsoft.AspNetCore": "Warning"` : aucun log de requête,
donc ni statut, ni durée, ni exception de pipeline. Le peu qui restait était noyé par les
`SELECT 1` du healthcheck toutes les 30 s. Côté web, c'était pire : aucun `console.*`,
aucune error boundary, et l'échec de connexion au hub avalé par un `catch(() => {})`.

Le symptôme est un problème de **temps**, pas d'exception : la complétion commit, puis
deux diffusions SignalR sont attendues *dans la requête*, avant l'écriture du 204.

## Options considérées

- **`ILogger` intégré + `AddJsonConsole`** — zéro dépendance, mais pas d'enrichissement
  par requête, pas de sink, et il faudrait quand même un collecteur pour chercher.
- **Serilog + Seq** (retenu) — une dépendance de journalisation, un conteneur, et la
  meilleure recherche sur propriétés .NET (`TraceId = '…'` rend la timeline d'une
  requête). Licence Individual gratuite : 1 utilisateur, 50 Go. Coût honnête : produit
  propriétaire, et Ariane ne pourrait pas y ouvrir de session.
- **Grafana Loki + Alloy** — 100 % libre et réutilisable en phase 3 pour les capteurs,
  mais trois conteneurs (~1 Go) et LogQL, plus faible que Seq pour le filtrage ad hoc
  qu'exige une enquête.
- **OpenObserve** — binaire Rust unique, AGPL, RUM navigateur inclus ; écarté pour une
  communauté .NET plus mince et un projet plus jeune.
- **Dozzle** — visualisation seule, rien de persisté : inutile pour attendre un incident.
- **OpenTelemetry (traces en cascade)** — l'outil théoriquement juste pour un problème de
  temps, mais quatre paquets et un second pipeline. Écarté au profit d'une timeline de
  logs corrélés, suffisante pour localiser la phase fautive.

## Décision

Proposé par l'agent, tranché par Alain le 2026-08-29 : **Serilog** pour l'émission,
**Seq** comme collecteur (conteneur du compose, LAN/tailnet seulement), **corrélation par
`TraceId`** renvoyé au client dans l'en-tête `X-Trace-Id` et dans les ProblemDetails, et
une **piste de session complète** expédiée par le navigateur à `/api/journal-client`.
Les durées de phase sont mesurées à la main aux endroits suspects ; pas d'OpenTelemetry.

Le sink Seq est branché **sans `controlLevelSwitch`**. Mesuré pendant cette session : ce
paramètre laisse Seq imposer son niveau minimum au sink, et les durées de phase en Debug
sortaient en console sans jamais atteindre Seq — précisément les évènements qu'on
collecte. Les niveaux se règlent dans la section `Serilog` d'`appsettings`.

## Conséquences

- Une dépendance de plus au démarrage (`Serilog.AspNetCore`, `Serilog.Sinks.Seq`).
  Le collecteur a d'abord été mis dans le compose de house-os ; il en est sorti le jour
  même vers son propre LXC — voir [[D-2026-08-29 Collecteur Dans Son Propre LXC]], qui
  amende cette conséquence.
- Toute réponse d'erreur porte désormais un identifiant citable — la bannière l'affiche.
- `/api/journal-client` est le **deuxième** endpoint anonyme du système (avec
  `/api/sante`) : corps plafonné, lot borné, niveaux sur liste blanche, propriétés
  structurées uniquement (jamais de gabarit) et rate limiting dédié.
- Les refus de validation des huit tranches passent par un helper unique
  (`ResultatsApi.Erreur`) : dédoublonnage réel, et un refus laisse enfin une trace.
- Rétention Seq à régler par l'API/`seqcli` (14 jours) — pas de variable d'environnement.
- La journalisation n'a pas d'outil MCP : la parité MCP vise les tranches du domaine.

## Confirmation

- `grep -rn "Microsoft.AspNetCore.*Warning" server/HouseOs.Api/appsettings.json` ne
  rapporte rien sous une clé `Logging` : la section `Logging` a disparu au profit de
  `Serilog`.
- `grep -rn "controlLevelSwitch" server/` ne rapporte rien.
- `grep -rn "Results.ValidationProblem" server/HouseOs.Api/Features/` ne rapporte
  aucune ligne hors de `Infrastructure/ResultatsApi.cs`.
- Tests : `NiveauRequeteTests` (la table des niveaux, dont le bruit du healthcheck et
  les WebSockets de longue durée) et `JournalisationApiTests` (en-tête `X-Trace-Id`,
  `traceId` dans les ProblemDetails, bornes de `/api/journal-client`).
- Web : `src/lib/journal.test.ts` (tampon, seuil, absence d'auto-journalisation) et
  `src/components/GardeErreur.test.tsx`.
