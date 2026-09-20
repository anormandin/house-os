---
type: recap
date: 2026-09-20
feature: "[[Vue Téléphone]]"
plan:
---

# Recap Vue Téléphone

Livrée en une session le 2026-09-20 (commit `a77b04a`, 21 fichiers, +3424 lignes) :
`useFormatPhone()` (< 900 px) comme point de bascule unique, `CoquilleTelephone`,
`FeuilleActions`, et six écrans sous `web/src/pages/telephone/`. Le bureau ne bouge
pas — `DialogueCalendrier` a été extrait de `Layout.tsx` pour servir les deux
coquilles, sans changement de rendu côté bureau.

**Écart notable** : [[D-2026-09-19 Interface Téléphone Distincte]] levait
explicitement l'interdiction de la barre d'onglets du bas ; la direction retenue
(« La pile ») n'en met pas. Le menu déroulant a été préféré parce qu'il peut porter
la pastille « en retard », ce qu'une barre d'onglets ne peut pas faire — c'est le
sujet du seul test de la coquille. La permission reste valide, simplement non exercée.

Deux conséquences de la décision restaient « à arbitrer » et l'ont été à
l'exécution : le seuil (900 px, pour prendre la tablette en portrait sans prendre
l'iPad en paysage) et le mécanisme (`matchMedia` dans un hook, pas des breakpoints
Tailwind — les écrans qui ne se replient pas demandaient un autre arbre, pas une
autre largeur).

Corrections attrapées au passage, hors périmètre téléphone : plancher de 10 px sur
les initiales d'avatar (elles tombaient à 7-8 px dans les listes du bureau) et le
sélecteur e2e « Nouvelle », devenu ambigu — **les deux parcours e2e échouaient déjà
avant ce lot**, le sélecteur a été rendu exact.

Vérifié : `tsc` propre, 189 tests unitaires, 3 parcours e2e, build, et une passe
Playwright en émulation iPhone 15 — aucun débordement horizontal sur les six écrans,
aucune cible sous 44 pt, aucun champ sous 16 px.

**Écart au contrat du vault** : aucune note de plan n'a été écrite avant l'exécution,
et la spec ainsi que ce Recap ont été écrits le lendemain, après constat de la dérive.
Le champ `plan:` reste vide à dessein — inventer un plan a posteriori vaudrait moins
que dire qu'il n'y en a pas eu.

**Reste ouvert** : la direction « La pile » a été choisie parmi plusieurs maquettes
du 2026-09-19 sans qu'un fichier de décision soit minté ; si les alternatives valent
d'être consignées, une décision dédiée est due.
