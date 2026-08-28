---
type: decision
status: accepted
date: 2026-08-28
feature: "[[Synchro]]"
tags: []
---

# Événements Par Intercepteur EF

## Contexte

La synchro devait couvrir **tous** les modules, pas seulement les tâches (choix de
l'utilisateur). Or le backend n'avait aucune infrastructure d'événements : ni médiateur,
ni événements de domaine, ni bus — que des appels directs de handlers avec
`SaveChangesAsync` pour seul commit. Il fallait décider comment une écriture devient un
événement, sachant qu'il y a deux chemins d'écriture (HTTP et MCP) convergeant sur les
mêmes opérations, plus quatre `BackgroundService` qui écrivent sans requête HTTP.

## Options considérées

- **Publication explicite partout** — un appel après chaque `SaveChanges`, dans chaque
  endpoint, chaque outil MCP, chaque service. Honnête et lisible, mais ~25 points de
  greffe et surtout une **règle perpétuelle** (« toute nouvelle slice doit publier »)
  qui échoue *en silence* quand on l'oublie : l'UI ne se rafraîchit simplement plus, et
  rien ne le signale.
- **Intercepteur `SaveChanges` d'EF Core** — un seul endroit inspecte le `ChangeTracker`
  et traduit les types d'entités touchés en noms de modules via une table statique. La
  couverture devient automatique et inoubliable ; un module futur coûte une ligne. Prix :
  l'événement dérivé ne porte pas d'acteur (le `ChangeTracker` ne sait pas qui a cliqué).
- **Événements de domaine complets** — les entités accumulent des événements, un
  répartiteur les draine après le commit. La réponse « DDD propre », et de la cérémonie
  pure à cette échelle.

## Décision

**Intercepteur pour le tier grossier, publications explicites pour le tier fin.**
Proposé par l'agent, approuvé par l'utilisateur.

L'intercepteur diffuse `{ module }` — de quoi invalider et refetch, sans acteur, ce qui
suffit puisque ce tier ne produit jamais de toast. Les quelques gestes qui méritent
d'être annoncés (complétion, annulation, lot MCP) publient en plus, explicitement, là
où l'acteur est déjà sous la main.

Oublier une publication fine ne casse rien : le tier grossier rafraîchit quand même,
seul le toast manque. Dégradation gracieuse voulue, à l'inverse du silence de l'option
« tout à la main ».

`OperationsTaches` étant une classe statique appelée ~35 fois dans les tests, le
diffuseur y entre en paramètre **optionnel** : les tests existants restent intacts et
les quatre appels de production le passent. Un paramètre requis aurait imposé 35 lignes
de bruit pour un bénéfice nul, l'intercepteur étant déjà le filet.

## Conséquences

- Une écriture dans une transaction explicite (l'ingestion météo) ne peut pas être
  diffusée au `SaveChanges` : le client refetcherait avant le commit et ne recevrait
  aucun second événement. L'intercepteur implémente donc aussi `IDbTransactionInterceptor`
  et reporte la diffusion au commit ; un rollback ne diffuse rien.
- L'intercepteur vit dans le chemin de `SaveChanges` : il doit avaler les pannes de
  diffusion, sinon une écriture déjà durable échouerait à cause du hub.
- Ajouter une table au `DbContext` sans l'inscrire dans la table de correspondance la
  rend muette. `Utilisateur` en est volontairement absent (l'amorçage au démarrage écrit
  des utilisateurs et n'a aucun client à prévenir).
- La capture se fait **avant** la sauvegarde (les états du `ChangeTracker` sont remis à
  `Unchanged` après), la diffusion après — d'où un intercepteur en deux temps, avec un
  état par instance de `DbContext`.

## Confirmation

`server/HouseOs.Api/Features/Synchro/IntercepteurSynchro.cs` existe et est enregistré
via `AddInterceptors` dans `Program.cs`. Le test paramétré
`IntercepteurSynchroTests.ChaqueEntitePersistee_estRattacheeAUnModule` couvre les 17
entités persistées : ajouter un `DbSet` sans l'inscrire dans la table fait échouer la
suite dès qu'on l'y ajoute. Voir aussi `DansUneTransaction_rienNEstDiffuseAvantLeCommit`,
`UneTransactionAnnulee_neDiffuseRien` et
`UnDiffuseurQuiEchoue_neFaitPasEchouerLEcriture`.
