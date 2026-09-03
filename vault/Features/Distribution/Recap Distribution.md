---
type: recap
date: 2026-09-02
feature: "[[Distribution]]"
plan: "[[Plan 2026-09-02 Ouverture Du Dépôt]]"
---

# Recap Distribution

Construit tel que planifié, avec trois écarts :

- Les prompts LLM ne lisent pas les noms du foyer en base : « un couple québécois » /
  « une maison au Québec » suffisent, sans changer les signatures.
- Les teintes d'avatar suivent le rang dans la liste `/api/utilisateurs` (ordre par
  nom d'affichage), via un registre de module alimenté par `Layout` — pas de hook,
  pour que les toasts et les tests sans QueryClient restent simples.
- Le changement de mot de passe reste une limite documentée (le journal de complétion
  cascade sur l'utilisateur : supprimer un compte pour le recréer aurait effacé son
  historique).

Reste ouvert : la migration de la prod (Partie D) et le passage en public sur GitHub.
