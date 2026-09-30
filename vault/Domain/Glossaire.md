---
type: domain
last-verified: 2026-09-30
verified-against: 6d1fda0
tags: []
---

# Glossaire

Vocabulaire du domaine House OS. Les termes du domaine restent en français dans le
code (voir [[Conventions]]).

## Utilisateur

Un des deux comptes du foyer. Attribue et complète des tâches. Jamais plus que des
adultes du couple (pas de comptes enfants, jamais). Porte un jeton iCal et une adresse
de courriel facultative, alimentée par la configuration — c'est là qu'arrive la lettre
du matin ([[D-2026-09-21 Adresse De Courriel Sur L'Utilisateur]]). Voir [[Auth]].

## Zone

Une pièce ou un espace extérieur (cuisine, sous-sol, cour arrière…). Liste plate,
sans hiérarchie ([[D-2026-08-23 Zones Plates]]) : un type `Interieur` | `Exterieur`
et un ordre. Situe les tâches, les équipements et les documents.

## Équipement

Un actif de la maison (fournaise, chauffe-eau, tondeuse…) : catégorie, marque,
modèle, série, date d'achat, garantie, manuels, specs libres. Voir [[Équipements]].

## Catégorie d'équipement

Ce qu'est un équipement, dans une liste fermée (`CategorieEquipement` : chauffage, eau
chaude, plomberie, électricité, toiture, extérieur, petits moteurs, électroménager,
véhicule, autre) ; nulle = pas encore classé. C'est la clé des packs d'entretien.
Voir [[D-2026-09-28 Catégorie D'équipement En Liste Fermée]].

## Pack d'entretien

Un lot de tâches récurrentes proposées — jamais imposées — pour une catégorie
d'équipement, plus le **programme de la maison** (les gestes qui ne tiennent à aucun
équipement). De la donnée d'édition lue d'un fichier au démarrage, pas une entité en
base : l'adoption crée de vraies tâches. Voir [[Emménagement V2]] et
[[D-2026-09-28 Packs D'entretien En Fichier De Données]].

## Tâche

La *définition* d'un travail à faire — titre, zone, équipement optionnel, spec de
récurrence, stratégie d'assignation (`Fixe` | `Alternance` | `MoinsLAFait`). Ne pas
confondre avec l'occurrence. Voir [[Tâches]].

## Échéance ferme

Drapeau posé à la main sur une tâche dont la date vient du dehors et ne se négocie pas
(notaire, livraison payée, date légale). Jamais déduit. Le journal mural ne relègue
jamais une échéance ferme ([[D-2026-09-20 Échéance Ferme Explicite Sur La Tâche]]).

## Spec de récurrence

Le régime de répétition d'une tâche : mode `Ponctuelle` | `Fixe` (jours de semaine,
jour du mois, annuelle) | `Intervalle` (depuis la complétion), fenêtre saisonnière
optionnelle, flag rollover. Stockée type + paramètres, jamais RRULE. Voir
[[D-2026-08-23 Moteur De Récurrence Trois Modes]].

## Occurrence

Une *instance* planifiée d'une tâche : date d'échéance, assigné, statut
(`EnAttente` | `Completee` | `Passee`), complétée par/le. La prochaine occurrence est
matérialisée à la complétion.

## Journal de complétion

Table d'historique (`EntreeJournal` : qui, quand, notes, coût) — jamais une simple
date mutée. La photo prévue au design d'origine n'a pas été bâtie (as of 2026-09).
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

## Boîte à classer

L'état d'un document arrivé tout seul (par courriel) tant qu'un humain n'a pas
confirmé sa fiche (`AClasser`). Voir [[D-2026-09-02 Boîte À Classer Des Documents]].

## Import de courriel

La trace d'un courriel relevé du dépôt (`ImportCourriel`) : ce qui en est sorti
(`Importe` | `Ignore` | `Erreur`), et la garde de déduplication par Message-ID. Voir
[[Courriel Entrant]].

## Compte à rebours

Un événement attendu du foyer (déménagement, visite, voyage…) : titre + date cible +
icône maison. Entité du foyer, pas d'assignation ; affiché en « dodos » dans
Aujourd'hui et publié dans les flux iCal. Voir [[Comptes À Rebours]].

## Flux externe

Un calendrier du monde extérieur (collectes, calendrier scolaire, ville…) géré dans
l'app, de type `Collecte` | `Ecole` | `Municipal` | `Autre`. Deux sources : un
abonnement **ICS** (URL + fenêtre de lecture, rafraîchi par worker) ou un flux
**poussé**, sans URL, dont un programme extérieur remplace les événements par l'API
avec sa propre clé ([[D-2026-09-20 Flux Externe Poussé]]). Ses **événements
externes** sont affichés seulement — jamais couplés aux tâches
([[D-2026-08-24 Flux ICS Dans L'app Affichage Seul]]). Voir [[Flux Externes]].

## Jeton iCal

Secret de 48 caractères hexadécimaux (192 bits) qui protège le flux iCal d'une
personne : il EST l'URL (`/ical/{jeton}.ics`), seule surface exposée à Internet
([[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]]). Rotation self-service
depuis « Mon calendrier » ou l'outil `mon_flux_ical` (action `regenerer`) :
l'ancienne URL meurt immédiatement.

## Fonds de prévoyance

Le compte bancaire réel dédié à l'argent de la maison (`CompteBudget`) : dépôt
mensuel, retraits pour taxes, équipements et projets. Un seul en v1 ; solde
courant dérivé du solde initial ancré + transactions importées. Voir [[Budget]].

## Enveloppe

Partition virtuelle du fonds de prévoyance : type `Equipement` | `Taxes` |
`Projet` | `Reserve`, cible optionnelle, lien optionnel vers une tâche ou un
équipement (échéance alors dérivée). Solde = Σ mouvements. Invariant : solde du
compte = Σ enveloppes + non affecté. Voir [[Budget]].

## Mouvement d'enveloppe

Écriture datée et signée du journal d'une enveloppe (`Provision` | `Retrait` |
`Ajustement` | `Transfert`), avec liens optionnels vers une transaction
bancaire et une entrée de journal de complétion. Jamais un solde muté.

## Transaction bancaire

Ligne importée du compte fonds de prévoyance (CSV/OFX en v1) : `Nouvelle`
jusqu'au rapprochement (`Liee`) ou au rejet (`Ignoree`) ; dédupliquée par
FITID/hash.

## Rapprochement

L'acte de lier une transaction bancaire à une enveloppe (créant le mouvement
correspondant) et, optionnellement, à une entrée du journal de complétion.

## Provision

Montant mensuel suggéré pour une enveloppe, toujours calculé à la lecture par
`MoteurProvision` — jamais stocké. La somme des provisions donne le virement
mensuel suggéré.

## Fait (fonds de tiroir)

Une petite chose vraie que la maison sait d'elle-même ou du monde autour, datée,
classée dans une des six familles (ciel, climat, maison, calendrier, ville, hasard)
et sans mise en forme (`FaitDeTiroir`). Son score — rareté × fraîcheur × pertinence —
décide s'il sort aujourd'hui. Voir [[Fonds De Tiroir]].

## Normales climatiques

Les repères du lieu (premier gel, première neige, mois le plus arrosé…) calculés une
fois l'an depuis l'archive météo et matérialisés avec les coordonnées du calcul : un
changement de coordonnées les invalide. Voir
[[D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo]].

## Édition

Le journal mural d'une journée, matérialisé une fois au créneau du matin : rang,
manchette, chapeau, corps, rubriques et faits publiés, figés pour la journée ; écrit
par un LLM ou, en repli, par gabarit. Les occurrences, la météo et l'heure restent
vivantes à chaque rendu. Voir [[Journal De La Maison]] et
[[D-2026-09-20 Une Édition Par Jour Matérialisée]].

## Lettre du matin

Le courriel que la maison écrit chaque matin à ses habitants : une ligne par date,
marquée envoyée après l'envoi (le garde-fou contre le double envoi). Même matière
que l'édition, écrite à part. Voir [[Lettre Du Matin]].

## Appareil d'affichage

Un écran e-ink enrôlé (`AppareilAffichage`) : son identité selon le protocole TRMNL
(MAC, identifiant court, clé) et sa dernière télémétrie (pile, signal, firmware,
dernier contact). Une ligne par appareil, pas d'historique. Voir [[Affichage E-ink]]
et [[D-2026-09-03 Registre Des Appareils D'affichage]].

## Appareil

(Phase 3, pas encore bâti) Un device IoT enregistré — capteur, bouton — découvert via
MQTT. Distinct d'Équipement : l'appareil est connecté, l'équipement est un actif
passif. Distinct aussi de l'appareil d'affichage, qui existe déjà et ne passe pas par
MQTT. Un appareil pourra un jour déclencher l'échéance d'une tâche (filtre après
N heures de soufflerie).

## Ancres de code

Les entités vivent dans `server/HouseOs.Api/Domaine/` ; pour vérifier un terme :

- `server/HouseOs.Api/Domaine/Utilisateur.cs`, `server/HouseOs.Api/Domaine/Zone.cs`,
  `server/HouseOs.Api/Domaine/Equipement.cs` (catégorie).
- `server/HouseOs.Api/Domaine/Tache.cs`, `server/HouseOs.Api/Domaine/SpecRecurrence.cs`,
  `server/HouseOs.Api/Domaine/Occurrence.cs`, `server/HouseOs.Api/Domaine/EntreeJournal.cs`,
  `server/HouseOs.Api/Domaine/Assignation.cs`.
- `server/HouseOs.Api/Domaine/Document.cs`, `server/HouseOs.Api/Domaine/ImportCourriel.cs`.
- `server/HouseOs.Api/Domaine/CompteARebours.cs`, `server/HouseOs.Api/Domaine/FluxExterne.cs`.
- `server/HouseOs.Api/Domaine/CompteBudget.cs`, `server/HouseOs.Api/Domaine/Enveloppe.cs`,
  `server/HouseOs.Api/Domaine/MouvementEnveloppe.cs`,
  `server/HouseOs.Api/Domaine/TransactionBancaire.cs`,
  `server/HouseOs.Api/Domaine/MoteurProvision.cs`.
- `server/HouseOs.Api/Domaine/Editorial/Edition.cs`,
  `server/HouseOs.Api/Domaine/Lettre/LettreDuMatin.cs`,
  `server/HouseOs.Api/Domaine/Meteo/NormalesClimatiques.cs`,
  `server/HouseOs.Api/Domaine/AppareilAffichage.cs`.
- `server/HouseOs.Api/Features/FondsDeTiroir/FaitDeTiroir.cs`,
  `server/HouseOs.Api/Features/Entretien/PacksEntretien.cs`.
