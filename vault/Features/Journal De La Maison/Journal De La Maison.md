---
type: feature
status: building
last-verified: 2026-09-20
verified-against: c8c11b9
tags: [iot]
---

# Journal De La Maison

## Intention

**L'écran mural n'est pas un miroir de l'app, c'est un quotidien qui édite.** Il choisit
une manchette, il connaît la saison, il écrit des phrases — et le jour où la maison ne
demande rien, il sort ses meilleures histoires au lieu d'afficher un écran vide.

Direction retenue par Alain et Ariane après trois tours de maquettes
([[Éditorialiste De L'Écran]], 2026-09-20). Remplace le contenu de la vue décrite dans
[[Affichage E-ink]] — la liste + zones — sans toucher au protocole de l'appareil ni à la
grammaire 1-bit.

> [!note] La broadsheet est en prod depuis le 2026-09-20.
> Étapes 1 et 2 du [[Plan 2026-09-20 Journal Éditorial]] livrées : la grille à rangs, le
> bloc-titre, et la famille « le ciel » de [[Fonds De Tiroir]] dans les widgets. La
> **manchette est encore le [[Titre D'humeur]]** : l'éditorialiste arrive à l'étape 7.
> [[Affichage E-ink]] reste la spec de l'appareil.

## Comportement

### La forme

Une **broadsheet** : bloc-titre, manchette avec lettrine, trois colonnes aux filets
fins, encadré de compte à rebours, pied. La grammaire 1-bit ne change pas
([[Affichage Mural Et E-ink]]) : noir plein sur blanc, trames à la place des gris, une
seule bande inversée, élaguer plutôt que rapetisser.

**Une seule mise en page** ([[D-2026-09-20 Une Seule Mise En Page À Rangs]]) : le rang
est une fonction du nombre de tâches dues et du plancher, pas un choix de gabarit.

#### Le bloc-titre

Trois rangs, dans l'ordre d'un quotidien : les **oreilles** (l'édition, le lieu de
publication, le numéro), le **nom** en capitales entre deux filets, la **dateline**
(date avec l'année, état du jour au centre, temps qu'il fait en mots).

- Le **lieu** est propre au foyer, donc dans le `.env` (`MAISON_LIEU` →
  `Affichage:Lieu`), vide par défaut ([[Distribution]]) ; les oreilles se composent
  avec ce qui reste, sans trou.
- Le **numéro d'édition** compte les jours depuis la première entrée du journal de
  complétion — nul sur une installation neuve. À l'étape 7, le compte d'éditions
  matérialisées le remplacera sans rien changer au rendu.
- L'**état du jour** (« Rien au programme », « 10 choses au programme ») vit dans la
  dateline. Il devient une **mention inversée** quand le plancher se déclenche
  (« C'est aujourd'hui »), et ne peut jamais coexister avec la bande du sommaire :
  le plancher force le rang « événement ».
- Le **surtitre de la manchette** est autre chose : une ligne éditoriale, écrite à
  l'étape 7. Jusque-là il ne porte que la raison du plancher, et reste vide le reste
  du temps plutôt que de répéter la dateline.

### La règle de bascule

Les widgets **rétrécissent avant de disparaître** (le tableau du ciel devient une
phrase, puis une demi-phrase). Ce qui part vraiment, c'est la **manchette**.

| Tâches dues | Manchette | Widgets |
|---|---|---|
| 0 | une chronique (le ciel, la saison, la maison) | 7 |
| 1–2 | la tâche — ou un widget assez rare pour la voler | 4–5 |
| 3–5 | la tâche qui porte du sens, corps raccourci | 4 |
| 6–9 | titre court, sans lettrine ni chronique | 2–3 |
| 10 et + | un sommaire en bande inversée, liste groupée par rubrique | 3, en bande de pied |
| événement | l'échéance ferme ou le compte à rebours à zéro, quoi qu'il arrive | 1, actionnable |

**Mesuré en montant les maquettes** : à 34 px d'item, trois colonnes tiennent ~9 items
chacune, donc ~27 tâches avant saturation réelle, quand la journée la plus chargée de
toute la prod en compte 14 (le 2026-10-20). La contrainte n'est pas la place, c'est la
manchette, qui n'a plus de sens passé ~6 items.

### Le plancher, non négociable

Une tâche **en retard de plus de trois jours**, une **échéance ferme** (notaire,
livraison payée, date légale) et un **compte à rebours à zéro** ne peuvent jamais être
relégués sous un widget. Sans ce plancher, le jour où la lune passe devant « remettre
les clés », l'écran perd sa crédibilité pour de bon.

L'**échéance ferme** est un booléen explicite sur la tâche, coché à la main
([[D-2026-09-20 Échéance Ferme Explicite Sur La Tâche]]) — rien ne se déduit d'une
récurrence ou d'une date. Il arrive à l'étape 7 du [[Plan 2026-09-20 Journal Éditorial]] ;
jusque-là le plancher tourne sur ses deux cas calculables.

Le plancher est **réévalué à chaque rendu**, pas seulement à l'écriture de l'édition :
s'il se déclenche après coup, il redéclenche une édition
([[D-2026-09-20 Une Édition Par Jour Matérialisée]]).

### Le regroupement des journées chargées

Au rang 10+, les tâches sont groupées par `Tache.ZoneId` puis `EquipementId` ; le paquet
sans zone — les démarches administratives, justement — est **nommé par l'éditorialiste**
([[D-2026-09-20 Regroupement Sans Catégorie De Tâche]]). Repli obligatoire en liste
plate quand l'API ne répond pas.

### Les widgets et le fonds de tiroir

Les widgets viennent de [[Fonds De Tiroir]], qui rend des faits **déjà classés et sans
mise en forme**. Le journal choisit la densité, et lui seul. Pour la famille « le ciel »,
trois formes (`formeDuCiel`, `web/src/lib/ecran-vues.ts`) :

| Forme | Quand | Ce qu'on voit |
|---|---|---|
| **tableau** | budget ≥ 5 widgets et ≥ 3 faits qui tiennent sur une rangée | un bloc à quatre rangées au plus, étiquette à gauche et valeur à droite |
| **phrase** | budget de 2 à 4 | le meilleur fait avec son texte long, les suivants avec leur seule valeur |
| **demi-phrase** | budget de 1 | le meilleur fait, sa valeur courte seulement |

Un fait dont l'étiquette et la valeur ne tiennent pas ensemble sur une ligne (34 signes,
mesurés au rendu) **sort du tableau** et garde sa forme de widget empilé, où il a deux
lignes : élaguer, pas rapetisser, appliqué jusque dans le tableau.

### Ce que l'éditorialiste écrit

Le LLM **n'invente aucun fait**. Il reçoit les faits déjà classés par
[[Fonds De Tiroir]] et écrit le surtitre, la manchette, le chapeau, les deux
paragraphes de corps et les rubriques du sommaire. Tout le reste est du gabarit.

- **Un appel par édition**, pas par rendu : la cadence d'écriture est découplée de la
  cadence de l'appareil (15 min, [[Affichage E-ink]]).
- **Opus 5** pour l'édition ([[D-2026-09-20 Édition Écrite Par Opus]]) ;
  [[Titre D'humeur]] garde Haiku pour ses deux créneaux.
- **Mémoire des sept derniers jours** pour ne pas radoter, **repli en gabarit** quand
  l'API ne répond pas — le journal ne dépend jamais du LLM pour être lisible.

### Figé et vivant

**Figé pour la journée** : rang, sélection et ordre des widgets, surtitre, manchette,
chapeau, corps, rubriques.
**Vivant à chaque rendu** : la liste des occurrences, les cochées, la météo du moment,
l'heure d'impression, la pile.

## Hors périmètre

- Le protocole de l'appareil, la cadence, la pile, la capture Chromium : c'est
  [[Affichage E-ink]], inchangé.
- La grammaire 1-bit : inchangée ([[Affichage Mural Et E-ink]]).
- Plusieurs mises en page ou « plugins » — la ligne du hors périmètre de
  [[Affichage E-ink]] **reste vraie**.
- La lettre du matin (courriel sortant) : feature à part, plus tard. House OS reçoit du
  courriel ([[Courriel Entrant]]) mais n'en envoie aucun.
- Toute interaction physique : l'écran reste en lecture seule.

## Décisions

- [[D-2026-09-20 Une Seule Mise En Page À Rangs]] — une grille, des rangs ; les widgets
  rétrécissent avant de disparaître.
- [[D-2026-09-20 Une Édition Par Jour Matérialisée]] — édition écrite au créneau du
  matin et matérialisée ; liste vivante ; plancher qui peut forcer une réédition.
- [[D-2026-09-20 Édition Écrite Par Opus]] — Opus 5 pour l'édition, Haiku reste sur la
  phrase du jour.
- [[D-2026-09-20 Regroupement Sans Catégorie De Tâche]] — zone d'abord, l'éditorialiste
  nomme le reste ; aucun changement de schéma sur `Tache`.
- [[D-2026-09-20 Fonds De Tiroir Séparé Du Journal]] — le journal consomme des faits
  déjà classés.
- [[D-2026-09-20 Échéance Ferme Explicite Sur La Tâche]] — un booléen sur `Tache`, bâti
  à l'étape 7 ; le troisième cas du plancher.
- [[D-2026-09-03 Rendu E-ink Par Chromium Headless]] — la page React capturée, inchangé.

## Ancres de code

- `web/src/pages/Ecran.tsx` — la page, réécrite en place
  ([[Plan 2026-09-20 Journal Éditorial]]).
- `web/src/lib/ecran-vues.ts` — les helpers purs de la vue : rang, plancher, capacité de
  liste, état du jour, densité du ciel.
- `server/HouseOs.Api/Features/Affichage/ComposerDonneesEcran.cs` — la composition
  serveur, à étendre au document d'édition.
- `server/HouseOs.Api/Features/Humeur/` — le patron LLM à étendre (`ConstruireEtat.cs`,
  `PolissageLlm.cs`, `HumeurService.cs`).

## Sources

- [[Éditorialiste De L'Écran]] — la direction retenue, la règle de bascule, les nombres
  mesurés.
- `design/maquettes/une-editorialiste.html` — les six rendus, la table du fonds de
  tiroir, le tableau de bascule.
- `design/maquettes/eink-publications.html` — les trois formes et leurs variantes.

## Historique

- [[Plan 2026-09-20 Journal Éditorial]]
