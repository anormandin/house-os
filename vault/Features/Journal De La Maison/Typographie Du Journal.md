---
type: reference
last-verified: 2026-09-30
verified-against: 6d1fda0
tags: []
---

# Typographie Du Journal

Sous-note de [[Journal De La Maison]] : les polices du mur et la calibration faite
devant l'écran. La forme de la page reste dans [[Journal De La Maison]] ; le matériel,
dans [[Affichage E-ink]].

## La typographie

Deux fontes, choisies sur la maquette `design/maquettes/eink-polices.html` (les dix
polices de journal d'Issuu, puis celles du New Yorker et du New York Times, avec un
seuillage 1 bit simulé). Le couple en place depuis le 2026-09-28 est « **Le grand
quotidien** », pris pour le plaisir de le voir au mur :

| Rôle | Fonte | D'où | Graisse |
|---|---|---|---|
| le nom du journal | **Playfair Display** (Claus Eggers Sørensen, OFL) | fontsource | 900, en capitales |
| le titrage (manchette, lettrine, encadré, bande) | **Playfair Display** | fontsource | 800 |
| le texte (dateline, liste, chronique, widgets, pied) | **Libre Franklin** (Impallari) — la reprise libre de Franklin Gothic | fontsource | 600 ; les étiquettes 800 |

Le couple précédent (2026-09-21 → 2026-09-28) était celui du **Times** : Chomsky (la
gothique OFL de la manchette, 400, bas de casse) pour le nom, Newsreader pour le titrage,
Gelasio pour le texte. Pour y revenir, voir le commit `7fc7325`.

- **Pas de gras synthétique** (`font-synthesis: none` sur le cadre) : une fonte qui n'a
  pas la graisse demandée rend la plus proche qu'elle a, plutôt qu'un contour épaissi
  par le navigateur, qui bave au seuillage.
- Les fontes sont chargées par `web/src/pages/ecran-polices.css`, avec la page `/ecran`
  seulement : le bureau garde Fraunces et Nunito Sans et ne paie pas les deux autres.
- Ce qui a été écarté à la maquette, et pourquoi : Playfair et Caslon perdent leurs
  déliés au 1 bit (Playfair est pourtant au mur depuis le 2026-09-28, par goût : à
  surveiller au seuillage) ; Irvin (le New Yorker) est une fonte de titrage à une seule graisse,
  pas une fonte de texte ; Spectral plafonne à 800 et paraît maigre à côté du reste.

## Calibration au mur (2026-09-21)

Le reTerminal E1003 fait 209 × 157 mm pour 1872 × 1404 px (227 ppp, 0,112 mm par
pixel). À hauteur de capitale et au seuil de lecture de 5′ d'arc, chaque taille de la
page a sa distance :

| Élément | Taille | Capitale | Lisible jusqu'à |
|---|---|---|---|
| Le nom | 126 px | 9,6 mm | ~6,5 m |
| La manchette | 96–116 px | 7–8 mm | ~5 m |
| La bande du sommaire | 46–76 px | 3,5–5,8 mm | 2,4–3,9 m |
| Valeur de widget, rangée large | 44 px | 3,5 mm | ~2,4 m |
| Rangée serrée | 34 px | 2,7 mm | ~1,8 m |
| Chronique | 32 px | 2,5 mm | ~1,7 m |
| Dateline | 28 px | 2,2 mm | ~1,5 m |
| Oreilles, étiquettes, en-têtes de rubrique | 22–24 px | 1,7–1,9 mm | ~1,3 m |

Le mur se lit donc **à deux distances**, comme un quotidien : de la pièce (2–3 m), le
nom, la manchette, la bande et les chiffres des widgets ; en s'approchant (~1 m), la
liste, la chronique et la dateline. C'est la conséquence des 34 px mesurés aux
maquettes, assumée : « élaguer plutôt que rapetisser » vaut aussi dans l'autre sens —
on ne grossit pas la liste au prix de ses rangées.

Vérifié au rendu 1-bit par le chemin de l'appareil, sur quatre journées (chronique,
événement, sommaire, date longue) : aucune trame — la page n'utilise aucun gris —,
lettrine, filets, pastilles et points de suspension en noir plein ; la dateline
(1744 px) absorbe la date la plus large de l'année (387 px à 28 px) ; garde de
débordement à 0 sur les quatre. La pile se suit dans l'étape 9 du plan et dans le
Recap.

> [!note] Mesuré avec le couple du Times, pas refait depuis.
> Le passage à Playfair Display et Libre Franklin (2026-09-28) n'a changé aucune taille
> en pixels de `web/src/pages/Ecran.tsx` — seulement les graisses et les capitales du nom. Les
> hauteurs de capitale, les distances et la vérification 1-bit ci-dessus datent donc du
> 2026-09-21 et restent à refaire au mur avec les nouvelles fontes.
