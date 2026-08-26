---
type: recap
date: 2026-08-25
feature: "[[Tâches]]"
plan: "[[Plan 2026-08-25 Ruban Des 7 Prochains Jours]]"
---

# Recap Ruban Des 7 Prochains Jours

Construit tel que planifié, frontend seulement :

- `web/src/lib/ruban.ts` — groupement pur (retard / 7 jours / plus-tard ≤ 30 j)
  + couleurs de zones déterministes ; 8 tests Vitest (`ruban.test.ts`), y compris
  passage de mois/année et statuts exclus.
- `web/src/components/Ruban.tsx` — le composant (focus par pièce, « + n autres »
  dépliable sur place, légende, événements externes non cliquables).
- Aujourd'hui : ruban pleine largeur, carte « Cette semaine » retirée, gestion
  des flux migrée dans l'en-tête du ruban. Pièces : ruban avec focus, la page
  requête maintenant `evenements-externes`.
- Vérifié dans l'app : ruban rendu sur les deux pages avec les vraies données,
  focus estompe bien, clic d'une puce ouvre l'éditeur complet.

Déviations : aucune au périmètre. Choix pris en route : le clic « + n autres »
déplie le jour sur place (retenu dans la décision) ; pas d'étiquette de
récurrence (« hebdo ») sur les puces — le DTO n'expose que le mode, on vit sans.
Suites au dossier : 44 tests web verts ; aucun changement backend.
