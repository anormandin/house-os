---
type: recap
date: 2026-09-21
feature: "[[Lettre Du Matin]]"
plan: "[[Plan 2026-09-21 Lettre Du Matin]]"
---

# Recap Lettre Du Matin

Bâti en une session le 2026-09-21, six étapes sur sept ; la septième (le `.env` de
prod et le release) attend les réglages SMTP. 998 tests serveur et 239 tests web verts.

**Ce qui a été bâti comme prévu** : SMTP par MailKit derrière `IEnvoyeurDeCourriel`,
l'adresse sur l'utilisateur, l'entité `LettreDuMatin` à une ligne par date, une plume à
part sur la matière étendue, le service de fond avec le second essai à une heure et la
note C3, les trois endpoints, les deux outils MCP, la page `/lettre`, l'atelier.

**Ce qui a dévié, et pourquoi** :

- La porte SMTP s'ouvre avec l'hôte et l'expéditeur seulement ; l'usager est facultatif
  (un relais maison sans authentification est un cas réel).
- Un modèle muet à l'heure laisse quand même une ligne (note + matière, source
  Gabarit, pas envoyée) : c'est son heure de composition qui borne le second essai,
  comme au mur, et la ligne dit « composée, pas partie » après un redémarrage.
- Le `GET` sans lettre rend un aperçu **en note sans modèle** ; le modèle ne s'appelle
  en requête que sur « Réécrire », comme le tirage du mur.
- Le contrat tolère **360** signes par paragraphe (le prompt en dit 320, en vise 250) :
  Opus a atterri à 338 puis 328 sur le paragraphe des faits aux rejeux. Le mur a une
  colonne, le courriel n'en a pas.
- Npgsql refuse tout `DateTimeOffset` non UTC : tous les instants de la lettre sont
  normalisés, comme `GenereLe` sur l'édition. Trouvé au test d'intégration.

**Observé aux rejeux** : la base de dev n'a ni journal récent ni échéance dans la
semaine devant ; « faites », « semaine devant » et « série » n'ont donc pas encore été
lus par le modèle sur une vraie journée. À relire après une semaine de lettres.
