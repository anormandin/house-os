---
type: recap
date: 2026-09-02
feature: "[[Courriel Entrant]]"
plan: "[[Plan 2026-09-02 Courriel Entrant]]"
---

# Recap Courriel Entrant

Construit le 2026-09-02 (code complet, tests verts : 629 serveur, 181 web) :

- Worker Cloudflare `infra/courriel-worker/` (allowlist d'expéditeurs, dépôt du MIME
  brut dans R2) ; relève R2 côté app (`AWSSDK.S3`, checksums « quand requis »),
  service singleton + hôte d'arrière-plan, endpoints relever/aperçu, outil MCP
  `relever_courriels`, filtre et action `classer`.
- Lecture MIME (MimeKit) avec conversion HTML → texte maison : `HtmlToText`
  n'existe pas dans MimeKit, le tokenizer suffit (blocs → lignes à la fermeture,
  retours de ligne du source = blancs).
- Enrichissement Haiku derrière `IEnrichisseurCourriel` (fictif en test — aucune
  suite ne parle à Anthropic, même avec une clé dans l'env).
- Service d'écriture partagé `EnregistrementDocument` extrait du POST ; les tests
  d'upload existants n'ont pas bougé.
- Déviations : un objet trop gros ou illisible est **effacé** de R2 après
  marquage en erreur (pas déplacé) ; les lignes Ignoré/Erreur ne portent pas de
  `MessageId` (index unique) — l'identifiant dupliqué est dans `Erreur`.
- Pièges rencontrés : Npgsql exige des `DateTimeOffset` en UTC (dates de courriel
  converties) ; SQLite ne trie pas les `DateTimeOffset` (convertisseur ajouté pour
  `Document.CreeLe` dans le contexte de test) ; MimeKit génère un Message-ID par
  défaut (retiré explicitement dans la fabrique de test).

Mise en service le 2026-09-02 (Cloudflare + prod) et premier vrai courriel importé :
« Commande LUSINE — lit, fauteuil et ottoman », titre proposé par Haiku, toast reçu.
Deux leçons de la mise en service :

- R2 refuse `message.raw` tel quel (« readable stream must have a known length ») :
  le Worker lit le flux en mémoire d'abord. Les deux premiers transferts ont
  échoué en silence — `npx wrangler tail houseos-courriel` est l'outil qui l'a
  montré ; `wrangler r2 bucket info` a un compteur d'objets en retard, inutile
  pour ce diagnostic.
- Le bucket créé s'appelle `houseos-courriel` (singulier) et le jeton lui est
  scopé ; un doublon `houseos-courriels` a existé le temps de l'aligner.

Vérifié le soir même : le contrat signé LUSINE (PDF joint) est arrivé comme document,
et l'outil MCP prod répond. Deux effets de bord observés et traités :

- Les transferts échoués à 16 h 27/16 h 28 ont été **rejoués par Gmail** à 21 h
  (nouveaux Message-ID côté Mail.app : la déduplication ne les voit pas) → trois
  documents « à classer » réapparus, doublons des deux déjà classés. Une relance
  SMTP produit toujours un nouvel import ; seule une empreinte du contenu
  attraperait ce cas (non fait).
- Mail.app transfère les images du corps en parties **`inline` nommées sans `cid:`**
  (multipart/mixed entrelacé de HTML) : la bannière Zoho Sign, 16 Ko en PNG, a
  franchi le seuil de 10 Ko et est devenue une « photo ». Seuil monté à 64 Ko
  (`LectureCourriel.TailleMinImageInline`), test « bannière Mail.app » ajouté.

Reste au fil de l'usage : un expéditeur hors liste.
