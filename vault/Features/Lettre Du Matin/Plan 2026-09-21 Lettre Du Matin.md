---
type: plan
status: approved
date: 2026-09-21
feature: "[[Lettre Du Matin]]"
---

# Plan 2026-09-21 Lettre Du Matin

Sept étapes, chacune livrable et testée seule. Le patron est celui du journal mural
([[Plan 2026-09-20 Journal Éditorial]]) : un service de fond par créneau, un rédacteur
derrière une interface, un gabarit qui rend toujours quelque chose, la matière
conservée pour l'atelier. Décisions qui gouvernent : voir la spec.

## Étape 1 — La porte de sortie

- [x] NuGet `MailKit` dans `server/HouseOs.Api/HouseOs.Api.csproj` (même auteur que
      MimeKit, déjà là). Vérifier la signature courante de `SmtpClient` par Context7
      avant d'écrire.
- [x] `Features/Lettre/LettreOptions.cs` : `HeureEnvoi` (06:30), `LimiteRattrapage`
      (12:00), `UrlDeLApp`, `Smtp { Hote, Port, Usager, MotDePasse, Expediteur }`,
      `Actif` = les cinq réglages SMTP présents.
- [x] `Features/Lettre/IEnvoyeurDeCourriel.cs` (`EnvoyerAsync(destinataires, sujet,
      texte, html)`), `EnvoyeurSmtp.cs` (MailKit, STARTTLS ou SSL selon le port),
      `EnvoyeurInactif.cs` (journalise une fois, comme `DepotCourrielsInactif`).
      Enregistrement dans `Program.cs` à côté du courriel entrant.
- [x] Configuration générique : `appsettings.json` (section `Lettre`),
      `docker-compose.yml` (`Lettre__*` ← `LETTRE_HEURE`, `LETTRE_SMTP_HOTE`,
      `LETTRE_SMTP_PORT`, `LETTRE_SMTP_USAGER`, `LETTRE_SMTP_MDP`,
      `LETTRE_SMTP_EXPEDITEUR`, `LETTRE_URL_APP`), `.env.example` commenté,
      `docs/configuration.md` (deux tableaux). Rien de propre au foyer.
- [x] Test : `EnvoyeurSmtp` n'est jamais construit sans configuration ; un fictif à
      compteur (`Tests/Features/Lettre/EnvoyeurFictif.cs`) sert au reste du plan.
- [x] **Écart au plan : `Actif` = hôte + expéditeur, pas les cinq.** Un relais maison
      sans authentification est un cas réel ; l'usager et le mot de passe sont
      facultatifs et l'authentification n'a lieu que si l'usager est là
      (`SmtpOptions.AvecAuthentification`). Le port commande le TLS : 465 dès la
      connexion, 587 STARTTLS, autre négocié. L'expéditeur se lit avec ou sans nom et
      une chaîne qui n'est pas une adresse est refusée au premier envoi.

## Étape 2 — Le schéma

- [x] `Domaine/Utilisateur.cs` : `string? Courriel`. `UtilisateurSeed` gagne
      `Courriel` ; `Infrastructure/AmorcageDb.cs` la pose à la création **et** la met à
      jour quand le `.env` change (le mot de passe, lui, n'est jamais réécrit).
      Compose : `Seed__Utilisateurs__n__Courriel` ← `COMPTE_n_COURRIEL`.
- [x] `Domaine/Lettre/Lettre.cs` : `Id`, `Date` (index unique), `Sujet`, `Paragraphes`
      (JSONB), `Matiere` (JSONB), `Source` (Llm | Gabarit), `Modele`, `ComposeeLe`,
      `EnvoyeeLe?`, `Destinataires` (JSONB). Configuration EF dans le DbContext, sur le
      modèle d'`Edition`.
- [x] Migration générée : `dotnet ef migrations add AjouterLettreEtCourrielUtilisateur`
      dans `server/HouseOs.Api` ; relire le fichier, jamais le retoucher à la main.
- [x] `GET /api/utilisateurs` et `lister_utilisateurs` (MCP, `Features/Mcp/`) exposent
      `courriel` ; DTO et `web/src/lib/api.ts` suivent.
- [x] Tests : amorçage avec et sans adresse, mise à jour de l'adresse au second
      amorçage, unicité de `Lettres.Date` (intégration).
- [x] **Précisions à l'exécution.** L'entité s'appelle `LettreDuMatin` (namespace
      `Domaine.Lettre`) pour ne pas entrer en collision avec le namespace
      `Features.Lettre`. L'amorçage est extrait en `AmorcageDb.AmorcerUtilisateursAsync`
      pour se tester ; l'adresse est normalisée (trim, minuscules) et **suit la config
      à chaque démarrage, vide = effacée**, là où le mot de passe n'est posé qu'à la
      création. `UtilisateurDto` gagne `Courriel` avec défaut nul : servi par les deux
      listes du foyer seulement, nul sur une tâche ou une occurrence. Migration
      `20260921185718_AjouterLettreEtCourrielUtilisateur`.

## Étape 3 — La matière et la plume

- [x] `Domaine/Lettre/MatiereDeLettre.cs` : enveloppe `MatiereDEdition` +
      `SemaineDevant` (occurrences ouvertes de J+1 à J+7 : date, titre, assigné),
      `FaitesDepuisLaDerniere` (journal de complétion depuis la dernière lettre
      envoyée, ou 24 h : titre, qui), `Serie` par tâche due (complétions consécutives
      au même jour de semaine, lues du journal de complétion ; c'est ce qui manquait au
      prompt pour « la même que les dix derniers dimanches », [[Prompt De La Lettre]]
      point 7) et `LettresPrecedentes` (sept : date, sujet, première ligne). Chaque
      ajout reçoit sa ligne dans le prompt. Sérialisation dans `RedactionLettre` sur le modèle de
      `RedactionLlm.SerialiserMatiere`.
- [x] `Features/Lettre/RedactionLettre.cs` : `PromptParDefaut` pris de
      [[Prompt De La Lettre]] (bloc de code de la note), contrat de sortie
      (`sujet` ≤ 60, `paragraphes` 3 à 5, bornes par paragraphe et plafond total tels
      que la note les fixe), `Extraire` strict, `RedigerAvecEcart` (écart nommé,
      texte brut) pour l'atelier. `DelaiMax` comme l'édition.
- [x] `Features/Lettre/IRedacteurLettre.cs` + `RedacteurLettreAnthropic` (clé de
      `HumeurOptions.CleEffective()`, modèle `Edition:Modele`), `PeutEcrire`.
- [x] `Domaine/Lettre/NoteDeRepli.cs` : la note C3 en C# pur, sujet + quatre lignes
      (la tâche qui compte ou « rien à faire », la météo, la prochaine échéance de la
      semaine, le compte à rebours en dodos), jamais vide.
- [x] `Features/Lettre/MemoireDesLettres.cs` : les sept dernières lettres envoyées.
- [x] Tests domaine (sans base) : `Extraire` refuse chaque écart (sujet trop long,
      deux paragraphes, six, plafond dépassé, JSON tordu) ; `NoteDeRepli` sur une
      journée vide, une chargée, sans météo ; la matière n'expose que ce qui est prévu.
- [x] **Précisions à l'exécution.** `TexteDeLettre` (sujet + paragraphes) et
      `Serie.Compter` (on remonte de sept jours en sept jours tant qu'une complétion
      tombe sur la date attendue ; le jour même ne compte pas) vivent dans
      `Domaine/Lettre/`. La matière conservée est **à plat** comme celle de l'édition,
      avec `serie` sur chaque tâche due et trois listes de plus ; le prompt a reçu ses
      trois lignes et la note du vault suit. La mémoire ne retient que les lettres
      **parties** (`EnvoyeeLe` non nul), sujet + 120 premiers signes.

## Étape 4 — Composer, envoyer, réessayer

- [x] `Features/Lettre/GenerationLettre.cs` : `ComposerAsync(date, remplacer)` lit les
      sources du jour (`ComposerDonneesEcran.LireLesSourcesAsync`, réutilisé tel quel),
      cadre la matière, appelle le rédacteur, écrit la ligne `Lettres` avec sa matière
      ; `EnvoyerAsync(lettre)` rend le courriel, envoie à tous les utilisateurs avec
      adresse, pose `EnvoyeeLe` et `Destinataires`. Jamais deux fois : une lettre
      `EnvoyeeLe` non nul ne repart pas sans `remplacer`.
- [x] `Features/Lettre/RenduCourriel.cs` : `text/plain` + HTML minimal (palette de la
      maquette C1, inline, aucune ressource distante) ; le gabarit pose la date,
      « Bonjour vous deux. », les paragraphes, « Bonne journée. », « — la maison », le
      pied avec l'heure, et le lien « Ouvrir la journée » si `UrlDeLApp`.
- [x] `Features/Lettre/LettreService.cs` (`BackgroundService`, patron de
      `EditorialisteService`) : réveil à `HeureEnvoi` ; au démarrage, rattrapage si
      l'heure est passée et qu'il est avant `LimiteRattrapage` ; quand le rédacteur
      **peut** écrire mais se tait, ne rien envoyer et poser un second essai à + 1 h ;
      si le second essai échoue, ou tomberait après la limite, composer la note en
      gabarit et l'envoyer ; sans clé, la note part tout de suite.
- [x] Tests (`Tests/Features/Lettre/LettreServiceTests.cs`, fictifs à compteur) :
      démarré à 9 h non partie → part ; démarré à 13 h → ne part pas et rien le
      lendemain ; modèle muet à 6 h 30 → zéro envoi, essai à 7 h 30 ; muet deux fois
      → un envoi, source Gabarit ; muet à 11 h 40 → note tout de suite ; déjà
      `EnvoyeeLe` → zéro appel, zéro envoi ; sans adresse nulle part → composée, pas
      envoyée, journalisé.
- [x] **Précisions à l'exécution.** Un modèle muet à l'heure laisse quand même une
      ligne `Lettres` (source Gabarit, la note et sa matière, `EnvoyeeLe` nul) : c'est
      son heure de composition qui borne le second essai, comme au mur, et la ligne
      dit « composée, pas partie » après un redémarrage. Un SMTP qui refuse ne marque
      rien et le service repasse **un quart d'heure** plus tard. Sans SMTP ou sans
      adresse, la lettre est composée, journalisée, jamais marquée envoyée. Le
      service ne connaît pas de signal : rien ne le réveille hors de son heure.
      `GenerationLettre.ApercuAsync` compose sans écrire, pour l'étape 5.

## Étape 5 — L'app et le MCP

- [x] `Features/Lettre/LettreEndpoints.cs` : `GET /api/lettre?date=` (la lettre
      écrite, sinon composée sans écrire), `POST /api/lettre/regenerer`
      (`date?`, `envoyer`), `POST /api/lettre/essai` (à l'utilisateur connecté
      seulement, `EnvoyeeLe` intact). Session requise, comme les autres tranches.
- [x] MCP `regenerer_lettre_du_matin(date?, envoyer?)` et `lire_lettre_du_matin(date?)`
      dans `Features/Mcp/OutilsMaison.cs`, descriptions au même registre que
      `regenerer_journal_mural`. Parité vérifiée : tout ce que REST fait, MCP le fait,
      sauf l'essai à moi (il n'y a pas de « moi » en MCP, dire pourquoi dans la
      description).
- [x] `web/src/pages/Lettre.tsx` (desktop seulement, route `/lettre` dans `App.tsx`,
      entrée de navigation dans `Layout`) : la lettre rendue comme le courriel,
      « Réécrire », « M'envoyer un essai », l'état (envoyée à H h MM à …, ou pas
      encore, ou envoi désactivé). `web/src/lib/api.ts` : types et appels.
- [x] Tests : intégration des trois endpoints ; test web de la page (deux états).
- [x] **Précisions à l'exécution.** Le `GET` sans lettre écrite rend un aperçu **en
      note, sans modèle** : un GET ne fait pas attendre Opus ; c'est « Réécrire »
      (`POST /api/lettre/regenerer`) qui appelle le modèle, en requête, comme le
      tirage du mur. L'essai à moi envoie la lettre écrite si elle existe, sinon une
      composée par le modèle et jamais écrite ; 400 sans adresse sur mon compte ou
      sans SMTP. Les trois appelants passent par `OperationsLettre`. Sur le téléphone,
      `/lettre` renvoie à l'accueil. Trouvé au test d'intégration : Npgsql refuse tout
      `DateTimeOffset` non UTC en paramètre — tous les instants de la lettre sont
      normalisés en UTC, comme `GenereLe` sur l'édition.

## Étape 6 — L'atelier

- [x] `server/HouseOs.Essais/Program.cs` : `prompt-lettre`, `matiere-lettre <date>`
      (colonne `Lettres.Matiere`, base de dev), `rediger-lettre <matiere.json>
      [--prompt fichier] [--fois n] [--modele id] [--brut]`. Même garde : n'écrit
      rien, n'envoie rien.
- [x] Rejouer les cinq journées de [[Exemples De Lettres]] contre le prompt en
      vigueur, lire, ajuster le prompt dans la note **puis** dans la constante.
- [x] **Précisions à l'exécution.** `matiere-lettre --composer <date>` compose la
      matière depuis la base (vrai contexte jsonb, météo et lieu de `appsettings`,
      banque du hasard de l'app) sans rien écrire : c'est ce qui a permis de rejouer
      des journées qui n'ont pas encore de lettre. Les cinq journées de
      [[Exemples De Lettres]] sont de la prose, pas des matières : deux journées
      **réelles** ont été rejouées à leur place (le 24 septembre, vide ; le 20 octobre,
      quatorze tâches). Résultat et réglage : [[Prompt De La Lettre]], « Rejoué au vrai
      modèle » — cible 250, contrat 360.

## Étape 7 — Fermer

- [x] `vault` : spec `status: implemented`, ancres de code posées, Recap, décisions
      liées ; [[Prompt De La Lettre]] nomme la constante et en garde la copie de
      travail ; `Home.md` à jour ; validation.
- [ ] `.env` de prod : `COMPTE_1_COURRIEL`, `COMPTE_2_COURRIEL`, `LETTRE_SMTP_*`,
      `LETTRE_URL_APP` (hors dépôt, [[Déploiement]]). Vérifier depuis le LXC que le
      port SMTP sort (`nc -vz hote 587`) avant le release.
- [ ] Release par le chemin habituel, `POST /api/lettre/essai` depuis l'app, puis la
      première vraie lettre le lendemain matin ; lire le journal Seq du service.
- [ ] **En attente d'Alain** : les valeurs du `.env` de prod (`LETTRE_SMTP_*`,
      `COMPTE_n_COURRIEL`, `LETTRE_URL_APP`) ne sont pas dans le dépôt ni dans la
      session ; le release attend qu'elles soient posées. Fournisseur retenu le
      2026-09-21 : **Resend**, le compte déjà en place pour un autre projet du foyer,
      dont le domaine d'envoi `mail.alainnormandin.dev` est vérifié (DKIM, SPF, MX de
      retour) — l'apex porte un SPF d'un autre fournisseur, d'où le sous-domaine.
      Port **587** (465 a déjà fait échouer un autre client), usager `resend`, une
      **clé API distincte** pour House OS, expéditeur
      `La maison <maison@mail.alainnormandin.dev>`. Aucun DNS à poser.

## Vérification d'ensemble

- `dotnet test` vert ; `npm test` vert ; `dotnet build` sans avertissement neuf.
- `grep -rn "Playwright\|Seuillage\|IRenduEcran\|1872\|1404" server/HouseOs.Api/Features/Lettre/`
  ne retourne rien : la lettre ne connaît pas le mur. (Elle lit les sources du jour par
  `ComposerDonneesEcran.LireLesSourcesAsync` et le lieu par `AffichageOptions`, qui
  vivent chez `Affichage` — comme l'édition ; c'est le lecteur commun, pas le rendu.)
- Les confirmations des cinq décisions du 2026-09-21 passent.
- Dépôt public : aucune adresse, aucun hôte SMTP, aucun prénom dans le code ni dans
  les défauts.

## Ce que ce plan ne fait pas

- Une lettre ou une heure par personne ; l'édition de l'adresse dans l'app.
- Une page d'archive des lettres.
- Push, ntfy, réponse à la lettre.
