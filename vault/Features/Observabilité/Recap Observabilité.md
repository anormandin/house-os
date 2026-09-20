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

> [!note] Point au 2026-09-20
> (1), (2) et (3) sont faits — le dépôt d'infra `unifi` donne `.36` comme réservation
> DHCP du LXC 106 et l'hôte NPM `logs.alainnormandin.dev → 192.168.4.36:8081` comme
> servi. **(4) est close** : voir le verdict ci-dessous.
Écarté au passage — le « connection was stopped during negotiation » observé en dev est
un artefact du double montage de React StrictMode (la seconde connexion réussit
immédiatement), pas la piste du 503.

## Verdict de l'enquête 503 (2026-09-20)

Première lecture des traces depuis que le lot 3 tourne en prod. Fenêtre examinée : **2026-09-06 → 2026-09-20** (toute la rétention de Seq),
sources croisées **app (Serilog)** et **proxy (NPM, `proxy-host-7` par imfile)**.

**Aucun incident à enquêter.** Zéro 503 dans la fenêtre, des deux côtés :

| Mesure | Résultat |
|---|---|
| Requêtes `POST /api/occurrences/*/completer` | **18** — 17 × `204`, 1 × `409`, `Abandonnee = false` partout |
| Statuts vus côté app, tous chemins | 101, 200, 201, 202, 204, 304, 401, 404, 409, 499, 500 — **jamais 503** |
| Lignes NPM `- 503 ` sur `houseos` | **0** (les 34 lignes contenant « 503 » sont des hachés d'image) |
| Lignes NPM contenant `completer` | **18** — exactement les 18 de l'app : aucune complétion perdue par le proxy |

Le seul `409` (2026-09-10 18:34) arrive **34 minutes après** un `204` réussi sur la même
occurrence, même `TraceId` distinct : c'est un re-clic tardif normal, pas un 503 masqué.

### La piste SignalR est morte — sur deux fondements indépendants

1. **La mesure.** Sur 17 complétions : `MsDiffusion` **max 1 ms**, moyenne 0,24 ms ;
   `MsEcriture` max 9 ms. Les récurrentes (7) ne sont pas plus lentes que les
   ponctuelles (10) — mêmes maxima. Sur **451 diffusions** tous modules confondus :
   **max 2 ms**, et **0** `Diffusion synchro lente`, **0** `annulée`, **0** `échouée`,
   **0** `course détectée`.
2. **Le code.** `DiffuseurSynchro.DiffuserAsync` attrape `Exception` et ne relance
   jamais : la diffusion **ne peut pas** changer le statut de la réponse. Et
   `TachesEndpoints.cs` (`MapPost .../completer`) ne rend que `404`, `409` ou `204` —
   **aucun chemin de code de l'app ne peut produire un 503 sur cette route**. Les seuls
   503 du dépôt sont `/api/sante`, `/api/courriel`, `/api/affichage` et le protocole
   TRMNL ; le rate limiter est réglé sur `429` et ne couvre que la connexion et
   `/api/journal-client`.

Donc : ou bien le 503 d'août venait **d'en avant de l'app** (proxy/tunnel), ou bien le
statut a été mal retenu. La diffusion, elle, est hors de cause.

### Ce que la fenêtre contient à la place — et qui a la bonne forme

Un vrai incident de prod, **2026-09-20 13:03 → 13:13 UTC** : Postgres cesse de répondre
pendant ~2 minutes.

```
System.InvalidOperationException: An exception has been raised that is likely due to a transient failure.
 ---> Npgsql.NpgsqlException: Exception while reading from stream
 ---> System.TimeoutException: Timeout during reading attempt
```

30 × `Exception non gérée` → `500`, 23 × `An error occurred using the connection to
database`, l'avertissement **`thread pool starvation`** du cadriciel, 12 × `504` et
16 × `499` au proxy, `Elapsed` de 126 s à 572 s. Le tout pendant un `docker build` sur
le LXC 105 (churn overlayfs visible dans le syslog de `pve` à 13:19).

C'est **la forme exacte du symptôme rapporté** — l'écriture est durable, la réponse
n'arrive jamais, le re-clic donne `409` — avec un statut différent : un `COMMIT` parti
vers Postgres dont la lecture de réponse expire laisse la transaction commitée et rend
une erreur au client. **Nouvelle hypothèse n°1 pour la prochaine occurrence :
`SaveChangesAsync` sous stalle Postgres, pas la diffusion.**

### Comment attraper la prochaine

- **Ne rien reconstruire** : l'instrumentation fonctionne et le `Debug` atteint bien le
  collecteur en prod (2 374 évènements `Debug`, dont les 451 diffusions).
- **Poser les alertes Seq maintenant**, tant que la mémoire est fraîche — elles
  n'existent pas encore : (a) `StatusCode >= 500`, (b) `@MessageTemplate like
  'Diffusion synchro lente%'`, (c) `thread pool starvation`, (d) tout `503`.
- **Déployer en dehors des heures d'usage**, ou construire l'image ailleurs : le
  `docker build` sur le 105 affame la stack et fabrique lui-même des faux incidents.
- Au prochain symptôme : relever l'heure à la minute, puis croiser `Chemin like
  '%completer%'` (app) et `proxy-host-7` (NPM) sur la même minute — si l'app n'a pas de
  ligne, le statut vient du proxy.

> [!warning] Piège découvert en lisant
> La garde d'abandon client de `GestionnaireExceptions` **n'a jamais servi** : 0 ligne
> `Requête abandonnée par le client` pour 16 requêtes abandonnées (logguées `499` par le
> cadriciel). `ExceptionHandlerMiddleware` court-circuite une requête abandonnée avant
> d'appeler les `IExceptionHandler`. La garde est un filet inerte, pas le mécanisme —
> le commentaire du fichier le dit désormais.
