---
type: decision
status: accepted
date: 2026-09-28
feature: "[[Équipements]]"
tags: []
---

# Catégorie D'équipement En Liste Fermée

## Contexte

Les packs d'entretien d'[[Emménagement V2]] proposent des tâches selon ce qu'est un
équipement : une thermopompe ne demande pas les mêmes gestes qu'un chauffe-eau. Au
2026-09-28, `Equipement` (`server/HouseOs.Api/Domaine/Equipement.cs`) ne porte que
nom, marque, modèle, série, dates, notes et specs JSONB libres — rien ne dit ce
qu'il est. Dix équipements en prod, saisis à la main.

## Options considérées

- **Colonne `Categorie`, liste fermée** (retenue) — enum C#, migration EF ; les packs
  s'y accrochent, la liste par zone peut filtrer, les validations la voient.
  Coût : une migration et une valeur à choisir par fiche.
- **Clé conventionnelle dans les specs JSONB** — pas de migration, mais du texte
  libre : « Chauffage », « chauffage » et « Fournaise » seraient trois catégories, et
  rien ne le validerait.
- **Pas de catégorie** — le pack se choisit à la main dans une liste au moment de
  proposer. L'équipement ne sait pas ce qu'il est ; chaque proposition redemande.

## Décision

Alain, 2026-09-28. `Equipement.Categorie`, nullable, enum `CategorieEquipement` :
`Chauffage`, `EauChaude`, `Plomberie`, `Electricite`, `Toiture`, `Exterieur`,
`PetitsMoteurs`, `Electromenager`, `Vehicule`, `Autre`. Null = « pas encore classé » :
les fiches existantes le restent jusqu'à ce qu'on les classe, et un équipement sans
catégorie ne reçoit pas de proposition de pack. La liste s'étend par migration, jamais
par saisie.

## Conséquences

- Migration EF (T3) ; REST, MCP et les deux vues web (bureau, téléphone) exposent la
  catégorie ; le skill `inventorier-maison` la renseigne à la création.
- Un équipement classé `Autre` n'a pas de pack : c'est la catégorie du reste, pas une
  invitation à en écrire un.
- Le jour où la liste devient trop courte, on l'étend ; on ne la remplace pas par du
  texte libre.

## Confirmation

- `grep -n "enum CategorieEquipement" server/HouseOs.Api/Domaine/Equipement.cs`
  trouve l'enum avec ses dix valeurs.
- Tests `EquipementsApiTests.CreerAvecCategorieInconnue_Repond400` (REST) et
  `OutilsMaisonTests.Gerer_equipement_refuse_une_categorie_inconnue` (MCP).

> [!note] Mise à jour de la seule Confirmation (2026-09-30)
> Le test nommé à l'écriture n'a jamais existé sous ce nom ; les deux tests qui couvrent
> le comportement sont nommés à sa place. Le corps de la décision n'a pas été touché.
