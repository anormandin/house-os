# House OS

Système auto-hébergé de gestion de la maison, pensé pour un foyer de deux adultes :
tâches récurrentes et ponctuelles, pièces, équipements, documents, budget, météo,
comptes à rebours — et un serveur MCP pour piloter le tout depuis Claude Code.
Interface 100 % en français.

Projet-hobby : le but est autant de construire que d'utiliser. Les outils existants
(Donetick, Grocy, Homebox…) servent d'inspiration, jamais d'intégration.

## Fonctionnalités

- **Tâches** — récurrence à trois modes (jours fixes / intervalle depuis la dernière
  complétion / ponctuelle), fenêtres saisonnières, glissement des occurrences
  manquées, assignation fixe, en alternance ou à qui l'a le moins fait ; journal de
  complétion (qui, quand, notes) ; vue Aujourd'hui, ruban des 7 jours, vue Année.
- **Pièces et équipements** — inventaire des appareils (marque, série, garantie,
  manuels, photos) rattaché aux pièces, avec les tâches d'entretien liées.
- **Documents** — factures, garanties, contrats… classés par catégorie et dossier,
  liés aux équipements ; boîte « À classer » alimentée par courriel (Cloudflare,
  facultatif).
- **Budget** — enveloppes virtuelles sur un compte, import CSV de la banque,
  rapprochement suggéré.
- **Météo** — Open-Meteo (sans clé) et règles « bonne journée pour… ».
- **Flux iCal** par personne pour les agendas des téléphones ; publication
  facultative par Tailscale Funnel.
- **Synchro temps réel** entre les écrans du foyer (SignalR) et **titre d'humeur**
  du jour (LLM facultatif, repli sur une banque de phrases).
- **Serveur MCP** intégré (`/mcp`) : planifier un lot de tâches en conversation avec
  Claude Code et le pousser d'un coup.

## Installer sa propre instance

Prérequis : Docker avec Compose, une machine toujours allumée sur le réseau de la
maison (le guide détaillé : [docs/installation.md](docs/installation.md)).

```bash
git clone https://github.com/anormandin/house-os.git && cd house-os
cp .env.example .env        # remplir la section « Requis »
docker compose up -d --build
open http://localhost:8080  # se connecter avec le compte COMPTE_1_NOM du .env
```

Toutes les variables sont décrites dans [docs/configuration.md](docs/configuration.md).

## Développer

Prérequis : .NET 10 SDK, Node 26, Docker.

```bash
cp infra/exemples/.env.dev .env          # valeurs de dev jetables
docker compose up -d postgres            # Postgres seul, port 5433
dotnet run --project server/HouseOs.Api  # API sur http://localhost:5000 (migrations + seed)
cd web && npm install && npm run dev     # UI sur http://localhost:5173, proxy vers l'API
```

Ou, une fois `npm install` fait : `./start` lance les trois (Postgres, API en
arrière-plan, Vite) et attend que l'API réponde ; `./stop` arrête tout. Logs dans
`/tmp/houseos-api.log` et `/tmp/houseos-vite.log`.

Comptes de dev : `alain` et `ariane`, mot de passe `45234523`
(`server/HouseOs.Api/appsettings.Development.json`). **Ce mot de passe est public :
il ne sert qu'en dev local.** Une vraie instance prend ses comptes du `.env`
(`COMPTE_n_MDP`, requis, sans défaut) — voir [docs/installation.md](docs/installation.md).
Le reste — tests, conventions, flux de contribution — est dans
[CONTRIBUTING.md](CONTRIBUTING.md).

## Structure du dépôt

| Dossier | Contenu |
|---|---|
| `server/` | API .NET 10 — monolithe modulaire en tranches verticales, EF Core + PostgreSQL, sert aussi le web |
| `web/` | Vite + React + TypeScript + TanStack Query + Tailwind (installable, sans service worker) |
| `vault/` | Vault de specs (Obsidian) — décisions, specs de features, architecture : la source de vérité |
| `docs/` | Guides d'installation et de configuration, rapports de recherche |
| `infra/` | Worker courriel Cloudflare, config Tailscale Funnel, exemples d'override compose |
| `scripts/` | `backup.sh` — dump Postgres + archive des fichiers |
| `design/` | Maquettes de la direction artistique |
| `.claude/` | Skills Claude Code du projet (démarrer en dev, planifier des tâches via MCP) |

## Licence

[AGPL-3.0](LICENSE). Vous pouvez l'installer, le modifier et le redistribuer ; si vous
en servez une version modifiée à d'autres, vous en partagez le code.
