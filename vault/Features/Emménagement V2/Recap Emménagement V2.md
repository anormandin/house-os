---
type: recap
date: 2026-09-30
feature: "[[Emménagement V2]]"
plan: "[[Plan 2026-09-28 Emménagement V2]]"
---

# Recap Emménagement V2

Planifié le 2026-09-28 après la revue d'état, codé le 2026-09-29 en une session
(commit `c4fdf0e`, 51 fichiers), semé en prod le même jour, fermé le 2026-09-30.

**Livré.** La catégorie d'équipement (liste fermée de dix, nullable, migration
`AjouterCategorieEquipement`), partout : REST, MCP, fiches bureau et téléphone, filtre
du bureau. La tranche Entretien : 58 items de packs pour une maison de zone 4
(`packs-entretien.qc.json`, remplaçable par `ENTRETIEN_FICHIER`), proposer / adopter en
REST et en MCP (`proposer_entretiens`, `adopter_entretiens`), le panneau sur la fiche
d'un équipement classé et « Programme de la maison » sur la page Tâches. Le skill
`inventorier-maison`. La dette : `AsSingleQuery()` sur les cinq requêtes à double
`Include`, la décision « La pile » rétro-datée, la feuille de route d'[[Architecture]]
au présent, et la cadence de nuit de l'écran réglée à 4 h. Tests : 1023 backend,
259 web.

**Écarts au plan.**
- La dette de la liste des tâches était **mal diagnostiquée** : le produit est borné par
  l'invariant « une occurrence en attente par tâche » (66 lignes contre 131 en requête
  scindée). Le correctif est donc `AsSingleQuery`, pas `AsSplitQuery`.
- Le volet Hydro-Québec est **tombé au grill** : le foyer n'est inscrit à aucune offre
  de pointe. L'API est documentée dans la spec pour le jour où.
- Le budget est ancré depuis une **capture d'écran** (à sa valeur du 2026-09-29 — pas
  de chiffres dans le vault, le dépôt est public), faute de CSV ; la confrontation du lecteur AccWeb et les enveloppes restent l'étape 4, la seule
  ouverte du plan, qui garde `status: approved` pour cette raison.
- Le semis s'est fait **avec Alain au clavier**, pas en essai à blanc : 10 équipements
  classés, 11 créés, la pièce Garage, 12 tâches d'Alain, 3 retouchées, 19 items de packs
  adoptés. Les premières occurrences tombant avant le 6 octobre ont été repoussées au
  2026-10-10 : une annuelle manquée glisse d'un an.
- Le plafond de nuit de l'écran passe de 3600 à **14400 s par défaut** (T2, test
  `Les_defauts_livres_menagent_la_pile` mis à jour) : la nuit du 2026-09-29 a montré le
  firmware 1.8.10 honorant le délai (22 h 09 → 2 h 08 → 5 h 28).

**Ce que l'usage a appris.**
- Les packs par **catégorie sont grossiers** : chaque équipement de plomberie se voit
  proposer fosse septique, puits et adoucisseur. Un sous-type d'équipement, ou des packs
  plus fins, serait la suite si l'agacement se confirme.
- Le « déjà présent » par titre laisse passer les doublons de sens (« Nettoyer les
  gouttières » du pack contre « Vider les gouttières » d'Alain) : c'est le prix accepté
  par [[D-2026-09-28 Packs D'entretien En Fichier De Données]], et l'humain filtre.
- Un nouvel outil MCP n'apparaît dans une session Claude Code qu'au **redémarrage de
  la session** ; en attendant, curl stateless sur `/mcp`.
- Le rapport d'inspection est une **source, pas une vérité** : Alain a écarté les
  évaluations par spécialiste, et deux équipements (fenêtres, réservoir de propane)
  avaient déjà été remplacés.

**Reste.** Étape 4 : le premier export CSV AccWeb, puis les enveloppes (taxes de
Sainte-Catherine, toiture 2036, chauffe-eau du garage). Les électroménagers (laveuse,
sécheuse, frigo, lave-vaisselle) ne sont pas encore inventoriés : leur pack attend.
