---
type: feature
status: building
last-verified: 2026-09-21
verified-against: 86faebd
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

> [!note] La broadsheet est en prod depuis le 2026-09-20 ; l'éditorialiste écrit en dev depuis le 2026-09-21.
> Étapes 1 à 7 du [[Plan 2026-09-20 Journal Éditorial]] bâties : la grille à rangs, le
> bloc-titre, les six familles de [[Fonds De Tiroir]] dans les widgets, et depuis
> l'étape 7 **l'édition du jour écrite par Opus** — surtitre, manchette, chapeau,
> chronique — avec le [[Titre D'humeur]] en repli de gabarit. Reste l'étape 8 (le
> rang 10 et plus) et la calibration au mur. [[Affichage E-ink]] reste la spec de
> l'appareil.

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
  complétion — nul sur une installation neuve. Il ne compte **pas** les éditions
  matérialisées, contrairement à ce qui était prévu : ça aurait fait repartir le mur à
  « N° 1 » le jour du release (tranché à l'étape 7).
- L'**état du jour** (« Rien au programme », « 10 choses au programme ») vit dans la
  dateline. Il devient une **mention inversée** quand le plancher se déclenche
  (« C'est aujourd'hui »), et ne peut jamais coexister avec la bande du sommaire :
  le plancher force le rang « événement ».
- Le **surtitre de la manchette** est autre chose : une ligne éditoriale, écrite par
  l'éditorialiste (« Le dernier lundi avant l'équinoxe »). Le gabarit n'y met que la
  raison du plancher, et le laisse vide le reste du temps plutôt que de répéter la
  dateline.

### La règle de bascule

Les widgets **rétrécissent avant de disparaître** (le tableau du ciel devient une
phrase, puis une demi-phrase). Ce qui part vraiment, c'est la **manchette**.

| Tâches dues | Manchette | Widgets |
|---|---|---|
| 0 | une chronique (le ciel, la saison, la maison) | 7 (bornés à 5 par la capacité, voir plus bas) |
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

L'**échéance ferme** est un booléen explicite sur la tâche, coché à la main dans
l'éditeur (« Cette date ne se négocie pas ») et exposé au MCP
([[D-2026-09-20 Échéance Ferme Explicite Sur La Tâche]]) — rien ne se déduit d'une
récurrence ou d'une date. Une ligne du jour qui le porte est due, donc entrante : elle
prend la manchette. L'ordre des trois cas, au client comme au serveur : le compte à
rebours, puis l'échéance ferme, puis le retard.

Le plancher est **réévalué à chaque rendu**, pas seulement à l'écriture de l'édition :
s'il se déclenche après coup, il redéclenche une édition
([[D-2026-09-20 Une Édition Par Jour Matérialisée]]) — **en deux temps** : un gabarit
tout de suite au rendu, Opus ensuite par le service de fond
([[D-2026-09-21 Réédition En Deux Temps]]). Le serveur porte sa propre évaluation
(`Domaine/Editorial/Plancher.cs`), la page la sienne (`plancher()`), avec les mêmes
cas et le même ordre ; l'édition suit celle du serveur.

### Le regroupement des journées chargées

Dès **dix tâches dues** (`RangDuJour.SeuilDuSommaire`, plancher ou pas), la liste se
range par rubrique : par `Tache.ZoneId` puis `EquipementId` — en noms, dans l'ordre
d'arrivée des tâches —, et le paquet sans zone ni équipement — les démarches
administratives, justement — est **nommé par l'éditorialiste**
([[D-2026-09-20 Regroupement Sans Catégorie De Tâche]]). Ce qu'il ne place pas tombe
dans « Le reste ». Bâti à l'étape 8 (`Domaine/Editorial/Regroupement.cs`, as of
2026-09-21).

- **Les noms sont figés, le rangement est vivant.** L'édition ne porte que les rubriques
  nommées par le modèle (titres réels, tâches à nommer seulement — une tâche déjà rangée
  par sa zone ne se laisse pas déplacer, un titre inventé est écarté, une rubrique
  « Le reste » nommée par le modèle est ignorée). Le rendu refait le rangement sur les
  lignes du moment, et une ligne **faite** suit son titre sous la rubrique du matin.
- **Le modèle nomme dès dix tâches dues, plancher ou pas** : au rang « événement »
  sur une journée chargée, la manchette est celle du plancher et la liste se range
  quand même par rubrique (revue de code, étape 8).
- **Repli obligatoire** : sans LLM, zone, équipement, « Le reste » ; une seule rubrique
  se lit en liste plate, sans titre pour rien.
- **Sur le mur** : la bande inversée porte la manchette de l'éditorialiste et le compte
  par personne (« 3 Alain · 3 Ariane · 8 pour la maison ») ; les colonnes se
  remplissent à la main (`colonnesDuSommaire`) — un en-tête jamais orphelin, une
  rubrique entière par colonne quand la place le permet, un en-tête compte une rangée
  serrée, « + N autres » est une rangée. Mesuré au rendu 1872×1404 sur la vraie journée
  du 2026-10-20 : 14 tâches en quatre rubriques + « Le reste », 0 px de débordement.

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

> [!warning] Le budget du rang n'est pas la capacité du papier.
> Le budget de la table de bascule dit ce que le journal **veut** montrer ; il ne dit pas
> ce que la colonne **tient**. Un widget empilé fait ~300 px pour ~690 px de corps :
> une colonne d'aparté en tient **deux**, et l'encadré du compte à rebours en coûte
> exactement un. Le budget de sept du rang 0 n'est donc jamais atteignable — au mieux
> cinq widgets, quand il y a un compte à rebours et trois colonnes.
>
> Sans cette borne, le septième widget passait **sous le pied** : 90 px de débordement,
> mesurés à l'étape 4 le jour où la famille « le hasard » a ajouté un fait de plus à une
> journée vide. `capaciteWidgets` (`web/src/lib/ecran-vues.ts`) est le pendant exact de
> `capaciteListe` pour l'autre moitié du corps. Ce qui déborde du budget **disparaît de
> lui-même**, et c'est ce qui compte le moins : le fonds est déjà classé, le journal ne
> retrie rien, il coupe à la fin.

### Ce que l'éditorialiste écrit

Le LLM **n'invente aucun fait**. Il reçoit les faits déjà classés par
[[Fonds De Tiroir]] et écrit le surtitre, la manchette, le chapeau, les deux
paragraphes de corps et les rubriques du sommaire. Tout le reste est du gabarit.
Bâti à l'étape 7 (`server/HouseOs.Api/Features/Editorial/`, as of 2026-09-21).

- **Un appel par édition**, pas par rendu : la cadence d'écriture est découplée de la
  cadence de l'appareil (15 min, [[Affichage E-ink]]). Le service de fond écrit au
  créneau du matin du titre d'humeur (`Humeur:HeureMatin`), rattrape au démarrage
  (toujours l'édition du **jour civil**, jamais la veille), et se réveille sur signal
  quand le rendu lui a laissé un gabarit à réécrire. Un gabarit laissé par un modèle
  qui n'a pas répondu (API surchargée) est **réessayé une fois**, une heure plus tard,
  dans la fenêtre d'une heure qui suit, et plus jamais dans la journée
  ([[D-2026-09-21 Réédition En Deux Temps]]). Une édition écrite avant le créneau du
  matin (un rattrapage de nuit) garde son drapeau : le créneau la réécrit avec les
  faits du matin.
- **Opus 5** pour l'édition ([[D-2026-09-20 Édition Écrite Par Opus]]), réglage
  `Edition:Modele` ; [[Titre D'humeur]] garde Haiku pour ses deux créneaux. La **clé**
  est la même (`ANTHROPIC_API_KEY`) : deux modèles, deux prompts, un seul compte.
- **Mémoire des sept derniers jours** pour ne pas radoter, **repli en gabarit** quand
  l'API ne répond pas — le journal ne dépend jamais du LLM pour être lisible. Le
  gabarit rend exactement ce que le mur montrait avant l'étape 7 : la phrase du jour
  en manchette et en chapeau, la raison du plancher en surtitre, **pas de corps**.
- **Ce que la matière contient** (`MatiereDEdition`) : la date et le jour de semaine,
  le lieu, le rang, le plancher, les tâches dues (titre, retard, ferme, assigné), le
  prochain compte à rebours en dodos, la météo du jour en mots, la **zone et
  l'équipement** des tâches dues (en noms, pour ne nommer que le reste), tous les faits du fonds
  avec la marque de ceux qui paraissent, et les sept éditions précédentes (surtitre,
  manchette, chapeau). Rien d'autre : ce que le modèle ne reçoit pas, il ne peut pas le
  citer.
- **La sortie est validée strictement** (`RedactionLlm.Extraire`) : surtitre ≤ 60,
  manchette 1–60, chapeau 1–160, exactement deux paragraphes visés à 200 signes et
  refusés au-delà de 240 (un modèle qui compte déborde d'une phrase ; le clamp du mur
  fait le filet), rubriques
  seulement au rang « sommaire » et sur des titres de tâches **réels** — un titre
  inventé est écarté, jamais affiché. Tout autre écart → gabarit.

### Ce que l'édition fige, et comment le rendu s'en sert

L'entité (`Domaine/Editorial/Edition.cs`) porte la date, le rang, les textes, les
**clés publiées** (JSONB), les rubriques (JSONB), le plancher, la source et le modèle.

- Le **rang** consigné est celui pour lequel la prose a été écrite ; la page calcule
  sa grille sur le compte **vivant**, comme avant — une tâche ajoutée à neuf heures
  doit avoir une colonne, même sur une journée écrite « chronique ».
- Les **clés publiées** sont le budget de widgets du rang, dans l'ordre du score, plus
  les clés que le journal dessine à part (`CLES_DEJA_AU_JOURNAL`, hors budget). Le
  rendu sert d'abord ces faits, dans cet ordre, puis les autres au score du moment ; la
  page coupe à sa capacité, comme avant.
- La **chronique** — les deux paragraphes — prend la première colonne du corps aux
  rangs « chronique » et « manchette », comme dans les maquettes, mais à la même
  largeur que les autres colonnes ; aux rangs plus chargés le corps n'est pas montré.
  Le gabarit n'a pas de corps, donc pas de colonne. Mesuré au rendu : à 32 px dans un
  tiers, une ligne porte ~32 signes, d'où la cible de **200 signes** par paragraphe et
  le clamp à sept lignes. Un texte refusé est nommé dans le journal (« paragraphe 2 :
  251 signes, 240 au plus »), pour savoir s'il faut retoucher le prompt ou la borne.
- Une **horloge d'essai** sur un autre jour compose l'édition de ce jour sans
  l'écrire ; `regenerer_journal_mural` avec `date` l'écrit, et c'est un geste explicite
  — mais elle garde son drapeau, et le jour venu l'éditorialiste la réécrit avec les
  faits du jour.

### Figé et vivant

**Figé pour la journée** : rang, sélection et ordre des widgets, surtitre, manchette,
chapeau, corps, les **noms** des rubriques et les titres que l'éditorialiste y a mis.
**Vivant à chaque rendu** : la liste des occurrences, les cochées, le rangement par
zone et par équipement, « Le reste », la météo du moment, l'heure d'impression, la pile.

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
- [[D-2026-09-21 Réédition En Deux Temps]] — gabarit au rendu, Opus par le service de
  fond, un second essai une heure plus tard ; acceptée à l'étape 8.
- [[D-2026-09-03 Rendu E-ink Par Chromium Headless]] — la page React capturée, inchangé.

## Ancres de code

- `web/src/pages/Ecran.tsx` — la page, réécrite en place
  ([[Plan 2026-09-20 Journal Éditorial]]).
- `web/src/lib/ecran-vues.ts` — les helpers purs de la vue : rang, plancher, capacité de
  liste **et de widgets**, état du jour, densité du ciel, le sommaire (rubriques,
  colonnes, bande).
- `server/HouseOs.Api/Features/Affichage/ComposerDonneesEcran.cs` — la composition
  serveur : les sources du jour, l'édition, l'ordre des faits.
- `server/HouseOs.Api/Features/Editorial/` — l'éditorialiste : le prompt et le parse
  (`RedactionLlm.cs`), la couture des trois appelants (`GenerationEdition.cs`), le
  service de fond, la mémoire des sept jours, le signal.
- `server/HouseOs.Api/Domaine/Editorial/` — l'entité, le plancher, le rang, le gabarit,
  le regroupement.
- `server/HouseOs.Api/Features/Humeur/` — le patron LLM dont l'éditorialiste est
  l'extension (`PolissageLlm.cs`, `HumeurService.cs`).

## Sources

- [[Éditorialiste De L'Écran]] — la direction retenue, la règle de bascule, les nombres
  mesurés.
- `design/maquettes/une-editorialiste.html` — les six rendus, la table du fonds de
  tiroir, le tableau de bascule.
- `design/maquettes/eink-publications.html` — les trois formes et leurs variantes.

## Historique

- [[Plan 2026-09-20 Journal Éditorial]]
