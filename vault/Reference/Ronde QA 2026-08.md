---
type: reference
last-verified: 2026-08-28
verified-against: 0d96d5f
tags: []
---

# Ronde QA 2026-08

Grande ronde d'assurance qualité (2026-08-27 → 2026-08-28) au moment où l'app
est passée en usage réel. Registre opérationnel : les issues GitHub étiquetées
`qa-2026-08` (57 issues au total). Cette note est la synthèse durable.

## Déroulé

1. **Audit complet** : 8 agents de revue en parallèle (7 lots de modules +
   passe sécurité transversale) + `/vault sync` → **91 trouvailles brutes**,
   ~86 après fusion, dont 18 majeures et 0 critique. Issues #1–#55.
2. **Triage** (décision d'Alain) : tout corriger.
3. **Fixes** : 6 vagues d'écrivains (Tâches, web ×2, Budget, modules
   simples + MCP, ingestion/infra/auth), single-writer par périmètre de
   fichiers. Deux décisions produit en chemin :
   [[D-2026-08-28 Glissement Hors Fenêtre Des Intervalles]] et
   [[D-2026-08-28 Passer Conserve L'assigné]].
4. **UAT** sur dev local avec un **dump de prod restauré** — première
   validation réelle de la procédure de restauration de `scripts/backup.sh`
   (dump 129 Ko + 30 Mo de fichiers, restaurés sans accroc). Parcours pilotés
   dans Chrome sur les 7 pages + 3 modaux, données réelles, zéro erreur
   console. Deux trouvailles tardives, corrigées sur-le-champ : la
   **déconnexion morte** (#56 — `clear()` ne notifie pas l'observateur de
   `moi` en TanStack v5) et les **503 AirPlay** du proxy Vite en dev (#57).

## Ce que la ronde a changé (pointeurs)

- Specs mises à jour avec le marqueur « QA 2026-08-28 » : [[Tâches]],
  [[Budget]], [[Documents]], [[Équipements]], [[Flux Externes]],
  [[Serveur MCP]], [[Déploiement]], [[Suite De Tests]] ; nouvelle spec
  [[Auth]] ; glossaire enrichi (« Jeton iCal »).
- Tests : backend 403 → **523**, web 69 → **125**, E2E 1 → **3 parcours**
  (fumée réécrit + récurrence + budget) — détail dans [[Suite De Tests]].
- Avant le prochain déploiement : voir l'encadré d'avertissement de
  [[Déploiement]] (variables `.env` désormais exigées, chown unique du volume
  fichiers).

## Postures acceptées (pas des oublis)

- Cookie non-Secure sur accès http direct du LAN (foyer de 2, Tailscale
  chiffré) — documenté dans [[Auth]] et [[Déploiement]].
- Injection de prompt via titres de tâches dans le polissage LLM
  ([[Titre D'humeur]]) : bornée (JSON contraint, longueurs, audience = le
  couple) ; à revisiter si des tâches naissent un jour de sources externes.
- DNS rebinding sur la garde SSRF des flux externes (résolution ≠ connexion) —
  assumé pour un serveur de maison.
