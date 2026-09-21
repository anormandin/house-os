---
type: feature
status: implemented
last-verified: 2026-09-21
verified-against: 5a332af
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

- `refresh_rate` de jour : **15 min** (`Affichage:CadenceJourSecondes`, défaut 900,
  réglable par `ECRAN_CADENCE_SECONDES`). La latence d'affichage est donc ≤ 15 min.
  C'était 5 min du 2026-09-03 au 2026-09-20 : mesuré sur l'appareil, ça faisait
  ~198 réveils Wi-Fi par jour et **0,25 V en 16 jours** (4,04 V à l'enrôlement →
  3,79 V le 2026-09-20). C'est le réveil Wi-Fi qui coûte, pas le rafraîchissement
  du panneau — la cadence est le seul levier de pile qui compte.
- La nuit (`Affichage:NuitDebut` 22 h → `Affichage:NuitFin` 5 h 30, défauts), le
  serveur répond un délai qui mène au prochain matin, **borné par
  `Affichage:PlafondSecondes` (1 h, `ECRAN_PLAFOND_SECONDES`)** : la nuit n'est donc
  pas un seul sommeil mais ~8 réveils. 3600 s reste la seule valeur éprouvée sur le
  firmware 1.8.10 ; au-delà, l'acceptation n'a jamais été confirmée sur une nuit
  (`Plan 2026-09-03 Affichage E-ink V1`, case restée ouverte). Lever le plafond
  vaudrait ~7 réveils par jour, soit ~10 % du total à 15 min : à faire quand
  quelqu'un veut passer la nuit à observer `dernierContact`, pas avant.
- Le bouton *Refresh* force un réveil et un rendu immédiat : rien à faire côté
  serveur. Détail des boutons et du tactile : « Interaction physique » ci-dessous.
- La tension de pile et le RSSI reçus sont des données de l'appareil ; « pile
  faible » comme tâche générée est une suite possible, hors v1.

### Interaction physique (boutons et tactile)

L'écran est en lecture seule par construction. Sous firmware TRMNL 1.8.10 sur
l'E1003, un seul geste a un effet (relevé 2026-09-20) :

| Geste | Effet |
| --- | --- |
| **Refresh** (bouton du dessus, appui simple) | Réveille l'appareil et déclenche un `GET /api/display` immédiat, donc un redessin. La seule interaction vivante. |
| **Page Up** ou **Page Down** seul | Rien. La notion de playlist n'existe que dans le cloud TRMNL ; en BYOS le serveur sert une vue unique. |
| **Page Up + Page Down**, 2 s | Portail Wi-Fi (SSID `TRMNL`, `http://4.3.2.1`) : reconfiguration, pas un geste du quotidien. |
| **Tactile capacitif** | Rien. Le panneau tactile n'est piloté que par le firmware SenseCraft/Seeedash de Seeed ; TRMNL ne l'initialise jamais. |
| **Interrupteur d'alimentation** (côté) | OFF puis ON = redémarrage. |

Côté serveur non plus, rien n'est branché : la réponse `/api/display` porte
`special_function: "none"` et aucun en-tête de raison de réveil n'est lu
(`Telemetrie.cs`). Une requête déclenchée par *Refresh* est donc indiscernable d'un
réveil de cadence — « bouton = compléter la première tâche » est hors d'atteinte sans
changer de firmware.

Si l'interaction au mur devient un besoin, deux pistes seulement : le NFC
tap-pour-compléter déjà prévu en phase 3, ou un firmware ESPHome maison sur l'E1003
(qui rend les boutons *et* le tactile, mais coûte le protocole BYOS et l'autonomie
mesurée).

### Contenu de l'écran (paysage 1872×1404, choix utilisateur 2026-09-03)

> [!note] Remplacé par la broadsheet depuis l'étape 1 du [[Plan 2026-09-20 Journal Éditorial]].
> Ce qui suit décrit la V1. La mise en page vit maintenant dans
> [[Journal De La Maison]] ; ce qui la nourrit, dans [[Fonds De Tiroir]]. Une
> différence de contrat vaut d'être notée ici parce qu'elle a été un défaut : la
> **prochaine collecte** ne vient plus d'un champ à part de `DonneesEcran` mais du fait
> `ville.collecte`, qui se tait quand le calendrier n'est plus alimenté (étape 6,
> 2026-09-21).

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

### Régénérer le mur à la demande (as of 2026-09-20)

`POST /api/affichage/regenerer?moment=matin|soir` — parité MCP
`regenerer_journal_mural` — **réécrit la phrase du créneau**, appel LLM compris et
même si elle existe déjà, puis tire l'image **par le chemin de l'appareil** : même
capture, même seuillage 1-bit, même taille annoncée. Ce qu'on vérifie est donc
exactement ce que le mur recevra. La réponse dit si la phrase vient du modèle
(`source: Llm`) ou de la banque de gabarits, et si le bitmap diffère de celui que
l'appareil a déjà.

Sans ça, voir l'édition du matin demandait d'attendre le matin : le service de fond
matérialise une phrase par créneau et **passe son tour dès qu'elle existe**. La
génération vit maintenant dans `GenerationHumeur`, partagée entre le service (qui la
veut *si elle manque*) et la demande à la main (qui la veut *même si elle existe*) —
un seul prompt, un seul repli.

> [!warning] Régénérer ne réveille pas l'appareil, et ne le peut pas.
> Le protocole est en **tirage** : le reTerminal dort et redemande son écran à sa
> cadence (15 min le jour). La phrase neuve paraît donc au **prochain réveil**, pas à
> la seconde. C'est la même limite que « Push vers l'appareil » plus bas, pas un oubli.
> `DernierFichier` n'est d'ailleurs pas touché : il veut dire « le dernier bitmap
> **servi** à l'appareil », et un tirage à la main n'est jamais allé jusqu'au mur.

> [!note] Tranché par Alain (2026-09-20) : régénérer **remplace** la phrase du créneau.
> Une phrase par créneau, réécrite sur place, pas d'empilement et pas d'historique —
> l'hypothèse prise à la livraison est confirmée. L'autre lecture possible — générer un
> aperçu sans rien stocker — laisserait le mur sur l'ancienne phrase, ce qui vide
> l'outil de son intérêt. À rouvrir seulement si le besoin apparaît d'essayer des
> prompts sans toucher à la journée en cours.

**L'horloge d'essai** (`MomentDEssai`) traverse la capture pour que le tirage porte le
bon visage : `apercu.png?moment=matin` et `donnees?maintenant=…` composent le journal
de ce moment-là — surtitre d'édition, heure d'impression, météo, et **la phrase de ce
créneau-là**. Elle n'existe que sur l'aperçu et le tirage demandé ; `/api/display` dit
toujours l'heure vraie, parce qu'un mur qui se daterait du matin à sept heures du soir
mentirait à la seule personne qui le lit de loin.

> [!note] Trouvé au rendu, 2026-09-20 : « la plus récente » n'est pas « celle du créneau ».
> `PhraseCouranteAsync` prenait la phrase la plus récente du jour sans regarder
> l'heure. Régénérer « le matin » en soirée écrivait donc bien la phrase du matin — et
> le journal continuait d'afficher celle du soir. Invisible en marche normale (le soir
> n'est écrit qu'à 17 h), immédiat dès qu'on compose un tirage d'essai. La lecture
> prend maintenant le créneau en paramètre, et retombe sur le soir de la **veille**
> quand le matin n'est pas encore écrit.

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
- `server/HouseOs.Api/Features/Affichage/TirageDuMur.cs` — régénération à la demande
  (phrase puis image) ; `MomentDEssai.cs` — l'horloge d'un tirage d'essai.
- `server/HouseOs.Api/Features/Humeur/GenerationHumeur.cs` — la couture partagée entre
  le service de fond et la demande à la main.
- `server/HouseOs.Api/Features/Mcp/OutilsMaison.cs` — `lister_appareils_affichage`,
  `gerer_appareil_affichage`, `regenerer_journal_mural`.
- `web/src/pages/Ecran.tsx` — la page (écran du jour et écran d'accueil),
  `web/src/lib/ecran-vues.ts`, `web/src/lib/meteo-vues.ts`.
- Tests : `server/HouseOs.Tests/Features/Affichage/` (composition, seuillage, délai,
  télémétrie) et `server/HouseOs.Tests/Integration/AffichageApiTests.cs`
  (protocole complet avec `RenduEcranFictif`).

## Sources

- `docs/research/2026-09-03-ecrans-eink-candidats.md` — relevé du marché, prix,
  API par appareil, contrat BYOS.
- `docs/research/2026-08-23-affichages-iot-hardware.md` — premier balayage.
- Mapping des boutons par modèle et tactile réservé à SenseCraft :
  `wiki.seeedstudio.com/reterminal_e10xx_trmnl` (vérifié 2026-09-20).
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
  de nuit. La calibration des tailles à 2–3 m est faite le 2026-09-21, avec la
  broadsheet (« Calibration au mur » dans [[Journal De La Maison]]).
- **2026-09-20** — 16 jours de service mesurés : 4,04 V → 3,79 V. Cadence de jour
  ramenée de 5 à 15 min (voir « Cadence et pile »). La cadence de nuit reste ouverte.
- **2026-09-20** — la colonne de droite débordait : mesurée à 1078 px de contenu pour
  1028 px disponibles dès que la date passe sur deux lignes (ce qu'elle fait pour la
  plupart des jours longs, « dimanche 20 septembre » faisant 1046 px à 104 px), donc
  le bas du compte à rebours était coupé. Corrigé dans `Ecran.tsx` : sous-titre
  d'humeur borné à deux lignes, icône et température de la météo à 150 px (elles
  étaient à 190), zones du côté droit en `py-7` — 1006 px, avec 22 px de marge.

## Suite (planifiée le 2026-09-20, exécutée le 2026-09-21)

La refonte éditoriale retenue aux maquettes a été **planifiée** le 2026-09-20,
**exécutée** le 2026-09-21, et vit dans deux features distinctes : [[Fonds De Tiroir]] (les faits — le ciel, le climat,
la maison, le calendrier, la ville, le hasard — et leur score) et
[[Journal De La Maison]] (la broadsheet à densité variable et l'éditorialiste LLM).
Matériau d'origine : [[Éditorialiste De L'Écran]]. Plan d'exécution :
[[Plan 2026-09-20 Journal Éditorial]].

Cette note reste la spec de **l'appareil** : protocole TRMNL, cadence, pile, interaction
physique, capture Chromium, registre des appareils. Rien de tout cela ne change. La
section « Contenu de l'écran » ci-dessus décrit la **V1**, remplacée ; elle est coiffée
d'un pointeur vers [[Journal De La Maison]].

Les deux points que la suite semblait devoir rouvrir ont été tranchés sans supersession :

- **Plusieurs mises en page** reste hors périmètre —
  [[D-2026-09-20 Une Seule Mise En Page À Rangs]] retient une grille unique dont le rang
  est une fonction, pas un second gabarit.
- **La source municipale** n'entre pas ici —
  [[D-2026-09-20 Sources Municipales Séparées Par Solidité]] la renvoie à
  [[Flux Externes]] (ICS pour les collectes, flux poussé pour les événements).

## Historique

- [[Plan 2026-09-03 Affichage E-ink V1]] · [[Recap Affichage E-ink]] (étape 5 —
  calibration sur l'écran réel — fermée le 2026-09-21 par l'étape 9 du
  [[Plan 2026-09-20 Journal Éditorial]])
