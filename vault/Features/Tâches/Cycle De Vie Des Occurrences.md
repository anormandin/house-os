---
type: reference
last-verified: 2026-09-30
verified-against: 6d1fda0
tags: []
---

# Cycle De Vie Des Occurrences

Sous-note de [[Tâches]] : ce qui arrive à une occurrence une fois posée — annuler une
complétion, passer, reporter, noter après coup —, les courses et cas de bord fermés, et
le bilan hebdo que le journal de complétion alimente. Le moteur de récurrence et le
contrat de la tâche restent dans [[Tâches]] ; les écrans, dans [[Écrans Des Tâches]].

## Comportement

### Annuler, passer, reporter, noter (2026-08-24)

Gouverné par [[D-2026-08-28 Annulation Et Passage D'occurrences]].

- **Annuler une complétion** (deux recours : le toast à action inverse pendant 6 s
  juste après le geste, et le bouton « Annuler » au survol de la rangée verte, qui ne
  s'éteint jamais) :
  journal effacé, occurrence remise en attente avec son échéance d'origine (elle
  redevient éligible au rollover), occurrence suivante matérialisée supprimée.
  Garde-fou : seulement la complétion la plus récente de la tâche, et si la suivante
  est encore en attente (sinon 409).
- **Passer** (récurrentes seulement, icône au survol) : statut `Passee` daté, aucun
  journal, la suivante est générée avec l'échéance qu'aurait donnée une complétion
  aujourd'hui, mais **conserve l'assigné** pour les stratégies tournantes — le tour
  n'a pas été pris ([[D-2026-08-28 Passer Conserve L'assigné]], QA 2026-08-28).
  Un passage n'est pas annulable.
- **Reporter** (icône au survol) : glisse l'échéance de l'occurrence en attente
  (préréglages demain / +2 j / +7 j, ou date libre ≥ aujourd'hui) sans toucher la
  définition.
- **Notes post-hoc** : la rangée verte permet d'ajouter/modifier la note de
  l'entrée de journal (la complétion reste à un clic) ; la note est retournée dans
  le DTO d'occurrence. Parité MCP : action `noter` de `gerer_occurrence`
  (2026-09-30).
- **Confirmation avant suppression** partout (composant partagé « Vraiment ? »,
  extrait de l'idiome Pièces/Équipements) ; le bouton poubelle d'une rangée supprime
  toujours la tâche entière.
- **Erreurs API visibles** : messages ProblemDetails parsés côté client et affichés
  dans une bannière globale (toutes les mutations, 401 exclus).

### Cas de bord durcis (2026-08-25)

- **Courses de complétion fermées en base** ([[D-2026-08-25 Invariants D'occurrence En Base]]) :
  deux clics simultanés donnent un 204 et un 409, jamais deux journaux ni deux
  occurrences suivantes.
- **Validation croisée récurrence × fenêtre saisonnière** : une combinaison qui ne
  planifie jamais rien (annuelle hors fenêtre, 31 avril…) répond 400 à la création
  au lieu de boucler puis 500.
- **Édition** : convertir une tâche sans occurrence en attente (ponctuelle complétée)
  en récurrente matérialise une occurrence — plus de tâche invisible ; en stratégie
  Fixe, la désassignation suit la tâche jusqu'à l'occurrence ; en stratégie
  tournante, l'assigné choisi par la stratégie est conservé.
- **Annulation** : refusée aussi quand un « passer » postérieur a fait avancer la
  chaîne (l'occurrence passée resterait orpheline), et signalée distinctement quand
  l'entrée de journal manque.
- **Listes** : filtre inconnu → 400 (au lieu de tout retourner en silence),
  `faites` exige ses bornes, les complétées sortent des plus récentes (tri avant le
  plafond de 200) ; FK inexistantes (zone/équipement/assigné) et titres trop longs
  → 400 au lieu de 500.

### Bilan hebdo du ménage (2026-08-25)

Gouverné par [[D-2026-08-25 Bilan Hebdo Du Ménage]].

- La carte « L'équipe » d'Aujourd'hui est remplacée par **« Bilan »** : total
  complété cette semaine + mini-histogramme des 8 dernières semaines (lundi au
  dimanche, semaine locale). Le total est celui du **ménage entier** — l'attribution
  individuelle (qui a cliqué) est indicative, jamais un fondement de feature.
- `GET /api/journal/bilan?de=&a=` retourne les instants de complétion dans `[de, a)` ;
  le client agrège par semaine locale (même patron que le filtre `faites`).
- Parité MCP : outil `bilan_taches` (comptes par semaine, heure du serveur).

## Décisions

- [[D-2026-08-28 Annulation Et Passage D'occurrences]] — annuler/passer/reporter,
  sort du journal, garde-fous, et les deux recours d'annulation (toast + survol).
  Supersède [[D-2026-08-24 Annulation Et Passage D'occurrences]], dont la clause
  « pas de toast » était devenue fausse.
- [[D-2026-08-28 Passer Conserve L'assigné]] — passer ne fait plus tourner
  l'assignation (précise la précédente).
- [[D-2026-08-25 Invariants D'occurrence En Base]] — index uniques (une en-attente
  par tâche, une complétion par occurrence), courses converties en 409.
- [[D-2026-08-25 Bilan Hebdo Du Ménage]] — carte Bilan (total par semaine) à la
  place de L'équipe ; attribution individuelle conservée mais indicative.

## Ancres de code

- `server/HouseOs.Api/Features/Taches/OperationsTaches.cs` — `AnnulerCompletionAsync`,
  `PasserAsync`, `ReporterAsync`, `AjouterNotesAsync` : les mêmes opérations pour REST
  et MCP.
- `server/HouseOs.Api/Features/Taches/TachesEndpoints.cs` — routes `/api/occurrences/…`
  et `/api/journal/bilan`.
- `server/HouseOs.Api/Features/Mcp/OutilsTaches.cs` — `gerer_occurrence`, `bilan_taches`.
- `server/HouseOs.Api/Domaine/Occurrence.cs` — complétion, passage, report.
- `web/src/components/OccurrenceListe.tsx` — rangées de tâches (retard, complétée,
  échéance, annuler/passer/reporter/notes).
- `web/src/components/ConfirmerSuppression.tsx` — suppression en deux temps « Vraiment ? ».
- `web/src/lib/erreurs.ts`, `web/src/components/BanniereErreur.tsx` — erreurs globales.
- `server/HouseOs.Tests/Features/Taches/` — tests d'orchestration (harnais Sqlite in-memory).

## Historique

- [[Plan 2026-08-24 Cycle De Vie Des Occurrences]] · [[Recap Cycle De Vie Des Occurrences]]
- Bilan hebdo et durcissement des cas de bord : tranches directes sans plan (2026-08-25),
  voir [[Suite De Tests]].
