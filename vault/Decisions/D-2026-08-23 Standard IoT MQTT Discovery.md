---
type: decision
status: accepted
date: 2026-08-23
feature:
tags: [iot]
---

# Standard IoT MQTT Discovery

## Contexte

L'utilisateur veut un cadre d'intégration ouvert pour capteurs et devices (phase 3).
Le standard officiel de l'industrie est Matter (contrôleur lourd, pas de SDK .NET
mature) ; le standard de facto du DIY est MQTT + une convention de payload.

## Options considérées

- **MQTT + convention Home Assistant MQTT Discovery** — les devices s'annoncent par
  topics retained `homeassistant/+/+/config` ; Zigbee2MQTT, ESPHome et Tasmota la
  parlent déjà ; la convention est indépendante de Home Assistant lui-même.
- **Convention Homie** — plus propre formellement, adoption bien moindre.
- **Contrôleur Matter en C#** — hors de portée raisonnable (commissioning,
  fabrics, certificats ; implémentations sérieuses en C++/Python seulement).
- **Tout passer par Home Assistant** — dépendance lourde qui déplace le cœur du
  projet.

## Décision

MQTT + la convention **HA MQTT Discovery** comme standard device de House OS :
le serveur s'abonne aux topics de découverte et auto-enregistre tout device
compatible. Firmwares maison en **ESPHome**. Matter, si jamais nécessaire, via un
sidecar (`python-matter-server` ou un HA-pont en conteneur) — jamais de contrôleur
C#. Les radios (Zigbee/Thread/Z-Wave) restent derrière des bridges
(Zigbee2MQTT + Mosquitto en Docker). Choix de l'utilisateur.

## Conséquences

- Compatibilité immédiate avec l'univers Zigbee2MQTT/ESPHome/Tasmota sans firmware
  spécifique House OS.
- Un module `Appareils` (registre + séries temporelles + adaptateurs de transport)
  doit garder MQTT comme adaptateur #1, extensible.
- Rien à construire en v1 — contrainte de design seulement.

## Confirmation

Quand la phase 3 démarre : le module Appareils s'abonne à `homeassistant/+/+/config` ;
aucune référence à un SDK Matter dans `server/`.
