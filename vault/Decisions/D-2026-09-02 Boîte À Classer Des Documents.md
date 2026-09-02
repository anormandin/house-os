---
type: decision
status: accepted
date: 2026-09-02
feature: "[[Documents]]"
tags: []
---

# Boîte À Classer Des Documents

## Contexte

Les documents qui arrivent par [[Courriel Entrant]] n'ont pas été créés par un humain
devant un formulaire : titre, catégorie, dossier et liens sont devinés (par le LLM ou
par repli). Faut-il les ranger directement dans le classeur, ou les faire passer par
une étape de vérification ? Et que devient un courriel dont le contenu est dans le
corps HTML, sans pièce jointe (reçus IKEA, Amazon…) ?

## Options considérées

- **Direct dans le classeur** avec les valeurs devinées — zéro friction, mais les
  erreurs de classement s'accumulent en silence.
- **Statut de document** (enum Nouveau/Classé/Archivé) — plus lourd qu'un besoin
  binaire ; ouvre la porte à un workflow que personne n'a demandé.
- **Drapeau `AClasser`** + bloc « À classer » en tête de la page Documents, bouton
  « Classer » dans le tiroir — le patron de l'inbox de rapprochement de [[Budget]].
- Pour les courriels sans pièce : **ignorer**, **convertir le corps en PDF**
  (Chromium dans l'image, +300 Mo) ou **archiver le `.eml` complet** avec un aperçu
  texte dans le tiroir.

## Décision

**Drapeau `AClasser`** sur `Document` (faux par défaut ; vrai pour tout document
importé), bloc « À classer (n) » au-dessus d'« Échéances proches », bouton primaire
« Classer » dans le tiroir (enregistre la fiche corrigée et sort de la boîte), parité
MCP (filtre `aClasser`, action `classer`). **Sans pièce jointe permise, le courriel
entier est archivé en `.eml`** (nouveau type `message/rfc822`, accepté par
l'ingestion seulement — pas par le téléversement web) ; le tiroir montre De / Date /
Sujet / texte extrait à la volée. Choix d'Alain (2026-09-02).

## Conséquences

- Une colonne `AClasser` et une FK `ImportCourrielId` (SET NULL) sur `Documents` ;
  migration `AjouterCourrielEntrant`.
- Le `.eml` se télécharge et s'ouvre dans Mail ; l'aperçu sert du texte brut, jamais
  du HTML (aucun script ni image distante).
- Un document reçu par courriel reste dans « À classer » tant qu'on ne le classe pas :
  la boîte est aussi le rappel qu'un courriel est entré.

## Confirmation

- `grep -n "AClasser" server/HouseOs.Api/Domaine/Document.cs` retourne la propriété.
- `grep -n "message/rfc822" server/HouseOs.Api/Features/Documents/EnregistrementDocument.cs`
  retourne le type dans la liste d'ingestion et pas dans `TypesMimePermis`.
- `web/src/pages/Documents.tsx` contient le libellé « À classer » et le bouton « Classer ».
