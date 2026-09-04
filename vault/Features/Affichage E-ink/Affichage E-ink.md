---
type: feature
status: implemented
last-verified: 2026-09-03
verified-against: bfc2719
tags: [iot]
---

# Affichage E-ink

## Intention

La seconde vue de House OS ([[D-2026-08-23 Interface Desktop Et Écran E-ink]]) :
une page imprimée au mur qui se réimprime toute seule. Un écran e-ink de 10,3" sur
pile, sans interaction, qui montre en un coup d'œil ce que la maison attend
aujourd'hui : les tâches du jour, la météo, la prochaine collecte, le titre
d'humeur. Il doit passer le test esthétique du vault (« est-ce qu'on
l'encadrerait ? ») et rester lisible à 2–3 m.

Matériel choisi (as of 2026-09) : **Seeed reTerminal E1003** (10,3", 1404×1872,
16 niveaux de gris, ESP32-S3, 3000 mAh ≈ 6 mois), commandé le 2026-09-03, sous
firmware TRMNL. Le contrat ci-dessous ne lui est pas propre : tout appareil qui
parle le protocole TRMNL (TRMNL OG/X, autres reTerminal, Kindle ou Kobo avec le
client TRMNL) est un client valide. Relevé du marché et des API :
`docs/research/2026-09-03-ecrans-eink-candidats.md`.

## Comportement

### Protocole appareil (serveur BYOS TRMNL)

House OS est le serveur du firmware TRMNL
([[D-2026-09-03 Protocole TRMNL BYOS Comme API D'affichage]]). L'appareil tire ;
le serveur ne pousse jamais. Trois routes, sous `/api/` comme le reste mais hors
cookie de session :

- **Enrôlement** — `GET /api/setup`, en-tête `ID` (adresse MAC). Un appareil inconnu
  est enrôlé sur-le-champ ([[D-2026-09-03 Registre Des Appareils D'affichage]]) :
  le serveur lui attribue un identifiant court (6 hex) et une clé, et répond avec
  cette clé et l'URL d'un écran d'accueil (« House OS · Écran enrôlé · identifiant »,
  la page `/ecran?accueil=`). Un appareil connu reçoit sa clé existante. Le réseau étant privé (LAN/Tailscale, jamais
  exposé), l'enrôlement est ouvert ; la clé n'est jamais réaffichée ailleurs.
- **Affichage** — `GET /api/display`, en-têtes `ID`, `Access-Token`, plus la
  télémétrie du firmware (`Battery-Voltage`, `RSSI`, `FW-Version`, `Width`,
  `Height`, `Model`, `Refresh-Rate`…). Le serveur : (1) authentifie la clé
  (comparaison en temps constant, comme `Mcp:Cle`) ; (2) enregistre la
  télémétrie et l'heure du dernier contact ; (3) rend l'écran à la taille annoncée
  par l'appareil (`Width`×`Height`, repli 1872×1404 paysage) ; (4) répond
  `image_url` (absolue, signée), `filename`, `refresh_rate`, `update_firmware:
  false`, `reset_firmware: false`. Quand le contenu n'a pas changé, `filename`
  est identique et l'appareil ne redessine pas.
- **Image** — `GET /api/affichage/{appareil}/{jeton}/{filename}.png`, anonyme mais
  non devinable (le jeton est un HMAC du nom de fichier avec la clé de
  l'appareil). Sert le dernier rendu gardé en mémoire ; si le serveur a redémarré
  entre-temps, re-rend.
- **Journal** — `POST /api/log` : les entrées du firmware (niveau, message,
  tension, RSSI, raison du réveil) sont reversées dans Serilog avec l'identifiant
  de l'appareil, donc visibles dans Seq ([[Observabilité]]).

### Cadence et pile

- `refresh_rate` de jour : 5 min (`Affichage:CadenceJourSecondes`, défaut 300 ;
  choix utilisateur 2026-09-03, pile estimée ~2 mois). La latence d'affichage
  est donc ≤ 5 min.
- La nuit (`Affichage:NuitDebut` 22 h → `Affichage:NuitFin` 5 h 30, défauts),
  le serveur répond un délai qui mène au prochain matin, borné à ce que le
  firmware accepte (plafond de sécurité 1 h ; l'acceptation par le firmware
  1.8.10 reste à confirmer sur une nuit).
- Le bouton *Refresh* (dessus de l'appareil) force un réveil et un rendu
  immédiat : rien à faire côté serveur. Le tactile de l'E1003 est ignoré par le
  firmware TRMNL (SenseCraft seulement) : toucher l'écran ne fait rien.
- La tension de pile et le RSSI reçus sont des données de l'appareil ; « pile
  faible » comme tâche générée est une suite possible, hors v1.

### Contenu de l'écran (paysage 1872×1404, choix utilisateur 2026-09-03)

Grammaire du vault ([[Affichage Mural Et E-ink]]) : noir plein sur blanc, 1-bit,
2–4 zones aux filets, une seule bande inversée, élaguer plutôt que rapetisser.

- **Bande d'en-tête (inversée)** : la date longue (« mercredi 3 septembre ») et le
  titre d'humeur du moment ([[Titre D'humeur]]).
- **⅔ gauche — Aujourd'hui** : les occurrences du jour, au plus 10, tronquées ;
  chaque ligne porte l'initiale de la personne assignée dans un cercle (le code
  couleur n'existe pas en 1-bit), un trait de retard si en retard ; celles déjà
  complétées sont barrées puis disparaissent au rendu suivant. Un filet puis
  « Ce soir » si des tâches sont marquées du soir (sinon, rien). Quand la liste
  est vide : « Tout est beau ✓ », énorme.
- **⅓ droit — zones Étiquette/Valeur empilées** : la météo du moment (icône
  1-bit, température, verdict « bonne journée pour… » s'il y en a un), la
  prochaine collecte ([[Flux Externes]]), le prochain compte à rebours
  ([[Comptes À Rebours]]).
- **Pied** : heure du rendu (« 14 h 30 ») et pile en pourcentage, petits.
- Échelle : ≈ 9 px/mm ; à 2–3 m, secondaire ≥ 90 px, primaire ≈ 2×, en-tête
  ≈ 110 px. À calibrer à la réception.
- Typographie québécoise ([[Typographie Québécoise]]) : dates sans majuscule,
  « 19 h 30 », « 21 °C ».

### Rendu et aperçu

- L'écran est une **page React `/ecran`** conçue en Tailwind avec la grammaire
  e-ink, capturée par **Chromium headless** dans le serveur
  ([[D-2026-09-03 Rendu E-ink Par Chromium Headless]]), puis seuillée en 1-bit
  et encodée en PNG par ImageSharp (aucun tramage ; lissage et animations
  neutralisés à la capture). La page lit toutes ses données d'un seul
  `GET /api/affichage/donnees` (cookie de session ou jeton interne de rendu) et
  signale qu'elle est prête (polices chargées, données rendues) avant la
  capture. Polices auto-hébergées (OFL) : même rendu en dev et en prod.
- Aperçu : ouvrir `/ecran?largeur=1872&hauteur=1404` connecté montre la page
  vivante ; `GET /api/affichage/apercu.png?largeur=&hauteur=` (cookie) rend le
  PNG exact que l'appareil recevrait, seuillage compris. C'est l'outil de
  conception avant la livraison et de diagnostic ensuite.
- `GET /api/affichage/appareils` (cookie) liste les appareils enrôlés avec leur
  télémétrie (dont la pile en pourcentage, estimation linéaire 3,3–4,2 V) ;
  `PUT …/{id}` renomme, `DELETE …/{id}` révoque (l'appareil se ré-enrôlera avec une
  nouvelle clé). Parité MCP ([[Serveur MCP]]) : `lister_appareils_affichage`,
  `gerer_appareil_affichage` (renommer, supprimer).

## Hors périmètre

- Tactile (réservé au firmware SenseCraft de Seeed) ; couleur (Spectra 6 n'a pas
  de violet et clignote 20–40 s) ; niveaux de gris (le 1-bit est la grammaire ;
  le 2-bit viendra si le firmware le sert proprement sur l'E1003).
- Push vers l'appareil : le protocole est en tirage, point.
- Firmwares ESPHome ou SenseCraft, cloud TRMNL, licence BYOD.
- Page web de gestion des appareils (v2 : renommer, révoquer, choisir la vue).
- Plusieurs mises en page ou « plugins » : une seule vue, celle de la maison.
- MQTT : l'écran n'est pas un device Discovery
  ([[D-2026-08-23 Standard IoT MQTT Discovery]] vise capteurs et actionneurs).

## Décisions

- [[D-2026-08-23 Interface Desktop Et Écran E-ink]] — l'e-ink est une seconde vue
  distincte, rendue côté serveur.
- [[D-2026-09-03 Protocole TRMNL BYOS Comme API D'affichage]] — House OS
  implémente le protocole du firmware TRMNL plutôt qu'un firmware maison.
- [[D-2026-09-03 Rendu E-ink Par Chromium Headless]] — page React capturée par
  Chromium, seuillage ImageSharp ; qualité d'image avant taille de l'image Docker.
- [[D-2026-09-03 Registre Des Appareils D'affichage]] — table d'appareils avec
  enrôlement automatique et clé par appareil.
- [[D-2026-08-23 Pas De N8n Dans Le Cœur]] — tout en C# testable.

## Ancres de code

- `server/HouseOs.Api/Features/Affichage/ProtocoleTrmnlEndpoints.cs` — `/api/setup`,
  `/api/display`, route d'image signée, `/api/log`.
- `server/HouseOs.Api/Features/Affichage/OperationsAppareils.cs` — enrôlement,
  authentification, HMAC des images, liste/renommage/révocation (REST et MCP).
- `server/HouseOs.Api/Features/Affichage/AffichageEndpoints.cs` — `donnees`,
  `apercu.png`, `appareils` ; `JetonRendu.cs` — le secret du navigateur de rendu.
- `server/HouseOs.Api/Features/Affichage/RenduEcran.cs` (Playwright),
  `Seuillage.cs` (1-bit + signature), `CacheImages.cs`, `DelaiReveil.cs`,
  `Telemetrie.cs`, `AffichageOptions.cs`, `ComposerDonneesEcran.cs`.
- `server/HouseOs.Api/Domaine/AppareilAffichage.cs` — l'entité ; migration
  `AjouterAffichage`.
- `server/HouseOs.Api/Features/Mcp/OutilsMaison.cs` — `lister_appareils_affichage`,
  `gerer_appareil_affichage`.
- `web/src/pages/Ecran.tsx` — la page (écran du jour et écran d'accueil),
  `web/src/lib/ecran-vues.ts`, `web/src/lib/meteo-vues.ts`.
- Tests : `server/HouseOs.Tests/Features/Affichage/` (composition, seuillage, délai,
  télémétrie) et `server/HouseOs.Tests/Integration/AffichageApiTests.cs`
  (protocole complet avec `RenduEcranFictif`).

## Sources

- `docs/research/2026-09-03-ecrans-eink-candidats.md` — relevé du marché, prix,
  API par appareil, contrat BYOS.
- `docs/research/2026-08-23-affichages-iot-hardware.md` — premier balayage.
- Contrat firmware : `github.com/usetrmnl/trmnl-firmware` (README, section API),
  `github.com/usetrmnl/terminus/doc/api.adoc` (en-têtes et réponses complets),
  `wiki.seeedstudio.com/reterminal_e10xx_trmnl` (flash de l'E1003).

## Mise en service (2026-09-04)

- Appareil reçu et enrôlé en prod le jour même : firmware TRMNL **1.8.10**, portail
  captif → Advanced > Custom Server = `https://houseos.alainnormandin.dev` (HTTPS
  Let's Encrypt via NPM accepté). Wi-Fi **Feynman-IoT** (2,4 GHz, WPA2) : le firmware
  ne fait pas WPA3+PMF, exigé sur Feynman.
- Le firmware annonce `Width`×`Height` = **1872×1404** (paysage), ce qui confirme le
  repli et le choix de composition ; télémétrie reçue (pile 4,04 V, RSSI −46).
- Flash depuis un Mac : la puce USB-série WCH (1a86:7522) n'a pas de driver Apple ;
  installer `wch-ch34x-usb-serial-driver` (Homebrew) et activer l'extension. Aucun
  bouton « boot » : le reset est automatique. Portail captif : Refresh pour réveiller,
  Page Up + Page Down 2 s, SSID `TRMNL`, `http://4.3.2.1`. Un champ serveur vide
  renvoie vers trmnl.com (« purchase a BYOD license »).
- Orientation et rendu 1-bit validés à l'œil sur le panneau. Reste ouvert : cadence
  de nuit, calibration des tailles à 2–3 m.

## Historique

- [[Plan 2026-09-03 Affichage E-ink V1]] · [[Recap Affichage E-ink]] (étape 5 —
  calibration sur l'écran réel — encore ouverte)
