---
type: recap
date: 2026-08-23
feature: "[[Comptes À Rebours]]"
plan: "[[Plan 2026-08-23 Comptes À Rebours V1]]"
---

# Recap Comptes À Rebours

Construit tel que planifié, en une session (2026-08-23) :

- Entité `CompteARebours` + migration `ComptesARebours` avec amorçage une-seule-fois
  (Déménagement 2026-10-06, Noël 2026-12-25) via `InsertData` — une suppression ne
  revient jamais, contrairement à un seed au démarrage.
- Tranche CRUD `/api/comptes-a-rebours` calquée sur Zones ; flux iCal enrichi des
  comptes à venir (évènements toute-la-journée).
- Carte d'Aujourd'hui branchée sur l'API : dodos ≥ 0, « C'est aujourd'hui ! » le
  jour J, état vide, « + » ouvrant le modal `ComptesAReboursGestion` (formulaire +
  liste complète, passés marqués et supprimables).
- 6 nouvelles icônes au trait (avion, valise, gâteau, cadeau, cœur, soleil) ajoutées
  à Camion/Sapin ; registre `ICONES_COMPTE` porte libellé + couleur d'accent.

Déviations : aucune sur le contrat. Détails d'exécution : la date du déménagement de
la phrase d'humeur a déménagé dans `web/src/lib/humeur.ts` (concern distinct,
documenté dans la spec) ; la flamme du gâteau est un petit cercle plein (lisibilité
à 28 px) — seule entorse au pur trait.

Vérifié : dotnet test (38 verts), builds serveur et web, CRUD + validations + .ics
exercés contre le conteneur reconstruit. Reste la passe visuelle dans l'app (login
requis), laissée à Alain.
