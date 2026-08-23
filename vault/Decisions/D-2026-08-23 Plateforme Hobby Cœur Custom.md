---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: []
---

# Plateforme Hobby Cœur Custom

## Contexte

House OS pouvait être un outil pratique assemblé d'exécutables existants (Donetick,
Grocy, Home Assistant) ou une plateforme construite maison. La recherche (voir
`docs/research/2026-08-23-logiciels-gestion-maison.md`) confirme qu'aucun outil ne
couvre bien le « OS de maison » complet.

## Options considérées

- **Plateforme hobby, cœur custom** — construire serveur + UI ; lent vers la première
  valeur, mais fondation extensible pour des années (IoT, règles météo, etc.).
- **Outil pratique d'abord** — adopter Donetick/Grocy ; rapide, mais peu de propriété
  et l'envie de construire reste insatisfaite.
- **Hybride** — cœur custom + services existants agressivement enveloppés.

## Décision

Plateforme hobby : cœur custom (.NET + React), réutilisation seulement en périphérie
(APIs météo, plus tard bridges de capteurs). Les outils existants servent
d'inspiration de design, jamais d'intégration. Choix de l'utilisateur.

## Conséquences

- Le moteur de tâches/récurrence est à nous — y compris les différenciateurs
  qu'aucun outil n'offre (fenêtres saisonnières, échéance par capteur).
- Première valeur plus lente ; la V0 « Déménagement » compense en livrant tôt.
- Chaque module futur (consommables, recettes) doit repasser par un choix
  build-vs-reuse explicite.

## Confirmation

Aucun conteneur Donetick/Grocy/Homebox dans `docker-compose.yml` ; le dossier
`server/` contient l'implémentation du moteur de tâches.
