---
type: decision
status: accepted
date: 2026-09-28
feature: "[[Tâches]]"
tags: []
---

# Recherche Globale Sur La Touche Slash

## Contexte

Inventaire des écrans du 2026-09-28 : au bureau, seule la page Documents se
cherchait. La console Tâches comptait 100 définitions en prod (as of 2026-09) sans
champ de recherche, alors que sa version téléphone en avait un ; Équipements
pareil. Deux pickers (`<select>`) avaient débordé : « Lier un document » (~40
documents) et le « Lien » d'une enveloppe (les 100 tâches). Alain a demandé les
recherches par page, les pickers cherchables **et** une recherche globale.

La convention la plus répandue pour une palette de recherche est ⌘K, mais ⌘K est
déjà le quick-add de création de tâche (pages Aujourd'hui et Tâches), documenté
dans [[Tâches]].

## Options considérées

- **⌘K devient la recherche**, le quick-add passe ailleurs (ou devient une action
  de la palette). Colle à la convention ; casse un geste en usage et documenté.
- **`/` pour la recherche**, ⌘K reste le quick-add. Convention GitHub, YouTube,
  Gmail ; aucune habitude à défaire. `/` doit être ignoré quand on écrit déjà.
- **Loupe seulement**, pas de raccourci. Le plus simple ; lent pour qui vit au
  clavier.

## Décision

`/` ouvre la recherche globale, et une loupe dans l'en-tête du bureau fait la même
chose ; ⌘K reste le quick-add. La touche est ignorée dans un champ de saisie ou
quand un éditeur de tâche est ouvert.

La palette cherche les tâches (titre, description, pièce), les équipements (nom,
marque, modèle, pièce), les documents (titre, notes, fichier, dossier) et les
pièces. Six résultats au plus par groupe, puis « et N autres ». Une tâche s'ouvre
dans son éditeur sur place ; le reste mène à sa page, fiche ouverte par un
paramètre d'URL à usage unique (`/equipements?id=`, `/documents?id=`,
`/pieces?zone=`), retiré aussitôt lu.

Toute recherche (pages, pickers, palette) passe par un seul comparateur :
insensible aux accents et à la casse, chaque mot du terme doit apparaître dans au
moins un champ, dans n'importe quel ordre. Filtrage côté client : les listes
tiennent en mémoire et sont déjà en cache pour les pages.

Bureau seulement : la vue téléphone garde ses recherches par écran
([[D-2026-09-19 Portée De La Vue Téléphone]]).

## Conséquences

- Pas d'endpoint de recherche ni d'outil MCP : l'agent MCP filtre déjà les
  listes qu'il lit. À revoir si une liste cesse de tenir en mémoire (OCR des
  documents, historique de complétions).
- Les pages Équipements, Documents et Pièces lisent un paramètre d'URL ; le
  helper de rendu des tests pose donc un routeur en mémoire.
- Le comparateur partagé rend accent-insensibles les recherches de Documents
  (bureau + téléphone) et d'Équipements (téléphone), qui ne l'étaient pas :
  « deneigement » ne trouvait pas « Facture déneigement ».

## Confirmation

- `web/src/components/RechercheGlobale.test.tsx` : `/` ouvre, chaque type mène au
  bon endroit, `/` dans un champ reste un caractère.
- `grep -rn "toLowerCase().includes" web/src/pages` ne doit rien trouver : toute
  recherche passe par `web/src/lib/recherche.ts`.
- `grep -n "'k'" web/src/components/QuickAdd.tsx` : ⌘K reste le quick-add.
