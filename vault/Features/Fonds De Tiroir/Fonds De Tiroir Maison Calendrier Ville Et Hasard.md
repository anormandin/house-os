---
type: reference
last-verified: 2026-09-30
verified-against: 6d1fda0
tags: []
---

# Fonds De Tiroir Maison Calendrier Ville Et Hasard

Sous-note de [[Fonds De Tiroir]] : les quatre familles qui lisent des données — le
journal et les équipements du foyer, ses échéances, les flux de la ville, et la banque
du hasard ([[D-2026-09-20 Banque Du Hasard En Fichier De Données]]). Le contrat du
fait, le score et les sources sont dans [[Fonds De Tiroir]] ; le ciel et le climat,
dans [[Fonds De Tiroir Ciel Et Climat]].

## La maison, en détail (as of 2026-09-20)

Les seuils sont éditoriaux, pas techniques : ils disent à partir de quand une chose
vraie devient une chose qu'on a envie de lire.

| Clé | Rareté | Pertinence | Ne sort que si |
|---|---|---|---|
| `maison.serie` | tous les jours | 1,5 | trois jours d'affilée au moins, et le fait `maison.record` ne parle pas (donc : série < record, **ou** série trop courte pour être un record) |
| `maison.record` | 6×/an | 2 | la série en cours **égale ou dépasse** le record, à partir de cinq jours |
| `maison.seances` | tous les jours | 1,5 · **3** aux dizaines | une tâche a été cochée dix fois ou plus |
| `maison.doyen` | tous les jours | 1,5 · **3** si l'entretien est dans la quinzaine | le plus vieil équipement daté a au moins un an |
| `maison.piece-oubliee` | tous les jours | 2 · **3** passé six mois | une zone **qui a des tâches** n'a rien eu de coché depuis soixante jours (ou jamais) |
| `maison.cout` | tous les jours | 1,5 | au moins un coût consigné depuis le 1er janvier |
| `maison.anniversaire` | 12×/an | 2 | un équipement ou un jalon du foyer a son mois-jour aujourd'hui, et au moins un an |
| `maison.an-dernier` | 180×/an | 1,5 | quelque chose a été coché un an jour pour jour avant aujourd'hui |

> [!warning] Deux seuils qui se croisent font un trou, et il faut les croiser exprès.
> La série se tait quand elle **est** le record (deux colonnes pour le même chiffre,
> c'est une colonne perdue) et le record ne parle qu'**à partir de cinq jours**. Pris
> séparément, les deux seuils laissaient muette une série de trois ou quatre jours qui
> est aussi le record — c'est-à-dire **la première série d'une maison neuve**, le moment
> précis que ce fait existe pour raconter. La série ne se tait donc que lorsque le record
> parle vraiment, et elle change de texte quand elle est le meilleur résultat à ce jour,
> plutôt que de citer un record égal au chiffre qu'elle affiche déjà. Trouvé en revue de
> code, étape 4.

> [!note] La série se compte **jusqu'à hier** quand la journée n'a rien donné.
> À six heures du matin rien n'est encore fait. Exiger une complétion du jour ferait
> annoncer « série rompue » chaque matin et « douze jours » chaque soir — un journal qui
> se contredit entre deux réveils. La série se casse à la fin de la journée, pas à son
> premier café.

## Le calendrier, en détail (as of 2026-09-20)

| Clé | Rareté | Pertinence | Ne sort que si |
|---|---|---|---|
| `calendrier.compte-a-rebours` | tous les jours | 1,5 · **3** la dernière semaine | la cible est devant et à moins de cent vingt jours |
| `calendrier.ca-s-en-vient` | tous les jours | 1,5 | **au moins deux** échéances ouvertes entre 7 et 30 jours — en deçà de sept, la liste du jour les montre déjà |
| `calendrier.saison` | 56×/an (fermeture), 28×/an (ouverture) | 2 · 1 | une fenêtre saisonnière se referme dans la quinzaine, sinon une qui s'est ouverte dans la semaine |
| `calendrier.expiration` | 60×/an | 1,5 · **3** dans la quinzaine | une garantie d'équipement ou l'échéance d'un document tombe dans les soixante jours |

> [!warning] Le compte à rebours sort **deux fois** si le consommateur n'y prend garde.
> [[Journal De La Maison]] dessine déjà son encadré, et le fonds ne sait pas qu'un
> encadré existe — c'est la décision, et la lettre du matin voudra le fait. C'est donc
> au consommateur d'écarter le doublon : `CLES_DEJA_AU_JOURNAL`
> (`web/src/lib/ecran-vues.ts`), testé. Trouvé à l'étape 3.

## La ville, en détail (as of 2026-09-21)

Trois items, et **rien de municipal** : la matière arrive par [[Flux Externes]], dont
le **type** fait le tri — un flux `Collecte` donne les collectes, un flux `Municipal`
donne les événements ([[D-2026-09-20 Sources Municipales Séparées Par Solidité]]).

| Clé | Rareté | Pertinence | Ne sort que si |
|---|---|---|---|
| `ville.collecte` | tous les jours | 1,5 · **3** la veille · 2 le jour même | une collecte est devant, à moins de sept jours — et ce n'est pas la collecte spéciale |
| `ville.collecte-speciale` | comptée dans le flux (fenêtre de 14 j) | 2 · **3** la veille | une collecte dont le titre **ne revient pas** dans la fenêtre du flux tombe dans les quinze jours, et le calendrier porte au moins quatre collectes |
| `ville.evenement` | comptée dans le flux (fenêtre de 7 j) | 1,5 · **2** aujourd'hui ou demain | un flux `Municipal` encore alimenté annonce quelque chose dans la semaine |

> [!warning] Un flux périmé cesse de sortir au lieu de mentir.
> C'est **la** règle de la famille ([[D-2026-09-20 Flux Externe Poussé]]). Un gratteur
> mort il y a un mois laisserait son programme passer pour celui de cette semaine ; un
> calendrier de collectes qui ne se télécharge plus finirait par annoncer l'an dernier.
> Au-delà de **sept jours** sans remplacement — téléchargement ICS réussi ou poussée
> reçue, c'est le même horodatage — un flux ne nourrit plus le fonds. Sept et non trois :
> un flux ICS se retélécharge toutes les six heures et un flux poussé une fois par jour ;
> sept jours de silence, des deux côtés, est une panne et non un creux.

> [!note] La collecte spéciale se reconnaît à ce qu'elle ne revient pas.
> On ne sait pas ce qu'une collecte **est** — aucune connaissance municipale n'entre
> dans le dépôt : on voit qu'un titre ne paraît qu'une fois dans la fenêtre du flux, là
> où le bac hebdomadaire y revient huit fois. C'est générique (ça vaut pour n'importe
> quelle ville) et c'est la seule marque disponible. Conséquence assumée : une collecte
> saisonnière qui n'a qu'une occurrence dans la fenêtre (les feuilles, au printemps)
> sort aussi comme « spéciale » — ce qui est vrai, et utile. Garde-fou : en deçà de
> quatre collectes, le calendrier n'a pas assez d'habitudes pour qu'on juge, et le fait
> se tait.

> [!note] Quand la spéciale est aussi la prochaine, une seule des deux parle.
> Deux widgets pour le même camion, c'est une colonne perdue — même croisement voulu
> que la série et le record de la maison. C'est `ville.collecte` qui se tait : la
> spéciale dit la même chose, mieux.

> [!warning] Ce que le journal dessine à part doit être **écrit par le fonds**.
> Le journal garde une place fixe à la prochaine collecte — sortir le bac est le geste
> du soir, il ne doit pas dépendre d'un classement — et `ville.collecte` rejoint donc
> `CLES_DEJA_AU_JOURNAL` (`web/src/lib/ecran-vues.ts`) avec le compte à rebours. Mais
> ce widget lisait jusqu'à l'étape 6 une **colonne à part** (`DonneesEcran.ProchaineCollecte`)
> qui ne jugeait ni la fraîcheur du flux ni la distance : la seule règle de la famille
> était court-circuitée par le seul consommateur qui l'affiche. Le widget prend
> maintenant les mots du fait, et la colonne a disparu du contrat. Trouvé en revue de
> code, étape 6.

> [!warning] Le bandeau du jour publiait la ville une seconde fois.
> `EvenementsDuJour` montrait **tous** les événements externes du jour, types compris,
> sans rien juger : le jour d'une séance du conseil, le mur l'affichait en « Aujourd'hui,
> au calendrier » *et* en widget « En ville » — et l'affichait encore si le gratteur était
> mort depuis un mois. Le bandeau laisse désormais les types `Collecte` et `Municipal` à
> leur famille (`ComposerDonneesEcran.EstDeLaVille`). Trouvé en revue de code, étape 6.

> [!note] La rareté d'un fait de flux se compte **dans le flux**.
> Comme la fête du hasard, dont la rareté se compte dans la banque : une ville qui
> publie deux événements par an donne un fait bien plus rare que celle qui en publie
> cinquante, et un nombre écrit en dur aurait menti pour l'une des deux. Le flux ne
> portant que sa fenêtre d'ingestion (60 jours), sa densité est ramenée à l'année puis
> multipliée par les jours d'avance où le fait parle. L'estimation penche du côté
> « plus fréquent que la vérité » quand l'événement rare tombe justement dans la
> fenêtre chargée — et c'est le bon penchant : elle fait **baisser** le score.

## Le hasard, en détail (as of 2026-09-20)

Deux items, et rien qui vienne d'un calcul ou d'une table : la matière est un **fichier
de données remplaçable** ([[D-2026-09-20 Banque Du Hasard En Fichier De Données]]).
L'app en livre une version québécoise ; `HASARD_FICHIER` la remplace.

| Clé | Rareté | Pertinence | Ne sort que si |
|---|---|---|---|
| `hasard.fete` | le nombre de fêtes **de la banque** (20 dans celle du Québec) | 2 si c'est un jour chômé, sinon 1,5 | une fête de la banque tombe aujourd'hui |
| `hasard.dicton` | tous les jours | **0,5** (bouche-trou) | la banque a au moins un dicton pour le mois |

La rareté de la fête **se compte dans la banque elle-même** : « combien de jours par
année le fait peut paraître » est exactement le nombre d'entrées. Un foyer qui n'inscrit
que ses huit jours chômés obtient un fait deux fois plus rare que celui qui en inscrit
vingt — et c'est exact, là où un nombre écrit en dur aurait menti pour l'un des deux.

Le dicton du jour est choisi par le **quantième**, dans la liste du mois : figé pour la
journée (comme tout `ContexteDuJour`), différent le lendemain. Rien d'aléatoire, malgré
le nom de la famille — un journal qui change de dicton entre deux réveils se contredirait
tout seul.

> [!note] Le dicton est le seul fait dont la matière vit dans le **texte long**.
> Un proverbe n'a pas de chiffre à mettre en valeur, et il ne tient pas dans les trente
> signes de la forme courte : c'est donc l'étiquette qui dit « Le dicton », la valeur qui
> dit le mois, et le texte qui porte le proverbe. Ça tombe bien — un bouche-trou ne
> paraît que lorsque le journal a de la place, donc lorsqu'il montre les textes longs.
> **Relevé au rendu de l'étape 4** : la hiérarchie typographique du widget s'en trouve
> inversée (le mois en gros, le proverbe en petit). C'est vivable et c'est le seul
> découpage qui ne coupe pas le proverbe ; à revoir avec l'éditorialiste (étape 7), pas
> avant.

> [!warning] Les fêtes mobiles sont la moitié des jours fériés du Québec.
> Pâques et ses deux congés, la Journée des patriotes, la fête du Travail, l'Action de
> grâce : une banque de dates fixes serait fausse quatre jours par an. Une fête déclare
> donc **quand elle tombe** sous l'une de quatre formes déclaratives — date fixe ;
> n-ième jour de semaine du mois (rang négatif = depuis la fin) ; dernier jour de semaine
> **avant** une date (« le lundi qui précède le 25 mai », strictement avant, même quand le
> 25 est lui-même un lundi) ; décalage en jours depuis Pâques. Le comput grégorien est
> écrit à la main, comme les éphémérides.

## Ancres de code

- `server/HouseOs.Api/Features/FondsDeTiroir/FaitsDeLaMaison.cs`,
  `server/HouseOs.Api/Features/FondsDeTiroir/FaitsDuCalendrier.cs`,
  `server/HouseOs.Api/Features/FondsDeTiroir/FaitsDuHasard.cs` — les faits, avec leur
  état (`EtatDeLaMaison.cs`, `EtatDuCalendrier.cs`) et la banque (`BanqueDuHasard.cs`).
- `server/HouseOs.Tests/Features/FondsDeTiroir/` — les tests de chaque famille.
