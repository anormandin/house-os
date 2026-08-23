---
type: feature
status: draft
last-verified: 2026-08-23
verified-against: 5f9f080
tags: []
---

# Comptes À Rebours

## Intention

Née des maquettes : le « 44 dodos avant le déménagement » a plu, mais il doit être
généralisable — un compte à rebours sur **n'importe quoi** (un voyage, Noël, une
visite), pas un widget codé en dur pour le déménagement. Demandé par Alain et Ariane
(2026-08-23).

## Comportement

- Quand un utilisateur crée un compte à rebours (titre, date cible, icône
  optionnelle), il apparaît dans la carte « Comptes à rebours » d'Aujourd'hui,
  formulé en « dodos » (registre maison) ou en jours.
- Quand la date est passée, le compte à rebours s'archive (ou se célèbre) — à
  préciser au design détaillé.
- Les comptes à rebours sont candidats naturels pour la vue e-ink et le flux iCal.

## Hors périmètre

- Récurrence (un compte à rebours est ponctuel ; Noël se recrée chaque année ou se
  régénère — à trancher plus tard).
- Rappels/notifications dédiés (v1 : affichage seulement).

## Décisions

- (Aucune encore — feature esquissée, non planifiée. Cible probable : V1b ou phase 2.)

## Intérim (V0, 2026-08-23)

En attendant la vraie feature, la carte « Comptes à rebours » d'Aujourd'hui affiche
deux comptes calculés côté client et codés en dur : Déménagement (2026-10-06, masqué
une fois passé) et Noël (prochain 25 décembre). Pas de bouton « Ajouter » tant que
le CRUD n'existe pas — on n'affiche pas de contrôle mort.

## Ancres de code

- `web/src/pages/Aujourdhui.tsx` — carte intérimaire (comptes codés en dur)
- `web/src/lib/format.ts` — calcul des dodos

## Sources

- Maquettes `design/maquettes/` (cartes compte à rebours dans les planches finales).

## Historique

<!-- Plan et recap à venir. -->
