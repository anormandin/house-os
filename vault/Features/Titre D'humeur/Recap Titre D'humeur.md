---
type: recap
date: 2026-08-24
feature: "[[Titre D'humeur]]"
plan: "[[Plan 2026-08-24 Titre D'humeur Serveur]]"
---

# Recap Titre D'humeur

Version serveur des trois couches livrée en une session :

- Couche 1 : `EtatMaison` calculé depuis les tables (tâches du jour/retards/
  faites, comptes à rebours en dodos, météo + verdicts [[Météo]]) avec clé
  d'état dérivée — la structure qui nourrira aussi l'e-ink.
- Couche 2 : `BanquePhrases` C# (port du client + touches météo-conscientes,
  accord des pluriels corrigé au passage).
- Couche 3 : `PolissageLlm` via le SDK C# officiel (`Anthropic` 12.42.0),
  Haiku 4.5, parse défensif ; `HumeurService` à deux créneaux (5 h 30 / 17 h)
  avec rattrapage au démarrage ; table `PhraseDuJour` ; endpoint
  `/api/phrase-du-jour` ; héros d'Aujourd'hui branché, `humeur.ts` en repli.

Vérifié : 126 tests verts (22 nouveaux), génération réelle au boot dans les deux
modes (Gabarit sans clé, puis Llm avec la clé d'Alain — « La soirée est encore
jeune. », 43 dodos exacts), héros vérifié dans le navigateur.

Déviations :

1. Clé dans `appsettings.local.json` (gitignoré, chargé par `Program.cs`) —
   `appsettings.Development.json` est suivi par git, on n'y met pas de secret.
2. Bornes de journée converties en UTC pour Npgsql (offset local refusé en
   paramètre timestamptz) — découvert au premier démarrage.
3. Trois exemples few-shot ajoutés au prompt : la première phrase Haiku titrait
   en salutation ; avec les exemples, le ton rejoint la banque.
4. `Extraire`/`SerialiserEtat` publics (pas d'`InternalsVisibleTo` dans le
   projet — convention existante).

## 2026-08-25 — Phrase axée tâches ([[Plan 2026-08-25 Météo Du Moment Et Phrase Axée Tâches]])

`EtatMaison` réorienté : titres des tâches du jour (≤ 5) et des prochaines de la
semaine (≤ 4, horizon en jours) remplacent le bloc météo permanent ; la météo n'y
figure plus que via `SignalMeteoRemarquable`
([[D-2026-08-25 Phrase Du Jour Axée Tâches]]). Prompt réécrit (motivant, tâches
d'abord, météo interdite hors signal),
banque de gabarits alignée + variante « Prochaine affaire : … » sur les journées
libres. Vérifié : régénération réelle en dev — « Les boîtes commencent à se
vider. / Appels administratifs et ménage ce matin — internet à résilier demain.
Le déménagement, c'est dans 42 dodos. » — tâches réelles nommées, zéro météo un
jour ordinaire.
