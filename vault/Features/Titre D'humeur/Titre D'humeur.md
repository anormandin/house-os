---
type: feature
status: draft
last-verified: 2026-08-23
verified-against: 5f9f080
tags: []
---

# Titre D'humeur

## Intention

Le geste signature de la direction chaleureuse : la bannière-héros d'Aujourd'hui
(« On y est presque. » / « 4 petites choses avant dodo — le camion attend depuis
hier, le reste est sous contrôle. »). Doit rester charmante sans devenir répétitive,
sans jamais culpabiliser, et sans dépendre d'un service externe pour s'afficher.

## Comportement (design en trois couches, 2026-08-23)

1. **Faits calculés (C#, déterministe)** : compte du jour, retards, items du soir,
   météo, jalons de comptes à rebours, complétions. Tout ce qui est numérique ou
   factuel vient de cette couche — jamais du LLM (pas de chiffres hallucinés).
   Cette même structure nourrira la vue e-ink.
2. **Banque de gabarits (plancher)** : ~20-30 phrases françaises par état de maison
   (tout-est-fait, retard, soirée chargée, veille de déménagement…), rotation
   ensemencée par la date. Zéro coût, testable, sert de repli permanent.
3. **Polissage LLM (optionnel, jamais dans le chemin de requête)** : un
   `BackgroundService` (cohérent avec [[D-2026-08-23 Pas De N8n Dans Le Cœur]])
   appelle l'API Claude 1-2×/jour (matin + changement d'état significatif) avec
   l'état structuré + règles de style (registre québécois, « dodos », jamais de
   culpabilisation — principe anti-harcèlement de [[Inspiration UI]], longueurs
   max), stocke titre + sous-titre dans une table `PhraseDuJour`. L'UI lit la
   table ; API absente → couche 2.

## Coût (as of 2026-08, prix API Anthropic)

~700 tokens entrée + ~60 sortie par appel. À 2 appels/jour : Haiku 4.5 (« claude-haiku-4-5 »,
1 $/5 $ par Mtok) ≈ **0,75 $/an** ; même Opus 5 ≈ 3,70 $/an. Le coût ne devient un
sujet que si un LLM entre dans un chemin interactif (ex. parsing en langage naturel
du quick-add — candidat futur, lui aussi peu coûteux à ~1 court appel par création).

## Hors périmètre

- LLM dans le chemin de requête (jamais).
- Notifications basées sur la phrase.

## Décisions

- (À mint au moment de l'implémentation : choix du modèle et de la cadence.)

## Ancres de code

<!-- À remplir à l'implémentation (phase 2, avec la météo — les phrases
météo-conscientes sont ce qui fera briller la feature). -->

## Sources

- Maquettes `design/maquettes/Main.dc.html` (bannière-héros).

## Historique

<!-- Plan et recap à venir. -->
