---
type: recap
date: 2026-08-23
feature: "[[Tâches]]"
plan: "[[Plan 2026-08-23 V0 Déménagement]]"
---

# Recap Tâches

V0 « Déménagement » livrée en une session, conforme au plan. Tâches ponctuelles de
bout en bout : quick-add (titre, échéance, assignation), vues Aujourd'hui (échues et
en retard) et Tâches (à faire / complétées), complétion avec attribution + entrée de
journal, suppression. Login cookie 2 comptes (alain, ariane — mot de passe temporaire
à changer). PWA française thème Kanagawa, servie par le conteneur .NET unique
(`docker compose up`, port 8080), Postgres migré + amorcé au démarrage.

Déviations du plan :
1. La spec de récurrence n'est pas un objet embarqué : `Mode` + `Rollover` sont des
   colonnes de `Tache` ; les paramètres fixe/intervalle viendront par migration V1.
2. Le journal n'a pas de FK vers Tache/Occurrence (ids historiques seulement) — il
   survit à la suppression d'une tâche.
3. Vérification visuelle UI non faite en session (extension Chrome déconnectée) —
   API validée par curl ; à confirmer par l'utilisateur sur http://localhost:8080.

## Addendum 2026-08-23 (soir) — reconstruction UI chaleureuse

L'UI V0 (coquille shadcn mobile, jetable par design) a été reconstruite dans le
design final « Cuisine chaleureuse » (look C5 + structure desktop) : thème complet
dans `web/src/index.css` (Fraunces + Nunito Sans auto-hébergées), Aujourd'hui avec
héros illustré + titre d'humeur (repli client), cartes latérales (Cette semaine,
L'équipe, Comptes à rebours intérimaires Déménagement/Noël), rangées complétées
« bravo X ✓ 14 h 10 », quick-add repliable ⌘K, Connexion et Tâches assorties.
Côté API : nouveau filtre `faites` (bornes d'instants de la journée locale du
client). Composants shadcn et bascule sombre supprimés (mode clair seulement pour
l'instant). Vérifié visuellement dans Chrome cette fois (connexion → création via
quick-add → listes → réactivité) ; `tsc`, lint et les 7 tests serveur au vert ;
image Docker reconstruite. Des tâches d'essai (camion, Hydro-Québec, boîtes, SAAQ,
internet) ont été créées pendant la vérification — **toutes les données actuelles
sont temporaires** : la base sera repartie à neuf quand Alain et Ariane commenceront
la vraie saisie (au plus tard au déploiement).

Reste ouvert pour la V1 : moteur de récurrence (3 modes), zones, équipements, flux
iCal, script de backup, déploiement Tailscale sur la machine de la nouvelle maison.

## Incrément 2026-08-26 — documents liés aux tâches

Exécute [[Plan 2026-08-26 Documents Liés]] ([[D-2026-08-26 Documents Liés Aux Tâches]],
demande d'Alain : plusieurs documents par tâche) :

- Backend : jointure `TacheDocuments` (migration `AjouterDocumentsDeTache`,
  cascade du lien seulement), `documentIds` dans détail/POST/PUT et MCP
  (`creer_taches`, `gerer_tache`) — null = conserver, [] = délier, liste =
  remplacer ; ids validés. 2 tests d'intégration.
- Web : section « Documents de référence » du `TacheEditeur` (chips cliquables
  vers le fichier, retrait ×, select d'ajout) ; fixture `TACHE_COMPLETE` étendue,
  invariant édition-sans-perte couvre `documentIds`. 2 tests de composant.
- Vérifié : 343 backend + 50 web verts, aller-retour complet dans l'app dev
  (lier → enregistrer → rouvrir → délier).
- Après release : le rapport d'inspection de la Colline a été lié aux 9 tâches
  d'entretien créées le même jour en prod.

Déviations : aucune.

## Incrément 2026-08-26 — page Tâches : Rythmes ⇄ Année

Exécute [[Plan 2026-08-26 Tâches Rythmes Et Année]]
([[D-2026-08-26 Page Tâches Rythmes Et Année]], itération A des maquettes en deux
rondes — artifacts « Maquettes Tâches » et « Itérations Tâches ») :

- Backend : `GET /api/taches` (nouveau `TacheResumeDto` — récurrence, stratégie,
  échéance/assigné de l'occurrence en attente, `nbDocuments`, `completee`) via
  `OperationsTaches.ListerTachesAsync`, réutilisé par l'outil MCP `lister_taches`.
  4 tests (2 opérations, 1 MCP exécuté, 1 intégration HTTP) → 347.
- Web : `Taches.tsx` réécrite — commutateur segmenté « Liste | Année » (dernier
  mode en localStorage, accès protégé), vue Rythmes (5 groupes, sous-titre
  « n en retard », progression des ponctuelles), vue Année (points
  mensuels/annuels, barres de fenêtres en cours/à venir avec chevauchement d'an,
  grappes de ponctuelles fusionnées sous 1,5 % d'année, jalons des comptes à
  rebours, ligne Aujourd'hui, bandeau « Le tempo court »), tout clic → éditeur,
  ⌘K conservé. Logique pure dans `web/src/lib/taches-vues.ts`. 8 tests
  (5 lib + 3 page) → 58. Le filtre À faire/Complétées et la barre quick-add
  pointillée disparaissent de la page.
- Vérifié visuellement dans Chrome (deux vues, données dev, éditeur depuis une
  rangée Année).

Déviations : fusion des grappes de ponctuelles ajoutée en cours de route (des
échéances à 1-3 jours d'écart s'empilaient sur le même pixel à l'échelle de
l'année) ; localStorage enveloppé de try/catch (absent du jsdom de Vitest, et
navigation privée).
