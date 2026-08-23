---
type: decision
status: accepted
date: 2026-08-23
feature: "[[Équipements]]"
tags: []
---

# Fichiers Sur Disque

## Contexte

Le module [[Équipements]] promet manuels PDF et photos. Où vivent les octets :
en base (bytea), sur disque, ou remis à plus tard?

## Options considérées

- **Disque + table `PiecesJointes` dès la V1** — le cœur de la valeur du module
  (inventorier la nouvelle maison avec ses manuels).
- **Métadonnées seulement d'abord** (liens URL), fichiers en phase 2 — moins de
  surface avant le 6 octobre.
- **bytea en base** — backups simples mais base obèse et streaming pénible.

## Décision

Fichiers sur disque dès la V1. Table `PiecesJointes` (nom client, chemin disque,
type MIME, taille) ; le nom sur disque = id + extension, **jamais le nom fourni par
le client**. Dossier configuré par `Fichiers:Chemin` (volume Docker `fichiers`,
monté sur `/app/donnees/fichiers`), inclus dans le backup. Limites : 50 Mo,
PDF/images seulement. Suppression d'un équipement = suppression en cascade des
fichiers. Choix de l'utilisateur.

## Conséquences

- Le backup doit couvrir base **et** volume fichiers (fait par `scripts/backup.sh`).
- Migration future vers un stockage objet possible sans toucher au schéma
  (seul `CheminDisque` change d'interprétation).

## Confirmation

Table `PiecesJointes` dans la migration `V1Emmenagement` ; volume `fichiers` dans
`docker-compose.yml` ; upload/download dans `Features/Equipements/EquipementsEndpoints.cs`.
