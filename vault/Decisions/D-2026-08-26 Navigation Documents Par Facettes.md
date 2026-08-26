---
type: decision
status: accepted
date: 2026-08-26
feature: "[[Documents]]"
tags: [ux]
---

# Navigation Documents par facettes

## Contexte

La liste plate des documents (rangées chronologiques + fiche à droite) devient
pénible à ~20 documents et le corpus va grossir (manuels, factures, assurances,
impôts). Cinq maquettes ont été produites (artifact « Maquettes Documents »),
puis quatre itérations sur la direction retenue (artifact « Facettes Documents »).

## Options considérées

- **Groupes repliables par dossier** — proche de l'existant, mais ne passe pas à
  l'échelle seul.
- **Ligne du temps** — zéro classement, mais faible pour les documents sans date.
- **Table dense seule** — dense mais sans aperçu du corpus.
- **Facettes latérales + liste** (maquette 4) puis 4 itérations : A tiroir,
  B hiérarchie/vues, C table dense, D barre horizontale.
- **Grille visuelle** — reconnaissance visuelle, densité trop faible.

## Décision

Itération **C** : barre latérale de facettes (Échéances proches épinglées en
haut ; Catégories, Lieux & dossiers, Équipements avec compteurs, une sélection
active par bloc, combinables en ET) + **table dense triable** au centre
(Titre, Catégorie, Lié à, Daté du, Échéance, téléchargement) + **pagination
côté client** (25/page) + **tiroir de détail** (itération A) par-dessus le bord
droit pour la fiche éditable. Filtrage, tri, compteurs et pagination restent
côté client — l'API liste tout comme avant. Choisi par Alain le 2026-08-26
(« i want C, build it ») parce que le corpus visé est grand.

## Conséquences

- `web/src/pages/Documents.tsx` est réécrite (facettes + table + tiroir) ; la
  bannière « Échéances proches » migre dans la barre latérale.
- La facette « Lieux & dossiers » exige un nouveau champ — voir
  [[D-2026-08-26 Dossier De Document]].
- Les vues enregistrées et la hiérarchie lieu→pièce (itération B) restent une
  évolution possible, non retenues pour l'instant.

## Confirmation

`grep -l "Lieux & dossiers" web/src/pages/Documents.tsx` retourne le fichier ;
la page ne rend plus de bannière pleine largeur d'échéances (bloc latéral).
