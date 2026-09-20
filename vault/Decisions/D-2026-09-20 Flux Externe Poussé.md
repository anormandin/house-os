---
type: decision
status: accepted
date: 2026-09-20
feature: "[[Flux Externes]]"
tags: []
---

# D-2026-09-20 Flux Externe Poussé

## Contexte

[[D-2026-09-20 Sources Municipales Séparées Par Solidité]] envoie les événements
municipaux dans House OS par une ingestion générique, le gratteur vivant hors dépôt.
Reste à décider **où ces faits atterrissent en base**.

`FluxExterne` (`server/HouseOs.Api/Domaine/FluxExterne.cs`) est aujourd'hui un
abonnement **ICS par URL** : `Url` est requise, un `BackgroundService` télécharge et
remplace les événements du flux en transaction toutes les 6 h
([[D-2026-08-24 Tables Flux Externes]]).

Le détail qui tranche : les **actualités** municipales n'ont pas de date sur la page de
liste — elle n'est que sur l'article — alors que `EvenementExterne.Date` est obligatoire.

## Options considérées

- **Nouvelle table `FaitExterne`** (titre, rubrique, date **optionnelle**, lien, source) —
  couvrirait actualités et événements du même coup et garderait le bandeau de l'app
  libre de bruit municipal. Mais table et endpoint neufs, et rien ne se rebranche sur ce
  qui existe.
- **Les deux chemins** — le modèle le plus juste, et deux chemins d'ingestion à écrire,
  tester et documenter pour une ville de 7 000 habitants.
- **Flux poussé, actualités reportées** (retenue).

## Décision

`FluxExterne` gagne une **source poussée** : `Url` devient nullable et un endpoint
authentifié remplace les événements d'un flux en transaction — **le même contrat de
remplacement** que le rafraîchissement ICS, avec les mêmes bornes de longueur.

Les événements municipaux sont de vrais événements datés : ils héritent **gratuitement**
du bandeau du jour, du ruban des 7 jours et de `ProchaineCollecte`
(`ComposerDonneesEcran.cs`).

Les **actualités attendent** — 8 par an, sans date : on n'invente pas une date en base
pour faire entrer un fait dans une colonne obligatoire.

Choix d'Alain, en grillage.

## Conséquences

- Migration EF : `FluxExterne.Url` nullable, plus une marque de source. Le
  rafraîchissement ICS doit **ignorer** les flux poussés (sinon il les viderait à la
  prochaine passe de 6 h) — c'est le piège principal de ce changement.
- L'UI de gestion des flux ([[Flux Externes]]) doit montrer un flux poussé sans champ
  URL, et son horodatage de dernière réception.
- Authentification : la clé partagée du [[Serveur MCP]]
  ([[D-2026-08-24 Clé API Partagée Et AgirComme]]) est le précédent du projet pour un
  appel machine ; l'endpoint n'est jamais derrière le cookie de session.
- Un flux poussé **périmé** (le gratteur est mort il y a un mois) ne doit jamais
  ressembler à un flux à jour : l'horodatage de réception est visible et le fonds de
  tiroir cesse de sortir ses faits au-delà d'une fenêtre.
- Parité MCP ([[Serveur MCP]]) : si la gestion des flux gagne un chemin REST, les outils
  MCP suivent dans la même tranche.

## Confirmation

`FluxExternesRafraichissement.cs` filtre explicitement les flux sans URL (un test le
couvre : un flux poussé n'est pas vidé par une passe de rafraîchissement), et
`server/HouseOs.Tests/` contient un test d'intégration qui pousse deux fois le même flux
et vérifie le remplacement complet.
