---
type: feature
status: implemented
last-verified: 2026-08-28
verified-against: 0d96d5f
tags: []
---

# Auth

## Intention

Deux personnes, une maison : un login volontairement minimal
([[D-2026-08-23 Auth Simple Deux Comptes]]) qui sert surtout à attribuer les
complétions et à filtrer les flux iCal — pas une forteresse, mais rien de
committé ni de silencieusement faible (durci à la ronde QA 2026-08-28).

## Comportement

- **Deux comptes seedés** (`alain`, `ariane`) au démarrage ; mots de passe
  fournis par l'environnement (`SEED_MDP_*`, exigés par le compose en prod —
  `docker-compose.yml`), valeurs dev dans
  `server/HouseOs.Api/appsettings.Development.json`. Aucun mot de passe dans
  `appsettings.json` ni de repli committé ; un seed à mot de passe vide est
  refusé par l'amorçage. Changer un mot de passe ensuite = SQL (assumé,
  [[Déploiement]]).
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
- **Tout est protégé par défaut** : FallbackPolicy d'authentification ; les
  seules routes anonymes (santé, flux iCal par jeton, fallback SPA) sont
  verrouillées par la liste explicite de
  `server/HouseOs.Tests/Integration/GardeAuthTests.cs`.
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

## Ancres de code

- `server/HouseOs.Api/Features/Auth/AuthEndpoints.cs` — login, moi, déconnexion.
- `server/HouseOs.Api/Program.cs` — cookie, FallbackPolicy, rate limiter,
  proxys connus.
- `server/HouseOs.Tests/Integration/GardeAuthTests.cs` — verrou des routes
  anonymes.

## Sources

—

## Historique

Née dans la V0 « Déménagement » ([[Tâches]]) ; spec extraite et durcissements
(rate limiting, re-hash, purge de cookie, secrets exigés) livrés à la ronde QA
du 2026-08-28.
