---
name: planifier-taches
description: Planifier un lot de tâches maison (ex. déménagement, corvées saisonnières) en conversation, puis les pousser dans House OS via les outils MCP house-os. Utiliser dès qu'Alain ou Ariane veut planifier, organiser ou créer plusieurs tâches, ou « envoyer le plan dans l'app ».
---

# Planifier des tâches et les pousser dans House OS

Workflow : converser → tableau récapitulatif → **confirmation humaine** → un seul
`creer_taches` en lot → vérification.

## 1. Extraire les tâches de la conversation

Discute librement pour dégager la liste. Pour chaque tâche, détermine :

- **titre** — court, actionnable, en français.
- **echeance** — toujours résolue en date explicite `YYYY-MM-DD` (année comprise).
  Jamais de « la semaine prochaine » non résolu : convertis à partir de la date du
  jour et confirme si ambigu. Une tâche peut être sans échéance.
- **assigneA** — `alain`, `ariane`, ou vide (non assignée). Ne devine pas : demande.
- **notes/description** — seulement si ça aide vraiment.
- **recurrence** — seulement si la tâche revient (la plupart des tâches d'un plan
  ponctuel n'en ont pas).

Si des tâches visent une pièce précise, appelle `lister_zones` et utilise les vrais
ids — **ne jamais inventer un `zoneId` ou `equipementId`**.

## 2. Tableau récapitulatif obligatoire

Avant toute écriture, présente le plan complet :

| # | Titre | Échéance | Assigné | Notes |
|---|-------|----------|---------|-------|

et demande une confirmation explicite (« Je pousse ces N tâches dans House OS ? »).
Ajuste tant que ce n'est pas approuvé.

## 3. Pousser en un seul lot

- `agirComme` = la personne qui parle (demande si tu ne sais pas qui est au clavier).
- Un **seul** appel `creer_taches` avec toutes les tâches — l'outil est tout-ou-rien :
  si une tâche est invalide, rien n'est créé et les erreurs reviennent par index.
  Corrige et renvoie le lot complet.

## 4. Vérifier et rapporter

- `lister_occurrences` avec `filtre: "avenir"` (et `"aujourdhui"` si des tâches sont
  déjà échues) pour confirmer que tout est là.
- Rapporte : nombre de tâches créées, prochaines échéances, et rappelle que la vue
  Aujourd'hui de l'app et les flux iCal (`mon_flux_ical`) reflètent le plan.

## Autres opérations courantes

- Marquer fait : `completer_occurrence` avec `agirComme` = qui l'a réellement fait
  (ça nourrit les statistiques d'équité — ne jamais deviner).
- Ajuster une tâche : `gerer_tache` (`obtenir` → `modifier`).
- Compte à rebours d'un événement : `gerer_comptes_a_rebours`.
