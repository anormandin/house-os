# Worker « houseos-courriel »

Reçoit `documents@alainnormandin.dev` (Cloudflare Email Routing) et dépose le
courriel brut dans le bucket R2 `houseos-courriel`. House OS relève le bucket
toutes les deux minutes (`server/HouseOs.Api/Features/Courriel/`).

## Mise en place (une fois, dans le tableau de bord Cloudflare)

1. **DNS** `alainnormandin.dev` : retirer les deux MX Mailgun (`mxa`/`mxb.mailgun.org`),
   le TXT SPF `include:mailgun.org` et le TXT DKIM `krs._domainkey` (colonne
   *Content* dans Cloudflare — le nom est l'apex nu).
2. **Email Routing** (niveau du compte, pas du domaine) : Compute → Email Service →
   Email Routing → *Onboard Domain* → `alainnormandin.dev` et accepter les
   enregistrements proposés (3 MX `routeN.mx.cloudflare.net`, SPF
   `v=spf1 include:_spf.mx.cloudflare.net ~all`, DKIM `cf2024-1._domainkey`).
3. **R2** : créer le bucket `houseos-courriel`. Puis *Manage API Tokens* → jeton
   « Object Read & Write » scopé à ce bucket ; noter Access Key ID, Secret et
   l'endpoint `https://<ACCOUNT_ID>.r2.cloudflarestorage.com` → `.env` du serveur
   (`COURRIEL_R2_*`, voir `.env.example`).
4. **Déployer le Worker** :

   ```sh
   cd infra/courriel-worker
   npm install
   npx wrangler login
   npx wrangler deploy
   ```

   Les expéditeurs permis vivent dans `wrangler.toml` (`EXPEDITEURS_PERMIS`,
   adresses séparées par des virgules) : les deux adresses d'Alain et les deux
   d'Ariane y sont déjà.
5. **Règle de routage** : Email Routing → le domaine → Routing rules → *Custom addresses* →
   `documents@alainnormandin.dev` → action *Send to a Worker* → `houseos-courriel`.

## Test local

```sh
npx wrangler dev
curl -X POST 'http://localhost:8787/cdn-cgi/handler/email' \
  --url-query 'from=alain.normandin@gmail.com' \
  --url-query 'to=documents@alainnormandin.dev' \
  --header 'Content-Type: message/rfc822' \
  --data-raw $'From: alain.normandin@gmail.com\r\nTo: documents@alainnormandin.dev\r\nSubject: Test\r\nMessage-ID: <test-1@local>\r\n\r\nBonjour'
```

L'objet apparaît dans le bucket (tableau de bord R2 ou `npx wrangler r2 object get`).

## Limites connues

- Cloudflare ne rejette une usurpation que selon la politique DMARC de l'expéditeur
  (Gmail publie `p=none`). La liste permise porte sur l'enveloppe ; le triage humain
  dans House OS (« À classer ») est le second filet.
- Taille maximale d'un courriel entrant : 25 Mio (limite Email Routing).
