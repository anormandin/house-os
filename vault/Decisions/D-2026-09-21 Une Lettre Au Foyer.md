---
type: decision
status: accepted
date: 2026-09-21
feature: "[[Lettre Du Matin]]"
tags: []
---

# D-2026-09-21 Une Lettre Au Foyer

## Contexte

La maquette C1 ([[Éditorialiste De L'Écran]]) s'adresse à « vous deux ». Mais chacun a
ses tâches, et la maquette laissait en placeholder une « heure d'envoi par personne ».
Il fallait trancher si la lettre est un objet du foyer ou de la personne.

## Options considérées

- **Une lettre au foyer, aux deux** (retenue) : un seul appel au modèle, une seule
  matière, un seul texte envoyé à toutes les adresses. La lettre nomme qui fait quoi
  quand ça compte.
- **Une lettre par personne** : chacun reçoit sa journée, ses tâches d'abord. Deux
  appels, deux mémoires, deux prompts à régler, et deux lettres qui racontent la même
  maison différemment.

## Décision

**Une lettre par jour pour la maison**, envoyée à chaque utilisateur qui a une adresse.
C'est la maison qui écrit à ses habitants, pas un assistant à chacun.

## Conséquences

- Une entité `Lettre` par date, pas par destinataire ; les destinataires sont
  consignés sur la lettre.
- Une seule mémoire des sept derniers jours, un seul atelier de prompt.
- L'heure d'envoi est celle de la maison (`Lettre:HeureEnvoi`). Une heure par personne
  reste possible plus tard sans rien déplacer, puisque l'adresse est sur
  l'utilisateur ([[D-2026-09-21 Adresse De Courriel Sur L'Utilisateur]]).

## Confirmation

Une seule ligne `Lettres` par `Date` : index unique sur la colonne, et le test de
`GenerationLettre` qui prouve qu'un second passage le même jour n'écrit ni n'envoie.
