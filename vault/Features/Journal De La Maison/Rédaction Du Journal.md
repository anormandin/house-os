---
type: reference
last-verified: 2026-09-30
verified-against: 6d1fda0
tags: []
---

# Rédaction Du Journal

Sous-note de [[Journal De La Maison]] : la part écrite de l'édition — ce que
l'éditorialiste produit ([[D-2026-09-20 Édition Écrite Par Opus]]), ce que l'édition
fige une fois écrite ([[D-2026-09-20 Une Édition Par Jour Matérialisée]],
[[D-2026-09-21 Matière Conservée Sur L'Édition]]) et l'atelier où le prompt se règle.
La forme de la page, la bascule et le plancher restent dans [[Journal De La Maison]].

## Ce que l'éditorialiste écrit

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
- **Mémoire des sept derniers jours** pour ne pas radoter : un fait déjà raconté ne
  revient que si sa valeur a changé — un compteur qui prend une unité n'a pas changé,
  et il ne revient pas non plus reformulé —, la manchette ne redit pas le surtitre, et le prompt pousse vers ce que la semaine n'a pas
  dit. Le **chapeau** a les interdits du surtitre — ni compte de tâches, ni météo, ni
  compte à rebours, que la dateline et l'encadré montrent déjà — et le compte à rebours
  ne monte en surtitre, manchette ou chapeau que la veille et le jour même (as of
  2026-09-28) ; **repli en gabarit** quand
  l'API ne répond pas — le journal ne dépend jamais du LLM pour être lisible. Le
  gabarit rend exactement ce que le mur montrait avant l'étape 7 : la phrase du jour
  en manchette et en chapeau, la raison du plancher en surtitre, **pas de corps**.
- **Ce que la matière contient** (`MatiereDEdition`) : la date et le jour de semaine,
  le lieu, le rang, le plancher, les tâches dues (titre, retard, ferme, assigné), le
  prochain compte à rebours en dodos, la météo du jour en mots, la **zone et
  l'équipement** des tâches dues (en noms, pour ne nommer que le reste), tous les faits du fonds
  avec la marque de ceux qui paraissent, et les sept éditions précédentes (surtitre,
  manchette, chapeau **et paragraphes** — sans eux, le modèle racontait la même cuisine
  oubliée quatre matins sur huit, [[D-2026-09-28 Fraîcheur Cumulée Des Faits]]). Rien d'autre : ce que le modèle ne reçoit pas, il ne peut pas le
  citer.
- **La sortie est validée strictement** (`RedactionLlm.Extraire`) : surtitre ≤ 60,
  manchette 1–60, chapeau 1–160, exactement deux paragraphes visés à 200 signes et
  refusés au-delà de 240 (un modèle qui compte déborde d'une phrase ; le clamp du mur
  fait le filet), rubriques
  seulement au rang « sommaire » et sur des titres de tâches **réels** — un titre
  inventé est écarté, jamais affiché. Tout autre écart → gabarit.

## Ce que l'édition fige, et comment le rendu s'en sert

L'entité (`Domaine/Editorial/Edition.cs`) porte la date, le rang, les textes, les
**clés publiées** (JSONB), les rubriques (JSONB), le plancher, la source, le modèle et
la **matière** (JSONB) — ce que le modèle a reçu, posé chaque fois qu'on
lui a demandé d'écrire, même quand il s'est tu ; nulle sur un gabarit posé par le
rendu ([[D-2026-09-21 Matière Conservée Sur L'Édition]]).

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
  tiers, une ligne porte ~32 signes, d'où la cible de **200 signes** par paragraphe.
  Un texte refusé est nommé dans le journal (« paragraphe 2 : 251 signes, 240 au
  plus »), pour savoir s'il faut retoucher le prompt ou la borne.
- **La chronique s'ajuste au papier** plutôt que de se faire couper (as of
  2026-09-21) : une fois les polices chargées et avant la garde de débordement, son
  texte part de 32 px et descend d'un pixel à la fois, jusqu'à 24 px, tant que les
  deux paragraphes ne tiennent pas dans la colonne (`ajusterAuPapier`,
  `pages/Ecran.tsx`, sur les blocs marqués `data-ajuster`). Les paragraphes ne se
  laissent plus écraser par la colonne : c'est ce qui coupait les deux au milieu
  d'une ligne au mur, le jour du passage à Gelasio, plus large que Nunito Sans, sur
  une manchette de deux lignes. L'ancien clamp à sept lignes est parti avec — il
  cachait des lignes entières là où la taille suffit. Sous le plancher, la colonne
  coupe et la garde le dit dans Seq.
- Une **horloge d'essai** sur un autre jour compose l'édition de ce jour sans
  l'écrire ; `regenerer_journal_mural` avec `date` l'écrit, et c'est un geste explicite
  — mais elle garde son drapeau, et le jour venu l'éditorialiste la réécrit avec les
  faits du jour.

## L'atelier du prompt

Le prompt se règle à la lecture, jamais à l'aveugle : on relit des éditions contre
leur matière, on note ce qui cloche, on retouche dans un fichier, on rejoue **la même
matière** contre l'ancien et le nouveau prompt, et seulement ensuite on touche la
constante du code (`RedactionLlm.PromptParDefaut`) et on release. L'outil est
`server/HouseOs.Essais` (as of 2026-09-21), hors de l'image Docker :

- `prompt` imprime le prompt en vigueur, à rediriger dans un fichier de travail.
- `matiere <date>` imprime la matière conservée sur l'édition de cette date, lue de la
  base de dev par défaut (`--base` ou `ConnectionStrings__HouseOs` pour une autre).
  Pour une journée de **prod**, la même colonne se lit sur la machine de prod avec
  `docker compose exec -T postgres psql -U houseos -Atc` sur `"Editions"."Matiere"`,
  et le fichier se rapporte chez soi : l'atelier ne se connecte jamais à la prod.
- `rediger <matiere.json> [--prompt fichier] [--fois n] [--modele id] [--brut]` envoie
  la matière au modèle avec le prompt du fichier (le défaut sans `--prompt`) et
  imprime chaque réponse avec la longueur de chaque champ et un repère au-delà de la
  cible — ou l'écart qui l'a fait refuser, avec le texte brut. La clé est celle du
  titre d'humeur (`ANTHROPIC_API_KEY`, ou `appsettings.local.json`).

Ce que l'atelier ne fait pas : il n'écrit rien en base, ne touche pas au mur, et ne
juge pas la prose — la lecture reste celle du foyer. Les vérifications mécaniques
(chaque chiffre dans la matière, les longueurs, les patrons de surtitre qui se
répètent) sont la suite naturelle, pas encore bâtie.
