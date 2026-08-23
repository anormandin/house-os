---
type: meta
---

# Conventions

## Langue

- Corps et titres des notes : **français**.
- Clés de frontmatter et valeurs énumérées : anglais, toujours (couche machine).
- Code : termes du domaine en français (Tâche, Occurrence, Équipement…), code
  technique (infra, helpers) en anglais si plus naturel. UI 100 % français, chaînes en
  dur (pas de lib i18n).

## Nommage

- Noms de fichiers : Title Case avec espaces, uniques dans tout le vault (accents permis).
- Décisions : `D-YYYY-MM-DD <Slug>.md` — date d'acceptation, jamais renuméroté.
- Plans : `Plan YYYY-MM-DD <Slug>.md` dans le dossier de la feature.
- Recaps : `Recap <Feature>.md` dans le dossier de la feature.

## Liens

- Wikilinks partout : `[[Note]]`, `[[Note#Titre]]`, `[[Note|alias]]`.
- Dans le frontmatter, wikilinks entre guillemets : `feature: "[[Tâches]]"`.
- Pas de liens markdown dans le vault. Les pointeurs vers le code sont en texte brut
  `chemin/vers/fichier.ext:ligne`.

## Faits

- Tout fait stocké est intemporel, daté `(as of YYYY-MM)`, ou un pointeur vers le code.
- Pas de blocs de code décrivant le code actuel — pointeurs seulement.
- Les notes vivantes portent `last-verified` + `verified-against` (sha court).

## Tags

Les tags servent aux facettes transversales plates ; la structure vient des liens et
dossiers. Taxonomie : `#backfill` (décisions rétro-datées), `#iot`, `#securite`,
`#dette`.
