# House OS

Système auto-hébergé de gestion de la maison — tâches récurrentes et ponctuelles,
équipements, projets, documents, consommables, et (plus tard) affichages IoT et
capteurs. UI en français, pensé pour deux personnes.

## Structure

| Dossier | Contenu |
|---|---|
| `server/` | API .NET 10 — monolithe modulaire, tranches verticales, EF Core + PostgreSQL |
| `web/` | PWA React (Vite + TypeScript + TanStack Query + Tailwind + shadcn/ui) |
| `firmware/` | (à venir) Projets ESP32 / ESPHome |
| `hardware/` | (à venir) `cad/` Fusion 360, `pcb/` KiCad |
| `vault/` | Vault de specs (Obsidian) — ADRs, specs de features |
| `docs/` | Rapports de recherche et références |

## Démarrage

```bash
docker compose up -d          # PostgreSQL
cd server && dotnet run --project HouseOs.Api
cd web && npm install && npm run dev
```
