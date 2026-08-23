---
type: decision
status: accepted
date: 2026-08-23
feature: "[[Tâches]]"
tags: []
---

# Moteur De Récurrence Trois Modes

## Contexte

L'entretien d'une maison mélange trois régimes que les todo-apps confondent :
horaire fixe (« détecteurs de fumée le 1er du mois »), intervalle depuis la dernière
complétion (« tondre ~7 jours après la dernière tonte »), et fenêtres saisonnières
(« gouttières à l'automne », « tonte mai-octobre »). La recherche Grocy/Donetick
(`docs/research/2026-08-23-logiciels-gestion-maison.md`) confirme les patterns et le
trou : personne n'offre les fenêtres saisonnières ni l'échéance par capteur.

## Options considérées

- **Les trois modes dès le départ** — moteur plus riche à concevoir, mais c'est LE
  cœur du système ; le retrofit serait douloureux.
- **Fixe + intervalle, saisons plus tard** — v1 plus simple, retrofit saisonnier à
  prévoir.
- **RRULE seulement** — « tondre » se comporte mal immédiatement.

## Décision

Les trois modes dès la conception, avec les patterns validés par la recherche :
récurrence stockée **type + paramètres (jamais RRULE/cron)** ; fenêtre saisonnière
combinable avec les modes fixe et intervalle ; flag **rollover** (une occurrence
manquée glisse au lieu de s'empiler) ; **prochaine échéance matérialisée à la
complétion** ; **journal de complétion en table séparée** (qui/quand/notes/coût) ;
stratégie d'assignation par tâche (fixe | alternance | moins-l'a-fait).
L'échéance déclenchée par capteur est anticipée dans le modèle, construite en
phase 3. Choix de l'utilisateur.

## Conséquences

- Le moteur mérite des types riches + tests unitaires exhaustifs (c'est la partie la
  plus testée du serveur).
- La V0 « Déménagement » n'utilise que le mode « ponctuel » du même modèle — rien à
  retrofitter.
- Différenciateur réel vs Donetick/Grocy : fenêtres saisonnières + capteurs.

## Confirmation

Tests unitaires du moteur dans `server/HouseOs.Tests/` couvrant les trois modes ;
absence de chaîne RRULE/cron dans le schéma des tâches ; table de journal de
complétion distincte dans les migrations EF.
