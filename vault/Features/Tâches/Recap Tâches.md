---
type: recap
date: 2026-08-23
feature: "[[Tâches]]"
plan: "[[Plan 2026-08-23 V0 Déménagement]]"
---

# Recap Tâches

V0 « Déménagement » livrée en une session, conforme au plan. Tâches ponctuelles de
bout en bout : quick-add (titre, échéance, assignation), vues Aujourd'hui (échues et
en retard) et Tâches (à faire / complétées), complétion avec attribution + entrée de
journal, suppression. Login cookie 2 comptes (alain, ariane — mot de passe temporaire
à changer). PWA française thème Kanagawa, servie par le conteneur .NET unique
(`docker compose up`, port 8080), Postgres migré + amorcé au démarrage.

Déviations du plan :
1. La spec de récurrence n'est pas un objet embarqué : `Mode` + `Rollover` sont des
   colonnes de `Tache` ; les paramètres fixe/intervalle viendront par migration V1.
2. Le journal n'a pas de FK vers Tache/Occurrence (ids historiques seulement) — il
   survit à la suppression d'une tâche.
3. Vérification visuelle UI non faite en session (extension Chrome déconnectée) —
   API validée par curl ; à confirmer par l'utilisateur sur http://localhost:8080.

Reste ouvert pour la V1 : moteur de récurrence (3 modes), zones, équipements, flux
iCal, script de backup, déploiement Tailscale sur la machine de la nouvelle maison.
