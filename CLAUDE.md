# House OS — contexte projet

Système auto-hébergé de gestion de la maison pour un foyer de deux adultes : tâches
récurrentes et ponctuelles, pièces, équipements, documents, budget, météo, iCal,
serveur MCP. Projet-hobby : le but est autant de construire (serveur, UI, un jour
hardware) que d'utiliser. Les outils existants (Donetick, Grocy, Homebox…) servent
d'inspiration, jamais d'intégration. **Décision produit : aucune feature enfants /
points / récompenses / kid-station, jamais.**

Dépôt public (AGPL-3.0) : d'autres foyers l'installent (`docs/installation.md`) et
contribuent (`CONTRIBUTING.md`). Rien de propre à un foyer ou à un hébergement ne va
dans le code ni dans les défauts : tout passe par le `.env` (`docs/configuration.md`).
Le contexte personnel du mainteneur vit dans `CLAUDE.local.md` (gitignoré).

## Décisions structurantes (ADRs détaillés dans vault/Decisions)

- **Monorepo** ; hébergé sur une machine maison toujours allumée, tout en **Docker Compose** (une image API + web, Postgres à côté).
- **Backend** : .NET 10, monolithe modulaire en **tranches verticales** (`Features/<Module>/` : endpoints + opérations + DTOs), minimal APIs, EF Core + Npgsql. Domaine riche (`Domaine/`) testé sans base.
- **DB** : PostgreSQL (JSONB pour métadonnées flexibles ; backups pg_dump). Migrations EF générées, jamais de schéma modifié à la main.
- **Frontend** : Vite + React + TypeScript + TanStack Query + Tailwind. **UI 100 % français, chaînes en dur** (pas de lib i18n). **Desktop d'abord** ; installable sans service worker ; l'écran e-ink mural sera une seconde vue distincte (rendu serveur, phase 3).
- **Auth** : login simple, comptes seedés depuis la config à la première mise en route, cookie de session ; clés API pour les devices IoT plus tard.
- **Notifications v1** : flux iCal (Ical.Net) par personne. Push/ntfy plus tard.
- **Pas de n8n/Node-RED dans le cœur** : l'ingestion (météo, ICS, courriel…) = un `BackgroundService` .NET par source vers des tables normalisées ; les règles (« bonne journée pour tondre ») = classes C# testables.
- **Serveur MCP intégré** (`/mcp`, clé partagée + paramètre `agirComme`) : **parité MCP / API** — toute tranche REST met à jour les outils MCP dans la même session ; les fichiers restent web seulement.
- **Observabilité** : Serilog structuré, `TraceId` par requête ; Seq facultatif.
- **IoT (phase 3)** : MQTT + convention **Home Assistant MQTT Discovery** comme standard device (indépendant de HA) ; Zigbee2MQTT + Mosquitto ; firmwares maison en ESPHome ; Matter uniquement via sidecar, jamais de contrôleur C#.

## Moteur de récurrence (le cœur)

- Récurrence stockée **type + paramètres, PAS de chaînes RRULE/cron** : `mode` = fixe (jours de semaine / jour du mois / annuel) | intervalle-depuis-complétion | ponctuelle ; + **fenêtre saisonnière** optionnelle (plage mois-jour, combinable) ; + flag **rollover** (une occurrence manquée glisse au lieu de s'empiler en retard).
- **Prochaine échéance matérialisée à la complétion**, pas calculée à la lecture.
- **Journal de complétion = table séparée** (qui/quand/notes/coût/photo), jamais une simple date mutée.
- Stratégie d'assignation sur la tâche : fixe | alternance | moins-l'a-fait.
- Différenciateur prévu : échéance déclenchée par capteur (phase 3).

## Domaine (termes français dans le code)

Utilisateur · Zone (pièce/extérieur) · Équipement (marque, série, garantie, manuels, specs JSONB) · Tâche (définition + spec de récurrence) · Occurrence (instance planifiée) · Journal de complétion · Document · Compte à rebours · Enveloppe (budget) · Flux externe. À venir : Consommable, Appareil (IoT).

## Feuille de route

Phases 1 et 2 livrées (tâches, récurrence, zones, équipements, iCal, météo, humeur,
ICS, documents, budget, courriel entrant, MCP, synchro). Reste de la phase 2 :
Hydro-Québec `evenements-pointe`, consommables. **Phase 3** : hub MQTT, tablette
murale, NFC tap-pour-compléter, e-ink, capteurs. Détail : `vault/Reference/Architecture.md`.

## Vault

Ce projet a un vault de specs dans `vault/` — sa source de vérité. Dans toute session
de design, planification ou changement de contrat, invoquer le skill `vault` et partir
de `vault/Home.md`. Tout changement de contrat met à jour le vault dans la même session.

## Conventions

- UI et termes du domaine en français ; code technique (infra, helpers) en anglais si plus naturel.
- Messages de commit `type: description` en français.
- Démarrer, tester, brancher le MCP local : skill `demarrer`.
