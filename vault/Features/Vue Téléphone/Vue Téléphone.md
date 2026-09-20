---
type: feature
status: implemented
last-verified: 2026-09-20
verified-against: a77b04a
tags: []
---

# Vue Téléphone

## Intention

Le téléphone est l'appareil qu'on a dans la main quand on coche une tâche en passant,
qu'on regarde une garantie dans le garage ou qu'on photographie un reçu. Jusqu'au
2026-09-19 il n'était pas une cible : l'app desktop s'y affichait réduite entre 51 % et
33 %. La vue téléphone lui donne sa **propre interface**, sœur de la vue e-ink — même
domaine, même API, présentation repensée — sans toucher au bureau, qui reste
l'application complète. C'est un **compagnon de commodité**, pas un remplacement
([[D-2026-09-19 Portée De La Vue Téléphone]]).

## Comportement

- **Aiguillage.** Quand la fenêtre fait moins de 900 px de large, l'app connectée rend
  l'arbre téléphone (coquille + écrans) ; au-delà, le bureau. Un seul point de bascule,
  `useFormatPhone()`, qui suit les changements de largeur et de rotation — pas seulement
  l'état au montage. Le seuil attrape le téléphone et la tablette en portrait (iPad :
  820 pt) et laisse l'iPad en paysage (1180 pt) au bureau. Rétrécir une fenêtre de
  bureau bascule aussi : c'est voulu, ça rend la vue testable au navigateur et dans
  Playwright.
- **Les routes sont communes.** `/`, `/taches`, `/pieces`, `/equipements`, `/documents`,
  `/budget` existent dans les deux vues ; seul le composant rendu change. Un lien
  partagé entre les deux appareils tombe donc au bon endroit.
- **Coquille.** Barre du haut collante (bouton menu, titre « Maison », avatars du foyer)
  + menu déroulant des six sections. **Pas de barre d'onglets du bas**, bien que
  [[D-2026-09-19 Interface Téléphone Distincte]] l'autorise : le menu porte la pastille
  du nombre de tâches en retard, ce qu'une barre d'onglets ne peut pas faire. Choisir
  une destination referme le menu.
- **Accueil « La pile ».** La journée se lit une carte à la fois dans la zone du pouce,
  au lieu du tableau de bord multi-colonnes du bureau.
- **Feuille d'actions.** Ce que le bureau ne révèle qu'au survol (Reporter · Passer ·
  Supprimer, `opacity-0` donc inatteignables au doigt) devient une feuille du bas.
  La suppression garde sa double confirmation.
- **Liste ⇄ détail dans le même écran.** Pièces et Équipements montrent côte à côte au
  bureau ; sur 390 px un état local (`zoneChoisie`, `equipementChoisi`) remplace la
  liste par le détail — **sans nouvelle route**, pour que le routeur reste commun aux
  deux vues.
- **Documents.** Le tableau à six colonnes (1161 px au bureau) devient une liste de
  cartes à une colonne ; la barre latérale de facettes (256 px) devient une feuille du
  bas, mêmes groupes et mêmes compteurs ; pas de pagination — la liste s'allonge par
  lots ; tri figé au défaut (le plus récemment daté d'abord), faute d'en-têtes de table.
- **Budget.** Mesures empilées, enveloppes en pleine largeur.
- **Les vues d'analyse n'existent pas ici.** Budget → Flux et Tâches → Année restent
  desktop seulement, et leurs commutateurs (« Aperçu | Flux », « Liste | Année »)
  **disparaissent** de la vue téléphone plutôt que d'afficher un segment inerte
  ([[D-2026-09-19 Portée De La Vue Téléphone]]).
- **Contraintes tactiles tenues sur les six écrans** (vérifiées en émulation iPhone 15) :
  aucun débordement horizontal, aucune cible sous 44 pt, aucun champ de saisie sous
  16 px (en dessous, iOS zoome au focus). `viewport-fit=cover` dans `web/index.html` —
  sans lui, `env(safe-area-inset-*)` vaut 0 et la vue passe sous l'encoche.
- **Direction visuelle inchangée** : [[D-2026-08-23 Direction Artistique Cuisine Chaleureuse]]
  gouverne les trois vues (bureau, téléphone, e-ink) — mêmes tokens, même typographie.

## Hors périmètre

- **Parité écran-par-écran avec le bureau** — explicitement pas un objectif, et ne doit
  pas le devenir. Chaque nouvelle tranche décide si elle a une présentation téléphone ;
  l'absence est une réponse valable. Un écran téléphone qui imite la densité du bureau
  est un signal qu'on s'éloigne du rôle de compagnon.
- **Budget → Flux (sankey) et Tâches → Année (ruban)** — maquettés en version verticale,
  écartés : trop de place pour une lecture qu'on ne fait pas debout dans un corridor.
- **Application native, service worker, mode hors-ligne** — le retrait du service worker
  ([[D-2026-08-25 Retrait Du Service Worker]]) tient ; rien ici ne le réintroduit.
- **Barre d'onglets du bas** — permise par la décision, non retenue par la direction
  « La pile ».
- **Vue téléphone de l'écran e-ink** (`/ecran`) — elle vit hors de la session et ignore
  l'aiguillage.

## Décisions

- [[D-2026-09-19 Interface Téléphone Distincte]] — le téléphone devient une cible avec
  son propre arbre de présentation ; supersède
  [[D-2026-08-23 Interface Desktop Et Écran E-ink]] sur « probablement jamais ».
- [[D-2026-09-19 Portée De La Vue Téléphone]] — compagnon de commodité, pas de parité ;
  Flux et Année restent desktop seulement.
- [[D-2026-08-23 Direction Artistique Cuisine Chaleureuse]] — mêmes tokens pour les
  trois vues.
- [[D-2026-08-23 Frontend Vite React PWA]] — le stack ne change pas.

## Ancres de code

- `web/src/hooks/useFormatPhone.ts` — le point de bascule unique (`SEUIL_TELEPHONE_PX`).
- `web/src/App.tsx` — l'aiguillage coquille + écrans dans les routes communes.
- `web/src/components/telephone/CoquilleTelephone.tsx` — barre du haut, menu, pastille
  des retards (+ `CoquilleTelephone.test.tsx`).
- `web/src/components/telephone/FeuilleActions.tsx` — équivalent tactile des actions au
  survol.
- `web/src/pages/telephone/` — les six écrans (`AujourdhuiTelephone`, `TachesTelephone`,
  `PiecesTelephone`, `EquipementsTelephone`, `DocumentsTelephone`, `BudgetTelephone`).
- `web/src/components/DialogueCalendrier.tsx` — « Mon calendrier » et la déconnexion,
  partagés par les deux coquilles.
- `web/index.html` — `viewport-fit=cover`.

## Sources

Maquettes des directions explorées : canevas Claude Design « House OS — UI téléphone »
(2026-09-19), en complément de `design/maquettes/`.

## Historique

Auditée puis livrée en une session le 2026-09-20 (commit `a77b04a`), sur la base des
deux décisions du 2026-09-19. Aucune note de plan n'a été écrite avant l'exécution —
écart au contrat du vault, constaté et consigné le 2026-09-20. Voir
[[Recap Vue Téléphone]].
