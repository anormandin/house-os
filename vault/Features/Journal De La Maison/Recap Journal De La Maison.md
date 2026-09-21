---
type: recap
date: 2026-09-21
feature: "[[Journal De La Maison]]"
plan: "[[Plan 2026-09-20 Journal Éditorial]]"
---

# Recap Journal De La Maison

Bâti du 2026-09-20 au 2026-09-21, en neuf étapes livrées une à une en prod par
remplacement direct (aucun `?vue=`, aucune seconde route) : la broadsheet à rangs et
son bloc-titre, les widgets du [[Fonds De Tiroir]], l'édition du jour écrite par Opus,
le sommaire des journées chargées, puis la calibration au mur. La prod tourne
l'étape 8 (`86faebd`) ; l'étape 9 n'a touché aucun code.

## Ce qui a dévié du plan, et pourquoi

- **Le numéro d'édition compte les jours du journal de complétion**, pas les éditions
  matérialisées : le mur serait reparti à « N° 1 » le jour du release (étape 7).
- **La réédition par plancher se fait en deux temps, avec un second essai** —
  [[D-2026-09-21 Réédition En Deux Temps]], acceptée à l'étape 8 : un gabarit laissé
  par un modèle surchargé est réessayé une fois, une heure plus tard, jamais plus.
- **Le service vise le jour civil**, pas « la veille avant l'heure du matin » : le
  signal d'un rendu de nuit tombait dans le vide jusqu'à 5 h 31.
- **Les colonnes du sommaire se remplissent à la main**, pas en `column-count` : en
  colonnes CSS la septième rangée partait dans une quatrième colonne cachée à droite,
  et la garde ne voyait rien. « + N autres » est une rangée de la liste.
- **Le modèle nomme les rubriques dès dix tâches, plancher ou pas** (revue de code,
  étape 8) : la vraie journée du 20 octobre aura sûrement un retard, donc un rang
  « événement ».
- **Le budget de sept widgets du rang 0 n'est jamais atteignable** : une colonne
  d'aparté tient deux widgets empilés, `capaciteWidgets` borne à cinq (étape 4).
- **La chronique prend une colonne**, pas la pleine largeur des maquettes (étape 7).
- **Le mur se lit à deux distances** (étape 9) : le nom, la manchette, la bande et les
  chiffres de 2–3 m ; la liste et la chronique à ~1 m. Assumé, pas corrigé.

## Après le plan

- **La matière est conservée sur l'édition** (2026-09-21, après l'étape 9) et un
  atelier hors image (`server/HouseOs.Essais`) la rejoue contre un fichier de prompt —
  [[D-2026-09-21 Matière Conservée Sur L'Édition]]. C'est l'outillage de la première
  ronde de réglage du prompt, pas encore faite ; « L'atelier du prompt » dans la spec
  dit le geste.

## À observer

- **Pile** : 3,79 V (2026-09-20 matin) → 3,77 V / 52 % (20, 22 h 08) → 3,75 V / 50 %
  (21, 10 h 17). Prochain relevé utile vers le **2026-10-04** ; si la pente des deux
  premiers jours se confirme, c'est la densité qui coûte, et [[Affichage E-ink]]
  (« Cadence et pile ») devra le dire.
- **La première vraie journée à dix tâches et plus** : le **2026-10-20** (14 tâches).
  Le rendu d'essai l'a montrée, le mur pas encore.
- La cadence de nuit reste la case ouverte de [[Affichage E-ink]], hors de ce plan.
- **Pistes pour la ronde de réglage du prompt**, relevées à la lecture du prompt le
  2026-09-21, à vérifier sur des matières rejouées avant d'y toucher : la règle
  anti-radotage demande de varier l'ouverture des paragraphes, mais `precedentes` ne
  porte que surtitre, manchette et chapeau (le modèle ne voit jamais un paragraphe
  passé) ; le bloc des rubriques pèse un tiers du prompt pour les seules journées
  chargées ; aucun exemple d'édition réussie n'est donné ; l'appel ne fixe ni
  température ni réflexion.
