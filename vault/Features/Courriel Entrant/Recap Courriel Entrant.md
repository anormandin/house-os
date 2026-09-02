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

Reste ouvert : la mise en service Cloudflare (étapes manuelles d'Alain, voir le
README du Worker) et la vérification avec de vrais courriels — le spec passe à
`implemented` après.
