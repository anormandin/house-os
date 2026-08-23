---
type: reference
last-verified: 2026-08-23
verified-against: 5f9f080
tags: []
---

# Inspiration UI

Point d'entrée du dossier d'inspiration UI. Cibles de design (voir
[[D-2026-08-23 Interface Desktop Et Écran E-ink]]) : **desktop d'abord**, écran
e-ink mural comme seconde vue distincte ; téléphone non prioritaire.

## Notes du dossier

- [[Directions Artistiques]] — les 3 directions candidates (Papier d'encre, Tableau
  de bord Kanagawa, Cuisine chaleureuse) et la recommandation hybride.
- [[Patterns Vue Aujourd'hui]] — mécaniques des meilleures vues « aujourd'hui »
  (Things 3, Fantastical, HA 2026).
- [[Affichage Mural Et E-ink]] — grammaire TRMNL, règles de lisibilité à distance,
  layouts muraux.
- [[Typographie Québécoise]] — normes OQLF (dates, heures, unités) et registre.
- [[Apps Similaires]] — patterns et anti-patterns des apps du domaine.

## Principes retenus (synthèse, as of 2026-08)

1. **Statut avant interaction** (calm tech) : l'écran par défaut répond « est-ce que
   tout est beau chez nous ? » sans un seul clic.
2. **Résumé d'abord, détail à la demande** (HA 2026.1) : une rangée-résumé en haut,
   les sections ensuite.
3. **Divulgation progressive** (Things 3) : une tâche = case + titre ; les métadonnées
   n'apparaissent qu'à l'ouverture.
4. **Hiérarchie par graisse, pas par échelle de tailles** ; une seule famille de
   caractères.
5. **Pas de théâtre d'urgence** : le retard est une date rouge discrète, jamais un
   écran qui crie. Pour l'entretien récurrent : indicateurs de fraîcheur (à la Tody)
   plutôt qu'alarme de retard — évite la dynamique « conjoint qui harcèle ».
6. **Le test du widget** : chaque panneau doit pouvoir tenir dans un widget iOS
   moyen ; sinon il est trop dense.
7. **Une grande Valeur + une petite Étiquette par panneau** (TRMNL) : garde le modèle
   de données rendable tel quel sur l'e-ink.

Rapports complets : `docs/research/2026-08-23-ui-dashboards-directions.md` et
`docs/research/2026-08-23-ui-apps-similaires.md`.
