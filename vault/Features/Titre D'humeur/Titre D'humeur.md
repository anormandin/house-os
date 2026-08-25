---
type: feature
status: implemented
last-verified: 2026-08-25
verified-against: eb830ec
tags: []
---

# Titre D'humeur

## Intention

Le geste signature de la direction chaleureuse : la bannière-héros d'Aujourd'hui
(« On y est presque. » / « 4 petites choses avant dodo — le camion attend depuis
hier, le reste est sous contrôle. »). Doit rester charmante sans devenir répétitive,
sans jamais culpabiliser, et sans dépendre d'un service externe pour s'afficher.

## Comportement (trois couches, serveur depuis 2026-08-24)

1. **Faits calculés (C#, déterministe)** : compte du jour, retards, complétions,
   **titres des tâches du jour et des prochaines de la semaine** (avec horizon en
   jours), jalons de comptes à rebours, et la météo **seulement quand elle sort de
   l'ordinaire** (signal `MeteoRemarquable` de [[Météo]] —
   [[D-2026-08-25 Phrase Du Jour Axée Tâches]]). Tout ce qui est numérique ou
   factuel vient de cette couche — jamais du LLM (pas de chiffres hallucinés).
   Cette même structure nourrira la vue e-ink.
2. **Banque de gabarits (plancher)** : une douzaine de titres en trois familles
   (calme / actif / compte à rebours ≤ 60 dodos) et 1-2 variantes de sous-titre
   par état de maison (`ToutFait`, `RienAuProgramme`, `Retard`, `JourneeChargee`,
   `Calme` — enum `EtatMaison`), rotation ensemencée par la date. Variante
   « Prochaine affaire : … » sur les journées libres ; la touche météo est
   retirée sur `Retard`/`JourneeChargee` même quand le signal remarquable est
   présent. Zéro coût, testable, sert de repli permanent.
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
   Le parse de la réponse LLM est défensif (durci 2026-08-25) : prose autour du
   JSON, champs non textuels, réponse tronquée → repli, jamais d'exception. Le
   signal météo est celui de la **date de la phrase** (au rattrapage de 3 h du
   matin, le soir d'hier n'annonce pas les orages d'aujourd'hui) ; le réveil des
   créneaux utilise l'offset de la date cible (changement d'heure) et un délai
   plancher (une config farfelue ne tue pas le service).
   Contenu priorisé (2026-08-25) : tâches nommées d'abord (motivation, ce qui
   s'en vient), comptes à rebours ensuite, météo uniquement sur signal
   remarquable et en passant — pas d'assignations par personne (choix
   utilisateur 2026-08-24).
   Clé API : `Humeur:CleApi`, `ANTHROPIC_API_KEY` (config à plat dans
   `appsettings.local.json`, gitignoré, ou env).

## Coût (as of 2026-08, prix API Anthropic)

~700-1 000 tokens entrée (prompt few-shot allongé le 2026-08-25) + ~60 sortie par appel. À 2 appels/jour : Haiku 4.5 (« claude-haiku-4-5 »,
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
- [[D-2026-08-25 Phrase Du Jour Axée Tâches]] — tâches nommées au premier plan,
  météo sur exception seulement (signal remarquable).
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
- [[Plan 2026-08-25 Météo Du Moment Et Phrase Axée Tâches]]
