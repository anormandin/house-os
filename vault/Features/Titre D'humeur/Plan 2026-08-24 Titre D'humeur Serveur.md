---
type: plan
status: executed
date: 2026-08-24
feature: "[[Titre D'humeur]]"
---

# Plan 2026-08-24 Titre D'humeur Serveur

Version serveur des trois couches : faits calculés (C#) + banque de gabarits
météo-consciente + polissage Haiku 4.5 matin et soir, table `PhraseDuJour`.
Gouverné par [[D-2026-08-24 Phrase Du Jour Haiku Matin Et Soir]]
et [[D-2026-08-23 Pas De N8n Dans Le Cœur]]. Faits v1 (choix utilisateur) :
tâches du jour, météo + verdicts, comptes à rebours — pas d'assignations par
personne.

## Étapes

### Domaine (`Domaine/Humeur/`)

- [x] `EtatMaison.cs` — l'état structuré (couche 1) : date + moment (matin/soir),
      compte d'ouvertes/en retard/faites, comptes à rebours proches (titre +
      dodos), météo du jour (min/max, prob. pluie, code) + verdicts favorables,
      et une clé d'état dérivée (tout-fait, retard, journée-chargée, calme…).
      Cette structure nourrira aussi la vue e-ink.
- [x] `BanquePhrases.cs` — couche 2 : ~25 phrases françaises par état, rotation
      ensemencée par la date, variantes météo-conscientes ; jamais de
      culpabilisation. Port et extension de `web/src/lib/humeur.ts`.
- [x] `PhraseDuJour.cs` — entité : date, moment, titre, sous-titre, source
      (`Gabarit`/`Llm`), généré le. Index unique (date, moment).
- [x] Tests : dérivation de la clé d'état, banque (états, rotation, météo).

### Infrastructure

- [x] DbSet + index dans `HouseOsDbContext` ; migration EF `AjouterPhraseDuJour`.

### Tranche `Features/Humeur/`

- [x] `HumeurOptions` — heures matin/soir (5 h 30 / 17 h), modèle
      (`claude-haiku-4-5`), clé (`Humeur:CleApi`, repli env `ANTHROPIC_API_KEY`).
- [x] `ConstruireEtat` — requêtes EF (occurrences, comptes à rebours, prévisions
      + règles [[Météo]]) → `EtatMaison`.
- [x] `PolissageLlm` — SDK C# officiel (`Anthropic` NuGet) : prompt système avec
      règles de style (registre québécois, « dodos », jamais de culpabilisation,
      longueurs max, aucun chiffre hors de l'état fourni), état en JSON, réponse
      JSON `{titre, sousTitre}` parsée défensivement. Testé sur le parsing, pas
      le réseau.
- [x] `HumeurService` (`BackgroundService`) — au démarrage puis à 5 h 30 et 17 h :
      construire l'état → LLM si clé présente, sinon banque → upsert
      `PhraseDuJour` ; échec LLM → banque, jamais de trou.
- [x] `HumeurEndpoints` — `GET /api/phrase-du-jour` : la phrase la plus récente
      du jour (soir sinon matin), 404 si rien.

### Web (Aujourd'hui)

- [x] `api.ts` — type + fetch `phraseDuJour`.
- [x] `Aujourdhui.tsx` — utiliser la phrase serveur quand elle existe ;
      `humeur.ts` reste le repli si la requête échoue (repli ultime).

### Vérification et clôture

- [x] `dotnet test` vert ; démarrage local : phrase générée au boot (banque sans
      clé, LLM avec clé si disponible), `curl /api/phrase-du-jour`, héros vérifié
      dans le navigateur (extension Chrome connectée — sinon STOP et demander).
- [x] Vault : spec [[Titre D'humeur]] as-built (`status: implemented`), Recap,
      stamps, validation.
- [x] Correctif : la clé vit finalement dans `appsettings.local.json` (déjà
      gitignoré), chargé par `Program.cs` — pas dans
      `appsettings.Development.json` qui est suivi par git.
- [x] Correctif : trois exemples few-shot ajoutés au prompt système — la première
      phrase Haiku titrait en salutation, fade à côté de la banque.
