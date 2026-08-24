---
type: plan
status: executed
date: 2026-08-23
feature: "[[Comptes À Rebours]]"
---

# Plan 2026-08-23 Comptes À Rebours V1

Remplacer la carte intérimaire codée en dur par une vraie entité CRUD, selon les
quatre décisions liées dans [[Comptes À Rebours]].

## Backend

- [x] Entité `CompteARebours` (Id, Titre ≤200, DateCible DateOnly, Icone enum
      `IconeCompteARebours` {Camion, Sapin, Avion, Valise, Gateau, Cadeau, Coeur,
      Soleil} stocké texte) dans `Domaine/CompteARebours.cs` ; DbSet + config dans
      `Infrastructure/HouseOsDbContext.cs`.
- [x] Migration EF `ComptesARebours` avec `InsertData` des deux comptes de départ :
      Déménagement (2026-10-06, Camion), Noël (2026-12-25, Sapin) — amorçage
      une-seule-fois, une suppression ne revient jamais.
- [x] Tranche `Features/ComptesARebours/ComptesAReboursEndpoints.cs` : GET (tous,
      triés DateCible), POST, PUT, DELETE, sur le modèle de
      `Features/Zones/ZonesEndpoints.cs` ; validation titre requis, date requise,
      icône connue. `MapComptesARebours()` dans `Program.cs`.
- [x] Flux iCal : évènements toute-la-journée UID `compte-{id}@houseos` pour
      DateCible ≥ aujourd'hui, dans `Features/FluxIcal/IcalEndpoints.cs`.

## Frontend

- [x] `web/src/lib/api.ts` : type `CompteARebours` + appels
      comptesARebours/creer/modifier/supprimer.
- [x] `web/src/components/Illustrations.tsx` : 6 nouvelles icônes au trait (Avion,
      Valise, Gateau, Cadeau, Coeur, Soleil), style existant (trait 2.5, couleurs
      var(--…)), chaque icône portant sa couleur.
- [x] `web/src/pages/Aujourdhui.tsx` : carte branchée sur l'API (useQuery),
      filtre dodos ≥ 0, « C'est aujourd'hui ! » à dodos = 0, état vide, bouton « + »
      dans l'en-tête ; suppression des constantes codées en dur (la date du
      déménagement reste pour la phrase d'humeur, déplacée vers
      `web/src/lib/humeur.ts` si elle vit dans la page).
- [x] Nouveau composant `web/src/components/ComptesAReboursGestion.tsx` : modal de
      gestion (formulaire titre/date/sélecteur d'icônes + liste complète avec
      passés marqués, modifier/supprimer).

## Vérification

- [x] `dotnet build` + `dotnet test` (server/).
- [x] `npm run build` (web/) — tsc + vite.
- [x] Passe manuelle : créer, modifier, supprimer, jour J simulé, flux iCal contient
      le compte.
- [x] Correctif de portée : la passe manuelle a couvert l'API (CRUD, validations,
      flux iCal vérifié dans le .ics) et le rendu isolé des 8 icônes ; la passe
      visuelle dans l'app (carte, modal, jour J) requiert le login — l'agent ne
      saisit pas de mot de passe dans le navigateur.
- [ ] Passe visuelle dans l'app par Alain (carte, modal de gestion, jour J avec un
      compte daté d'aujourd'hui).

## Clôture

- [x] Spec [[Comptes À Rebours]] à `implemented`, ancres vérifiées, Recap écrit,
      plan à `executed`, validation du vault.
