---
type: decision
status: accepted
date: 2026-09-21
feature: "[[Lettre Du Matin]]"
tags: []
---

# D-2026-09-21 Lettre Matérialisée Et Rattrapée Le Jour Même

## Contexte

Un courriel envoyé deux fois est pire qu'un courriel manqué, et un courriel manqué sans
raison fait qu'on cesse de compter dessus. Le conteneur redémarre (release, backup) et
l'heure d'envoi peut tomber pendant un redémarrage. Il fallait fixer ce que le service
garde et ce qu'il rattrape.

## Options considérées

- **Une entité `Lettre` en base** (retenue) : date, sujet, texte, matière JSONB,
  source, horodatage d'envoi, destinataires. Le garde-fou contre le double envoi survit
  au redémarrage ; la matière se rejoue ; l'app relit la lettre.
- **Envoyer et oublier** : aucune table, Seq garde la trace ; mais le garde-fou vit en
  mémoire, un redémarrage après l'heure renvoie.

Sur l'heure : **réglage de la maison, rattrapage le jour même jusqu'à une limite**
(retenue) ; une heure par personne (peu de valeur tant que la lettre est au foyer) ;
pas de rattrapage (un redémarrage à 6 h 25 saute la lettre).

## Décision

**Une ligne `Lettres` par date**, écrite quand la lettre est composée et marquée
envoyée après l'envoi. **Heure de la maison** `Lettre:HeureEnvoi` (`LETTRE_HEURE`,
défaut 06:30). Au démarrage après l'heure, la lettre du jour part si elle n'est pas
partie, **jusqu'à midi** ; passé midi, la journée est perdue et n'est jamais rattrapée
le lendemain. **Une lettre tous les jours**, même quand la maison ne demande rien : le
[[Fonds De Tiroir]] garantit la matière.

## Conséquences

- Migration EF pour `Lettres` (une seule migration avec la colonne `Courriel` de
  [[D-2026-09-21 Adresse De Courriel Sur L'Utilisateur]] si le plan les livre ensemble).
- Le second essai d'une heure ([[D-2026-09-21 Lettre Écrite À Part Sur La Même Matière]])
  n'a lieu que s'il tombe avant midi ; sinon la note part tout de suite.
- Un envoi d'essai (« M'envoyer un essai ») ne compte pas comme l'envoi du jour : il
  n'écrit pas `EnvoyeeLe`.

## Confirmation

Index unique sur `Lettres.Date` dans le snapshot du modèle, et le test de
`LettreService` qui prouve « démarré à 9 h, pas encore partie : part ; démarré à
13 h : ne part pas, et n'est pas rattrapée le lendemain ».
