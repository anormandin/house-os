---
type: feature
status: implemented
last-verified: 2026-08-29
verified-against: 5082ea1
tags: []
---

# Observabilité

## Intention

Quand Alain dit « j'ai coché et rien ne s'est passé », il doit exister quelque chose à
relire. Avant cette feature, le système était muet des deux côtés : aucun log de requête
côté serveur, rien du tout côté navigateur. Un 503 intermittent sur la complétion a
survécu à trois sessions d'enquête faute de preuves.

## Comportement

- **Une ligne par requête** (méthode, chemin, statut, durée, utilisateur, IP, hôte,
  agent, et `Abandonnee` quand le client est parti avant la réponse). Le healthcheck
  `/api/sante` et les assets statiques en succès sortent en Verbose : sinon la sonde
  toutes les 30 s noie tout le reste.
- **Un `TraceId` par requête**, posé sur chaque évènement, renvoyé au client dans
  l'en-tête `X-Trace-Id` et dans le `traceId` des ProblemDetails. La bannière d'erreur
  l'affiche en petit — c'est la référence qu'on colle dans Seq.
- **Durées de phase sur le chemin de complétion** : écriture (`SaveChanges`, avec la
  matérialisation de l'occurrence suivante pour une récurrente) et diffusion du geste
  mesurées **séparément**, plus la durée de chaque envoi SignalR. Une diffusion au-delà
  de 500 ms sort en Warning. C'est ce qui permettra de localiser le 503 : il se produit
  après le commit et avant l'écriture de la réponse.
- **Cycle de vie du hub** journalisé (connexion, transport, déconnexion propre ou en
  erreur) — la connexion fantôme est le suspect n°1 du 503.
- **Écritures métier** en Information avec l'id et l'acteur, dans toutes les tranches ;
  **refus de validation** en Warning via un helper unique ; **connexion et échec de
  connexion** en Information/Warning avec l'IP ; **rejets du rate limiter** en Warning ;
  **appels d'outils MCP** (nom, noms d'arguments, issue, durée) via un filtre unique ;
  **passages des services d'arrière-plan** avec durée et compteurs.
- **Exceptions non gérées** : journalisées puis rendues en ProblemDetails portant le
  `traceId`. Un abandon client est distingué d'une panne.
- **Piste de session du navigateur** expédiée par lots à `/api/journal-client` :
  navigation, chaque appel API (statut, durée, `TraceId`), erreurs de requête et de
  mutation, gestes de complétion et d'annulation, états de connexion SignalR, erreurs
  non attrapées, promesses rejetées, et plantages de rendu via une error boundary.
  Ré-émis dans Serilog sous `HouseOs.Client.*` avec `Source = 'Client'` — jamais
  persisté en base.
- **Collecte** : Seq dans son **propre LXC** (106 « observabilite », dépôt
  `/opt/observabilite`), partagé par les apps du lab
  ([[D-2026-08-29 Collecteur Dans Son Propre LXC]]). Une clé d'ingestion par application,
  ingestion anonyme fermée, rétention 14 jours. house-os y expédie par le LAN ; un tampon
  disque garde les évènements quand le collecteur redémarre. Dev et prod se distinguent
  par la propriété `Environnement`.

## Hors périmètre

- **OpenTelemetry / traces en cascade** — la timeline de logs corrélés suffit à localiser
  la phase fautive ; à rouvrir seulement si un incident capté reste illisible.
- **Métriques et alertes** — rien ne réveille personne la nuit dans une maison de deux.
- **Exposition publique de l'UI de Seq** — jamais, Funnel inclus. Elle est publiée par
  NPM en HTTPS sur le LAN/tailnet seulement.
- **Outil MCP de journalisation** — rien à y piloter.
- **Persistance des logs en base** — ils vivent dans Seq, pas dans Postgres.

## Décisions

- [[D-2026-08-29 Journalisation Structurée Serilog Et Seq]] — Serilog + Seq, corrélation
  par `TraceId`, piste de session navigateur, pas d'OpenTelemetry.
- [[D-2026-08-29 Collecteur Dans Son Propre LXC]] — le collecteur sort du compose de
  house-os : les logs du lab ne dépendent pas d'une seule app.

## Ancres de code

- `server/HouseOs.Api/Infrastructure/Journalisation/JournalisationExtensions.cs` —
  câblage Serilog, log de requête, table des niveaux (`NiveauRequete`).
- `server/HouseOs.Api/Infrastructure/Journalisation/EnrichisseurTrace.cs` — `TraceId`.
- `server/HouseOs.Api/Infrastructure/Journalisation/Trace.cs` — en-tête `X-Trace-Id`.
- `server/HouseOs.Api/Infrastructure/Journalisation/GestionnaireExceptions.cs` — filet.
- `server/HouseOs.Api/Infrastructure/ResultatsApi.cs` — refus de validation journalisés.
- `server/HouseOs.Api/Features/Journalisation/JournalClientEndpoints.cs` — réception.
- `server/HouseOs.Api/Features/Taches/OperationsTaches.cs` — durées de phase.
- `server/HouseOs.Api/Features/Synchro/DiffuseurSynchro.cs` — durée de diffusion.
- `web/src/lib/journal.ts` — tampon et expédition de la piste de session.
- `web/src/components/GardeErreur.tsx` — error boundary.
- `docker-compose.yml` — expédition vers le collecteur, rotation des logs Docker.
  Le collecteur lui-même : `/opt/observabilite/docker-compose.yml` sur le LXC 106.

## Historique

[[Plan 2026-08-29 Journalisation Structurée]] · [[Recap Observabilité]]
