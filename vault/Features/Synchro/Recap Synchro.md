---
type: recap
date: 2026-08-28
feature: "[[Synchro]]"
plan: "[[Plan 2026-08-28 Synchro Temps Réel]]"
---

# Recap Synchro

Canal SignalR (`/hubs/synchro`, authentifié par le cookie de session, diffusion à tous)
livré conforme au plan. Le tier grossier est dérivé automatiquement des sauvegardes par
un intercepteur EF — HTTP, MCP et les quatre services d'arrière-plan sont couverts sans
qu'aucune slice ne publie ; le tier fin (complétion, annulation, lot MCP) porte l'acteur
et un libellé pour les toasts.

Deux corrections nées des tests plutôt que du plan :

- L'intercepteur laissait remonter une panne de diffusion, ce qui aurait fait échouer une
  écriture **déjà commitée**. La garde appartient au chemin de `SaveChanges`, pas
  seulement à l'implémentation de production — corrigé là.
- Passer les toasts en file a d'abord cassé l'undo : le toast « Tâche complétée /
  Annuler » survivait à sa propre annulation et un second clic rejouait la mutation
  (409). D'où l'emplacement exclusif pour mes propres gestes, les annonces distantes
  s'empilant à côté. Deux tests existants avaient signalé la régression.

La question « dix tâches d'un coup = dix notifications ? » a été posée en cours de route
et a ajouté un troisième mécanisme au plan initial : dédoublonnage par sauvegarde
(déjà acquis — `creer_taches` est tout-ou-rien avec un seul `SaveChanges`), agrégation à
la source pour le lot, et **fusion comptée en place** côté client pour le cas restant
(trois cases cochées coup sur coup). Vérifié en réel : dix tâches poussées par MCP
produisent exactement deux événements.

Suites : backend 532 → 571, web 131 → 150. Vérifié de bout en bout avec un vrai client
WebSocket pendant des écritures MCP et HTTP.

Reste ouvert : rien de bloquant. Le jour où une vue e-ink/tablette sans cookie devra
écouter, il faudra un jeton d'URL comme pour le flux iCal.
