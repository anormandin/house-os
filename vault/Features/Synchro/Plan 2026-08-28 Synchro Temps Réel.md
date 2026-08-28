---
type: plan
status: executed
date: 2026-08-28
feature: "[[Synchro]]"
---

# Plan 2026-08-28 Synchro Temps Réel

Canal serveur→client pour rafraîchir les UI ouvertes. Deux tiers : grossier
(intercepteur EF, invalidation) et fin (publications explicites, toasts). Décisions
gouvernantes : [[D-2026-08-28 Synchro Temps Réel Par SignalR]] et
[[D-2026-08-28 Événements Par Intercepteur EF]].

## Backend

- [x] Slice `Features/Synchro/` : `EvenementSynchro` (contrat), `ModulesSynchro` (noms
      partagés avec le web), `SynchroHub` (hub sans méthode client→serveur),
      `IDiffuseurSynchro` + `DiffuseurSynchro` (diffusion qui avale ses pannes).
- [x] `IntercepteurSynchro` : `ISaveChangesInterceptor` + `IDbTransactionInterceptor`.
      Capture avant sauvegarde, diffusion après ; report au commit si transaction
      ouverte ; table type CLR → module ; types possédés rattachés à leur propriétaire ;
      table de jointure `TacheDocuments` rattachée à `taches`.
- [x] `Program.cs` : `AddSignalR()`, diffuseur et intercepteur en singleton,
      `AddInterceptors` sur le `AddDbContext`, `MapHub` **avant** le fallback SPA.
- [x] Tier fin dans `OperationsTaches.CompleterAsync` / `AnnulerCompletionAsync`
      (diffuseur en paramètre optionnel) ; le nom d'affichage de l'acteur est résolu là,
      les deux appelants n'ayant que l'id.
- [x] Appelants : `TachesEndpoints` (`source: web`), `OutilsTaches` (`source: mcp`),
      et une **unique** publication agrégée pour `creer_taches` après son `SaveChanges`.

## Frontend

- [x] `@microsoft/signalr` ajouté ; proxy Vite `/hubs` en forme longue avec `ws: true`
      (la forme courte ne relaie pas l'upgrade WebSocket).
- [x] `lib/synchro.ts` : table module → clés, réutilisant `invaliderAutourOccurrences` ;
      coalescence 300 ms ; règle du toast (acteur ≠ moi **ou** source mcp) ; libellés.
- [x] `hooks/useSynchro.ts` : connexion avec reconnexion automatique, invalidation en
      bloc à la reconnexion, démontage propre.
- [x] `App.tsx` : branche authentifiée extraite en `AppConnectee` pour que le hook
      n'existe qu'une session établie.
- [x] `lib/toast.ts` : file de 3, `actionLibelle`/`onAction` optionnels, fusion comptée
      en place, **emplacement exclusif** pour mes propres gestes.

## Tests

- [x] Backend : table type→module sur les 17 entités, dédoublonnage, exclusion
      `Utilisateur`, transaction (commit / rollback / sauvegardes multiples), diffuseur
      qui lève.
- [x] Backend : lot de 10 via MCP = **un seul** événement fin compté ; lot refusé = rien ;
      complétion MCP porte `source: mcp`.
- [x] Intégration : les deux tiers sur une complétion HTTP, annulation, autre module,
      écriture refusée ; **vrai client SignalR** sur le TestServer (négociation à travers
      l'antiforgery global) et refus sans cookie.
- [x] Web : mapping, coalescence, règle du toast, fusion, file, fenêtre de fusion.

## Vérification

- [x] Suites : backend 532 → 571, web 131 → 150, typecheck propre.
- [x] Négociation réelle : 401 sans cookie, 200 avec, WebSockets offert.
- [x] Client WebSocket réel branché pendant que MCP crée 10 tâches → exactement deux
      événements (un grossier, un fin `nombre: 10`).
- [x] Complétion MCP → `source: mcp`, acteur Ariane. Rafale de 3 complétions HTTP →
      3 paires d'événements, coalescées côté client.
- [x] Données de test supprimées du dev.

## Corrections en cours de route

- [x] L'intercepteur laissait remonter une panne de diffusion et faisait échouer une
      écriture déjà commitée — garde ajoutée dans l'intercepteur lui-même (le diffuseur
      de production avalait déjà, mais la robustesse appartient au chemin d'écriture).
- [x] La file de toasts empilait mes propres gestes : le « Annuler » d'une complétion
      déjà défaite restait cliquable et rejouait la mutation. D'où l'emplacement exclusif.
