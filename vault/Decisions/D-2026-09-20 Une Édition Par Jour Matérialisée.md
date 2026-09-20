---
type: decision
status: accepted
date: 2026-09-20
feature: "[[Journal De La Maison]]"
tags: [iot]
---

# D-2026-09-20 Une Édition Par Jour Matérialisée

## Contexte

L'appareil tire `GET /api/display` toutes les **15 minutes** ([[Affichage E-ink]]), donc
~60 fois par jour de veille. L'éditorialiste, lui, écrit un surtitre, une manchette, un
chapeau et deux paragraphes — un texte qu'on ne veut ni voir changer entre deux passages
dans le corridor, ni payer soixante fois.

Mais la maquette 4 montre, à 14 h 30, des tâches cochées sous une manchette du matin :
le texte est figé, **la journée ne l'est pas**. Et le score de fraîcheur (« pénalité si
le widget est sorti récemment ») suppose de **se souvenir de ce qui est sorti**.

[[Titre D'humeur]] résout déjà ce problème pour une phrase de deux lignes : état
structuré → Haiku → repli gabarit, matérialisé aux créneaux fixes dans `PhrasesDuJour`.

## Options considérées

- **Recalculée à chaque rendu** — toujours à jour, mais 60+ appels LLM par jour, une
  manchette qui peut changer d'un passage à l'autre, et **aucune mémoire** pour
  pénaliser un widget sorti hier — donc pas de surprise, qui est tout l'intérêt du score.
- **Deux éditions, matin et soir** (les deux créneaux de [[Titre D'humeur]]) — le mur
  change de voix le soir. Mais le coût et le risque de radotage doublent, et le
  bloc-titre dit « Édition du matin » dans les six maquettes.
- **Une édition par jour, liste vivante** (retenue).

## Décision

Un `BackgroundService` écrit **une édition par jour**, au créneau du matin, et la
matérialise dans une table — même patron que [[Titre D'humeur]]
([[D-2026-08-24 Phrase Du Jour Haiku Matin Et Soir]]).

**Figé pour la journée** : le rang, la sélection et l'ordre des widgets, le surtitre, la
manchette, le chapeau, le corps, les rubriques du sommaire.
**Vivant à chaque rendu** : la liste des occurrences, les cochées, la météo du moment,
l'heure d'impression, la pile.

**Le plancher est réévalué à chaque rendu.** S'il se déclenche après coup — un compte à
rebours qui tombe à zéro, un retard qui passe trois jours, une échéance ferme qui entre
dans la journée — il **redéclenche une édition** plutôt que de laisser la manchette
mentir.

La **mémoire des sept derniers jours** est la lecture des sept dernières lignes de la
table : c'est elle qui alimente la pénalité de fraîcheur et qui évite le radotage de
formulation.

Choix d'Alain, en grillage.

## Conséquences

- Une table d'éditions (T3) : date, textes rendus, **liste des clés de widgets publiées**
  en JSONB, source (LLM ou gabarit), horodatage. Une seule table — la fraîcheur se lit
  dans l'historique des éditions, pas dans un second journal de sorties.
- La cadence d'écriture est **découplée** de la cadence de l'appareil : passer l'écran de
  15 à 30 min ne change rien à l'édition.
- Un appel LLM par jour en régime normal ; le redéclenchement par plancher est rare et
  borné par nature (un compte à rebours ne tombe à zéro qu'une fois).
- Le repli en gabarit est **obligatoire** : si l'API ne répond pas, l'édition s'écrit
  quand même, marquée comme telle, et la journée reste lisible.
- Une édition manquante (serveur redémarré, créneau raté) doit être **rattrapée au
  démarrage**, comme `HumeurService` le fait déjà.

## Confirmation

Une entité d'édition existe sous `server/HouseOs.Api/Domaine/` avec sa migration EF, et
un test vérifie qu'un second rendu dans la même journée **réutilise** l'édition
matérialisée sans appeler le LLM (compteur d'appels sur un client fictif).
