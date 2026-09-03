---
type: plan
status: approved
date: 2026-09-03
feature: "[[Affichage E-ink]]"
---

# Plan 2026-09-03 Affichage E-ink V1

## But

Livrer la vue e-ink de House OS avant l'arrivée du reTerminal E1003 (9–13
septembre 2026) : une page `/ecran` conçue et prévisualisable tout de suite, sa
capture en PNG 1-bit par Chromium dans le serveur, puis les trois routes du
protocole TRMNL pour que l'appareil s'enrôle et tire l'image dès qu'il est flashé.
Ordre voulu par l'utilisateur : **l'image d'abord**, le protocole ensuite.

## Étapes

### 1. La page `/ecran` (l'image, sans matériel)

- [x] `server/HouseOs.Api/Features/Affichage/AffichageEndpoints.cs` :
  `GET /api/affichage/donnees` — un seul DTO `DonneesEcran` composé côté
  serveur (date, phrase du jour, occurrences du jour ouvertes + faites triées
  comme Aujourd'hui, événements externes du jour et prochaine collecte, météo du
  moment + verdicts `Bon`, prochain compte à rebours, heure de rendu). Autorisé
  par le cookie de session **ou** l'en-tête `X-Rendu-Jeton` égal au jeton
  interne de rendu (`JetonRendu`, singleton aléatoire par processus) — le
  endpoint est `AllowAnonymous` et vérifie lui-même.
- [x] `server/HouseOs.Api/Features/Affichage/ComposerDonneesEcran.cs` — la
  composition (réutilise `OperationsTaches.ListerOccurrencesAsync`, les tables
  météo, `PhrasesDuJour`, `EvenementsExternes`, `ComptesARebours`) ; testée dans
  `server/HouseOs.Tests/Features/Affichage/ComposerDonneesEcranTests.cs`
  (ordre, plafond de 10 lignes, prochaine collecte = premier événement
  `Collecte` ≥ aujourd'hui).
- [x] `web/src/pages/Ecran.tsx` — route `/ecran?largeur=1872&hauteur=1404`
  montée **hors** de `AppConnectee` dans `web/src/App.tsx` (pas de Layout, pas
  de hub SignalR) ; conteneur fixe largeur×hauteur, noir sur blanc, grammaire
  du vault : bande d'en-tête inversée (date longue + titre d'humeur), ⅔ gauche
  liste du jour (≤ 10, initiale de l'assigné dans un cercle, complétées barrées,
  « Tout est beau ✓ » si vide), ⅓ droit zones Étiquette/Valeur (météo du
  moment + verdict, prochaine collecte, prochain compte à rebours), pied
  (heure du rendu, pile si connue). Fraunces + Nunito Sans (déjà auto-hébergées
  via fontsource). Quand les polices et les données sont là :
  `document.documentElement.dataset.pret = "1"`.
- [x] `web/src/lib/ecran-vues.ts` (+ test) — helpers purs : troncature, initiale,
  formatage québécois via `web/src/lib/format.ts`, icône météo 1-bit par code
  WMO (jeu réduit de tracés SVG traits épais).
- [x] Vérifier dans le navigateur (connecté) à 1872×1404 : lisibilité à 2–3 m
  simulée (zoom arrière), aucun gris, aucune ombre.

### 2. La capture (Chromium dans le serveur)

- [x] Paquet `Microsoft.Playwright` (1.62.0, as of 2026-09) dans
  `server/HouseOs.Api/HouseOs.Api.csproj`.
- [x] `server/HouseOs.Api/Features/Affichage/RenduEcran.cs` — `IRenduEcran`
  + implémentation Playwright : navigateur Chromium lancé une fois
  (`IHostedService`), contexte avec `ExtraHTTPHeaders` = jeton de rendu,
  viewport largeur×hauteur, `DeviceScaleFactor = 1`, flags
  `--disable-lcd-text --font-render-hinting=none`, attente de
  `html[data-pret="1"]`, capture PNG. URL de la page :
  `Affichage:UrlEcran` (défaut `http://localhost:8080/ecran` ; dev :
  `http://localhost:5173/ecran` dans `appsettings.Development.json`).
- [x] `server/HouseOs.Api/Features/Affichage/Seuillage.cs` — ImageSharp :
  `BinaryThreshold`, PNG 1-bit niveaux de gris ; testé
  (`SeuillageTests` : une image grise devient strictement noir/blanc, PNG à
  1 bit). Signature du fichier = SHA-256 des octets (16 hex).
- [x] `GET /api/affichage/apercu.png?largeur=&hauteur=` (cookie) — le PNG exact.
- [x] Dev sur le Mac : `dotnet tool install --global Microsoft.Playwright.CLI`
  puis `playwright install chromium` ; documenter dans le skill `demarrer`.
  (Fait via `pwsh bin/…/playwright.ps1 install chromium` — la révision 1234 était
  déjà là grâce au Playwright du dossier `web/` ; le CLI dotnet est trop vieux.)
- [x] Correctif en passant : un paramètre de requête qui ne se lie pas répond 400
  « Requête invalide » au lieu de 500 (`Infrastructure/Journalisation/GestionnaireExceptions.cs`).

### 3. Le protocole TRMNL (l'appareil)

- [x] Entité `AppareilAffichage` (`Domaine/AppareilAffichage.cs`, à plat comme les autres) : MAC (unique),
  identifiant court (6 hex, unique), clé, nom, modèle, largeur, hauteur,
  version firmware, tension de pile, RSSI, dernier contact, enrôlé le, dernier
  fichier servi. `DbSet` + migration EF `AjouterAffichage` (générée, jamais à
  la main).
- [x] `GET /api/setup` (anonyme) — enrôlement auto par `ID`, réponse
  `{status:200, api_key, friendly_id, image_url (écran d'accueil), filename}`.
- [x] `GET /api/display` (anonyme, vérifie `Access-Token` en temps constant) —
  télémétrie → ligne ; rendu à `Width`×`Height` (repli 1872×1404) ; cache
  mémoire des 3 derniers rendus par appareil ; réponse `{status:0, image_url,
  filename, refresh_rate, update_firmware:false, firmware_url:null,
  reset_firmware:false}`. `refresh_rate` = `CalculerDelaiReveil` (jour 300 s,
  nuit → jusqu'à `NuitFin`, plafond `Affichage:PlafondSecondes` 3600) — testé.
- [x] `GET /api/affichage/{id}/{jeton}/{filename}.png` (anonyme, HMAC-SHA256 du
  nom avec la clé de l'appareil, comparaison en temps constant) ; re-rend si
  le cache est vide.
- [x] `POST /api/log` — reversé dans Serilog (`HouseOs.Affichage`) avec
  l'identifiant de l'appareil ; toujours 204.
- [x] Humains : `GET /api/affichage/appareils`, `PUT …/{id}` (nom),
  `DELETE …/{id}` (révocation) — cookie. Parité MCP dans
  `Features/Mcp/OutilsMaison.cs` : `lister_appareils_affichage`,
  `gerer_appareil_affichage` (renommer / révoquer).
- [x] Tests d'intégration `server/HouseOs.Tests/Integration/AffichageApiTests.cs`
  avec un `IRenduEcran` factice substitué dans `HouseOsFactory` :
  `Setup_EnroleUnAppareilInconnu`, setup répété = même clé, display sans jeton
  → 401, display → `image_url` valide puis GET image → `image/png`, mauvais
  HMAC → 404, log → 204.
- [x] `docs/configuration.md` : `Affichage:CadenceJourSecondes`, `NuitDebut`,
  `NuitFin`, `PlafondSecondes`, `UrlEcran`, `UrlBase` (facultatifs).
- [x] Écran d'accueil après l'enrôlement : `/ecran?accueil=<identifiant>` (marque +
  identifiant), servi par la route d'image sous le nom réservé `accueil`.
- [x] Vérifié à la main avec curl comme faux appareil : setup → display (1872×1404,
  1-bit, 22 Ko, `refresh_rate` 300) → image → second display « inchangée » → log
  reversé dans Serilog → registre et révocation ; outils MCP présents dans
  `tools/list`.

### 4. Livraison

- [x] `Dockerfile` : Chromium de Playwright dans l'image finale (base
  `aspnet:10.0` = Ubuntu 24.04 ; installer via `playwright.ps1 install
  --with-deps chromium` dans une étape SDK avec pwsh, ou copier
  `/ms-playwright` depuis `mcr.microsoft.com/playwright:v1.62.0-noble` et
  installer ses dépendances) + `PLAYWRIGHT_BROWSERS_PATH`. Vérifier par un
  `docker compose up --build` local et un `apercu.png` depuis le conteneur.
  (Fait : `dotnet exec --runtimeconfig … Microsoft.Playwright.dll install chromium` en
  étape SDK, `install-deps chromium` + `fonts-dejavu-core` en étape finale, `chmod`
  du node embarqué ; aperçu 1-bit identique au dev en 0,6 s ; image ≈ 1,9 Go arm64.)
- [ ] Release prod (push + `pct exec 105`), vérifier `/api/sante` et
  `apercu.png` en prod.
- [ ] Vault : Recap, spec `status: building` → `implemented`, ancres de code.

### 5. À la réception de l'E1003

- [ ] Flasher le firmware TRMNL (`usetrmnl.com/flash`, cible reTerminal E1003),
  portail captif : Wi-Fi + serveur personnalisé = URL de House OS.
- [ ] Vérifier l'enrôlement (ligne créée, écran d'accueil), puis l'écran réel ;
  confirmer orientation, `Width`/`Height` envoyés, plafond de `refresh_rate`
  accepté, aspect du 1-bit ; calibrer les tailles à 2–3 m ; noter dans la spec.

## Vérification

- `dotnet test` (unitaires : composition, délai de réveil, seuillage ;
  intégration : protocole complet avec rendu factice).
- `npm test` (helpers `ecran-vues`) et lint.
- À la main : `/ecran?largeur=1872&hauteur=1404` connecté ; `apercu.png` en dev
  puis dans le conteneur ; puis l'appareil lui-même.
