---
type: index
---

# House OS — Vault

Source de vérité de House OS : specs de features, registres de décisions, notes de
référence et de domaine. Quand ce vault et le code se contredisent, l'un des deux a
tort — corriger dans la même session. Maintenu sous le contrat du skill global `vault`.

## À lire, toujours

- Ce fichier, en premier, dans toute session de design/planification/changement de contrat.
- [[Conventions]] — langue, nommage, tags, schémas du projet.

## À lire selon le contexte

| Si tu… | Lis |
|---|---|
| Touches une feature existante | `Features/<Feature>/<Feature>.md` et ses décisions liées |
| Démarres une nouvelle feature | Lance `/vault feature <nom>` |
| T'apprêtes à un choix T3 (schéma, architecture, sécurité, convention) | Le dossier `Decisions/` pour les précédents |
| Travailles sur la logique ou le vocabulaire du domaine | [[Glossaire]] |
| As besoin du portrait d'ensemble | [[Architecture]] |

## Échelle de cérémonie (ce qu'une tâche doit au vault)

- **T1 — trivial** (typo, renommage, refactor sans changement de comportement) : rien.
- **T2 — changement de contrat** (comportement, API, modèle de données, flux UX) :
  mettre à jour les notes touchées + Recap dans la même session ; décision seulement
  si un vrai choix entre alternatives a été fait.
- **T3 — coûteux à renverser** (schéma, architecture, sécurité, convention
  transversale, contrat externe) : fichier de décision obligatoire + spec + note de plan.

## Contexte projet (as of 2026-08)

Déménagement le 2026-10-06 à Sainte-Catherine-de-la-Jacques-Cartier. V0
« Déménagement » visée mi-septembre (tâches ponctuelles), V1 autour de l'emménagement
(récurrence + équipements). Jamais d'enfants — aucune feature famille/points, jamais.
Recherche initiale : `docs/research/` (3 rapports, 2026-08-23).

## Map of content

### Décisions
![[Decisions.base]]

### Features
![[Features.base]]

### Référence
- [[Architecture]] — stack, structure du monorepo, feuille de route par phases.

### Inspiration UI
- [[Inspiration UI]] — index du dossier : directions artistiques, patterns, e-ink,
  typographie québécoise. Maquettes : `design/maquettes/` (canvas Artifact publié).

### Domaine
- [[Glossaire]] — vocabulaire du domaine (Tâche, Occurrence, Zone, Équipement…).
