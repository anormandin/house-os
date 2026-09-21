---
type: feature
status: implemented
last-verified: 2026-09-21
verified-against: c095ef2
tags: []
---

# Lettre Du Matin

## Intention

**Un courriel par jour, avant que la maison se lève, qui dit la journée en vingt
secondes, dans le lit.** Pas un rapport, pas une infolettre : une lettre en prose, de
la maison à ses deux habitants, où les faits sont calculés et où le modèle ne fait
que les coudre. La maison dit « je », s'adresse à « vous deux », a plus de place que
sur le mur (plusieurs paragraphes), et se permet un trait d'esprit, un jeu de mots,
une pensée du jour.

C'est la troisième forme retenue des maquettes ([[Éditorialiste De L'Écran]],
2026-09-20, forme **C**) : la une au mur ([[Journal De La Maison]]), l'almanach comme
seconde page, la lettre dans la boîte. « Les trois disent la même journée à trois
distances : trois mètres, un mois, vingt secondes au lit. » Le
[[Fonds De Tiroir]] a été séparé du journal précisément pour que la lettre soit son
**second consommateur** ([[D-2026-09-20 Fonds De Tiroir Séparé Du Journal]] : « le
fonds de tiroir pourrait aussi servir au courriel du matin »).

Ce que la lettre fait que ni le mur ni l'app ne font : elle **porte du contexte** —
« la même que les dix derniers dimanches », « probablement la dernière fin de semaine
complète avant que ça se remplisse ». Et elle règle le problème du mur : on la lit au
lit, pas en passant dans le corridor.

## Comportement

> [!note] As-built depuis le 2026-09-21 : les six premières étapes du
> [[Plan 2026-09-21 Lettre Du Matin]] sont exécutées et testées ; le release attend
> les réglages SMTP du `.env` de prod. Écarts et observations :
> [[Recap Lettre Du Matin]].

### Ce qui part, et à qui

- **Une lettre par jour pour la maison** ([[D-2026-09-21 Une Lettre Au Foyer]]),
  envoyée à chaque utilisateur qui a une adresse
  ([[D-2026-09-21 Adresse De Courriel Sur L'Utilisateur]] : colonne `Courriel`,
  seedée de `COMPTE_n_COURRIEL`). Un utilisateur sans adresse ne reçoit rien.
- **Par SMTP** ([[D-2026-09-21 Courriel Sortant Par SMTP]]), section `Lettre:Smtp`
  alimentée par `LETTRE_SMTP_*`. Sans configuration, l'envoi est désactivé et
  journalisé une fois ; la lettre est quand même composée et lisible dans l'app.
- **Tous les jours**, même quand la maison ne demande rien : le fonds garantit sept
  faits. Registre C1 (la lettre) ; jamais C2 (le bulletin, écarté aux maquettes).

### Ce que la maison écrit

- **Un appel à part, sur la même matière** que le journal, étendue
  ([[D-2026-09-21 Lettre Écrite À Part Sur La Même Matière]]). `MatiereDeLettre`
  enveloppe `MatiereDEdition` (date, plancher, tâches dues avec retard, ferme,
  assigné, zone, équipement ; prochain compte à rebours en dodos ; météo ; tous les
  faits du fonds, texte long et court) et y ajoute ce que la lettre seule veut : la
  **semaine devant** (échéances de 1 à 7 jours), **ce qui a été fait** depuis la
  dernière lettre (journal de complétion), la **série** de chaque tâche due (combien
  de fois de suite elle a été faite ce même jour de semaine, ce qui rachète « la même
  que les dix derniers dimanches », non dérivable des sept lettres seules), et les
  **sept lettres précédentes** (sujet et première ligne) pour ne pas radoter. Rien d'autre : ce que le modèle ne reçoit
  pas, il ne peut pas le citer.
- **Le registre** : la maison écrit à ses habitants. Trois à cinq paragraphes de prose,
  pas de liste. La première ligne tient seule comme aperçu de notification ; le sujet
  dit la journée en moins de 60 signes. Le contexte, l'humour, les jeux de mots et une
  pensée du jour sont bienvenus ; la leçon de morale, jamais. Prompt et bornes :
  [[Prompt De La Lettre]] ; lectures : [[Exemples De Lettres]].
- **Le modèle n'invente aucun fait.** Sortie JSON (`sujet`, `paragraphes`) validée
  strictement par `RedactionLettre` : sujet ≤ 60 sans point final, 3 à 5 paragraphes
  de 60 à 360 signes (le prompt vise 250 et dit 320 : Opus déborde sa consigne de dix
  à quinze pour cent, rejoué le 2026-09-21), premier paragraphe ≥ 100, total ≤ 1400,
  jamais de point d'exclamation, ni salutation, signature ou liste déguisée ; tout
  écart écarte la réponse.
- **Le modèle** est celui de l'édition (`Edition:Modele`, Opus), la clé est celle de
  toute la maison (`ANTHROPIC_API_KEY`).

### Quand, et quand ça rate

([[D-2026-09-21 Lettre Matérialisée Et Rattrapée Le Jour Même]])

- **Heure de la maison** `Lettre:HeureEnvoi` (`LETTRE_HEURE`, défaut 06:30). Le
  service de fond `LettreService` compose et envoie à l'heure, sur le patron de
  `EditorialisteService`.
- **Rattrapage le jour même** : démarré après l'heure, le service envoie la lettre du
  jour si elle n'est pas partie, jusqu'à **midi**. Passé midi, la journée est perdue et
  jamais rattrapée le lendemain.
- **Quand le modèle se tait** (clé présente, API muette ou hors contrat) : rien ne
  part ; **second essai une heure plus tard** ; s'il échoue aussi, ou s'il tomberait
  après midi, la **note C3** part en gabarit : sujet + quatre lignes composées en C#
  depuis la matière (la tâche du jour, la météo, la prochaine échéance, le compte à
  rebours). Sans clé du tout, la note est le fonctionnement normal, pas un échec.
- **Jamais deux fois** : une ligne `Lettres` par date, marquée `EnvoyeeLe` après
  l'envoi ; un redémarrage après l'envoi ne renvoie pas.

### Ce que l'entité fige

`Lettre` : date (unique), sujet, paragraphes (JSONB), matière (JSONB, ce que le modèle
a reçu), source (modèle ou gabarit), modèle, composée le, envoyée le (nul tant que
rien n'est parti), destinataires (JSONB). La matière conservée se rejoue dans
l'atelier, comme celle de l'édition
([[D-2026-09-21 Matière Conservée Sur L'Édition]]).

### Le courriel lui-même

- `text/plain` et une partie HTML simple, palette « Cuisine chaleureuse » comme la
  maquette C1 (fond crème, feuille, titre en Fraunces si disponible, sinon serif) ;
  aucun tracking, aucune image distante.
- Expéditeur `Lettre:Smtp:Expediteur` (« La maison <maison@…> »), sujet = le sujet
  écrit. Le **gabarit** pose la date, « Bonjour vous deux. », puis les paragraphes du
  modèle, puis « Bonne journée. » et la signature « — la maison », puis le pied
  « House OS · écrite à H h MM ». Le modèle n'écrit ni salutation ni signature, et la
  validation refuse celles qu'il redonnerait par réflexe ([[Prompt De La Lettre]]). Lien « Ouvrir la journée » seulement si `Lettre:UrlDeLApp`
  (`LETTRE_URL_APP`) est réglé.

### Dans l'app et par MCP

- `GET /api/lettre?date=` rend la lettre du jour (ou d'une date) telle qu'envoyée, ou
  un **aperçu en note**, composé sans modèle et sans rien écrire, si elle n'existe pas
  encore : un GET ne fait pas attendre Opus. `ecrite` dit lequel des deux.
- `POST /api/lettre/regenerer` (`date`, `envoyer`) réécrit la lettre, appel LLM
  compris, et l'envoie si demandé ; outil MCP `regenerer_lettre_du_matin` en parité
  ([[Serveur MCP]]). `POST /api/lettre/essai` l'envoie **à moi seulement** sans marquer
  `EnvoyeeLe`.
- Page desktop `/lettre` : la lettre du jour rendue comme le courriel, « Réécrire »
  (appel du modèle, en requête, comme le tirage du mur), « M'envoyer un essai ». Pas
  de page d'archive : la boîte de réception l'est déjà. Sur le téléphone, `/lettre`
  renvoie à l'accueil.
- `lister_utilisateurs` et `GET /api/utilisateurs` exposent l'adresse.

### L'atelier

`HouseOs.Essais` a `prompt-lettre`, `matiere-lettre <date> [--composer]` et
`rediger-lettre <matiere.json> [--prompt] [--fois] [--modele] [--brut]`, mêmes règles
que l'édition : n'écrit rien en base, ne touche pas au courriel, ne se connecte jamais
à la prod. `--composer` compose la matière d'une journée depuis la base, pour rejouer
une journée qui n'a pas encore de lettre (as of 2026-09-21).

## Hors périmètre

- Une notification push ou ntfy : la lettre est un courriel, rien d'autre.
- Une lettre par personne, une heure par personne : plus tard, l'adresse sur
  l'utilisateur le permet.
- Modifier l'adresse dans l'app : le `.env` est la source, comme le nom d'affichage.
- Toute mise en page e-ink ou 1-bit : c'est [[Journal De La Maison]].
- Répondre à la lettre : House OS ne lit pas ses réponses.
- Une page d'archive des lettres.

## Décisions

- [[D-2026-09-21 Courriel Sortant Par SMTP]] — MailKit, quatre variables génériques,
  envoi désactivé sans configuration.
- [[D-2026-09-21 Une Lettre Au Foyer]] — une lettre par jour pour la maison, à tous
  ceux qui ont une adresse.
- [[D-2026-09-21 Adresse De Courriel Sur L'Utilisateur]] — colonne `Courriel`, seedée
  de `COMPTE_n_COURRIEL`.
- [[D-2026-09-21 Lettre Écrite À Part Sur La Même Matière]] — son propre prompt et
  son propre appel ; second essai à une heure, sinon la note C3.
- [[D-2026-09-21 Lettre Matérialisée Et Rattrapée Le Jour Même]] — entité `Lettre`,
  heure de la maison, rattrapage jusqu'à midi, jamais deux fois.
- [[D-2026-09-20 Fonds De Tiroir Séparé Du Journal]] — la lettre est le second
  consommateur du fonds.
- [[D-2026-09-20 Édition Écrite Par Opus]] — le modèle de l'édition écrit aussi la
  lettre.

## Ancres de code

- `server/HouseOs.Api/Features/Lettre/` — la tranche : options et porte SMTP
  (`LettreOptions.cs`, `EnvoyeurSmtp.cs`), la plume et son prompt
  (`RedactionLettre.cs`), la couture des trois appelants (`GenerationLettre.cs`,
  `OperationsLettre.cs`), le gabarit du courriel (`RenduCourriel.cs`), le service de
  fond (`LettreService.cs`), la mémoire des sept jours, les endpoints.
- `server/HouseOs.Api/Domaine/Lettre/` — l'entité `LettreDuMatin`, la matière étendue
  et la série (`MatiereDeLettre.cs`), la note de repli (`NoteDeRepli.cs`).
- `server/HouseOs.Api/Features/Mcp/OutilsMaison.cs` — `lire_lettre_du_matin`,
  `regenerer_lettre_du_matin`.
- `server/HouseOs.Api/Infrastructure/AmorcageDb.cs` — l'adresse seedée et resynchronisée.
- `server/HouseOs.Essais/Program.cs` — l'atelier de la lettre.
- `web/src/pages/Lettre.tsx` — la page ; `web/src/lib/api.ts` — `LettreDuMatin`.
- `design/maquettes/eink-publications.html` — les trois maquettes C1, C2, C3.

## Sources

Aucune source externe ; le matériau est la maquette et [[Éditorialiste De L'Écran]].
Lectures de conception : [[Prompt De La Lettre]], [[Exemples De Lettres]].

## Historique

- [[Plan 2026-09-21 Lettre Du Matin]] · [[Recap Lettre Du Matin]] — bâti le 2026-09-21, release en attente des réglages SMTP.
