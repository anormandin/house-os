---
type: feature
status: implemented
last-verified: 2026-08-24
verified-against: b9e7e9b
tags: []
---

# Titre D'humeur

## Intention

Le geste signature de la direction chaleureuse : la bannière-héros d'Aujourd'hui
(« On y est presque. » / « 4 petites choses avant dodo — le camion attend depuis
hier, le reste est sous contrôle. »). Doit rester charmante sans devenir répétitive,
sans jamais culpabiliser, et sans dépendre d'un service externe pour s'afficher.

## Comportement (trois couches, serveur depuis 2026-08-24)

1. **Faits calculés (C#, déterministe)** : compte du jour, retards, items du soir,
   météo, jalons de comptes à rebours, complétions. Tout ce qui est numérique ou
   factuel vient de cette couche — jamais du LLM (pas de chiffres hallucinés).
   Cette même structure nourrira la vue e-ink.
2. **Banque de gabarits (plancher)** : ~20-30 phrases françaises par état de maison
   (tout-est-fait, retard, soirée chargée, veille de déménagement…), rotation
   ensemencée par la date. Zéro coût, testable, sert de repli permanent.
3. **Polissage LLM (optionnel, jamais dans le chemin de requête)** : un
   `BackgroundService` (cohérent avec [[D-2026-08-23 Pas De N8n Dans Le Cœur]])
   appelle Haiku 4.5 à deux créneaux fixes — matin 5 h 30 et soir 17 h,
   configurables ([[D-2026-08-24 Phrase Du Jour Haiku Matin Et Soir]]) — avec
   l'état structuré en JSON + règles de style (registre québécois, « dodos »,
   jamais de culpabilisation — principe anti-harcèlement de [[Inspiration UI]],
   longueurs max, exemples few-shot) et stocke titre + sous-titre dans la table
   `PhraseDuJour` (une ligne par date + moment). L'UI lit
   `GET /api/phrase-du-jour` ; clé absente ou appel/parse raté → couche 2 ;
   requête HTTP ratée côté web → banque client (`humeur.ts`), le repli ultime.
   Faits v1 : tâches du jour, météo + verdicts [[Météo]], comptes à rebours —
   pas d'assignations par personne (choix utilisateur 2026-08-24).
   Clé API : `Humeur:CleApi`, `ANTHROPIC_API_KEY` (config à plat dans
   `appsettings.local.json`, gitignoré, ou env).

## Coût (as of 2026-08, prix API Anthropic)

~700 tokens entrée + ~60 sortie par appel. À 2 appels/jour : Haiku 4.5 (« claude-haiku-4-5 »,
1 $/5 $ par Mtok) ≈ **0,75 $/an** ; même Opus 5 ≈ 3,70 $/an. Le coût ne devient un
sujet que si un LLM entre dans un chemin interactif (ex. parsing en langage naturel
du quick-add — candidat futur, lui aussi peu coûteux à ~1 court appel par création).

## Hors périmètre

- LLM dans le chemin de requête (jamais).
- Notifications basées sur la phrase.

## Décisions

- [[D-2026-08-24 Phrase Du Jour Haiku Matin Et Soir]] — Haiku 4.5, deux
  générations fixes par jour, repli en cascade LLM → banque serveur → banque
  client.
- [[D-2026-08-23 Pas De N8n Dans Le Cœur]] — le polissage est un
  `BackgroundService` C#, jamais dans le chemin de requête.

## Ancres de code

- `server/HouseOs.Api/Domaine/Humeur/` — état structuré (`EtatMaison`), banque
  de gabarits, entité `PhraseDuJour`.
- `server/HouseOs.Api/Features/Humeur/` — construction de l'état, polissage
  Haiku (SDK C# officiel), worker deux-créneaux, `GET /api/phrase-du-jour`.
- `server/HouseOs.Tests/Domaine/BanquePhrasesTests.cs` et
  `server/HouseOs.Tests/Features/Humeur/PolissageLlmTests.cs` — tests.
- `web/src/lib/humeur.ts` — banque client (ex-intérim V0, désormais repli
  ultime) ; `web/src/pages/Aujourdhui.tsx` — héros branché sur la phrase
  serveur.

## Sources

- Maquettes `design/maquettes/Main.dc.html` (bannière-héros).

## Historique

- [[Plan 2026-08-24 Titre D'humeur Serveur]] · [[Recap Titre D'humeur]]
