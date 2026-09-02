---
type: decision
status: accepted
date: 2026-09-02
feature: "[[Courriel Entrant]]"
tags: []
---

# Enrichissement LLM À L'ingestion

## Contexte

Un reçu transféré par courriel arrive avec un nom de fichier du genre
`Invoice_48211.pdf` et un sujet marketing. Sans enrichissement, la boîte « À classer »
([[D-2026-09-02 Boîte À Classer Des Documents]]) demande de tout retaper. La clé
Anthropic est déjà configurée pour [[Titre D'humeur]]. Faut-il un enrichissement LLM
dès la v1, et sous quelles contraintes ?

## Options considérées

- **V1 déterministe, LLM plus tard** — date du courriel, expéditeur et sujet dans les
  notes, catégorie par mots-clés ; moins de code, mais le triage reste laborieux.
- **LLM dès la v1, vocabulaire ouvert** — le modèle invente dossiers et catégories ;
  explosion de taxonomie (leçon paperless-ai, [[Banque D'idées]]).
- **LLM dès la v1, vocabulaire fermé + repli déterministe** — le modèle propose
  titre, catégorie, dossier, équipement lié, date, montant et résumé, mais seules les
  valeurs existantes sont acceptées ; tout échec retombe sur le repli.

## Décision

**LLM dès la v1, vocabulaire fermé, repli déterministe** (choix d'Alain, 2026-09-02).
Un seul appel Haiku par courriel (pas par pièce jointe), corps tronqué à ~6000
caractères, 30 s max, hors chemin de requête ; catégorie validée contre l'enum,
dossier seulement s'il existe déjà, équipement seulement s'il existe ; les notes
reçoivent le résumé, le montant et une ligne de provenance
« Reçu par courriel de X le AAAA-MM-JJ — sujet ». Clé et modèle partagés avec
`HumeurOptions`. Sans clé ou en cas d'erreur : titre = nom de pièce / sujet,
catégorie par mots-clés, date = date du courriel.

## Conséquences

- Un coût marginal par courriel (Haiku) ; aucun appel en test (les fonctions
  `Extraire`/`Appliquer`/`Repli` sont testées sans réseau).
- Le LLM ne crée jamais de dossier ni de catégorie : la taxonomie reste celle des
  humains.
- Même patron que `PolissageLlm` : parse JSON défensif, tout écart → repli.

## Confirmation

- `grep -n "Repli" server/HouseOs.Api/Features/Courriel/EnrichissementCourriel.cs`
  retourne la fonction de repli.
- `server/HouseOs.Tests/Features/Courriel/EnrichissementCourrielTests.cs` couvre
  « dossier hors liste → null » et « catégorie inconnue → repli ».
