---
type: reference
last-verified: 2026-08-23
verified-against: 5f9f080
tags: []
---

# Typographie Québécoise

Normes de rédaction pour que l'UI se sente *native*, pas traduite. Source : OQLF
(vitrine linguistique) + norme fédérale ISO 8601.

## Dates et heures

- Date longue : « samedi 23 août » — pas de majuscule au jour ni au mois.
- Date numérique : **ISO 8601 (2026-08-23)**, jamais 08/23.
- Heure : **24 h avec « h » espacé : « 19 h 30 », « 9 h »** — pas de zéro de tête en
  texte courant ; « 19:30 » acceptable en contexte tabulaire/technique. Jamais
  d'AM/PM.

## Unités et symboles

- Température : « 21 °C » (espace avant °C).
- Espace insécable avant % et les unités.

## Registre et libellés

- Registre : l'app parle *de la maison*, pas *à un utilisateur* — libellés
  impersonnels (« À faire ce soir », « Tout est beau ✓ »). Quand un verbe s'impose :
  tutoiement (naturel dans sa propre cuisine).
- Capitale sur le premier mot seulement : « Liste d'épicerie », pas
  « Liste D'Épicerie ».

## Application

- Centraliser le formatage dans `web/src/lib/` (helpers date/heure français) plutôt
  que d'éparpiller des `toLocaleDateString`.
- Ces règles s'appliquent aussi au rendu e-ink et aux flux iCal (titres d'événements).
