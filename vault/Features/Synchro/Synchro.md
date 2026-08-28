---
type: feature
status: implemented
last-verified: 2026-08-28
verified-against: 8cc560c
tags: []
---

# Synchro

## Intention

Un onglet ouvert doit dire la vérité. Avant, la fraîcheur reposait sur `staleTime`
(30 s) plus un refetch au retour de focus : quand Ariane complétait une tâche, l'écran
d'Alain restait faux jusqu'à ce qu'il reclique dedans, et surtout **une écriture du
serveur MCP était totalement invisible** — Claude pousse un plan de dix tâches, aucun
navigateur ne bronche. Les quatre services d'arrière-plan (météo, phrase du jour, flux
ICS, rollover) écrivaient pareillement dans le noir.

La synchro pousse donc du serveur vers les clients de quoi rafraîchir ce qui est
affiché, et annonce les gestes qui ne viennent pas de moi.

## Comportement

**Deux tiers sur un même canal.**

- **Grossier** — quand une sauvegarde réussit, le système diffuse un événement par
  module touché (`{ module }`, sans acteur). Le client invalide les requêtes de ce
  module et refetch. Aucun toast. Dérivé automatiquement du `ChangeTracker`, donc
  couvre HTTP, MCP et l'arrière-plan sans que les slices aient rien à publier.
- **Fin** — quand un geste mérite d'être annoncé (complétion, annulation de
  complétion, création d'un lot MCP), le système diffuse en plus un événement portant
  `genre`, `source` (`web` ou `mcp`), l'acteur, un libellé et un `nombre`.

**Quand un client se connecte** : il reçoit tout ce qui est diffusé tant qu'il est
connecté ; rien n'est persisté, rien n'est rejoué. Un onglet fermé ne rate rien — il
refait ses requêtes en s'ouvrant.

**Quand la connexion se rétablit** après une coupure : le client invalide tout en bloc,
faute de savoir ce qu'il a manqué.

**Quand un événement fin arrive** : il s'annonce par un toast si l'acteur n'est pas moi,
**ou** si la source est `mcp`. Cette seconde condition existe parce que le MCP agit « au
nom de » quelqu'un ([[D-2026-08-24 Clé API Partagée Et AgirComme]]) : sans elle, une
création faite par Claude au nom d'Alain resterait muette dans l'onglet d'Alain, ce qui
est précisément le trou qu'on ferme.

**Quand plusieurs écritures arrivent coup sur coup** — trois cases cochées, dix tâches
poussées d'un lot — le système ne produit ni dix rafraîchissements ni dix toasts :

1. une sauvegarde qui touche dix entités du même module ne diffuse qu'un événement
   (dédoublonnage par module) ;
2. un lot MCP diffuse un seul événement fin portant son décompte ;
3. côté client, les invalidations sont regroupées sur ~300 ms, et les toasts de même
   `(genre, acteur, source)` arrivés à moins de ~2 s fusionnent **en place** — le
   premier toast s'affiche tout de suite puis se réécrit en « … a complété 3 tâches »
   et son minuteur repart.

**Quand une écriture se fait dans une transaction explicite** (l'ingestion météo),
rien n'est diffusé avant le commit : diffuser au `SaveChanges` ferait refetcher le
client sur des données pas encore visibles, et aucun second événement ne suivrait.

**Quand la diffusion échoue** (hub indisponible), l'écriture réussit quand même : la
synchro est un confort, jamais une raison de perdre une écriture.

**Toasts locaux vs annonces** : mes propres gestes occupent un emplacement exclusif
(une seule action inverse « Annuler » valide à la fois — un « Annuler » périmé rejouerait
une mutation déjà défaite) ; les annonces distantes s'empilent à côté, trois au plus.

## Hors périmètre

- **Notifications persistantes** — pas de boîte de réception, pas de lu/non-lu, pas de
  rejeu de ce qui a été manqué. Le canal « me rejoindre hors de l'app » reste le flux
  iCal ([[D-2026-08-23 Notifications Par Flux iCal]], toujours en vigueur).
- **Push web** — le service worker a été retiré volontairement
  ([[D-2026-08-25 Retrait Du Service Worker]]) ; rien ici ne le réintroduit.
- **Ciblage par personne** — diffusion à tous les clients connectés, sans groupes.
  Deux comptes dans le foyer : le ciblage serait de la plomberie sans usage.
- **Sens client→serveur** — le hub n'expose aucune méthode appelable ; toute écriture
  passe par l'API REST.
- **Exposition publique** — le hub reste sur le tailnet/LAN. Le Funnel ne publie que
  `/ical` ([[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]]) et ne doit pas
  gagner le chemin du hub.
- **Outil MCP** — rien à piloter, donc pas d'outil ; la règle de parité MCP ne
  s'applique pas ici.
- **Régénération de la phrase du jour** — la synchro la diffuse quand elle arrive, elle
  ne change pas son horaire ([[Titre D'humeur]]).

## Décisions

- [[D-2026-08-28 Synchro Temps Réel Par SignalR]] — SignalR plutôt que SSE ou WebSocket
  brut ; portée « live sync » distincte des notifications iCal.
- [[D-2026-08-28 Événements Par Intercepteur EF]] — le tier grossier est dérivé
  automatiquement des sauvegardes plutôt que publié à la main dans chaque slice.

## Ancres de code

- `server/HouseOs.Api/Features/Synchro/EvenementSynchro.cs` — le contrat diffusé.
- `server/HouseOs.Api/Features/Synchro/ModulesSynchro.cs` — noms de modules, partagés
  avec le web.
- `server/HouseOs.Api/Features/Synchro/IntercepteurSynchro.cs` — tier grossier : table
  type CLR → module, dédoublonnage, report au commit de transaction.
- `server/HouseOs.Api/Features/Synchro/SynchroHub.cs` — le hub (chemin `/hubs/synchro`).
- `server/HouseOs.Api/Features/Synchro/DiffuseurSynchro.cs` — diffusion réelle, avale
  ses pannes.
- `server/HouseOs.Api/Features/Taches/OperationsTaches.cs` — tier fin des complétions.
- `server/HouseOs.Api/Features/Mcp/OutilsTaches.cs` — tier fin des écritures MCP
  (lot agrégé).
- `web/src/lib/synchro.ts` — table module → clés à invalider, coalescence, règle du
  toast, libellés.
- `web/src/hooks/useSynchro.ts` — connexion, reconnexion, montée des toasts.
- `web/src/lib/toast.ts` — file de toasts, emplacement exclusif, fusion comptée.

## Historique

[[Plan 2026-08-28 Synchro Temps Réel]] · [[Recap Synchro]]
