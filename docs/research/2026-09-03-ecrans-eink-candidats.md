# Écran e-ink mural — candidats prêts à l'emploi (2026-09-03)

Complète `2026-08-23-affichages-iot-hardware.md` (section e-ink) avec un balayage
du marché en septembre 2026, centré sur le **prêt à l'emploi** (boîtier, batterie,
Wi-Fi, rien à souder) et sur **l'API disponible** pour que House OS pousse une image.
Contexte : `vault/Inspiration UI/Affichage Mural Et E-ink.md` (grammaire e-ink,
pipeline HTML → capture → 1-bit).

Prix vérifiés sur les boutiques le 2026-09-03. Les estimations « livré à Québec »
supposent 1 USD ≈ 1,37 CAD, taxes QC 15 %, et des frais de douane/courtage de
35 à 70 CAD pour un colis US (TRMNL documente 38,85 CAD au Québec, 70 CAD à
Montréal selon le transporteur). À revalider au moment d'acheter.

## Le point structurant : le protocole TRMNL comme API universelle

Presque tous les candidats sérieux parlent le **protocole TRMNL** (« BYOS », Bring
Your Own Server). Le firmware, ouvert, fait deux appels HTTP :

- `GET /api/setup` — première connexion, le serveur rend un `api_key` + `friendly_id`.
- `GET /api/display` — à chaque réveil, avec les en-têtes `ID` (MAC),
  `Access-Token`, `Refresh-Rate`, `Battery-Voltage`, `FW-Version`, `RSSI`, et le
  modèle. Réponse JSON : `image_url`, `filename`, `refresh_rate` (secondes avant
  le prochain réveil), `update_firmware`, `firmware_url`, `reset_firmware`.
- `POST /api/log` — journal d'erreurs du device.

L'image est un PNG (ou BMP 1-bit) à la résolution exacte de l'écran ; le device
redessine seulement si `filename` change. **BYOS est gratuit et sans licence** ;
la licence BYOD (50 USD) ne sert que si l'on veut utiliser le cloud TRMNL avec un
appareil non-TRMNL. Implémentations de référence : `usetrmnl/byos_hanami`,
`byos_laravel`, `byos_phoenix`, `byos_django`, `byos_fastapi`, `byos_sinatra`.

Conséquence : **House OS implémente ces trois routes une seule fois** (un
`Features/Affichage/`), et n'importe quel appareil TRMNL, Seeed reTerminal, Kindle
ou Kobo devient un client. Le choix du matériel est découplé du logiciel. La
tension batterie et le RSSI reçus à chaque appel deviennent des données de
device (« pile faible » comme tâche générée, plus tard).

## Candidats tout-en-un monochrome (mur ou tablette)

| Appareil | Écran | Prix boutique | ≈ livré QC | Dispo | API pour House OS |
|---|---|---|---|---|---|
| TRMNL OG 7.5" | 800×480, 4 gris, refresh rapide, partiel | 139 USD | 240–260 CAD | En stock, 1–2 j | Protocole TRMNL (BYOS), serveur configurable dans le portail captif |
| TRMNL X 10.3" | 1872×1404, 16 gris, plein < 1,2 s, partiel < 200 ms, barre tactile gestes | 229 USD (+10 batterie 12 Ah) | 380–410 CAD | Backlog, expédition « septembre 2026 » | Protocole TRMNL (BYOS) |
| Seeed reTerminal E1001 7.5" | 800×480, 4 gris, 2–5 s, partiel | 69 USD (rég. 79) | 140–160 CAD | En stock (entrepôt US) | Firmware TRMNL officiel · ESPHome `online_image` · Arduino/ESP-IDF · SenseCraft (cloud) |
| Seeed reTerminal E1003 10.3" | 1404×1872, 16 gris, 2–3 s, partiel, **tactile capacitif** | 159,90 USD | 270–300 CAD | En stock ; aussi chez RobotShop (Mirabel, QC) | Idem E1001 (ESPHome ≥ 2026.7 ; tactile seulement sous SenseCraft, TRMNL = boutons) |
| Inkplate 10 (Soldered) 9.7" | 1200×825, 3 bits gris, 1,6 s, partiel, 22 µA en veille | 189,95 € nu / ~220 € boîtier + batterie | 380–420 CAD (EU) ; Mouser.ca à vérifier | 54 en stock (EU) | Pas d'API livrée : sketch Arduino/MicroPython qui fetch un PNG, ou ESPHome (composant `inkplate`), ou firmware communautaire HomePlate (TRMNL + HA) |
| Kindle Paperwhite usagé 6–7" | 300 ppp, rétroéclairé | 40–120 CAD (Kijiji) | idem | Abondant | Jailbreak (WinterBreak/AdBreak, firmware ≤ 5.18.x seulement) puis `trmnl-kindle` (protocole TRMNL) ou `kindle-dash` (cron + PNG) |
| Kobo usagé (Clara HD, Aura 2, Mini) 6" | 300 ppp | 50–110 CAD | Abondant au Canada | `trmnl-kobo` : pas de jailbreak, un script (protocole TRMNL) |

### Notes par appareil

**TRMNL OG.** Le produit de référence : firmware GPL, dock/kickstand, énorme
communauté. Trop petit pour un mur à 2–3 m (écran ≈ 16 × 10 cm) ; parfait sur un
comptoir ou un bureau. Douanes documentées par TRMNL pour le Canada.

**TRMNL X.** La meilleure densité monochrome prête à accrocher (1872×1404 sur
10,3" ≈ 227 ppp), Wi-Fi 5 GHz, accéléromètre (portrait/paysage), dock magnétique,
batterie 6 ou 12 Ah (2–6 mois). Le tactile est une barre de gestes, pas un écran
tactile. En backlog ; ~400 CAD livré.

**reTerminal E1001.** Le tout-en-un le moins cher, boîtier métal, expédié des
États-Unis. Même écran que le TRMNL OG. Boîtier industriel, moins joli au mur.
Trois firmwares au choix, dont TRMNL officiel.

**reTerminal E1003.** Le meilleur rapport taille/prix en monochrome : même panneau
que le TRMNL X, 3000 mAh (~6 mois), tactile capacitif. Le tactile n'existe que
sous SenseCraft (cloud Seeed) : sous TRMNL ou ESPHome on retombe sur les boutons.
RobotShop est à Mirabel : pas de douanes, taxes seulement.

**Inkplate 10.** Panneau recyclé (variabilité, prix bas), ESP32 v1, boîtier plastique
imprimé, garantie 2 ans, doc excellente, la base des MagInkCal. Aucune API de
device fournie : c'est un board de dev, on écrit le fetch soi-même (30 lignes en
Arduino) ou on passe par ESPHome / HomePlate. Le plus cher une fois livré d'Europe.

**Kindle usagé.** L'expérience la moins chère, écran 300 ppp très lisible, et le
seul candidat **rétroéclairé** (lisible le soir sans lampe). Le jailbreak est une
loterie de firmware sur un usagé, et le Scribe récent (10,2") n'est pas
jailbreakable ; un Scribe de 2022–2023 (~350 CAD usagé) l'est.

**Kobo usagé.** Marque canadienne, donc très courant sur Kijiji. `trmnl-kobo`
s'installe sans jailbreak. Liste de modèles supportés courte.

## Candidats couleur (E Ink Spectra 6)

Six couleurs seulement : noir, blanc, rouge, jaune, bleu, vert. **Pas de violet**
(le code couleur Ariane ne passe pas), rafraîchissement 12–40 s avec clignotement
multicolore, jamais de partiel. Pour House OS la couleur n'apporte rien que la
grammaire e-ink (Valeur/Étiquette, 1-bit) n'exige ; à considérer seulement si
l'écran sert aussi de cadre photo.

| Appareil | Écran | Prix | ≈ livré QC | API |
|---|---|---|---|---|
| reTerminal E1002 7.3" | 800×480, 15–20 s | 99 USD | 180–200 CAD | TRMNL (en mono seulement) · ESPHome (couleur, ex. `rock3r/reterminal-e1002-ha-dashboard`) |
| reTerminal E1004 13.3" | 1200×1600, ~20–40 s, 5000 mAh (~6 mois) | 279,90 USD | 470–500 CAD ; RobotShop CA | TRMNL (mono) · ESPHome ≥ 2026.7 · Arduino |
| Inkplate 13SPECTRA 13.3" | 1600×1200, ESP32-S3 | 309 / 349 USD (boîtier + 3000 mAh) | 480–560 CAD | Comme Inkplate 10 (Arduino / MicroPython / ESPHome) |
| Waveshare ESP32-S3-PhotoPainter 7.3" | 800×480, cadre bois massif avec crochet mural, batterie en option | ~85 USD | 150–170 CAD | Stock : carte SD + page web locale. Firmware `aitjcize/esp32-photoframe` + serveur Docker (proxy d'URL, dithering) |
| Pimoroni Inky Impression 7.3" / 13.3" | 800×480 / 1600×1200, ~12 s | 109,95 / 356,95 CAD (PiShop.ca) + Pi Zero 2 W | 160 / 440 CAD | HAT Raspberry Pi : Python (`inky`) ou FrameOS ; **sur secteur** (le Pi vide une batterie en jours) |
| Waveshare ESP32-S3-ePaper-13.3E6 | 1600×1200, sans boîtier | 250–260 USD | ~400 CAD | Exemples Arduino Waveshare seulement |

## Grand format et tablettes

| Appareil | Écran | Prix | API |
|---|---|---|---|
| InkPoster Tela 28.5" / Affresco 31.5" | Spectra 6, batterie 12 mois | 1699 USD (Affresco) | **Aucune API publique** trouvée : application mobile seulement. À écarter tant que ça ne change pas. |
| Onyx Boox Note Air 4C 10.3" | Android, couleur Kaleido | ~700 CAD | Pas d'API device : un navigateur en kiosque sur la vue e-ink de House OS. Batterie 3700 mAh = jours en always-on, donc câble. Cher pour ce que c'est. |
| Xteink X4 4.3", M5Paper S3 4.7", M5 PaperColor 4" | 69–80 USD | Trop petits pour un mur ; bureau seulement | Firmware ouvert (ESP32), fetch à écrire |

## Verdict

> **Décision 2026-09-03** : reTerminal E1003 10.3" commandé, livraison attendue
> entre le 9 et le 13 septembre 2026.

1. **Logiciel d'abord, indépendamment du matériel** : implémenter le protocole
   TRMNL BYOS dans House OS (`/api/setup`, `/api/display`, `/api/log`) devant la
   route de rendu e-ink déjà esquissée dans le vault. Ça rend compatibles TRMNL,
   reTerminal, Kindle et Kobo sans code spécifique.
2. **Expérience à 100 CAD** : un Kindle Paperwhite ou un Kobo Clara HD de Kijiji
   pour valider le rendu, la cadence et le contenu avant d'acheter grand.
3. **Achat mural** : **reTerminal E1003** (10,3", 1404×1872, ~280 CAD via
   RobotShop, en stock) est le meilleur rapport ; **TRMNL X** (~400 CAD, backlog)
   si l'on préfère le produit fini (dock, 5 GHz, esthétique). L'Inkplate 10 ne
   gagne que si l'on veut construire le cadre bois/impression 3D autour, et il
   coûte plus cher livré.
4. **Couleur** : non. Le violet n'existe pas en Spectra 6, le rafraîchissement
   clignote 20–40 s, et la grammaire e-ink de House OS est 1-bit par conception.

## Sources

- trmnl.com (boutique OG et X, blogue « Model X Progress ») · help.trmnl.com
  (International Customs / Duty Fees ; Seeed devices) · docs.trmnl.com (BYOD/S,
  Display API, webhooks) · github.com/usetrmnl (firmware, byos_*, trmnl-kindle,
  trmnl-kobo, trmnl-display)
- seeedstudio.com (E1001 p-6534, E1002 p-6533, E1003 p-6731, E1004 p-6692) ·
  wiki.seeedstudio.com (reTerminal E10xx main page, ESPHome cookbook, Work with
  TRMNL) · ca.robotshop.com (E1003, E1004)
- soldered.com (Inkplate 10, 13SPECTRA, blogue « best home assistant dashboards »)
  · crowdsupply.com/soldered · esphome.io/components/display/inkplate ·
  github.com/lanrat/homeplate
- waveshare.com (ESP32-S3-PhotoPainter, ESP32-S3-ePaper-13.3E6, 13.3inch e-Paper
  HAT (K)) · github.com/aitjcize/esp32-photoframe(-server)
- pishop.ca (Inky Impression 7.3 et 13.3, 2025 Edition) · shop.pimoroni.com (Inky
  Frame 7.3)
- inkposter.com · kindlemodding.org (WinterBreak, AdBreak) ·
  terminalbytes.com (Kindle dashboard 2026) · kijiji.ca (Kindle, Kobo, Québec)
- xteink.com · shop.m5stack.com · lilygo.cc
