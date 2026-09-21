---
type: decision
status: accepted
date: 2026-09-21
feature: "[[Lettre Du Matin]]"
tags: []
---

# D-2026-09-21 Courriel Sortant Par SMTP

## Contexte

House OS reçoit du courriel ([[Courriel Entrant]], par Cloudflare Email Routing et R2)
mais n'en envoie aucun. La [[Lettre Du Matin]] est la première sortie par courriel, et
le dépôt est public : le transport doit se brancher chez n'importe quel foyer sans
marier un fournisseur ([[Distribution]]).

## Options considérées

- **SMTP via MailKit** (retenue) : MimeKit est déjà dans le projet, MailKit est son
  jumeau. Quatre variables `.env` génériques (hôte, port, usager, mot de passe) et une
  adresse d'expéditeur : Gmail avec mot de passe d'application, Fastmail, Proton
  Bridge, un relais maison, tout marche. Aucun compte chez un service.
- **API d'un service** (Resend, Mailgun…) : un appel HTTPS, pas de port sortant à
  ouvrir, bonne délivrabilité. Mais un compte et une clé de plus, et le dépôt public
  marie un fournisseur.
- **Cloudflare** : déjà en place pour l'entrant, mais n'envoie pas de courriel sortant
  arbitraire ; il faudrait un Worker qui relaie vers un tiers, deux sauts pour rien.

## Décision

**SMTP par MailKit.** Section de configuration `Lettre:Smtp` (`Hote`, `Port`,
`Usager`, `MotDePasse`, `Expediteur`), alimentée par des variables `LETTRE_SMTP_*` du
`.env`. Configuration absente = envoi désactivé, journalisé une fois, comme le relevé
R2 sans clés.

## Conséquences

- Une dépendance NuGet de plus (`MailKit`), du même auteur que MimeKit.
- Le LXC de prod doit pouvoir sortir sur le port SMTP choisi (587 ou 465) ; à vérifier
  au release, pas à supposer.
- L'envoi est derrière une interface (`IEnvoyeurDeCourriel`) remplaçable en test par un
  fictif à compteur : la suite de tests n'ouvre jamais de connexion réseau.
- Le jour où un autre envoi sortant arrive (accusé, rappel), il passe par la même porte.

## Confirmation

`grep -rn "MailKit" server/HouseOs.Api/*.csproj` retourne une ligne, et
`grep -rln "SmtpClient" server/HouseOs.Api/` ne retourne que l'implémentation de
l'envoyeur sous `Features/Lettre/`.
