---
type: decision
status: accepted
date: 2026-09-21
feature: "[[Lettre Du Matin]]"
tags: []
---

# D-2026-09-21 Adresse De Courriel Sur L'Utilisateur

## Contexte

`Utilisateur` n'a pas d'adresse de courriel : les comptes sont seedés depuis le `.env`
(`COMPTE_n_NOM`, `COMPTE_n_AFFICHAGE`, `COMPTE_n_MDP`) et l'app n'a jamais eu besoin
d'écrire à quelqu'un. La [[Lettre Du Matin]] en a besoin.

## Options considérées

- **Colonne `Courriel` sur `Utilisateur`, seedée du `.env`** (retenue) :
  `COMPTE_n_COURRIEL` remplit la colonne à l'amorçage, comme le jeton iCal. L'adresse
  appartient à la personne : le jour où chacun veut son heure ou se désabonne, rien à
  déplacer. Coût : une migration EF.
- **Liste au niveau de la maison** (`LETTRE_DESTINATAIRES=a@x,b@y`) : aucun schéma
  touché, mais l'adresse n'est reliée à personne ; l'app ne peut pas dire « envoyée à
  Alain et Ariane » ni brancher un réglage par personne plus tard.

## Décision

**Une colonne `Courriel` nullable sur `Utilisateur`**, remplie à l'amorçage depuis
`Seed:Utilisateurs:n:Courriel` (variable `COMPTE_n_COURRIEL`, facultative). Un
utilisateur sans adresse ne reçoit rien et n'empêche rien. L'amorçage met la colonne
à jour si la valeur du `.env` change (contrairement au mot de passe, jamais réécrit).

## Conséquences

- Migration EF générée (jamais de schéma à la main).
- `lister_utilisateurs` (MCP) et `GET /api/utilisateurs` exposent l'adresse, parité
  oblige ([[Serveur MCP]]).
- Pas d'édition de l'adresse dans l'app pour l'instant : le `.env` est la source, comme
  pour le nom d'affichage.

## Confirmation

`grep -n "Courriel" server/HouseOs.Api/Domaine/Utilisateur.cs` retourne la propriété,
et `grep -n "COMPTE_1_COURRIEL" docker-compose.yml .env.example docs/configuration.md`
retourne une ligne dans chacun.
