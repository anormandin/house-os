---
type: recap
date: 2026-09-21
feature: "[[Lettre Du Matin]]"
plan: "[[Plan 2026-09-21 Lettre Du Matin]]"
---

# Recap Lettre Du Matin

Bâti et mis en prod en une session le 2026-09-21, sept étapes. Transport : le compte
Resend déjà en place pour un autre projet du foyer (domaine `mail.alainnormandin.dev`
vérifié), port 587, clé distincte. 999 tests serveur et 236 tests web verts.

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
- **Le premier envoi de prod était la note.** La tâche du jour s'appelle « Boites! » ;
  le modèle l'a citée telle quelle, comme demandé, et le validateur refusait tout
  point d'exclamation. Les titres de la matière qui en portent un sont maintenant
  retirés du texte avant la vérification. Corrigé et renvoyé le soir même.

**Observé en prod** : la première vraie lettre a bien utilisé la matière étendue (ce
qu'Ariane a coché la veille, les échéances de mardi et mercredi, treize séances de la
même tâche). La série au sens strict (même jour de semaine) reste à voir sur une
tâche hebdomadaire. À relire après une semaine de lettres.
