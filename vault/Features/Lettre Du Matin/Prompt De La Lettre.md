---
type: reference
last-verified: 2026-09-30
verified-against: 6d1fda0
tags: []
---

# Prompt De La Lettre

Le prompt système de [[Lettre Du Matin]], écrit et éprouvé le 2026-09-21 contre cinq
journées (voir [[Exemples De Lettres]]). Il est au courriel ce que `PromptParDefaut`
(`server/HouseOs.Api/Features/Editorial/RedactionLlm.cs`) est au mur : mêmes principes
d'atelier, aucun fait inventé, sortie JSON validée strictement, registre et espace
différents. Devenu la constante `PromptParDefaut` de la classe `RedactionLettre`
([[D-2026-09-21 Lettre Écrite À Part Sur La Même Matière]]).

Il est écrit contre `MatiereDeLettre` (`server/HouseOs.Api/Domaine/Lettre/`) : la
matière de l'édition, plus la semaine devant, ce qui a été fait depuis la dernière
lettre, la série de chaque tâche due et les sept lettres précédentes — sujet, première
ligne et, depuis le 2026-09-28, paragraphes entiers. Les trois ajouts ont reçu leur ligne à l'étape 3 du
plan (as of 2026-09-21), et la série débloque la phrase que la première version
interdisait (point 7 plus bas).

## Le prompt système

Le texte se lit dans le code, à un seul endroit : la constante
`RedactionLettre.PromptParDefaut`
(`server/HouseOs.Api/Features/Lettre/RedactionLettre.cs`). Cette note en garde les
raisons — le contrat de sortie, les choix de rédaction et ce que les essais ont
appris — pas la copie, qui dérivait à chaque retouche (retirée le 2026-09-30, alors
identique à la constante).

## Le contrat de sortie

```json
{"sujet": "Demain, le camion", "paragraphes": ["…", "…", "…"]}
```

Validation stricte, sur le patron de `RedactionLlm.Extraire` : accolades et prose
tolérées autour du JSON, puis tout écart rend `null`. Un `null` ne part pas en note tout
de suite : un second essai a lieu une heure plus tard, et c'est seulement s'il échoue
aussi que la note de quatre lignes part
([[D-2026-09-21 Lettre Écrite À Part Sur La Même Matière]]). Les textes sont normalisés
avant mesure (trim, blancs multiples réduits à un espace), comme au mur.

| Champ | Règle |
|---|---|
| `sujet` | chaîne, 1 à **60** signes, sans saut de ligne, sans point final |
| `paragraphes` | tableau de **3 à 5** chaînes |
| `paragraphes[n]` | 60 à **360** signes chacun (le prompt dit 320 : voir « Rejoué au vrai modèle ») |
| `paragraphes[0]` | au moins **100** signes, pour que l'aperçu ne soit pas un moignon |
| total | somme des paragraphes ≤ **1400** signes |

Trois refus de plus, propres à la lettre et absents du contrat du mur :

- **Salutation ou signature**. `paragraphes[0]` ne commence pas par « Bonjour », « Salut »
  ou « Allô » ; aucun paragraphe ne contient « la maison » précédé d'un tiret, et le
  dernier ne se termine pas par « Bonne journée ». Le gabarit les pose, le modèle les
  redonne par réflexe, et deux bonjours dans un courriel se voient tout de suite.
- **Mise en forme**. Un paragraphe qui commence par un tiret, une puce, un dièse ou un
  chiffre suivi d'un point est une liste déguisée : refusé. La prose est la feature.
- **Point d'exclamation**. Un seul suffit à faire basculer la lettre dans le registre de
  l'infolettre — sauf ceux que la matière porte elle-même : la première lettre de prod
  (2026-09-21) a été refusée parce que la tâche du jour s'appelle « Boites! » et que le
  modèle l'a citée telle quelle, comme demandé. Les titres de la matière qui portent un
  « ! » sont retirés du texte avant la vérification (`RedactionLettre.TitresAvecExclamation`).

### Ce que les bornes valent, mesuré

Les cinq lettres d'[[Exemples De Lettres]], écrites sans regarder les bornes puis
mesurées : sujets de 17 à 51 signes, paragraphes de 173 à 291 signes, quatre lettres à
quatre paragraphes et une à cinq, totaux de 823 à 1183 signes. Le maximum de 291 signes
tombe sur la journée chargée, celle qui pousse le plus à l'énumération. Les bornes
proposées au départ (320 par paragraphe, 1400 au total, 3 à 5 paragraphes) sont donc
justes : assez serrées pour que le plafond morde, assez larges pour qu'une lettre écrite
naturellement passe sans se faire couper. La seule que j'ajoute est le **plancher** de
100 signes sur le premier paragraphe, parce que le contrat ne peut pas vérifier qu'un
aperçu porte la journée, mais il peut vérifier qu'il y a un aperçu.

## Les choix de rédaction, et ce que les essais ont appris

1. **La salutation et la signature sortent du modèle**, au gabarit : il ne peut plus les
   faire dériver ni dépenser de signes dessus, et la borne mesure la lettre.
2. **Ce qui radotait, c'est l'ouverture, pas les formules.** Trois brouillons sur cinq
   ouvraient sur la météo ; le prompt a dû interdire le patron d'ouverture, pas
   seulement l'angle et l'image comme au mur.
3. **Ce qui débordait, c'est la journée chargée.** Quatorze tâches appellent une liste ;
   la règle du plafond a fait tenir le 2026-10-20 en cinq paragraphes.
4. **Ce qui sonnait faux, ce sont les objets** : le râteau, la lumière de la cuisine, les
   boîtes pas défaites, et la veille du déménagement un passé inventé (« je vous ai vus
   arriver »). Mes meilleures phrases, aucune dans la matière. D'où la clause explicite.
5. **Les prénoms sans pronom** : la matière donne `assigne` et rien d'autre, et aucune
   des cinq lettres n'a eu besoin d'en déduire un genre.
6. **Le registre tient à une chose** : commenter est permis, prescrire ne l'est pas. Un
   retard se constate. C'est la seule frontière que les cinq essais ont frôlée souvent.
7. **Le trou de matière.** « La même que les dix derniers dimanches », la ligne la plus
   admirée de la maquette C1, **n'est pas dérivable** : la matière ne porte que sept
   lettres et `joursDeRetard`. Le prompt l'interdit et se rabat sur « comme dimanche
   dernier » ; la série de `MatiereDeLettre`
   ([[D-2026-09-21 Lettre Écrite À Part Sur La Même Matière]]) la rachètera.

## Rejoué au vrai modèle (2026-09-21, étape 6 du plan)

Deux journées composées depuis la base de dev par `matiere-lettre --composer` et
envoyées à Opus par `rediger-lettre`. Le 24 septembre (rang Chronique, aucune tâche)
a passé du premier coup : 1001 signes, quatre paragraphes, deux d'entre eux à 293 et
302 signes. Le 20 octobre (rang Sommaire, quatorze tâches) a été **refusé** : le
quatrième paragraphe faisait 338 signes. Opus déborde la cible de quinze à vingt pour
cent, comme au mur. La cible est passée de 280 à **250** signes et le 20 octobre a
été rejoué : refusé encore, à 328. Le paragraphe des faits (le ciel, le climat, les
dodos) atterrit autour de 330 quoi que dise la consigne. Le mur a une colonne, le
courriel n'en a pas : le **contrat tolère 360** pendant que le prompt continue de
dire 320, exactement le couple 200/240 de l'édition. Le sujet et le total, eux,
sont restés loin des bornes (1001 et 1157 signes).

## La première semaine relue (2026-09-28)

Huit lettres de prod, du 21 au 28 septembre, relues ensemble : le même plan chaque
matin (les tâches, un compteur, le ciel et la météo, la semaine devant), la remarque
« ni échéance ferme ni prénom : elles n'appartiennent à personne » sept fois sur huit,
la clarté qui raccourcit huit fois, le gel et la douceur six, la cuisine jamais cochée
et les séances cinq, un sujet en « X, les boîtes, et Y » cinq fois. Même cause qu'au
mur : la mémoire ne portait que le sujet et la première ligne
([[D-2026-09-28 Fraîcheur Cumulée Des Faits]]). Trois règles ajoutées — un fait ou une
remarque déjà dits ne reviennent pas, même reformulés, un compteur qui prend une unité
n'a pas changé ; pas de plan fixe ni de patron de sujet ; la nouveauté avant
l'exhaustivité. Rejoué sur la matière du 28 : l'ancien prompt, même avec les
paragraphes, refait les séances et le ciel ; le nouveau, trois essais sur trois, n'en
reprend rien et va chercher dimanche, le propane, Tanguay et le lit en vente. Il a
fallu nommer « seul à porter un prénom » : sans ça, la remarque revenait reformulée
deux fois sur trois.
