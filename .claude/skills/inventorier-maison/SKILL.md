---
name: inventorier-maison
description: Semer l'inventaire d'une maison dans House OS à partir d'un rapport d'inspection (PDF local) ou d'une visite pièce par pièce, puis proposer et adopter les packs d'entretien de chaque équipement et le programme de la maison, via les outils MCP house-os. Utiliser quand un membre de la maison veut inventorier la maison, entrer les équipements d'un rapport d'inspection, ou « mettre en place l'entretien » d'un équipement ou de la maison.
---

# Inventorier la maison et mettre l'entretien en place

Workflow : lire la source → tableau des équipements → **confirmation humaine** →
`gerer_equipement` pour chacun → `proposer_entretiens` par équipement classé et pour la
maison → **confirmation** → `adopter_entretiens` → vérification. Même contrat que
`planifier-taches` : rien ne s'écrit sans un « oui » explicite, jamais d'id inventé.

Décision de fond : le semis passe par ce skill, pas par un import dans l'app
(vault : `D-2026-09-28 Semis De La Maison Par Skill MCP`). Les fichiers restent web
seulement : le PDF se lit depuis un chemin local que la personne donne.

## 1. Lire la source

- **Rapport d'inspection** : demande le chemin local du PDF et lis-le (outil Read,
  par pages). Relève chaque système décrit : chauffage (fournaise, thermopompe,
  plinthes, échangeur d'air), eau chaude, plomberie (entrée d'eau, pompe de puisard,
  adoucisseur, fosse septique, puits), électricité (panneau, génératrice), toiture,
  extérieur (patio, entrée, clôture), avec marque, modèle, année ou âge probable,
  numéro de série et remarques de l'inspecteur quand ils y sont.
- **Sans rapport** : fais le tour pièce par pièce en conversation, une pièce à la fois.
- Appelle `lister_equipements` d'abord : ce qui existe déjà ne se recrée pas — on le
  **classe** (action `modifier` avec la fiche complète et la `categorie`) s'il n'a pas
  encore de catégorie. Appelle `lister_zones` pour les vrais `zoneId` ; jamais d'id
  inventé — une pièce absente se crée avec `gerer_zone` seulement si la personne le
  demande.

## 2. Tableau des équipements (obligatoire avant d'écrire)

Une catégorie parmi la liste fermée : `Chauffage`, `EauChaude`, `Plomberie`,
`Electricite`, `Toiture`, `Exterieur`, `PetitsMoteurs`, `Electromenager`, `Vehicule`,
`Autre` (« Autre » n'a pas de pack d'entretien — réserve-la au reste).

| # | Nom | Catégorie | Pièce | Marque / modèle | Notes (âge, remarques du rapport) | Déjà présent ? |
|---|-----|-----------|-------|-----------------|-----------------------------------|----------------|

Les notes gardent ce que le rapport dit d'utile (« fournaise 2010, à remplacer d'ici
5 ans », « anode à vérifier ») ; l'année probable va dans `specs` (`{"annee": "2010"}`)
quand elle est connue. Demande : « Je crée ces N équipements et j'en classe M ? »
Ajuste tant que ce n'est pas approuvé.

## 3. Écrire les équipements

- `gerer_equipement` `creer` par équipement (une fiche complète par appel : nom,
  zoneId, categorie, marque, modele, numeroSerie, dateAchat, finGarantie, notes,
  specs). Pour un existant à classer : `obtenir_equipement` puis `modifier` avec la
  fiche entière et la catégorie — modifier remplace tout.
- Une catégorie hors liste est refusée ; corrige et renvoie.

## 4. L'entretien

- Pour **chaque équipement classé** (hors `Autre`) : `proposer_entretiens` avec son
  `equipementId`. Présente les propositions en tableau (titre, rythme, fenêtre,
  stratégie), en marquant « déjà présent » ce que l'outil renvoie ainsi. Demande
  lesquelles garder : la personne décoche ce qui ne s'applique pas (pas de foyer au
  bois → pas de ramonage).
- Puis le **programme de la maison** : `proposer_entretiens` sans `equipementId`, même
  tableau, même confirmation.
- `adopter_entretiens` avec `agirComme` = la personne au clavier, les `cles` retenues
  et l'`equipementId` (ou rien pour la maison). L'outil est tout-ou-rien : une clé
  déjà présente ou inconnue refuse le lot, corrige et renvoie. Un appel par équipement,
  un pour la maison.

## 5. Vérifier et rapporter

- `lister_equipements` (catégories en place) et `lister_taches` ou
  `lister_occurrences` `filtre: "avenir"` pour voir les échéances créées.
- Rapporte : équipements créés / classés, tâches adoptées par équipement et pour la
  maison, les premières échéances. Rappelle que les fiches Équipements et la page Tâches
  montrent tout, et que « Proposer les entretiens » sur une fiche refait la même chose
  plus tard.
