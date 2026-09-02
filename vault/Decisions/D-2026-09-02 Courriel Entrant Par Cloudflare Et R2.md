---
type: decision
status: accepted
date: 2026-09-02
feature: "[[Courriel Entrant]]"
tags: []
---

# Courriel Entrant Par Cloudflare Et R2

## Contexte

Ajouter un reçu ou une facture reçue par courriel dans [[Documents]] demandait un
téléchargement manuel puis un téléversement web, ou un contournement fragile via le
connecteur Gmail de Claude Code. Alain veut transférer le courriel à une adresse de
son domaine (`alainnormandin.dev`) et retrouver le document dans le classeur. Le
domaine est chez Cloudflare (NS `johnny/lina.ns.cloudflare.com`) ; son MX pointait vers
Mailgun, un vieil essai mort (aucun projet, aucun courriel de compte ; MX, SPF et une clé
DKIM `krs._domainkey` orphelins).
L'app n'est pas exposée à Internet hors `/ical`
([[D-2026-08-27 Flux iCal Public Via Tailscale Funnel]]).

> [!note] Correctif de fait
> La décision du 2026-08-27 affirmait que la zone DNS n'était pas chez Cloudflare et
> qu'un Cloudflare Tunnel exigerait de la migrer. C'était faux : la zone y est déjà.
> La décision elle-même (Funnel pour `/ical`) reste valable ; seule cette prémisse
> est corrigée ici, sans réécrire la note d'origine.

## Options considérées

- **Étiquette Gmail relevée par IMAP** — mot de passe d'application sur le compte
  perso, MailKit, aucune adresse dédiée ; les courriels d'Ariane transitent par la
  boîte d'Alain.
- **Route Mailgun `store()` + relève par API** — élégant, mais le compte Mailgun est
  inaccessible/mort ; à jeter.
- **Boîte IMAP hébergée sur le domaine** (Migadu, Purelymail ~15 $/an) — un
  fournisseur de plus et un abonnement pour une boîte que personne ne lit.
- **Cloudflare Email Routing → Worker → POST via Funnel** — temps réel, mais ouvre
  un chemin d'écriture public et perd les courriels reçus pendant un redéploiement.
- **Cloudflare Email Routing → Worker → bucket R2 → House OS relève R2** — gratuit,
  aucune surface publique, retries gratuits (l'objet attend dans R2), même patron
  qu'un `BackgroundService` d'ingestion.

## Décision

**Cloudflare Email Routing sur l'apex** (le MX Mailgun et son SPF sont remplacés),
adresse `documents@alainnormandin.dev` → **Email Worker** (`infra/courriel-worker/`)
qui filtre l'expéditeur sur une liste permise et dépose le MIME brut dans le bucket R2
`houseos-courriels` → **House OS relève R2** par l'API S3 (`AWSSDK.S3`, jeton scopé
au bucket) toutes les 2 minutes, importe, puis efface l'objet. Choix d'Alain
(2026-09-02) : boîte dédiée sur son domaine, puis « Worker → R2 » parmi les
consommations proposées.

## Conséquences

- Nouvelle infra hors du compose : règle Email Routing, Worker (déployé par
  `wrangler`), bucket et jeton R2 — documentés dans le dépôt infra du lab.
- Le principe « app jamais exposée à Internet » tient : rien n'entre par HTTP.
- Risque assumé : Cloudflare ne rejette les usurpations que selon la politique
  DMARC de l'expéditeur (Gmail publie `p=none`) ; la liste permise + le triage
  humain ([[D-2026-09-02 Boîte À Classer Des Documents]]) bornent l'exposition à un
  document indésirable dans « À classer ».
- Un objet R2 trop volumineux ou illisible est marqué en erreur puis supprimé
  (sinon relance toutes les 2 min) ; Cloudflare coupe de toute façon à 25 MiB.
- Une deuxième adresse (ex. `maison@`) = une règle de routage de plus, sans code.

## Confirmation

- `infra/courriel-worker/wrangler.toml` existe et déclare un binding `r2_buckets`.
- `grep -r "AmazonS3Client" server/HouseOs.Api/Features/Courriel/` retourne le
  dépôt R2.
- `infra/tailscale-serve.json` ne monte toujours que `/ical`.
- Aucun paquet MailKit dans `server/HouseOs.Api/HouseOs.Api.csproj` (MimeKit seul).
