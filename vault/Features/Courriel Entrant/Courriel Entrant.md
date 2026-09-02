---
type: feature
status: building
last-verified: 2026-09-02
verified-against: f29b6fd
tags: []
---

# Courriel Entrant

## Intention

Transférer un reçu, une facture ou un papier important à
`documents@alainnormandin.dev` et le retrouver dans le classeur de [[Documents]] sans
rien téléverser à la main. Troisième source d'ingestion après [[Météo]] et
[[Flux Externes]] : un `BackgroundService` par source, jamais de n8n
([[D-2026-08-23 Pas De N8n Dans Le Cœur]]).

## Comportement

- **Réception** ([[D-2026-09-02 Courriel Entrant Par Cloudflare Et R2]]) : Cloudflare
  Email Routing reçoit `documents@` et confie le message à l'Email Worker
  `houseos-courriel` (`infra/courriel-worker/`). Le Worker rejette (SMTP 5xx) tout
  expéditeur absent de `EXPEDITEURS_PERMIS` et tout message > 25 MiB, sinon dépose le
  MIME brut dans le bucket R2 `houseos-courriel` sous `entrants/<horodatage>-<uuid>.eml`.
- **Relève** : `CourrielEntrantHote` relève R2 au démarrage puis toutes les
  `Courriel:CadenceMinutes` (défaut 2, plancher 1) via `CourrielEntrantService`
  (singleton, verrou anti-passe concurrente). Sans configuration R2, le service
  journalise une fois et reste inactif. Un bouton « Relever le courrier » (page
  Documents), `POST /api/documents/relever-courriels` et l'outil MCP
  `relever_courriels` déclenchent un passage immédiat.
- **Par courriel**, dans son propre scope : téléchargement → lecture MimeKit → chaque
  pièce jointe de type permis (PDF/JPEG/PNG/WebP/HEIC, disposition « attachment » ou
  nommée et non référencée en `cid:`, hors petites images inline) devient un
  `Document` **à classer** ; sans pièce retenue, le `.eml` entier devient le document
  ([[D-2026-09-02 Boîte À Classer Des Documents]]). Une ligne `ImportCourriel`
  (clé R2, Message-ID, expéditeur, sujet, statut, nb documents) est écrite dans la
  même transaction ; l'objet R2 est effacé après le commit.
- **Déduplication** : Message-ID déjà importé → statut Ignoré, objet effacé, aucun
  document ; clé R2 déjà vue (delete R2 raté après commit) → effacée sans réimport.
  Objet trop gros ou illisible → statut Erreur, effacé.
- **Enrichissement** ([[D-2026-09-02 Enrichissement LLM À L'ingestion]]) : un appel
  Haiku par courriel propose titre, catégorie, dossier, équipement, date, montant,
  résumé, contraints aux valeurs existantes ; repli déterministe sinon.
- **Temps réel** : événement fin `documents.recus` (source `courriel`, nombre) →
  toast « N document(s) reçu(s) par courriel » sur les onglets ouverts ([[Synchro]]).
- **Configuration** : section `Courriel` (`R2:Endpoint/Bucket/CleAcces/CleSecrete/
  Prefixe`, `CadenceMinutes`, `TailleMaxOctets`) ; en prod, variables
  `COURRIEL_R2_*` du `.env` (facultatives : absentes = relevé désactivé).

## Hors périmètre

- Réponse automatique ou accusé de réception.
- OCR des pièces jointes ; extraction dans le corps des PDF.
- Boîtes IMAP (Gmail ou hébergée) ; Mailgun.
- Vérification SPF/DKIM dans le Worker (Cloudflare ne l'expose pas de façon
  documentée) — suivi si l'usurpation devient un problème réel.

## Décisions

- [[D-2026-09-02 Courriel Entrant Par Cloudflare Et R2]] — réception et transport.
- [[D-2026-09-02 Boîte À Classer Des Documents]] — triage et archivage `.eml`.
- [[D-2026-09-02 Enrichissement LLM À L'ingestion]] — vocabulaire fermé + repli.

## Ancres de code

- `infra/courriel-worker/src/index.js` — Email Worker (allowlist, dépôt R2).
- `server/HouseOs.Api/Features/Courriel/` — options, dépôt R2, lecture MIME,
  enrichissement, service de relève, hôte, endpoints.
- `server/HouseOs.Api/Domaine/ImportCourriel.cs` — journal des imports.
- `server/HouseOs.Api/Features/Documents/EnregistrementDocument.cs` — service
  partagé « enregistrer un document » (REST + ingestion).
- `server/HouseOs.Tests/Features/Courriel/` — tests unitaires et de service ;
  `server/HouseOs.Tests/Integration/CourrielApiTests.cs`.

## Sources

- Conversation de design 2026-09-02 (choix : boîte sur le domaine, Worker → R2,
  `.eml` archivé, boîte À classer, LLM dès la v1).

## Historique

- [[Plan 2026-09-02 Courriel Entrant]]
