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
| `HOUSEOS_POUSSEE_CLE` | non | vide | `FluxExternes:ClePoussee` | la poussée de calendrier externe refuse tout |
| `FUSEAU_HORAIRE` | non | `America/Toronto` | `TZ` du conteneur + `Meteo:FuseauHoraire` | |
| `APP_PORT_HOTE` | non | `8080` | — | |
| `POSTGRES_PORT_HOTE` | non | `5433` | — | publié sur 127.0.0.1 seulement |
| `RESEAU_PROXIES_CONNUS` | non | vide | `Reseau:ProxiesConnus` | `X-Forwarded-*` ignoré : cookie non `Secure` derrière un proxy |
| `ANTHROPIC_API_KEY` | non | — | `Humeur:CleApi` (aussi lue telle quelle) — une seule clé pour le titre d'humeur, l'éditorialiste du journal mural et le courriel entrant | repli sans LLM : gabarits partout |
| `JOURNALISATION_SEQ_URL` | non | vide | `Journalisation:Seq:Url` | console seulement |
| `JOURNALISATION_SEQ_CLE` | non | — | `Journalisation:Seq:CleApi` | ingestion anonyme si le Seq l'accepte |
| `COMPOSE_PROFILES` | non | — | — | `funnel` démarre le sidecar Tailscale |
| `TS_AUTHKEY` | non | — | — | requis si `funnel` |
| `MAISON_LIEU` | non | vide | `Affichage:Lieu` | le bloc-titre de l'écran mural n'imprime pas de lieu |
| `HASARD_FICHIER` | non | vide | `Hasard:Fichier` | la banque du hasard livrée avec l'app sert (dictons et fêtes du Québec) |
| `ICAL_URL_PUBLIQUE_BASE` | non | vide | `Ical:UrlPubliqueBase` | l'UI n'affiche que l'URL interne |
| `COURRIEL_R2_ENDPOINT`, `COURRIEL_R2_BUCKET`, `COURRIEL_R2_CLE_ACCES`, `COURRIEL_R2_CLE_SECRETE` | non | — | `Courriel:R2:*` | relevé des courriels désactivé (il faut les quatre) |

## Clés de configuration sans variable `.env`

| Clé | Défaut | Rôle |
|---|---|---|
| `Meteo:CadenceMinutes` | 60 | fréquence de l'ingestion Open-Meteo |
| `Meteo:RetentionRelevesJours` | 7 | rétention des payloads bruts |
| `Meteo:NormalesAnnees` | 10 | années d'archive tirées pour les normales climatiques |
| `Meteo:NormalesCadenceHeures` | 6 | fréquence de la **vérification** des normales (pas du tirage) |
| `Meteo:NormalesAgeMaxJours` | 360 | âge au-delà duquel les normales se recalculent |
| `Humeur:HeureMatin`, `Humeur:HeureSoir` | 05:30, 17:00 | créneaux du titre d'humeur |
| `Humeur:Modele` | `claude-haiku-4-5` | modèle du titre d'humeur (deux appels par jour) |
| `Edition:Modele` | `claude-opus-5` | modèle de l'éditorialiste du journal mural (un appel par jour, au créneau `Humeur:HeureMatin`, avec la clé `ANTHROPIC_API_KEY`) ; sans clé, le gabarit écrit |
| `Courriel:CadenceMinutes` | 2 | fréquence du relevé R2 |
| `Courriel:R2:Prefixe` | `entrants/` | préfixe des objets relevés |
| `Affichage:CadenceJourSecondes` | 900 | délai entre deux réveils de l'écran e-ink le jour (`ECRAN_CADENCE_SECONDES`) |
| `Affichage:NuitDebut`, `Affichage:NuitFin` | 22:00, 05:30 | la nuit, l'écran dort jusqu'à `NuitFin` — par tranches de `PlafondSecondes` |
| `Affichage:PlafondSecondes` | 3600 | plus long sommeil demandé au firmware (`ECRAN_PLAFOND_SECONDES`) |
| `Affichage:UrlEcran` | `http://localhost:8080/ecran` | la page que le Chromium du serveur capture (dev : Vite) |
| `Affichage:UrlBase` | déduite de la requête | base des URL d'image renvoyées à l'appareil |
| `Journalisation:SeuilRequeteLenteMs` | 1000 | seuil du log « requête lente » |
| `Auth:LimiteConnexion:Tentatives` | 5 / min / IP | limite du login |
| `Fichiers:Chemin` | `<racine>/donnees/fichiers` | stockage des fichiers (volume `fichiers` en compose) |
| `Securite:CheminCles` | profil utilisateur | clés du cookie (volume `protection` en compose) |
| `Serilog:*` | console, `HouseOs` en Debug | niveaux et sinks |

## Les normales climatiques suivent les coordonnées

La famille « le climat » du fonds de tiroir (premier gel, première neige, dernière
journée à vingt degrés, mois le plus sec et le plus arrosé, « il a fait X° ce jour-là
l'an dernier ») se calcule sur une dizaine d'années de l'archive Open-Meteo, pour
`METEO_LATITUDE`/`METEO_LONGITUDE`. **Rien de neuf à saisir** : ce sont les mêmes
coordonnées que les prévisions.

Ce qu'il faut savoir : les normales matérialisées **portent les coordonnées du calcul**.
Changer `METEO_LATITUDE`/`METEO_LONGITUDE` (un déménagement) les invalide — au
redémarrage suivant, l'archive de l'ancienne adresse est effacée et les normales sont
retirées pour la nouvelle. Le journal se tait sur le climat entre les deux, le temps
d'un appel réseau, plutôt que d'annoncer le gel d'ailleurs.

Le tirage se fait **à l'âge, pas à date fixe** : au démarrage et toutes les quelques
heures, l'app vérifie (une lecture d'une ligne) si les normales manquent, décrivent un
autre lieu, ou ont plus de `Meteo:NormalesAgeMaxJours`. Une seule requête réseau par
an en sort. Open-Meteo injoignable ce jour-là n'a **aucun** effet : les normales
connues restent servies, et une installation neuve sans réseau garde tout le reste de
son journal.

## Les calendriers poussés

Un calendrier externe est d'ordinaire un **abonnement iCal** : l'app télécharge son URL
toutes les six heures. Il peut aussi être **poussé** — il n'a alors pas d'URL, et c'est
un programme extérieur qui remplace ses événements par l'API. C'est ce qu'il faut quand
la source n'est pas un calendrier : une page à gratter, un PDF, un tableau.

Le calendrier se crée dans l'app (Aujourd'hui → Calendriers externes → « Poussé ») ou
par le MCP (`gerer_flux_externe`), puis le programme extérieur pousse :

```
POST /api/flux-externes/<id>/evenements
Authorization: Bearer $HOUSEOS_POUSSEE_CLE
Content-Type: application/json

{"evenements": [{"titre": "Séance du conseil", "date": "2026-10-13", "heure": "19:30"}]}
```

- La liste **remplace** tout ce que le calendrier portait : ce qui n'y est plus
  disparaît, comme pour un téléchargement iCal. Une liste vide est une réponse valide
  (« rien à annoncer »), pas une panne.
- Les événements hors de la fenêtre d'ingestion (d'hier à +60 jours) sont écartés sans
  faire échouer l'appel ; au-delà de 500 événements, l'appel est refusé.
- `HOUSEOS_POUSSEE_CLE` est **sa propre clé**, distincte de `HOUSEOS_MCP_KEY` : le
  programme qui pousse vit souvent ailleurs que la maison, et n'a aucune raison de
  porter la clé qui ouvre tout le reste. Absente, la poussée refuse tout.
- La page de gestion affiche la **dernière réception** de chaque calendrier poussé.
  Passé sept jours sans réception, le journal du mur cesse de publier ce qu'il porte :
  un programme mort ne doit pas avoir l'air d'un programme à jour.

## La banque du hasard

La famille « le hasard » du fonds de tiroir (dicton de l'almanach, fête du jour) lit un
fichier de données, jamais une table en dur. L'app en livre une version québécoise
(`server/HouseOs.Api/Features/FondsDeTiroir/banque-du-hasard.qc.json`) ; son en-tête
documente le format, qui tolère les commentaires et les virgules finales.

Pour un autre pays : copier ce fichier, le traduire, et régler `HASARD_FICHIER` sur son
chemin **dans le conteneur** (donc le monter, par exemple
`- ./banque-du-hasard.json:/app/banque-du-hasard.json:ro` dans un
`docker-compose.override.yml`). Un chemin réglé mais introuvable ou illisible **ne
retombe pas** sur la banque québécoise : la famille se tait, et le reste du journal
sort normalement — servir des jours fériés du Québec à un foyer qui a justement demandé
les siens serait pire que le silence. Le démarrage journalise le fichier lu et le nombre
d'entrées retenues.

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
