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
- `server/HouseOs.Api/Features/Humeur/HumeurOptions.cs` porte les deux créneaux par
  défaut : `HeureMatin` 5 h 30 et `HeureSoir` 17 h.

> [!note] Mise à jour de la seule Confirmation (2026-09-30)
> La clause « `AnthropicClient` n'apparaît jamais dans un endpoint — seulement dans le
> worker de la tranche Humeur » est retirée. Elle gardait un principe (« LLM jamais
> dans le chemin de requête ») que le Contexte dit « déjà fixé » : il venait du premier
> jet de la spec (2026-08-23) et n'a jamais été une décision du foyer — Alain,
> 2026-09-30 : il n'a jamais voulu interdire les LLM. Le client sert depuis à
> l'éditorialiste, à la lettre, à l'enrichissement des courriels et aux régénérations à
> la demande. Ce que la décision tranche — Haiku, deux créneaux fixes — se vérifie
> ci-dessus. Le corps de la décision n'a pas été touché — voir [[Titre D'humeur]].
