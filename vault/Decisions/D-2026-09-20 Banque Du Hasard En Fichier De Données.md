---
type: decision
status: accepted
date: 2026-09-20
feature: "[[Fonds De Tiroir]]"
tags: []
---

# D-2026-09-20 Banque Du Hasard En Fichier De Données

## Contexte

La famille « le hasard » ([[Fonds De Tiroir]], étape 4 du
[[Plan 2026-09-20 Journal Éditorial]]) a besoin de deux banques : des dictons météo et
une table de fêtes et de journées nationales. Les deux sont **régionales** : les dictons
sont en français et parlent du climat d'ici, et les jours fériés du Québec ne sont pas
ceux de l'Ontario, encore moins ceux de la Bavière.

Le dépôt est public et un autre foyer l'installe ([[Distribution]]) : écrire vingt
fêtes québécoises en dur dans une classe C# donnerait à un foyer allemand un journal qui
lui souhaite bonne Saint-Jean. L'étape est par ailleurs sans migration et sans schéma.

Complication propre aux fêtes : **la moitié des jours fériés du Québec sont mobiles**
(Pâques et ses deux congés, la Journée des patriotes, la fête du Travail, l'Action de
grâce). Une liste de dates fixes serait fausse quatre jours par an.

## Options considérées

- **Une table amorcée en base** — modifiable dans l'UI, mais elle demande une migration
  (hors périmètre de l'étape), un écran de gestion, et une parité MCP, pour de la donnée
  d'édition que personne ne change deux fois par an.
- **Une section d'`appsettings.json`** — zéro plomberie neuve, mais `appsettings.json`
  est la configuration technique de l'app, dans le dépôt : y verser quarante-quatre
  entrées de contenu la noie, et « remplacer la banque » voudrait dire éditer un fichier
  suivi par git.
- **Un fichier de données livré avec l'app, remplaçable par une clé de configuration**
  (retenue) — la banque est du contenu, elle vit dans un fichier de contenu ; l'app en
  livre une version québécoise par défaut, comme elle livre déjà des coordonnées météo
  de la ville de Québec.

## Décision

La banque du hasard est un **fichier JSON de données**. L'app en livre une version
québécoise dans sa tranche (`banque-du-hasard.qc.json`) ; la clé `Hasard:Fichier`
(`.env` : `HASARD_FICHIER`) la remplace par celle du foyer. Documenté dans
`docs/configuration.md`.

**Les règles restent du C# testable** ([[D-2026-08-23 Pas De N8n Dans Le Cœur]]) : le
fichier ne contient aucune expression à évaluer. Une fête déclare quand elle tombe sous
l'une de **quatre formes déclaratives** — date fixe ; n-ième jour de semaine du mois
(rang négatif = depuis la fin) ; dernier jour de semaine avant une date ; décalage en
jours depuis Pâques. Les quatre suffisent aux systèmes de fêtes occidentaux ; le comput
grégorien est écrit à la main, comme les éphémérides.

## Conséquences

- La banque est lue **une fois au démarrage** (singleton) : c'est de la donnée
  d'édition, pas de l'état, et un fichier immobile n'a pas à être relu à chaque rendu.
- Une banque **configurée** mais introuvable ou illisible **ne retombe pas** sur celle du
  Québec : la famille se tait. Servir des jours fériés d'ici à un foyer qui a justement
  demandé les siens serait pire que le silence. Une banque **absente de la configuration**
  utilise celle qui est livrée. Le démarrage journalise le fichier lu et le compte
  d'entrées retenues.
- Une entrée incomplète est **écartée**, pas fatale : une faute dans une ligne ne coûte
  ni la banque, ni le journal du matin. Le fichier tolère les commentaires et les
  virgules finales — il se modifie à la main, à côté du `.env`.
- Le **nombre d'entrées de la banque devient la rareté du fait** `hasard.fete` : « combien
  de jours par année il peut paraître » se compte dans la donnée, au lieu d'être un
  nombre deviné et écrit en dur.
- Un foyer qui ne veut pas de dictons vide la section : la famille se tait, le reste du
  journal sort.

## Confirmation

La banque n'est jamais en dur : aucune fête ni aucun dicton n'apparaît dans du C#.

```
grep -rn "Saint-Jean\|dicton" server/HouseOs.Api --include=*.cs
```

ne retourne que des commentaires de documentation, la clé `hasard.dicton` et son
étiquette — jamais un nom de fête ni un proverbe. `LectureDeLaBanque.Lire` est le seul
chemin par lequel une banque entre dans l'app.
