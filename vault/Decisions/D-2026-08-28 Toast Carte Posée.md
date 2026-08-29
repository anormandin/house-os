---
type: decision
status: accepted
date: 2026-08-28
feature: "[[Synchro]]"
tags: []
---

# Toast Carte Posée

## Contexte

Le toast introduit par l'issue #60 (confirmation d'un geste + son action inverse) a
été rejoint par [[D-2026-08-28 Synchro Temps Réel Par SignalR]], qui pousse dans la
même file les gestes des autres — l'autre membre du foyer, l'agent MCP, les services
d'arrière-plan. Un seul rendu servait les deux : pastille vert plein, texte crème
gras, message d'une ligne.

Trois manques en découlaient :

- **Mes gestes et ceux des autres se confondaient** — même vert, même forme, alors
  qu'un seul des deux porte une action que je peux défaire.
- **« Annuler » ne disait pas sur quoi il portait.** Avec trois toasts au plus et un
  emplacement exclusif pour mes gestes, mon toast peut se retrouver empilé sous deux
  annonces distantes ; « Tâche complétée » seul devient ambigu.
- **Les 6 s étaient invisibles.** Rien n'annonçait que la fenêtre de rattrapage se
  refermait, ce qui rend le filet peu fiable : on regarde ailleurs, il a disparu.

Cinq directions visuelles ont été maquettées (`design/maquettes/toasts-directions.html`,
rendu vivant : minuteurs, entrées, fusion) avant de trancher.

## Options considérées

- **La carte posée** — le toast est une carte de l'app en plus petit (même crème, même
  rayon, même liseré gauche coloré que les rangées en retard), médaillon d'auteur,
  deux lignes. Continuité maximale ; la moins pressante des cinq.
- **Le liseré** — un rail de 4 px et une ligne de texte, presque pas de chrome. La
  moins interruptive ; l'action inverse n'y est qu'un lien souligné.
- **La barre** — bande d'encre pleine largeur, dense, registre « le système parle ».
  Efficace en pile ; seule tache sombre de l'app.
- **Le geste au premier plan** — le bouton d'annulation est l'objet, le message sa
  légende, le minuteur se vide dans le bouton. Rate le moins un mauvais clic ; impose
  deux formes très différentes à l'écran.
- **L'ancre** — bulle à queue sortant de la rangée cochée, la rangée elle-même
  reprenant sa couleur en 6 s. La plus juste conceptuellement ; exige un second
  comportement (repli quand la rangée est hors écran) et pousse le contenu.

## Décision

(Choix d'Alain, 2026-08-28, sur recommandation du designer.)

**« La carte posée », augmentée de deux emprunts.**

- **La forme** est celle de la carte : crème `--carte`, rayon 20, `shadow-carte-lg`,
  liseré gauche de 6 px. Un seul composant sert les trois registres — mon geste de
  complétion (vert), son retour arrière (orange), une annonce distante (la teinte
  pastel de l'auteur). La hiérarchie « moi / les autres » se joue en teinte et en
  médaillon, jamais en second système.
- **Le médaillon** porte l'auteur : coche pour ma complétion, flèche de retour pour
  mon annulation, initiales pastel pour un membre du foyer, étincelle pour l'agent
  MCP, « ? » pour un acteur inconnu.
- **La sous-ligne** nomme la tâche sur mes propres gestes — c'est ce qui désambiguïse
  « Annuler » dans une pile.
- **Emprunt 1 — la jauge dans le bouton** (de « Le geste au premier plan ») : quand
  le toast porte une action inverse, le compte à rebours se vide *dans* ce bouton. Le
  temps restant appartient à l'objet dont il conditionne la validité. Les toasts sans
  action gardent une barre fine au pied de la carte.
- **Emprunt 2 — le lavis de la rangée** (de « L'ancre ») : la rangée qu'on vient de
  cocher part d'un vert soutenu et redescend vers son vert de repos sur les mêmes 6 s.
  Le geste et sa confirmation se lisent au même endroit, sans payer le positionnement
  de l'ancre.
- **Le minuteur reste un confort, jamais la seule voie.** Si le toast disparaît, la
  rangée complétée garde son bouton « Annuler » au survol : perdre le toast ne perd
  pas le recours.

## Ce que ça révise

La clause UI de [[D-2026-08-24 Annulation Et Passage D'occurrences]] (« bouton
"Annuler" au survol de la rangée verte ; pas de toast ») était déjà dépassée par
l'issue #60. Les deux coexistent désormais et c'est voulu : le toast est le filet
immédiat, le bouton au survol est le recours durable. Le reste de cette décision
(sémantique de l'annulation, garde-fou, « passer », « reporter ») reste en vigueur.

## Conséquences

- Le modèle de toast gagne `sousTitre`, `ton` et `auteur` ; la synchro doit fournir
  l'auteur (`auteurPour`) en plus du message, et les gestes locaux doivent passer le
  titre de la tâche — donc les mutations de complétion prennent `{ id, titre }` et
  non plus un id nu.
- Le hook de complétion expose l'occurrence fraîchement complétée : c'est lui qui
  arme le lavis, pas la liste.
- Les annonces distantes n'ont pas de sous-ligne : aucune information n'y méritait la
  seconde ligne (« à l'instant » est du remplissage — un toast qui vient d'apparaître
  l'est toujours). La forme à deux lignes sert là où elle porte quelque chose.
- Les trois minuteurs partagent une durée unique (6 s) écrite à deux endroits :
  `DUREE_TOAST_MS` et les animations de `index.css`. Un changement doit toucher les
  deux.
- `prefers-reduced-motion` coupe les animations : les jauges disparaissent, le toast
  et le lavis restent lisibles et le recours au survol demeure.

## Confirmation

Tests `web/src/components/ToastConfirmation.test.tsx` (sous-ligne, initiales de
l'auteur, étincelle de l'agent MCP), `web/src/components/OccurrenceListe.test.tsx`
(« la rangée qu'on vient de compléter porte le lavis », toast portant le titre de la
tâche) et `web/src/lib/synchro.test.ts` (`auteurPour`). La durée partagée :
`grep -n "DUREE_TOAST_MS" web/src/lib/toast.ts` et `grep -n "6s" web/src/index.css`
doivent s'accorder.
