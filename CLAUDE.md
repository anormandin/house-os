# House OS — contexte projet

Système auto-hébergé de gestion de la maison pour Alain et sa femme (jamais d'enfants —
aucune feature points/récompenses/kid-station, jamais). Déménagement le **6 octobre 2026**
à **Sainte-Catherine-de-la-Jacques-Cartier, QC** (~46.85, −71.62). Projet-hobby : le but
est autant de construire (serveur, UI, un jour hardware) que d'utiliser. Les outils
existants (Donetick, Grocy, Homebox…) servent d'inspiration, jamais d'intégration.

## Décisions structurantes (2026-08-23 — ADRs détaillés dans vault/)

- **Monorepo** ; hébergé sur une machine maison toujours allumée + **Tailscale** ; tout en **Docker Compose** (le homelab change au déménagement).
- **Backend** : .NET 10, monolithe modulaire en **tranches verticales** (endpoint + handler + data par feature), minimal APIs, EF Core + Npgsql. Domaine riche + tests unitaires pour le moteur de récurrence.
- **DB** : PostgreSQL (JSONB pour métadonnées flexibles ; backups pg_dump).
- **Frontend** : Vite + TypeScript + TanStack Query + Tailwind + shadcn/ui, PWA (vite-plugin-pwa). **UI 100 % français, chaînes en dur** (pas de lib i18n).
- **Auth** : login simple, 2 comptes, session cookie ; clés API pour les devices IoT plus tard.
- **Notifications v1** : flux iCal (Ical.Net) auquel chaque téléphone s'abonne. Push/ntfy plus tard.
- **Pas de n8n/Node-RED dans le cœur** : l'ingestion (météo, ICS…) = un `BackgroundService` .NET par source vers des tables normalisées ; les règles (« bonne journée pour tondre ») = classes C# testables.
- **IoT (phase 3)** : MQTT + convention **Home Assistant MQTT Discovery** comme standard device (indépendant de HA) ; Zigbee2MQTT + Mosquitto ; firmwares maison en ESPHome ; Matter uniquement via sidecar, jamais de contrôleur C#.

## Moteur de récurrence (le cœur — patterns validés par la recherche Grocy/Donetick)

- Récurrence stockée **type + paramètres, PAS de chaînes RRULE/cron** : `mode` = fixe (jours de semaine / jour du mois / annuel) | intervalle-depuis-complétion | ponctuelle ; + **fenêtre saisonnière** optionnelle (plage mois-jour, combinable) ; + flag **rollover** (une occurrence manquée glisse au lieu de s'empiler en retard).
- **Prochaine échéance matérialisée à la complétion**, pas calculée à la lecture.
- **Journal de complétion = table séparée** (qui/quand/notes/coût/photo), jamais une simple date mutée.
- Stratégie d'assignation sur la tâche : fixe | alternance | moins-l'a-fait.
- Différenciateurs prévus (aucun outil existant ne les a) : fenêtres saisonnières + échéance déclenchée par capteur (v3).

## Domaine (termes français dans le code du domaine)

Utilisateur · Zone (pièce/extérieur) · Équipement (asset : marque, série, garantie, manuels, specs JSONB) · Tâche (définition + spec de récurrence) · Occurrence (instance planifiée) · Journal de complétion. À venir : Projet, Document, Consommable, Appareil (IoT).

## Feuille de route

1. **Phase 1a — V0 « Déménagement »** (mi-sept.) : tâches ponctuelles seulement (créer/assigner/échéance/compléter), vue Aujourd'hui, quick-add, login, PWA — les tâches du déménagement = premières vraies données.
2. **Phase 1b — V1** (autour du 6 oct.) : moteur de récurrence 3 modes + zones + module équipements complet + flux iCal + script de backup.
3. **Phase 2** : météo Open-Meteo (gratuit, sans clé, modèle HRDPS canadien) + règles « bonne journée pour… » ; ingestion ICS (collectes Recollect, calendriers) ; Hydro-Québec `evenements-pointe` ; documents ; consommables.
4. **Phase 3** : hub MQTT, tablette murale Fully Kiosk, NFC tap-pour-compléter, e-ink, panneaux openHASP, capteurs.

Rapports de recherche complets : `docs/research/`.

## Vault

Ce projet a un vault de specs dans `vault/` — sa source de vérité. Dans toute session
de design, planification ou changement de contrat, invoquer le skill `vault` et partir
de `vault/Home.md`. Tout changement de contrat met à jour le vault dans la même session.

## Conventions

- UI et termes du domaine en français ; code technique (infra, helpers) en anglais si plus naturel.
- Toujours demander avant de committer.
