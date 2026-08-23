---
type: recap
date: 2026-08-23
feature: "[[Tâches]]"
plan: "[[Plan 2026-08-23 V1 Emménagement]]"
---

# Recap V1 Emménagement

V1 livrée en une session, conforme au plan. Le moteur de récurrence (pur, 38 tests
verts dont 20+ sur le moteur et les stratégies) couvre les trois modes, les fenêtres
saisonnières chevauchant l'an, le rollover (service quotidien, fixes seulement) et
les trois stratégies d'assignation (l'assigné vit désormais sur l'occurrence).
Zones CRUD + vue Pièces (fraîcheur douce), module Équipements complet (specs JSONB,
manuels/photos sur disque, historique d'entretien), flux iCal par personne (jeton),
`scripts/backup.sh` (pg_dump + volume fichiers, rétention 30 j, restauration
vérifiée). Migration `V1Emmenagement` avec recopie des assignés V0. Trois décisions
mintées : [[D-2026-08-23 Flux iCal Par Personne]], [[D-2026-08-23 Fichiers Sur Disque]],
[[D-2026-08-23 Zones Plates]].

Vérifié : tests serveur, E2E navigateur (zones, tâche hebdo → complétion → prochaine
occurrence au bon samedi avec alternance Alain→Ariane, équipement + PDF
téléversé/téléchargé, boîte « Mon calendrier »), flux `.ics` par curl, backup exécuté.

Déviations et notes :
1. Rollover limité aux tâches fixes (une tâche à intervalle reste due — glisser
   n'aurait pas de sens) ; précisé dans la spec.
2. `EnableDynamicJson` requis côté Npgsql pour les specs JSONB (découvert au test).
3. La tranche s'appelle `FluxIcal` (collision de namespace avec le paquet Ical.Net).
4. Le proxy Vite relaie aussi `/ical` en dev.

Reste ouvert : déploiement Tailscale sur la machine de la nouvelle maison ;
[[Comptes À Rebours]] et [[Titre D'humeur]] (serveur) toujours en attente ;
mot de passe temporaire à changer au vrai déploiement.
