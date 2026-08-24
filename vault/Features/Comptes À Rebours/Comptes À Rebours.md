---
type: feature
status: implemented
last-verified: 2026-08-24
verified-against: 24c1562
tags: []
---

# Comptes À Rebours

## Intention

Née des maquettes : le « 44 dodos avant le déménagement » a plu, mais il doit être
généralisable — un compte à rebours sur **n'importe quoi** (un voyage, Noël, une
visite), pas un widget codé en dur pour le déménagement. Demandé par Alain et Ariane
(2026-08-23).

## Comportement

- Un compte à rebours = titre + date cible + icône (set maison de 8, chaque icône
  porte sa couleur — voir [[D-2026-08-23 Icônes Maison Comptes À Rebours]]). Entité
  du foyer, pas d'assignation.
- La carte « Comptes à rebours » d'Aujourd'hui affiche les comptes à venir en
  « dodos » (registre maison, toujours — pas d'option « jours »), triés par date.
- Le jour J, la ligne se célèbre (« C'est aujourd'hui ! ») ; dès le lendemain elle
  est masquée de la carte mais conservée en base
  ([[D-2026-08-23 Célébration Puis Masquage Des Comptes]]).
- Gestion (créer/modifier/supprimer) dans un modal ouvert par le « + » de la carte ;
  les comptes passés y restent visibles et supprimables
  ([[D-2026-08-23 Gestion Des Comptes Dans Aujourdhui]]). La carte est toujours
  affichée, avec un état vide.
- Chaque compte à venir est un évènement toute-la-journée dans les flux iCal
  personnels ([[D-2026-08-23 Comptes À Rebours Au Flux iCal]]).
- Données amorcées : Déménagement (2026-10-06, camion) et Noël (2026-12-25, sapin),
  remplaçant les deux comptes codés en dur de l'intérim V0.

## Hors périmètre

- Récurrence (un compte à rebours est ponctuel ; Noël se recrée chaque année ou se
  régénère — à trancher plus tard).
- Rappels/notifications dédiés au-delà du flux iCal.
- La phrase d'humeur du héros garde sa propre date de déménagement codée en dur
  (`web/src/lib/humeur.ts`) — concern distinct, retiré naturellement après le
  6 octobre 2026.

## Décisions

- [[D-2026-08-23 Célébration Puis Masquage Des Comptes]]
- [[D-2026-08-23 Icônes Maison Comptes À Rebours]]
- [[D-2026-08-23 Gestion Des Comptes Dans Aujourdhui]]
- [[D-2026-08-23 Comptes À Rebours Au Flux iCal]]

## Ancres de code

- `server/HouseOs.Api/Domaine/CompteARebours.cs` — entité + enum d'icônes
- `server/HouseOs.Api/Features/ComptesARebours/ComptesAReboursEndpoints.cs` — CRUD
- `server/HouseOs.Api/Features/FluxIcal/FluxIcalEndpoints.cs` — évènements iCal
- `web/src/pages/Aujourdhui.tsx` — carte + modal de gestion
- `web/src/components/Illustrations.tsx` — les 8 icônes SVG
- `web/src/lib/format.ts` — calcul des dodos

## Sources

- Maquettes `design/maquettes/` (cartes compte à rebours dans les planches finales).

## Historique

- [[Plan 2026-08-23 Comptes À Rebours V1]]
- [[Recap Comptes À Rebours]]
