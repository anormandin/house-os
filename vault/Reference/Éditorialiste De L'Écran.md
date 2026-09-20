---
type: reference
last-verified: 2026-09-20
verified-against: eb62d0e
tags: [iot]
---

# Éditorialiste De L'Écran

Résultat de trois tours de maquettes (2026-09-20) sur la seconde vue de House OS
([[Affichage E-ink]]). La direction retenue par Alain et Ariane : **l'écran mural n'est
pas un miroir de l'app, c'est un quotidien qui édite**. Il choisit une manchette, il
connaît la saison, il écrit des phrases — et le jour où la maison ne demande rien, il
sort ses meilleures histoires au lieu d'afficher un écran vide.

Maquettes : `design/maquettes/eink-directions.html` (11 directions),
`eink-publications.html` (3 × 3 variantes), `une-editorialiste.html` (la densité
variable, le fonds de tiroir et la règle de bascule, rendus). Index :
[[Inspiration UI]].

> [!warning] Rien de ceci n'est implémenté (as of 2026-09-20).
> La vue `/ecran` en prod est encore la liste + zones décrite dans [[Affichage E-ink]].
> Cette note est le matériau d'une planification, pas un état du code.

> [!note] Planifié le 2026-09-20.
> Ce matériau a été tranché en neuf décisions et découpé en deux features :
> [[Fonds De Tiroir]] (les faits et leur score) et [[Journal De La Maison]] (la mise en
> page et l'éditorialiste). Le plan d'exécution est
> [[Plan 2026-09-20 Journal Éditorial]]. Cette note reste le **matériau** : quand une
> règle d'ici et une spec de feature se contredisent, la spec a raison.

## La forme retenue

Une **broadsheet** : bloc-titre, manchette avec lettrine, trois colonnes aux filets
fins, encadré de compte à rebours, pied. La grammaire 1-bit du vault ne change pas
(noir plein sur blanc, trames à la place des gris, une seule bande inversée).

Ce qui s'ajoute : un **fonds de tiroir** de petites choses vraies dont l'éditorialiste
tire ce qui adonne, et une **densité variable** qui fait de la place aux tâches quand
il y en a.

## Le fonds de tiroir (six familles)

Détail complet, avec source et rareté par item, dans la table du
`design/maquettes/une-editorialiste.html` (section « Le fonds de tiroir »).

| Famille | Ce qu'elle donne | Dépendance |
|---|---|---|
| **Le ciel** | lever, coucher, durée du jour, dérive quotidienne, phase lunaire, équinoxes et solstices, changement d'heure | calcul local depuis `METEO_LATITUDE`/`METEO_LONGITUDE` — aucune |
| **Le climat** | premier gel, première neige, dernière journée à 20°, « il a fait X° ce jour-là l'an dernier » | normales (constantes par région) + tables météo existantes |
| **La maison** | ce jour-là l'an dernier, série en cours et record, N séances depuis, plus vieil équipement, zone la plus négligée, coût de l'année | journal de complétion — **ne donne rien la première année** |
| **Le calendrier** | compte à rebours, ça s'en vient (7–30 j), travaux de la saison, garantie qui expire | occurrences, fenêtres saisonnières, documents |
| **La ville** | prochaine collecte, collecte spéciale, événement municipal, séance du conseil, nouvelle de la ville, règle qui entre en vigueur | voir « Sources municipales » ci-dessous |
| **Le hasard** | dicton météo québécois, fête ou journée nationale, phrase écrite pour aujourd'hui | banque locale, LLM |

## Comment un widget gagne sa place

`score = rareté × fraîcheur × pertinence du jour`

- **rareté** — combien de fois par année l'item peut paraître (l'équinoxe, 2 fois ;
  la durée du jour, tous les jours).
- **fraîcheur** — pénalité s'il est sorti récemment. C'est ce qui crée la surprise
  quotidienne.
- **pertinence du jour** — est-ce que le fait change quelque chose à aujourd'hui.
  « Le soleil se couche à 18 h 25 » est de la décoration un mardi ordinaire et une
  consigne le jour du déménagement.

**Plancher non négociable** : une tâche en retard de plus de trois jours, une échéance
ferme (notaire, livraison payée, date légale) et un compte à rebours à zéro ne peuvent
jamais être relégués sous un widget. Sans ce plancher, le jour où la lune passe devant
« remettre les clés », l'écran perd sa crédibilité pour de bon.

## La règle de bascule

Une seule mise en page, un seul budget. Les widgets **rétrécissent avant de
disparaître** (le tableau du ciel devient une phrase, puis une demi-phrase).

| Tâches dues | Manchette | Widgets |
|---|---|---|
| 0 | une chronique (le ciel, la saison, la maison) | 7 |
| 1–2 | la tâche — ou un widget assez rare pour la voler | 4–5 |
| 3–5 | la tâche qui porte du sens, corps raccourci | 4 |
| 6–9 | titre court, sans lettrine ni chronique | 2–3 |
| 10 et + | un sommaire en bande inversée, liste groupée par rubrique | 3, en bande de pied |
| événement | l'échéance ferme ou le compte à rebours à zéro, quoi qu'il arrive | 1, actionnable |

**Mesuré en montant les maquettes, pas estimé** : à 34 px d'item, trois colonnes
tiennent ~9 items chacune, donc **~27 tâches avant saturation réelle**, quand la
journée la plus chargée de toute la prod en compte 14 (le 2026-10-20). La contrainte
n'est donc pas la place : c'est la **manchette**, qui n'a plus de sens passé ~6 items.

## Ce que l'éditorialiste écrit

Le LLM **n'invente aucun fait**. Il reçoit les faits déjà classés et écrit le surtitre,
la manchette, le chapeau et les deux paragraphes de corps. Tout le reste est du
gabarit. Un appel **par édition**, pas par rendu — la cadence d'écriture est découplée
de la cadence de l'appareil (15 min, [[Affichage E-ink]]). Mémoire des sept derniers
jours pour ne pas radoter, repli en gabarit quand l'API ne répond pas. C'est la couche
3 de [[Titre D'humeur]] étendue du titre au feuillet.

## Sources municipales (vérifiées au curl le 2026-09-20)

> [!warning] Spécifique à Sainte-Catherine-de-la-Jacques-Cartier.
> Le dépôt est public et rien de propre à un foyer ne va dans le code
> ([[Distribution]]). Ces trois sources sont donc un cas d'espèce : le contrat à
> concevoir est une abstraction « source municipale », pas un client `villescjc.com`.

| Source | Ce que ça donne | Comment | Solidité |
|---|---|---|---|
| **Calendrier des collectes** | toutes les collectes de l'année par secteur et par jour, **et l'index des rues par secteur** | `villescjc.com/storage/app/media/collectes/sainte-catherine-2026.pdf` (763 ko, HTTP 200 au curl, aussi téléchargeable une fois l'an à la main). `pdftotext -bbox-layout` ; les codes `DS/DN OS/ON RS/RN ES/EN FS/FN` s'alignent sur les colonnes de jours de la grille | solide — un PDF par année |
| **Événements** | date, rubrique, titre, lieu, heures, lien | `villescjc.com/evenements`, HTML rendu au serveur ; cartes `c-event-card` (`__date`, `__surtitle`, `__title`, `__info-text`) | fragile — grattage de gabarit |
| **Actualités** | rubrique, titre, lien ; la date n'est que sur la page de l'article (`c-infos-above-cms-content__date`) | `villescjc.com/actualites`, cartes `c-publication-card` | fragile — grattage de gabarit |

- Aucun flux RSS, aucun JSON-LD, pas de `robots.txt` (404). Un tirage par jour,
  User-Agent identifiable, `If-Modified-Since`.
- **Volume réel : 8 actualités étalées sur des mois.** Ce n'est pas un fil quotidien,
  c'est un bouche-trou — exactement le rôle prévu.
- Les deux pages grattées sont des **widgets qui ont le droit de manquer**, jamais une
  source d'échéance. Le PDF, lui, peut porter une vraie échéance.

### Le secteur se lit dans la source

Le PDF des collectes porte un index des rues en deux colonnes,
`SECTEUR NORD DE LA RIVIÈRE` (codes `DN ON RN SN FN EN`) et `SECTEUR SUD DE LA RIVIÈRE`
(codes `DS OS RS SS FS ES`). **« de la Colline » est dans la colonne SUD**, dont les
codes tombent le **jeudi**. Le secteur n'a donc pas à être deviné ni configuré à la
main : il se déduit de l'adresse et de la source.

Deux rapprochements qu'aucune liste ne ferait, trouvés en lisant le PDF :
le **6 octobre 2026**, jour du camion, est aussi la collecte des **encombrants** du
secteur Sud (une des deux de l'année) ; et la première collecte ordinaire au 17 rue de
la Colline tombe le **jeudi 8**, deux jours après l'emménagement.

## Ce qui manque au modèle

Tranché le 2026-09-20 — ce qui suit dit maintenant ce qui a été décidé, pas ce qui reste
ouvert :

- Une **catégorie ou étiquette** sur la tâche : **refusée**
  ([[D-2026-09-20 Regroupement Sans Catégorie De Tâche]]). Le regroupement se fait sur la
  zone, et l'éditorialiste nomme le paquet qui n'en a pas.
- Les **fenêtres saisonnières** exposées comme donnée lisible : retenues, famille « le
  calendrier » de [[Fonds De Tiroir]].
- Une table de **jours fériés** québécois : retenue, en données de configuration et non
  en dur ([[Distribution]]).
- Un contrat de **source municipale** : **pas dans le dépôt**
  ([[D-2026-09-20 Sources Municipales Séparées Par Solidité]]) — ICS pour les collectes,
  flux poussé pour les événements ([[D-2026-09-20 Flux Externe Poussé]]).
- Des **éphémérides** calculées : formules NOAA écrites à la main, aucune dépendance. Les
  normales climatiques viennent de l'archive Open-Meteo
  ([[D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo]]), pas de constantes
  québécoises.

## Notes liées

- [[Fonds De Tiroir]] — la feature qui produit et classe les faits.
- [[Journal De La Maison]] — la feature qui les publie.
- [[Plan 2026-09-20 Journal Éditorial]] — le plan d'exécution commun.
- [[Affichage E-ink]] — la spec vivante de la vue actuelle (état du code).
- [[Affichage Mural Et E-ink]] — la grammaire 1-bit, inchangée.
- [[Titre D'humeur]] — la couche LLM dont l'éditorialiste est l'extension.
- [[Flux Externes]] — où une source municipale viendrait se brancher.
- [[Distribution]] — pourquoi rien de propre à SCJC ne peut aller dans le code.
- [[Banque D'idées]] — « Mur, e-ink et IoT » et « IA et MCP ».
