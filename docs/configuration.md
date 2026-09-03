# Référence de configuration

Deux couches : les variables du `.env` (lues par `docker-compose.yml`) et les clés
de configuration ASP.NET Core qu'elles alimentent (`appsettings.json`, surchargées par
`appsettings.{Environnement}.json`, `appsettings.local.json` gitignoré, puis les
variables d'environnement `Section__Cle`). En dev sans Docker, on règle directement
les clés dans `appsettings.local.json`.

## Variables du `.env`

| Variable | Requis | Défaut | Clé de config | Effet si absente |
|---|---|---|---|---|
| `POSTGRES_PASSWORD` | oui | — | `ConnectionStrings:HouseOs` | le compose refuse de démarrer |
| `METEO_LATITUDE`, `METEO_LONGITUDE` | oui | — | `Meteo:Latitude/Longitude` | idem |
| `COMPTE_1_NOM`, `COMPTE_1_MDP` | oui | — | `Seed:Utilisateurs:0` | idem |
| `COMPTE_1_AFFICHAGE` | non | = nom d'utilisateur | `Seed:Utilisateurs:0:NomAffichage` | |
| `COMPTE_2_NOM`, `COMPTE_2_AFFICHAGE`, `COMPTE_2_MDP` | non | — | `Seed:Utilisateurs:1` | un seul compte |
| `HOUSEOS_MCP_KEY` | oui | — | `Mcp:Cle` | le compose refuse ; clé vide côté app = `/mcp` refuse tout |
| `FUSEAU_HORAIRE` | non | `America/Toronto` | `TZ` du conteneur + `Meteo:FuseauHoraire` | |
| `APP_PORT_HOTE` | non | `8080` | — | |
| `POSTGRES_PORT_HOTE` | non | `5433` | — | publié sur 127.0.0.1 seulement |
| `RESEAU_PROXIES_CONNUS` | non | vide | `Reseau:ProxiesConnus` | `X-Forwarded-*` ignoré : cookie non `Secure` derrière un proxy |
| `ANTHROPIC_API_KEY` | non | — | `Humeur:CleApi` (aussi lue telle quelle) | repli sans LLM |
| `JOURNALISATION_SEQ_URL` | non | vide | `Journalisation:Seq:Url` | console seulement |
| `JOURNALISATION_SEQ_CLE` | non | — | `Journalisation:Seq:CleApi` | ingestion anonyme si le Seq l'accepte |
| `COMPOSE_PROFILES` | non | — | — | `funnel` démarre le sidecar Tailscale |
| `TS_AUTHKEY` | non | — | — | requis si `funnel` |
| `ICAL_URL_PUBLIQUE_BASE` | non | vide | `Ical:UrlPubliqueBase` | l'UI n'affiche que l'URL interne |
| `COURRIEL_R2_ENDPOINT`, `COURRIEL_R2_BUCKET`, `COURRIEL_R2_CLE_ACCES`, `COURRIEL_R2_CLE_SECRETE` | non | — | `Courriel:R2:*` | relevé des courriels désactivé (il faut les quatre) |

## Clés de configuration sans variable `.env`

| Clé | Défaut | Rôle |
|---|---|---|
| `Meteo:CadenceMinutes` | 60 | fréquence de l'ingestion Open-Meteo |
| `Meteo:RetentionRelevesJours` | 7 | rétention des payloads bruts |
| `Humeur:HeureMatin`, `Humeur:HeureSoir` | 05:30, 17:00 | créneaux du titre d'humeur |
| `Humeur:Modele` | `claude-haiku-4-5` | modèle utilisé |
| `Courriel:CadenceMinutes` | 2 | fréquence du relevé R2 |
| `Courriel:R2:Prefixe` | `entrants/` | préfixe des objets relevés |
| `Journalisation:SeuilRequeteLenteMs` | 1000 | seuil du log « requête lente » |
| `Auth:LimiteConnexion:Tentatives` | 5 / min / IP | limite du login |
| `Fichiers:Chemin` | `<racine>/donnees/fichiers` | stockage des fichiers (volume `fichiers` en compose) |
| `Securite:CheminCles` | profil utilisateur | clés du cookie (volume `protection` en compose) |
| `Serilog:*` | console, `HouseOs` en Debug | niveaux et sinks |

## Variables de la session Claude Code (`.mcp.json`)

| Variable | Défaut | Rôle |
|---|---|---|
| `HOUSEOS_MCP_URL` | `http://localhost:5000/mcp` | l'instance visée par le serveur MCP |
| `HOUSEOS_MCP_KEY` | — (401 sans elle) | la clé de cette instance |

Résolues au démarrage de Claude Code : changer d'instance = relancer la session.

## Variables des tests E2E (`web/e2e`)

| Variable | Défaut |
|---|---|
| `E2E_URL` | `http://localhost:5173` |
| `E2E_UTILISATEUR` | `alain` |
| `E2E_MOT_DE_PASSE` | `45234523` |
