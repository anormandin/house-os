# Installer House OS chez soi

House OS tourne en Docker Compose sur une machine toujours allumée du réseau de la
maison (mini-PC, NAS, conteneur LXC…). Une seule image contient l'API et le web ;
Postgres tourne à côté. Aucun service externe n'est requis.

## 1. Prérequis

- Docker Engine avec le plugin Compose (`docker compose version`).
- Un accès au dépôt : `git clone https://github.com/anormandin/house-os.git`.
- Facultatif : un reverse proxy HTTPS (Nginx Proxy Manager, Caddy, Traefik) si vous
  voulez l'ouvrir aux téléphones de la maison en HTTPS (requis pour l'installer
  comme app sur l'écran d'accueil).

## 2. Configurer

```bash
cd house-os
cp .env.example .env
```

Remplir la section **Requis** du `.env` :

| Variable | Quoi |
|---|---|
| `POSTGRES_PASSWORD` | mot de passe de la base (`openssl rand -hex 32`) |
| `METEO_LATITUDE`, `METEO_LONGITUDE` | position de la maison, pour la météo |
| `COMPTE_1_NOM`, `COMPTE_1_AFFICHAGE`, `COMPTE_1_MDP` | premier compte (nom d'utilisateur en minuscules) |
| `COMPTE_2_*` | second compte, facultatif |
| `HOUSEOS_MCP_KEY` | clé du serveur MCP (`openssl rand -hex 32`) |

Les comptes sont créés **à la première mise en route seulement** (base vierge). Pour
changer un mot de passe ensuite, voir plus bas. Toutes les autres variables sont
facultatives et décrites dans [configuration.md](configuration.md).

## 3. Démarrer

```bash
docker compose up -d --build
docker compose logs -f app     # attendre « Now listening on »
curl -s localhost:8080/api/sante
```

Ouvrir `http://<machine>:8080` et se connecter avec `COMPTE_1_NOM` / `COMPTE_1_MDP`.

## 4. Derrière un reverse proxy HTTPS

1. Router `https://houseos.votre-domaine` → `http://<machine>:8080`, avec
   WebSockets activés (la synchro temps réel passe par `/hubs`).
2. Dans le `.env`, `RESEAU_PROXIES_CONNUS=<IP du proxy>` (ou un CIDR) : l'app
   n'honore `X-Forwarded-*` que depuis ces adresses, et c'est ce qui marque le cookie
   de session `Secure`. Si le login HTTPS ne tient pas, Docker réécrit probablement
   l'IP source : ajouter le sous-réseau du compose (ex. `172.18.0.0/16`).
3. `docker compose up -d` pour appliquer.

Postgres n'est publié que sur `127.0.0.1:5433` ; rien d'autre que le port 8080 n'a
besoin d'être joignable.

## 5. Mettre à jour

```bash
git pull && docker compose up -d --build
```

Les migrations s'appliquent au démarrage de l'app. Les sessions survivent aux
rebuilds grâce au volume `protection` (clés du cookie).

## 6. Sauvegarder

`scripts/backup.sh [dossier]` fait un `pg_dump` (format custom, vérifié) et une
archive du volume des fichiers (manuels, photos, documents), avec une rétention de
30 jours. À planifier en cron sur la machine hôte :

```
0 3 * * * /opt/house-os/scripts/backup.sh /var/backups/house-os
```

L'aide-mémoire de restauration est en commentaire à la fin du script.

## 7. Changer un mot de passe

Il n'y a pas encore de page ni de commande pour ça (limite connue : le hash est un
`PasswordHasher` ASP.NET Core, pas reproductible en SQL). Les comptes ne sont créés
qu'à la première mise en route : choisissez bien `COMPTE_n_MDP` avant le premier
`docker compose up`. Une contribution qui ajoute une page « changer mon mot de
passe » est la bienvenue.

## 8. Modules facultatifs

- **Titre d'humeur et classement des courriels par LLM** — `ANTHROPIC_API_KEY`.
  Sans clé : banque de phrases et classement déterministe.
- **Journalisation structurée** — `JOURNALISATION_SEQ_URL` (+ clé) vers un serveur
  Seq. Pour envoyer aussi le stdout de Postgres à un collecteur GELF, copier
  `infra/exemples/docker-compose.override.gelf.yml` vers `docker-compose.override.yml`.
- **Flux iCal public** (abonnement Google Agenda) — sidecar Tailscale Funnel :
  `COMPOSE_PROFILES=funnel`, `TS_AUTHKEY`, `ICAL_URL_PUBLIQUE_BASE`. Seul `/ical`
  est publié (`infra/tailscale-serve.json`).
- **Documents par courriel** — Cloudflare Email Routing → Worker → R2 :
  [infra/courriel-worker/README.md](../infra/courriel-worker/README.md).
- **Serveur MCP pour Claude Code** — `.mcp.json` à la racine du dépôt lit
  `HOUSEOS_MCP_URL` (défaut `http://localhost:5000/mcp`, mettre l'URL de votre
  instance + `/mcp`) et `HOUSEOS_MCP_KEY`. Le skill `planifier-taches` du dossier
  `.claude/` sait s'en servir.

## Dépannage

- **Le compose refuse de démarrer** avec « … requis (dans le .env) » : une variable
  de la section Requis manque.
- **Upload de fichier en erreur 500** sur un volume `fichiers` créé avant la version
  non-root : `docker run --rm -v house-os_fichiers:/f mcr.microsoft.com/dotnet/aspnet:10.0 chown -R 1654:1654 /f`.
- **Heure décalée** dans les échéances : régler `FUSEAU_HORAIRE` (défaut
  `America/Toronto`).
