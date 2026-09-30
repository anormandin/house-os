---
type: feature
status: implemented
last-verified: 2026-09-30
verified-against: c4fdf0e
tags: []
---

# Comptes À Rebours

## Intention

Née des maquettes : le « 44 dodos avant le déménagement » a plu, mais il doit être
généralisable — un compte à rebours sur **n'importe quoi** (un voyage, Noël, une
visite), pas un widget codé en dur pour le déménagement. Demandé par Alain et Ariane
(2026-08-23).

## Comportement

- Un compte à rebours = titre + date cible + icône (set maison de 16, chaque icône
  porte sa couleur — voir [[D-2026-08-23 Icônes Maison Comptes À Rebours]] ; 8 icônes
  saisonnières/activités ajoutées le 2026-08-25 : flocon, citrouille, feuille, fleur,
  tente, vélo, ballon, étoile). Entité du foyer, pas d'assignation.
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
- **Aucune donnée amorcée** (as of 2026-09-02, dépôt public) : les comptes d'un foyer
  lui appartiennent. L'`InsertData` d'origine (deux comptes du premier foyer) a été
  retiré de la migration `ComptesARebours` ; sans effet sur une base déjà migrée.
- Modifier sans redonner l'icône **conserve** celle du compte (REST comme MCP) ; à la
  création, le défaut est `Soleil`.
- Parité MCP : `gerer_comptes_a_rebours` (lister avec les passés, creer, modifier,
  supprimer — [[Serveur MCP]]).
- Le **prochain** compte à venir sert ailleurs, en dodos : la banque client du
  [[Titre D'humeur]] (repli ultime), la matière du [[Journal De La Maison]] et de la
  [[Lettre Du Matin]] (où un compte à zéro fait plancher).

## Hors périmètre

- Récurrence (un compte à rebours est ponctuel ; Noël se recrée chaque année ou se
  régénère — à trancher plus tard).
- Rappels/notifications dédiés au-delà du flux iCal.
- Une date de foyer écrite dans le code : la date codée en dur de la phrase d'humeur
  (`web/src/lib/humeur.ts`) a été retirée (dépôt public, 2026-09-02) — la banque client
  lit le prochain compte à rebours de l'API, et ses titres de compte proche ne parlent
  plus de déménagement (2026-09-30 : mêmes mots que la banque serveur).

## Décisions

- [[D-2026-08-23 Célébration Puis Masquage Des Comptes]]
- [[D-2026-08-23 Icônes Maison Comptes À Rebours]]
- [[D-2026-08-23 Gestion Des Comptes Dans Aujourdhui]]
- [[D-2026-08-23 Comptes À Rebours Au Flux iCal]]

## Ancres de code

- `server/HouseOs.Api/Domaine/CompteARebours.cs` — entité + enum d'icônes
- `server/HouseOs.Api/Features/ComptesARebours/ComptesAReboursEndpoints.cs` — CRUD
- `server/HouseOs.Api/Features/FluxIcal/FluxIcalEndpoints.cs` — évènements iCal
- `web/src/pages/Aujourdhui.tsx` — la carte et le « + » ;
  `web/src/components/ComptesAReboursGestion.tsx` — le modal de gestion
- `server/HouseOs.Api/Features/Mcp/OutilsMaison.cs` — `gerer_comptes_a_rebours`
- `server/HouseOs.Tests/Integration/ComptesAReboursApiTests.cs` — le CRUD, l'icône conservée
- `web/src/components/Illustrations.tsx` — les icônes SVG (`ICONES_COMPTE`)
- `web/src/lib/format.ts` — calcul des dodos

## Sources

- Maquettes `design/maquettes/` (cartes compte à rebours dans les planches finales).

## Historique

- [[Plan 2026-08-23 Comptes À Rebours V1]]
- [[Recap Comptes À Rebours]]
