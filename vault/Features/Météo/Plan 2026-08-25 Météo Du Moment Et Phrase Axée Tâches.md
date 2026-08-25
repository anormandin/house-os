---
type: plan
status: executed
date: 2026-08-25
feature: "[[Météo]]"
tags: []
---

# Plan 2026-08-25 Météo Du Moment Et Phrase Axée Tâches

Corrige l'incohérence observée en prod (bruine dehors, héros vantant « la belle
météo ») et réoriente la phrase du jour vers les tâches et la motivation. Gouverné
par [[D-2026-08-25 Code Météo Horaire Et Conditions Du Moment]] et
[[D-2026-08-25 Phrase Du Jour Axée Tâches]]. Touche aussi [[Titre D'humeur]].

## Étapes

### Météo du moment

- [x] `PrevisionHoraire` : colonne `CodeMeteo` + prédicat précipitation (code ≥ 51) ;
      migration EF `AjouterCodeMeteoHoraire`.
- [x] Ingestion : `weather_code` dans la série `hourly` ; normalisation + fixture de
      test mises à jour.
- [x] Règles : « Être dehors », « Tondre », « Aérer » traitent tout code ≥ 51 comme
      précipitation même à 0 mm ; tests bruine ajoutés.
- [x] `GET /api/meteo` : objet `maintenant` (température, code) depuis la ligne
      horaire courante ; `MeteoCarte.tsx` affiche le moment en grand (repli : jour).

### Phrase axée tâches

- [x] Évaluateur `MeteoRemarquable` (description française + polarité, seuils en
      constantes) + tests.
- [x] `EtatMaison` : retirer `MeteoDuJour`, ajouter `TachesDuJour` (≤ 5 titres),
      `ProchainesTaches` (≤ 4, horizon 7 jours), `MeteoRemarquable?` ;
      `ConstruireEtat` alimente le tout.
- [x] `PolissageLlm` : nouvelle sérialisation + prompt réécrit (motivant, tâches
      d'abord, météo interdite sauf signal remarquable) ; tests de contrat.
- [x] `BanquePhrases` : touche météo conditionnée au remarquable ; variante
      « prochaine tâche » sur les journées calmes ; tests mis à jour.

### Clôture

- [x] `dotnet test` vert ; `tsc` vert côté web.
- [x] Vault : specs [[Météo]] et [[Titre D'humeur]] à jour, recaps, validation.
- [x] Déploiement prod + suppression de la phrase du matin périmée pour forcer une
      régénération (demander avant). *(Fait au /vault sync du 2026-08-25 : prod à
      `6a0c130`, rangée Matin supprimée + conteneur redémarré, phrase régénérée.)*

## Vérification

- Tests unitaires règles + remarquable + banque + sérialisation.
- En dev : carte « Dehors » montre les conditions de l'heure ; phrase régénérée ne
  mentionne pas la météo un jour ordinaire.
