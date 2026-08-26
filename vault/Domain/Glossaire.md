---
type: domain
last-verified: 2026-08-26
verified-against: d52fad1
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

## Document

Un papier de la maison numérisé (acte, assurance, facture, manuel, photo…) :
fichier sur disque + titre + catégorie fixe + liens optionnels vers un équipement
ou une zone + dossier libre + échéance optionnelle. A absorbé l'ancienne « pièce
jointe » d'équipement ([[D-2026-08-24 Document Unifié Sur Disque]]). Voir
[[Documents]].

## Dossier (document)

Étiquette texte libre de classement d'un document (« 17 rue de la Colline »,
« Déménagement »…), autocomplétée depuis les valeurs existantes. Pas une entité :
pas de CRUD, « sans dossier » est un état légitime. Distinct de la Zone (pièce).
Voir [[D-2026-08-26 Dossier De Document]].

## Compte à rebours

Un événement attendu du foyer (déménagement, visite, voyage…) : titre + date cible +
icône maison. Entité du foyer, pas d'assignation ; affiché en « dodos » dans
Aujourd'hui et publié dans les flux iCal. Voir [[Comptes À Rebours]].

## Flux externe

Un calendrier ICS du monde extérieur (collectes Recollect, calendrier scolaire…)
suivi par l'app : URL + fenêtre de lecture, rafraîchi par worker. Ses **événements
externes** sont affichés seulement — jamais couplés aux tâches
([[D-2026-08-24 Flux ICS Dans L'app Affichage Seul]]). Voir [[Flux Externes]].

## Appareil

(Phase 3) Un device IoT enregistré — capteur, bouton, écran — découvert via MQTT.
Distinct d'Équipement : l'appareil est connecté, l'équipement est un actif passif.
Un appareil pourra un jour déclencher l'échéance d'une tâche (filtre après N heures
de soufflerie).
