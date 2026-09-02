---
type: plan
status: approved
date: 2026-09-02
feature: "[[Courriel Entrant]]"
---

# Plan 2026-09-02 Courriel Entrant

## But

`documents@alainnormandin.dev` → Cloudflare Email Routing → Worker → R2 → House OS :
chaque courriel transféré devient un ou des documents dans la boîte « À classer »,
enrichis par LLM, avec parité MCP et toast temps réel. Approuvé par Alain le
2026-09-02.

## Étapes

- [x] Vault : spec [[Courriel Entrant]], ce plan, décisions
  [[D-2026-09-02 Courriel Entrant Par Cloudflare Et R2]],
  [[D-2026-09-02 Boîte À Classer Des Documents]],
  [[D-2026-09-02 Enrichissement LLM À L'ingestion]].
- [x] Worker `infra/courriel-worker/` (wrangler.toml, src/index.js, README).
- [x] Domaine : `Document.AClasser`, `Document.ImportCourrielId`, entité
  `ImportCourriel` ; DbContext ; migration `AjouterCourrielEntrant` ; paquets
  MimeKit + AWSSDK.S3.
- [x] Extraire `EnregistrementDocument.EnregistrerDocumentAsync` du POST ; `DocumentDto`
  += `AClasser`, `ImportCourrielId` (3 sites) ; `DocumentRequete.AClasser` ; filtre
  `aClasser` sur GET.
- [x] `Features/Courriel/` : options, `IDepotCourriels` + R2 + inactif,
  `LectureCourriel` (règle MIME, HTML→texte), `EnrichissementCourriel`,
  `CourrielEntrantService`, `CourrielEntrantHote`, endpoints relever + aperçu.
- [x] Synchro : `GenreDocumentsRecus`, `SourceCourriel` (serveur + web).
- [x] Program.cs, appsettings, docker-compose, .env.example.
- [x] MCP : `lister_documents(aClasser)`, `gerer_document classer`, `relever_courriels`.
- [x] Web : types, api, VignetteDocument (EML), page Documents (bloc À classer,
  bouton Relever, Classer, aperçu courriel), synchro.ts.
- [x] Tests : unitaires Courriel, service avec dépôt fictif, intégration, vitest.
- [x] Docs : dépôt infra `unifi` (DNS, Worker, R2), specs vault touchées, Recap.
- [ ] Mise en service (Alain) : DNS, Email Routing, bucket + jeton R2, `wrangler
  deploy`, règle de routage, `.env` prod ; puis push + release.
- [ ] Vérification réelle : courriel avec PDF, courriel HTML seul, doublon,
  expéditeur hors liste, bouton et MCP « relever ».

## Vérification

- `dotnet test` et `npm test` verts ; migration listée.
- Courriel réel avec PDF → « À classer (1) », toast, métadonnées enrichies ; même
  courriel deux fois → ignoré ; courriel HTML seul → `.eml` avec aperçu ; adresse hors
  liste → rejet ; bouton et MCP « relever » → même rapport.
