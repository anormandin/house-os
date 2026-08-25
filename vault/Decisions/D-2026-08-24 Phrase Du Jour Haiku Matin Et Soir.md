---
type: decision
status: accepted
date: 2026-08-24
feature: "[[Titre D'humeur]]"
tags: []
---

# Phrase Du Jour Haiku Matin Et Soir

## Contexte

La version serveur de [[Titre D'humeur]] (design en trois couches, spec 2026-08-23)
laissait deux choix ouverts : le modèle Claude du polissage et la cadence de
régénération. Le principe « LLM jamais dans le chemin de requête » était déjà fixé.

## Options considérées

- **Modèle** : Haiku 4.5 (≈ 0,75 $/an à 2 appels/jour) · Sonnet 5 (quelques $/an) ·
  Opus 5 (≈ 4 $/an).
- **Cadence** : matin seulement · **matin + soir fixes** · vérification horaire avec
  régénération sur changement d'état (plus réactif mais plus de mécanique).

## Décision

**Haiku 4.5** (`claude-haiku-4-5`) et **deux générations fixes par jour** — matin
(5 h 30) et soir (17 h), heures locales, configurables. Une ligne `PhraseDuJour`
par (date, moment), upsertée par un `BackgroundService` ; l'UI lit la table via
`GET /api/phrase-du-jour`. Clé API via configuration `Humeur:CleApi` ou env
`ANTHROPIC_API_KEY` ; clé absente ou appel raté → banque de gabarits C# (couche 2),
et le client web garde `humeur.ts` en repli ultime si l'API HTTP échoue.
Choix de l'utilisateur (2026-08-24).

## Conséquences

- Coût plafonné (~2 appels Haiku/jour, ~700 tokens entrée / ~60 sortie).
- La phrase du soir reflète la journée (tout-est-fait, retard apparu) sans
  détection de changement d'état à maintenir.
- Trois niveaux de repli : LLM → banque serveur → banque client ; le héros
  d'Aujourd'hui ne peut jamais être vide.

## Confirmation

- `grep -r "claude-haiku-4-5" server/HouseOs.Api` touche uniquement
  `Features/Humeur/` et la config (`appsettings*.json`, valeur par défaut de
  `HumeurOptions`).
- `grep -r "AnthropicClient" server/HouseOs.Api` n'apparaît jamais dans un
  endpoint — seulement dans le worker de la tranche Humeur.
