---
type: domain
last-verified: 2026-08-23
verified-against: 7b407d4
tags: []
---

# Glossaire

Vocabulaire du domaine House OS. Les termes du domaine restent en français dans le
code (voir [[Conventions]]).

## Utilisateur

Un des deux comptes du foyer. Attribue et complète des tâches. Jamais plus que des
adultes du couple (pas de comptes enfants, jamais).

## Zone

Une pièce ou un espace extérieur (cuisine, sous-sol, cour arrière…). Hiérarchie
légère intérieur/extérieur. Situe les tâches et les équipements.

## Équipement

Un actif de la maison (fournaise, chauffe-eau, tondeuse…) : marque, modèle, série,
date d'achat, garantie, manuels, specs libres. Voir [[Équipements]].

## Tâche

La *définition* d'un travail à faire — titre, zone, équipement optionnel, spec de
récurrence, stratégie d'assignation. Ne pas confondre avec l'occurrence. Voir
[[Tâches]].

## Spec de récurrence

Le régime de répétition d'une tâche : mode `Ponctuelle` | `Fixe` (jours de semaine,
jour du mois, annuelle) | `Intervalle` (depuis la complétion), fenêtre saisonnière
optionnelle, flag rollover. Stockée type + paramètres, jamais RRULE. Voir
[[D-2026-08-23 Moteur De Récurrence Trois Modes]].

## Occurrence

Une *instance* planifiée d'une tâche : date d'échéance, statut, complétée par/le.
La prochaine occurrence est matérialisée à la complétion.

## Journal de complétion

Table d'historique (qui, quand, notes, coût, photo) — jamais une simple date mutée.
Alimente statistiques, équité d'assignation et futur ordonnancement adaptatif.

## Fenêtre saisonnière

Plage mois-jour (ex. 1er mai → 31 octobre) restreignant la génération d'occurrences
d'une tâche, combinable avec les modes fixe et intervalle.

## Pièce jointe

Un fichier attaché à un équipement (manuel PDF, photo). Métadonnées en base, octets
sur disque ([[D-2026-08-23 Fichiers Sur Disque]]).

## Appareil

(Phase 3) Un device IoT enregistré — capteur, bouton, écran — découvert via MQTT.
Distinct d'Équipement : l'appareil est connecté, l'équipement est un actif passif.
Un appareil pourra un jour déclencher l'échéance d'une tâche (filtre après N heures
de soufflerie).
