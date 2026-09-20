---
type: reference
last-verified: 2026-09-20
verified-against: 01f4ff1
tags: []
---

# Inspiration UI

Point d'entrée du dossier d'inspiration UI. Cibles de design (voir
[[D-2026-08-23 Interface Desktop Et Écran E-ink]]) : **desktop d'abord**, écran
e-ink mural comme seconde vue distincte ; téléphone non prioritaire — renversé
depuis : le téléphone a sa propre interface ([[Vue Téléphone]], 2026-09-20).

## Notes du dossier

- [[Directions Artistiques]] — les 3 directions candidates (Papier d'encre, Tableau
  de bord Kanagawa, Cuisine chaleureuse) et la recommandation hybride.
- [[Patterns Vue Aujourd'hui]] — mécaniques des meilleures vues « aujourd'hui »
  (Things 3, Fantastical, HA 2026).
- [[Affichage Mural Et E-ink]] — grammaire TRMNL, règles de lisibilité à distance,
  layouts muraux.
- [[Typographie Québécoise]] — normes OQLF (dates, heures, unités) et registre.
- [[Apps Similaires]] — patterns et anti-patterns des apps du domaine.

## Maquettes de composants

- `design/maquettes/toasts-directions.html` — cinq directions pour le toast (carte
  posée, liseré, barre, geste au premier plan, ancre), rendues vivantes : minuteurs,
  animations d'entrée, fusion. Retenue : la première
  ([[D-2026-08-28 Toast Carte Posée]]).
- `design/maquettes/eink-directions.html` — onze directions pour l'écran mural
  ([[Affichage E-ink]]), rendues à 1872 × 1404 en 1-bit avec les données réelles du
  2026-09-20 : l'écran actuel, sept directions jouables (journal, ruban des heures,
  colonne par personne, grand chiffre, plan de la maison, semaine devant, organes de
  la maison) et trois idées lâchées lousses (almanach, lettre de la maison, année
  tissée). Bouton « Test du recul » pour simuler le coup d'œil à 2–3 m.
  **Retenues (2026-09-20)** : la une, l'almanach, et la lettre — reprises et déclinées
  dans `eink-publications.html`.
- `design/maquettes/eink-publications.html` — deuxième tour sur les trois formes
  retenues, trois variantes chacune, avec les vraies données de prod du 2026-09-20.
  **A — la une** : broadsheet, manchette seule, cahier à six rubriques. **B —
  l'almanach** : page du jour, page du mois, roue de l'année (fenêtres saisonnières en
  arcs). **C — la lettre du matin**, qui devient un **courriel** et non un écran :
  lettre en prose, bulletin structuré, note d'aperçu de notification. Principe commun :
  l'écran n'est pas un miroir de l'app, il *édite*. **Retenues (2026-09-20)** : A1 (la
  broadsheet) comme mise en page, et les « widgets » de B1 (le ciel, l'équinoxe, le
  premier gel…) comme matière.
- `design/maquettes/une-editorialiste.html` — la broadsheet à **densité variable** :
  une seule grammaire CSS, six remplissages sur six vraies journées de la prod (0, 1,
  1, 5, 14 tâches, et le jour J). Contient le **fonds de tiroir** (≈ 25 widgets en
  5 familles — le ciel, le climat, la maison, le calendrier, le hasard — avec source et
  rareté) et la **règle de bascule** (budget de widgets par nombre de tâches, score
  rareté × fraîcheur × pertinence, et le plancher : retard grave, échéance ferme ou
  compte à rebours à zéro ne peuvent jamais être relégués). Mesure faite en montant les
  maquettes : trois colonnes tiennent ~9 items chacune, donc ~27 tâches avant
  saturation — la journée la plus chargée de la prod en compte 14. La contrainte réelle
  n'est pas la place mais la manchette, qui n'a plus de sens au-delà de ~6 items.
  Famille de widgets **« La ville »** ajoutée le 2026-09-20, avec faisabilité vérifiée
  au curl (section « ce que j'ai vérifié » en bas de page) : `villescjc.com/actualites`
  et `/evenements` sont rendus au serveur et grattables (cartes `c-publication-card` et
  `c-event-card`), sans RSS ni JSON-LD ni `robots.txt` — donc fragiles au gabarit ; le
  **calendrier des collectes** (`villescjc.com/storage/app/media/collectes/sainte-catherine-2026.pdf`,
  aussi téléchargeable une fois l'an) s'analyse avec `pdftotext -bbox-layout`, les codes
  `DS/DN OS/ON RS/RN ES/EN FS/FN` s'alignant sur les colonnes de jours. **Le PDF porte
  l'index des rues par secteur : « de la Colline » est au SECTEUR SUD DE LA RIVIÈRE,
  dont les collectes tombent le jeudi** — le secteur se lit dans la source, il n'a pas
  à être configuré.

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
