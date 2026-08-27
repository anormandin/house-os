---
type: decision
status: accepted
date: 2026-08-26
feature: "[[Budget]]"
tags: []
---

# Import Manuel D'abord Sync Ensuite

## Contexte

Alain veut que les transactions du compte fonds de prévoyance entrent dans
l'app. Deux chemins : la synchronisation automatique (SimpleFIN Bridge,
~15 $US/an, couvre les banques canadiennes dont Desjardins) ou l'import manuel
de fichiers CSV/OFX exportés de la banque. La sync coûte de l'argent, exige des
identifiants tiers et un choix de fournisseur — décisions d'Alain, pas urgentes
pour valider le module.

## Options considérées

- **SimpleFIN dès la v1** — le rêve, mais couple la v1 à un service payant et à
  la configuration bancaire avant même de savoir si le module sert.
- **Import CSV/OFX seulement, pour toujours** — friction mensuelle permanente.
- **Import manuel v1, derrière une abstraction `IFournisseurTransactions` ;
  SimpleFIN comme second fournisseur, activé par configuration plus tard** —
  le module vit tout de suite, la sync s'ajoute sans refonte.

## Décision

**V1 : import manuel CSV/OFX (web seulement), derrière une abstraction
`IFournisseurTransactions` ; la sync SimpleFIN sera un second fournisseur
activé par configuration, dans une phase ultérieure** (proposé par l'agent,
design délégué par Alain, 2026-08-26). Déduplication par FITID (OFX) ou hash
(date, montant, description) pour que réimporter un fichier soit sans danger.

## Conséquences

- Aucun secret bancaire dans la v1 ; l'ajout de SimpleFIN sera un
  `BackgroundService` de plus, même patron que météo et flux ICS.
- L'upload de fichiers reste web-only (cohérent avec la règle MCP existante).
- Une étape mensuelle manuelle demeure tant que la sync n'est pas activée.

## Confirmation

- `grep -r "IFournisseurTransactions" server/HouseOs.Api/` retourne
  l'abstraction.
- `grep -ri "simplefin" server/HouseOs.Api/ --include="*.cs"` ne retourne
  aucun code d'implémentation en v1 (mention en commentaire/config tolérée).
- Un test d'import couvre la déduplication au réimport du même fichier.
