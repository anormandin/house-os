# Worker courriel — documents par courriel

Reçoit les courriels envoyés à une adresse de votre domaine (Cloudflare Email Routing)
et dépose le message brut dans un bucket R2 ; House OS relève le bucket toutes les
2 minutes et classe les pièces jointes dans Documents (« À classer »). Module
**facultatif** : sans les variables `COURRIEL_R2_*` du `.env`, rien ne tourne.

Prérequis : un domaine géré par Cloudflare (zone DNS chez eux), un compte R2 (gratuit
jusqu'à 10 Go).

## Mise en place (tableau de bord Cloudflare)

1. **Email Routing** — dans la zone du domaine : *Email* → *Email Routing* →
   *Onboard Domain* et accepter les enregistrements proposés (3 MX
   `route*.mx.cloudflare.net`, SPF `include:_spf.mx.cloudflare.net`, DKIM
   `cf2024-1._domainkey`). Si le domaine avait déjà des MX (autre fournisseur de
   courriel), ils doivent partir : Email Routing veut être le seul.
2. **R2** — créer le bucket `houseos-courriel` (ou un autre nom : ajuster
   `wrangler.toml` et `COURRIEL_R2_BUCKET`). Puis *R2* → *Manage API Tokens* → jeton
   **Object Read & Write** limité à ce bucket → noter l'Access Key ID et la Secret
   Access Key : ce sont `COURRIEL_R2_CLE_ACCES` / `COURRIEL_R2_CLE_SECRETE` dans le
   `.env`, avec `COURRIEL_R2_ENDPOINT=https://<ACCOUNT_ID>.r2.cloudflarestorage.com`.
3. **Déployer le Worker** — depuis ce dossier :

   ```bash
   npm install
   npx wrangler login
   npx wrangler deploy --var EXPEDITEURS_PERMIS:"vous@exemple.com,conjoint@exemple.com"
   ```

   `EXPEDITEURS_PERMIS` = les adresses (enveloppe *MAIL FROM*, minuscules) autorisées
   à déposer ; tout autre expéditeur reçoit un rebond SMTP. La valeur passée par
   `--var` prime sur le `wrangler.toml` (laissé vide exprès pour ne pas committer
   d'adresses personnelles). Pour ne pas la retaper, gardez-la dans un
   `infra/courriel-worker/.env` gitignoré et déployez avec
   `npx wrangler deploy --var EXPEDITEURS_PERMIS:"$(grep EXPEDITEURS_PERMIS .env | cut -d= -f2-)"`.
4. **Règle de routage** — *Email Routing* → *Routing rules* → adresse personnalisée
   (ex. `documents@votre-domaine`) → action *Send to a Worker* → `houseos-courriel`.
5. Remplir les quatre `COURRIEL_R2_*` dans le `.env` de House OS, puis
   `docker compose up -d`. Le journal de l'app annonce le relevé actif.

## Tester en local

```bash
npx wrangler dev   # puis, dans un autre terminal :
curl -s -X POST 'http://localhost:8787/cdn-cgi/handler/email' \
  --url-query 'from=vous@exemple.com' \
  --url-query 'to=documents@votre-domaine' \
  -H 'Content-Type: application/octet-stream' \
  --data-raw $'From: vous@exemple.com\r\nTo: documents@votre-domaine\r\nSubject: Test\r\nMessage-ID: <test-1@local>\r\n\r\nBonjour'
```

(`wrangler dev` lit `EXPEDITEURS_PERMIS` d'un fichier `.dev.vars` du dossier.)

## Limites connues

- Cloudflare n'expose pas le résultat SPF/DKIM au Worker : la liste d'expéditeurs
  est la seule garde. Un domaine expéditeur en DMARC `p=none` peut être usurpé —
  acceptable pour un foyer, à garder en tête.
- 25 Mio par message (limite Email Routing) ; le Worker rejette au-delà.
