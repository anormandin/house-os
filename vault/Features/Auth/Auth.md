---
type: feature
status: implemented
last-verified: 2026-09-30
verified-against: 6d1fda0
tags: []
---

# Auth

## Intention

Deux personnes, une maison : un login volontairement minimal
([[D-2026-08-23 Auth Simple Deux Comptes]]) qui sert surtout à attribuer les
complétions et à filtrer les flux iCal — pas une forteresse, mais rien de
committé ni de silencieusement faible (durci à la ronde QA 2026-08-28).

## Comportement

- **Comptes seedés depuis la configuration** au démarrage (`Seed:Utilisateurs`,
  vide dans `appsettings.json`) : en prod, un ou deux comptes fournis par le `.env`
  (`COMPTE_1_*` requis, `COMPTE_2_*` facultatif — `docker-compose.yml`,
  [[Distribution]]) ; en dev, `alain`/`ariane` en entier dans
  `server/HouseOs.Api/appsettings.Development.json`. Aucun repli committé ; une
  entrée sans nom est ignorée, une entrée sans mot de passe est refusée par
  l'amorçage (`AmorcerUtilisateursAsync`, `server/HouseOs.Api/Infrastructure/AmorcageDb.cs`) ;
  un nom d'affichage vide prend le nom d'utilisateur. Le mot de passe n'est posé
  qu'à la **création** du compte — le changer ensuite = SQL (assumé,
  [[Déploiement]]).
- **Adresse de courriel** (facultative, `COMPTE_n_COURRIEL`) : la seule donnée du
  compte que l'amorçage **réaligne à chaque démarrage** sur la configuration (vide =
  effacée), normalisée en minuscules
  ([[D-2026-09-21 Adresse De Courriel Sur L'Utilisateur]]). Elle sert à la
  [[Lettre Du Matin]] et n'est servie que par les listes du foyer
  (`GET /api/utilisateurs`, `lister_utilisateurs`) — nulle sur l'assigné d'une tâche
  ou d'une occurrence (`UtilisateurDto`).
- **Session cookie** `houseos_session` 180 jours, HttpOnly, SameSite=Lax,
  `SecurePolicy.SameAsRequest` (Secure derrière NPM/HTTPS via les proxys
  connus) — `server/HouseOs.Api/Program.cs`.
- **Login** `POST /api/auth/connexion` : vérification par `PasswordHasher`
  (hash + sel), réponses 401 uniformes (pas d'oracle utilisateur/mot de passe),
  **rate limiting** 5 tentatives/min par IP (429, configurable
  `Auth:LimiteConnexion`), re-hash transparent quand Identity augmente son coût
  (`SuccessRehashNeeded`).
- **`GET /api/auth/moi`** : cookie valide mais compte disparu → purge du cookie
  + 401 ; même contrat pour la rotation iCal (`/api/ical/rotation`).
- **Tout est protégé par défaut** : FallbackPolicy d'authentification ; une route
  n'est ouverte que par un `AllowAnonymous()` explicite. La surface anonyme
  (as of 2026-09) : la santé, le login, le flux iCal (le jeton EST l'URL), le
  fallback SPA, le journal du navigateur (`POST /api/journal-client`, limité à
  120/min — [[Observabilité]]) et les routes de l'écran mural, gardées à la main
  plutôt que par le cookie — protocole TRMNL (`/api/setup`, `/api/display`,
  `/api/log`, image par identifiant + jeton) et `/api/affichage/donnees` (cookie
  d'un humain ou jeton du navigateur de rendu) : voir [[Affichage E-ink]].
  `server/HouseOs.Tests/Integration/GardeAuthTests.cs` tient la liste **explicite** de
  ces routes et la compare à la table de routage
  (`LesRoutesAnonymes_SontExactementLaListeConnue`, depuis le 2026-09-30) : un
  `AllowAnonymous` de plus ou de moins casse la suite. Il vérifie aussi le 401 sur un
  échantillon de routes `/api`.
- **Deux clés API à côté du cookie**, portées par le même handler
  (`AuthentificationCleApiHandler`, deux schémas) : la clé MCP pour `/mcp`, et la
  clé de poussée (`HOUSEOS_POUSSEE_CLE`) pour
  `POST /api/flux-externes/{id}/evenements` — distincte exprès, elle ne peut que
  remplacer les événements d'un flux poussé ([[D-2026-09-20 Flux Externe Poussé]]).
- **Côté web** : garde 401 globale (`web/src/lib/query-client.ts`) — toute
  requête ou mutation qui répond 401 purge `moi` et ramène à l'écran de
  connexion, sans boucle sur le 401 de `moi` lui-même.
- Les identités des appels MCP passent par `agirComme`, pas par la session —
  voir [[Serveur MCP]] et [[D-2026-08-24 Clé API Partagée Et AgirComme]].

## Hors périmètre

- Page de changement de mot de passe, comptes supplémentaires, 2FA, OAuth —
  hors de propos pour un foyer de deux.
- Clés par device (reporté aux clés IoT de la phase 3).

## Décisions

- [[D-2026-08-23 Auth Simple Deux Comptes]] — le choix fondateur.
- [[D-2026-08-24 Clé API Partagée Et AgirComme]] — l'identité côté MCP.
- [[D-2026-09-21 Adresse De Courriel Sur L'Utilisateur]] — le courriel vit sur le
  compte, alimenté par le `.env`.
- [[D-2026-09-20 Flux Externe Poussé]] — la seconde clé API, celle de la poussée.

## Ancres de code

- `server/HouseOs.Api/Features/Auth/AuthEndpoints.cs` — login, moi, déconnexion.
- `server/HouseOs.Api/Program.cs` — cookie, FallbackPolicy, rate limiter,
  proxys connus, schémas de clé API.
- `server/HouseOs.Api/Infrastructure/AmorcageDb.cs` — seed des comptes, courriel
  réaligné.
- `server/HouseOs.Api/Features/Mcp/AuthentificationCleApi.cs` — le handler des
  deux clés API.
- `server/HouseOs.Tests/Integration/GardeAuthTests.cs` — liste exacte des routes
  anonymes (dérivée de la table de routage), 401 sur un échantillon de routes.

## Sources

—

## Historique

Née dans la V0 « Déménagement » ([[Tâches]]) ; spec extraite et durcissements
(rate limiting, re-hash, purge de cookie, secrets exigés) livrés à la ronde QA
du 2026-08-28. Depuis : comptes génériques par `COMPTE_n_*` ([[Distribution]],
2026-09-02), routes de l'écran (2026-09-03), clé de poussée (2026-09-20), adresse
de courriel (2026-09-21).
