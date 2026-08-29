---
type: recap
date: 2026-08-29
feature: "[[Observabilité]]"
plan: "[[Plan 2026-08-29 Journalisation Structurée]]"
---

# Recap Observabilité

Construit en une session, sur la base du handoff de l'enquête 503 (`HANDOFF.md`,
supprimé — son contenu vit dans le plan et la décision).

- **Serilog** câblé (`Infrastructure/Journalisation/`), section `Logging` d'appsettings
  remplacée par `Serilog`. `Microsoft.EntityFrameworkCore.Database.Command` en Warning :
  les `SELECT 1` du healthcheck ne noient plus rien.
- **`TraceId` bout en bout** : enrichisseur → en-tête `X-Trace-Id` → `ApiError.traceId` →
  bannière. Les ProblemDetails le portent aussi.
- **Chemin du 503 instrumenté** : durées d'écriture et de diffusion séparées, envois
  SignalR chronométrés (Warning > 500 ms), `catch { }` nu de `IntercepteurSynchro`
  remplacé par un catch journalisé, cycle de vie du hub, course d'unicité de complétion
  (avalée jusqu'ici) désormais tracée.
- **Toutes les tranches** journalisent leurs écritures et leurs refus. Les huit copies du
  helper `Erreur(champ, message)` fusionnées en `Infrastructure/ResultatsApi.cs`.
- **Piste de session navigateur** (`web/src/lib/journal.ts`) + première **error boundary**
  du projet + `/api/journal-client`.
- **Seq** d'abord ajouté au compose de house-os, puis — même session, sur question
  d'Alain — sorti vers son **propre LXC 106 « observabilite »**
  ([[D-2026-08-29 Collecteur Dans Son Propre LXC]]) : clé d'ingestion par app,
  ingestion anonyme fermée, rétention 14 jours. Rotation des logs Docker partout.

Écarts, tous consignés dans le plan : `controlLevelSwitch` retiré (il empêchait
silencieusement les Debug d'atteindre Seq), statut 101 exclu de l'alerte de lenteur,
`Hote`/`origine` ajoutés, et le collecteur déplacé hors du compose.

Vérifié en dev : complétion d'une tâche récurrente jetable (créée puis supprimée, aucune
donnée réelle touchée) → 5 évènements corrélés dans Seq sur un même `TraceId`, avec les
durées de phase. Piste navigateur reçue et attribuée. Tests : 589 backend, 176 web.

**Reste ouvert** : (1) déploiement prod (`git pull && docker compose up -d --build` sur
le 105 — sa `.env` a déjà la clé d'ingestion) ; (2) hôte NPM `logs.alainnormandin.dev`
→ `192.168.4.36:8081`, à créer dans l'UI de NPM ; (3) réservation DHCP pour le 106, dont
l'IP est aujourd'hui en bail ; (4) **l'enquête elle-même** : attendre l'incident, puis
lire la trace.
Écarté au passage — le « connection was stopped during negotiation » observé en dev est
un artefact du double montage de React StrictMode (la seconde connexion réussit
immédiatement), pas la piste du 503.
