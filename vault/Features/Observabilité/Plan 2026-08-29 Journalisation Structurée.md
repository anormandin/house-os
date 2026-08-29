---
type: plan
status: executed
date: 2026-08-29
feature: "[[Observabilité]]"
---

# Plan 2026-08-29 Journalisation Structurée

## Contexte

Un 503 intermittent frappe `POST /api/occurrences/{id}/completer`, en dev **et** en prod.
L'écriture commit (recliquer donne 409 « déjà complétée ») mais la réponse ne revient
jamais. Trois sessions d'enquête ont écarté le refactor du toast, ControlCenter sur le
port 5000, un backend périmé et un redémarrage de conteneur — sans rien trouver, parce
qu'**il n'existe aucune trace à lire** : `appsettings.json` met `"Microsoft.AspNetCore":
"Warning"`, ce qui supprime tout log de requête (statut, durée, exception de pipeline),
et le peu qui reste est noyé par les `SELECT 1` du healthcheck toutes les 30 s. Côté web,
c'est pire : zéro `console.*`, zéro error boundary, `connexion.start().catch(() => {})`
sur le hub — les erreurs du navigateur ne vont **nulle part**.

Le symptôme est un problème de **temps**, pas d'exception : `CompleterAsync` commit, puis
`IntercepteurSynchro.SavedChangesAsync` **et** `DiffuserGesteAsync` attendent chacun une
diffusion SignalR *dans la requête*, avant l'écriture du 204
(`server/HouseOs.Api/Features/Taches/OperationsTaches.cs:279-292`). Une connexion
navigateur morte mais pas encore détectée bloquant `hub.Clients.All.SendAsync` produit
exactement ça — et Alain rapporte le symptôme surtout sur les **récurrentes**, la branche
qui fait ce travail supplémentaire.

Résultat visé : des logs structurés partout (backend, MCP, services d'arrière-plan,
navigateur), corrélés par un `TraceId` unique, collectés dans un **Seq** sur le LXC 105 —
pour que la prochaine occurrence de l'incident soit lisible au lieu d'être devinée.

Remplace et absorbe `HANDOFF.md`, **supprimé à la fin** de cette session.

## Décisions arrêtées (avec Alain, ce jour)

- **Collecteur : Seq**, licence Individual gratuite (1 utilisateur, 50 Go). Conteneur
  unique, meilleure UX de requête sur les propriétés .NET.
- **Émission : Serilog** + `Serilog.Sinks.Seq` natif (CLEF, tampon durable sur disque si
  Seq est absent, pilotage dynamique du niveau depuis l'UI de Seq).
- **Temps : timeline de logs**, pas d'OpenTelemetry. Un `TraceId` par requête, et une
  durée mesurée sur chaque phase suspecte. Fidèle au « peu de pièces » du projet.
- **Frontend : piste de session complète** — erreurs, appels API, navigation, cache
  TanStack, états de connexion SignalR.

## Lot 1 — Émission Serilog (backend)

Paquets sur `server/HouseOs.Api/HouseOs.Api.csproj` : `Serilog.AspNetCore`,
`Serilog.Sinks.Seq`. Nouveau dossier `server/HouseOs.Api/Infrastructure/Journalisation/`.

- `JournalisationExtensions.cs` — `AjouterJournalisation(builder)` /
  `UtiliserJournalisation(app)`, pour que `Program.cs` reste lisible (2 lignes ajoutées).
  Serilog configuré par `ReadFrom.Configuration` : les niveaux se règlent dans
  `appsettings*.json` sans recompiler.
- **Config** (`appsettings.json`, section `Serilog`) : défaut `Information` ;
  `Microsoft.AspNetCore` → `Warning` (le log de requête de Serilog le remplace) ;
  `Microsoft.EntityFrameworkCore.Database.Command` → `Warning` — **c'est ce qui fait
  taire les `SELECT 1`** ; `HouseOs` → `Debug` en Development.
- **Sinks** : Console (gabarit lisible avec `TraceId`, pour `docker logs` et le dev) +
  Seq (`serverUrl` depuis `Journalisation:Seq:Url`, `bufferBaseFilename` pour survivre à
  un Seq arrêté, `controlLevelSwitch` pour monter la verbosité depuis l'UI de Seq).
- `app.UseSerilogRequestLogging()` **juste après `UseForwardedHeaders()`**
  (`Program.cs:162`) : une ligne par requête, avec méthode, chemin, statut, durée.
  - `GetLevel` : ≥500 → Error · 4xx → Warning · durée > 1 s → Warning ·
    `/api/sante` et `/assets/*` en succès → **Verbose** (sinon le healthcheck et les
    fichiers statiques renoient tout).
  - `EnrichDiagnosticContext` : `Utilisateur`, `AdresseClient`, `AgentUtilisateur`,
    `Source` (web/mcp selon le schéma d'auth), et **`Abandonnee`**
    (`contexte.RequestAborted.IsCancellationRequested`) — teste directement
    l'hypothèse « client abort / Kestrel ».
- **Gestionnaire d'exceptions global**, absent aujourd'hui : `AddProblemDetails()` +
  `app.UseExceptionHandler()` + `GestionnaireExceptions : IExceptionHandler` qui
  journalise en Error et renvoie un ProblemDetails portant le `traceId`.
- **Garde-fou secrets** : aucun objet d'options (`HumeurOptions`, `Mcp:Cle`,
  chaînes de connexion) ne doit être destructuré dans un log. Uniquement des propriétés
  structurées explicites.

## Lot 2 — Corrélation `TraceId` bout en bout

- `EnrichisseurTrace.cs` (~15 lignes, `ILogEventEnricher`) ajoute `TraceId`/`SpanId`
  depuis `Activity.Current` — évite le paquet `Serilog.Enrichers.Span`.
- Le middleware existant de `Program.cs:164-169` (celui du `nosniff`) pose aussi
  `X-Trace-Id` sur la réponse, avant que le corps ne commence.
- `web/src/lib/api.ts:366-390` : `ApiError` gagne `traceId?: string`, lu depuis l'en-tête
  `X-Trace-Id` dans `requete()`.
- `web/src/components/BanniereErreur.tsx` affiche le `traceId` en petit texte atténué —
  Alain le colle dans Seq quand ça pète.

## Lot 3 — Instrumentation du chemin du 503

C'est le lot qui justifie tout le reste. Chaque phase mesurée au `Stopwatch`.

1. `Features/Synchro/DiffuseurSynchro.cs` — durée de `SendAsync` en Debug ;
   **Warning au-delà de 500 ms** (le blocage recherché) ; `OperationCanceledException`
   distinguée du reste dans le `catch`.
2. `Features/Synchro/IntercepteurSynchro.cs:188-203` — le `catch { }` nu devient un
   `catch` journalisé (injecter `ILogger<IntercepteurSynchro>`) ; Debug avec les modules
   diffusés et la durée totale.
3. `Features/Taches/OperationsTaches.cs:239-295` (`CompleterAsync`) — nouveau paramètre
   optionnel `ILogger? journal = null`, cohérent avec `diffuseur`/`source` déjà
   optionnels. Journalise : entrée (`OccurrenceId`, `Recurrente`, `Source`), durée du
   `SaveChangesAsync`, durée du `DiffuserGesteAsync` **séparément**, et surtout la
   `DbUpdateException` de violation d'unicité aujourd'hui avalée en silence (l.281-287),
   qui peut se déguiser en 409. Appelants à passer le logger :
   `Features/Taches/TachesEndpoints.cs:180` et `Features/Mcp/OutilsTaches.cs:313`.
4. `Features/Synchro/SynchroHub.cs` — ajouter `OnConnectedAsync`/`OnDisconnectedAsync`
   (connexion, utilisateur, exception de déconnexion). C'est là qu'apparaîtra le
   `The connection was stopped during negotiation` observé en dev.
5. `Features/Sante/SanteEndpoint.cs` — journaliser l'exception derrière le 503 dégradé,
   muette aujourd'hui, pour ne jamais la confondre avec le 503 de complétion.

## Lot 4 — Logs par tranche verticale (le « full logs »)

- **Refus de validation** : les 10 helpers `Erreur(ErreurValidation)` dupliqués
  (`TachesEndpoints.cs:281`, `EquipementsEndpoints.cs:235`, `DocumentsEndpoints.cs:520`,
  `BudgetEndpoints.cs:802`, `ZonesEndpoints.cs`, `ComptesAReboursEndpoints.cs`,
  `FluxExternesEndpoints.cs`, `ImportTransactionsEndpoints.cs`, `AuthEndpoints.cs:33`)
  fusionnent en un `ResultatsApi.Erreur(erreur, journal)` unique qui journalise en
  Warning (champ + raison) avant de rendre le `ValidationProblem`. Déduplication réelle,
  pas seulement un point de log.
- **Écritures métier** en Information avec l'id et l'acteur : tâche créée/modifiée,
  occurrence complétée/annulée/reportée, zone, équipement, document téléversé,
  transaction importée, mouvement d'enveloppe.
- **Auth** (`Features/Auth/AuthEndpoints.cs`) : succès de connexion en Information ;
  **échec en Warning** avec le nom d'utilisateur et l'IP (pertinent sécurité) ;
  réhachage du mot de passe en Information. Et le `OnRejected` du rate limiter
  (`Program.cs:99-103`), muet aujourd'hui, journalise le rejet en Warning.
- **Outils MCP** (`Features/Mcp/OutilsTaches.cs`, `OutilsMaison.cs`, `OutilsIcal.cs`) :
  un helper d'enrobage journalisant nom de l'outil, résumé des arguments, issue et durée ;
  les `McpException` levées passent en Warning au lieu de disparaître.
- **Services d'arrière-plan** (`RolloverService`, `MeteoIngestionService`,
  `FluxExternesRafraichissement`, `HumeurService`) : début/fin de passage avec durée et
  compteurs (ils ne journalisent que les erreurs aujourd'hui).
- Pas d'outil MCP pour la journalisation elle-même : rien à y piloter (la parité MCP vise
  les tranches du domaine).

## Lot 5 — Journal client (piste de session complète)

**Backend** — nouvelle tranche `server/HouseOs.Api/Features/Journalisation/` :
`POST /api/journal-client`, `AllowAnonymous` (pour capter aussi l'écran de connexion),
politique de rate limiting dédiée, corps plafonné (64 Ko, 50 évènements par lot).
Les évènements sont **ré-émis dans le pipeline Serilog** avec `Source = 'Client'` —
jamais persistés en Postgres. Entrée non authentifiée : niveau sur liste blanche,
longueurs bornées, propriétés en nombre borné, et **jamais** dans une chaîne de format
(propriétés structurées uniquement — pas de log forging).

**Frontend** — nouveau `web/src/lib/journal.ts` : tampon circulaire (200 max), vidange
toutes les 5 s / à 20 évènements / sur `pagehide` et `visibilitychange` via
`navigator.sendBeacon`. `sessionId` en `sessionStorage` pour regrouper une session
entière dans Seq. **Le journal ne se journalise jamais lui-même** (échec de vidange →
`console.warn`, rien de plus) — sinon boucle.

Points de branchement :
- `web/src/main.tsx` — `window.onerror` + `unhandledrejection`.
- **`web/src/components/GardeErreur.tsx` (nouveau)** — error boundary autour de
  `<AppConnectee>`. Il n'y en a aucune aujourd'hui : un crash de rendu donne un écran
  blanc sans trace.
- `web/src/lib/api.ts` `requete()` — chaque appel (méthode, chemin, statut, durée,
  `traceId`) ; Warning sur non-ok, Error sur panne réseau.
- `web/src/lib/query-client.ts` — les deux `onError` (QueryCache + MutationCache) avec la
  clé de requête / mutation.
- `web/src/hooks/useSynchro.ts` — remplacer `configureLogging(LogLevel.Warning)` par un
  `ILogger` SignalR qui déverse dans `journal`, journaliser
  `onreconnecting`/`onreconnected`/`onclose`, et remplacer le
  `connexion.start().catch(() => {})` par un `catch` journalisé.
- `web/src/App.tsx` — navigation (effet sur `location`).
- `web/src/lib/completion.ts` — le geste (complétion, annulation) avec l'id d'occurrence,
  qui s'apparie au log serveur par `TraceId`.

## Lot 6 — Conteneur Seq et déploiement

`docker-compose.yml` (racine) — service `seq` : image `datalust/seq` **épinglée**,
`ACCEPT_EULA=Y`, `SEQ_FIRSTRUN_ADMINPASSWORDHASH`, `SEQ_CACHE_SYSTEMRAMTARGET=0.2`
(borne la RAM), volume nommé `seq-data`, healthcheck, UI publiée sur `8081:80`.
L'ingestion reste **interne au réseau compose** (`http://seq:5341`) ; `app` reçoit
`Journalisation__Seq__Url`. Pas de `depends_on` bloquant — le tampon durable couvre un
Seq absent.

- **Jamais dans le Funnel Tailscale** : `infra/tailscale-serve.json` ne publie que
  `/ical` et ne change pas. L'UI de Seq reste LAN/tailnet.
- **Rotation des logs Docker** : aujourd'hui aucun service n'a de `logging:` — ajouter
  `json-file` / `max-size: 10m` / `max-file: 3` sur les trois services existants, sinon
  le stdout grossit sans borne sur un disque de 12 Go.
- **Rétention Seq** : la politique se règle dans l'UI au premier démarrage
  (14 jours). Étape manuelle, à consigner dans le Recap.
- `.env.example` : documenter `SEQ_ADMIN_PASSWORD_HASH` (généré par
  `docker run --rm datalust/seq config hash`) et `JOURNALISATION__SEQ__URL`.

> [!warning] Contrainte RAM à vérifier avant de déployer
> Le LXC 105 est en **2 vCPU / 2 Go** (`vault/Features/Déploiement/Recap Déploiement.md`),
> déjà partagés par Postgres 17 et l'app .NET. Seq prend ~250-400 Mo. **Vérifier
> `free -m` sur le LXC avant la release** ; si la marge est mince, monter le LXC à 4 Go
> dans Proxmox (trivial sur un conteneur) plutôt que de rogner sur Seq.

## Lot 7 — Vault (T3 : décision obligatoire) et ménage

- `vault/Decisions/D-2026-08-29 Journalisation Structurée Serilog Et Seq.md` — `accepted`,
  avec les options écartées (AddJsonConsole, Loki+Alloy, OpenObserve, Dozzle, OTel) et
  une section Confirmation mécanique.
- `vault/Features/Observabilité/Observabilité.md` — nouvelle spec vivante.
- `vault/Features/Observabilité/Plan 2026-08-29 Journalisation Structurée.md` — ce plan.
- `vault/Features/Déploiement/Déploiement.md` — son « Hors périmètre » exclut
  explicitement le monitoring : à réviser et à lier.
- `vault/Features/Synchro/Synchro.md` — le contrat du diffuseur change (T2 : il
  journalise et alerte sur la lenteur).
- `vault/Reference/Architecture.md` — ajouter la brique observabilité.
- Valider : `python3 ~/.claude/skills/vault/scripts/validate-vault.py <repo>/vault`.
- **Supprimer `HANDOFF.md`** — son contenu vit désormais dans le plan et la décision.

## Vérification

**Tests automatisés** (aucun test existant ne doit être contourné)
- `dotnet test server/` et `npm test --prefix web` doivent rester verts (web : 165 tests).
- Nouveaux tests d'intégration : `X-Trace-Id` présent sur 200 et sur erreur ;
  ProblemDetails porte `traceId` ; `/api/journal-client` accepte un lot, refuse un corps
  surdimensionné, se fait limiter en débit.
- Nouveaux tests unitaires : la table `GetLevel` (`/api/sante` → Verbose, 500 → Error).
- Nouveaux tests Vitest : tampon/vidange/beacon de `journal.ts` et absence de récursion ;
  `api.ts` lit `X-Trace-Id` dans `ApiError` ; `GardeErreur` attrape un enfant qui lève.
- `npx playwright test` en fin de parcours.

**Manuel, en dev** (backend redémarré par moi : `pkill -f HouseOs.Api` puis
`dotnet run --project server/HouseOs.Api` en arrière-plan, Seq monté via compose)
1. Se connecter, compléter une tâche **récurrente**, ouvrir Seq.
2. Confirmer sur un même `TraceId` : la ligne de requête (statut + durée), la durée du
   `SaveChangesAsync`, celle de la diffusion du geste, et la piste client (clic →
   requête → évènement SignalR).
3. Confirmer que `/api/sante` et les `SELECT 1` **n'apparaissent pas** au niveau
   Information.
4. Couper Seq, faire un geste, le relancer : les évènements tamponnés doivent arriver.
5. **Annuler toute complétion faite pour tester** — les données de dev sont les vraies
   données du foyer (l'annulation supprime aussi l'occurrence suivante matérialisée).

**Prod** — `free -m` sur le LXC 105 avant, push GitHub, puis
`ssh proxmox "pct exec 105 -- bash -lc 'cd /opt/house-os && git pull && docker compose up -d --build'"` ;
vérifier `/api/sante` (200 + `db: ok`), le bundle servi, l'UI Seq sur `:8081`, l'arrivée
des évènements, et `free -m` après.

**Ensuite** : attendre l'incident, lire la trace de la requête fautive, rouvrir l'enquête.

Rien n'est committé sans demander.

## Écarts à l'exécution (append-only)

- **`controlLevelSwitch` retiré du sink Seq.** Le plan le prévoyait pour piloter la
  verbosité depuis l'UI de Seq. Mesuré à la vérification : ce paramètre laisse Seq
  imposer son niveau minimum au sink, et les durées de phase en Debug sortaient en
  console **sans jamais atteindre Seq** — exactement les évènements que cette feature
  existe pour collecter. Les niveaux restent dans la section `Serilog` d'appsettings.
- **Statut 101 exclu de l'alerte de lenteur.** Non prévu : une connexion WebSocket au
  hub vit aussi longtemps que l'onglet, et sortait donc en Warning « requête lente » à
  chaque session normale. Couvert par un test.
- **`Hote` (serveur) et `origine` (navigateur) ajoutés.** Manquaient pour distinguer un
  accès direct, le proxy Vite et NPM — trois chemins réseau où le même geste peut
  échouer autrement. Découvert en enquêtant sur un 403 de négociation pendant la
  vérification.
- **Port d'ingestion 5342 au lieu de 5341** : le Mac d'Alain héberge déjà un Seq
  personnel sur 5341, les deux doivent coexister.
- **`Journalisation:Seq:Url` vidé dans `HouseOsFactory`** : sinon la suite
  d'intégration tentait de joindre un collecteur local à chaque test.
