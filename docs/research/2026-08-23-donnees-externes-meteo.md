# External data sources & integration patterns — final report (house OS, Quebec)

> Rapport d'agent de recherche, 2026-08-23. Conservé verbatim (anglais).

## 1. Weather APIs for chore decision support ("which day to mow / stain the deck / wash windows")

**Winner: Open-Meteo.** ECCC GeoMet as the authoritative Canadian complement for alerts and AQHI.

| API | Free tier | Auth | Hourly horizon | Canadian model | Verdict |
|---|---|---|---|---|---|
| Open-Meteo (open-meteo.com) | 10,000 calls/day, non-commercial | **none** | 7–16 days | **GEM/HRDPS 2.5 km** | **Primary** |
| ECCC GeoMet (api.weather.gc.ca) | free, effectively unlimited | none | 24 h (citypage) | native | Alerts (FR) + AQHI |
| OpenWeatherMap One Call 3.0/4.0 | 1k/day then pay-per-call | key + credit card | 48 h | no | skip (billing footgun) |
| Met.no Locationforecast 2.0 | ~20 req/s courtesy | identifying User-Agent required (403 without) | ~9 days | no (Nordic-optimized) | fallback only |
| Pirate Weather | 20k/month | key | 7 days | no — HRDPS NOT integrated (HRRR/GFS) | skip |

**Open-Meteo details**: no API key, clean flat JSON, one call returns hourly temp, precipitation + probability, wind/gusts, humidity, **soil moisture** (great "is the lawn dry" signal), UV, cloud cover, plus daily aggregates incl. sunrise/sunset. `/v1/forecast` auto-picks best models (incl. Canadian HRDPS); `/v1/gem` forces the Canadian family. GEM updates every 6 h. Also free air-quality and historical endpoints on the same pattern.

**ECCC GeoMet details**: anonymous OGC API (GeoJSON) — AQHI collections, experimental `citypageweather-realtime` (official forecast incl. French text); raw City Page Weather XML on dd.weather.gc.ca (current + 24 h hourly, updated hourly); WMS radar. Clunkier to consume; its unique value is official French alerts + AQHI.

## 2. Other harvestable household data

- **Garbage/recycling (QC)**: many municipalities use **Recollect**, whose "Add to calendar" exposes a stable **iCal feed** (`https://recollect.a.ssl.fastly.net/api/places/{PLACE_ID}/services/{SERVICE_ID}/events.en-US.ics`, fr-CA variant exists) — grab the URL once via the town's address-lookup widget. Reference for QC parsing patterns: `github.com/mampfes/hacs_waste_collection_schedule` (Montréal, Pointe-Claire, MRC de Roussillon/Info-Collectes scrapers + generic ICS source). Non-Recollect towns = PDF calendars → yearly manual seed.
- **Air quality**: ECCC **AQHI** GeoJSON, free, no auth: `api.weather.gc.ca/collections/aqhi-forecasts-realtime` (forecast 2×/day).
- **Pollen**: weak spot for Canada. Open-Meteo pollen = **Europe only**. Only real option is **Google Pollen API** (Maps Platform: ~10k free calls/SKU/month but requires GCP billing account; ~$10 CPM after). Suggest skipping in v1.
- **Sunrise/sunset**: compute locally (.NET: CoordinateSharp or SunCalcNet NuGet) or take it from Open-Meteo's daily block — no separate API needed.
- **Hydro-Québec peak events (Crédit hivernal / Flex-D)**: official open-data dataset **`evenements-pointe`** at donnees.hydroquebec.com — Opendatasoft **JSON records API, no auth**, active Dec 1–Mar 31. Reference impls: `Beat-YT/hydropeak-ha` (public data only) and `hydroqc/hydroqc-ha` (full account integration incl. consumption via unofficial login API — fragile). Use the public dataset.
- **School calendars (QC)**: centres de services scolaires publish yearly calendars, mostly PDF; some (e.g. CSS des Bois-Francs) offer **iCalendar downloads**. No province-wide API — ingest the CSS ICS when it exists.
- **Gas prices**: no viable free source (GasBuddy has no public API; commercial APIs paid; CAA-Québec daily regional price is scrapeable HTML but fragile; OilPriceAPI free tier is weekly NRCan province-level only). Leave out of v1.

## 3. Integration & automation patterns

- **Ingestion**: for a custom .NET server, the right pattern is a **`BackgroundService` worker (or Hangfire/Quartz job) per source** — weather every 1–3 h, ICS feeds daily, HQ events every 15 min in winter — normalizing into your own tables and archiving raw payloads. This mirrors what Home Assistant/n8n do but stays typed, testable, and single-stack; don't bolt on n8n/Node-RED for v1.
- **Rules ("bonne journée pour tondre")**: no reusable open-source outdoor-chore recommender exists; closest precedents are HA robot-mower schedulers (e.g. `shawwellpete/luba-mower-scheduler`: skip if rain today > 3 mm or rain prob > 50%). Own the logic — it's small: score day/hour windows per chore. Examples: **tonte** = no rain last 12 h/next 4 h, precip prob < 30%, > 10 °C, wind < 30 km/h; **teinture patio** = dry 24 h before + 24–48 h after, 10–30 °C, low humidity; **vitres** = dry, cloudy, > 5 °C. Implement as plain C# strategy classes over a normalized `DailyOutlook`/`HourlyOutlook` model (rules never see API shapes); graduate to `microsoft/RulesEngine` (JSON lambda rules) only if rules multiply. NRules is overkill.

## 4. Calendar integration (couple sharing tasks)

- **Publish out (do first)**: serve iCal feeds from the server (`/calendars/chores.ics`, `/calendars/collectes.ics`) with the **Ical.Net** NuGet; each partner subscribes by URL in Google/Apple Calendar. Gotcha: Google refreshes subscribed URLs lazily (~12–24 h) — fine for chores/collectes, not for urgent changes.
- **Ingest in**: Ical.Net also parses external ICS — Recollect, school feeds, and any Google calendar's read-only "secret address in iCal format" (zero OAuth).
- **True two-way Google sync**: needs the **Google Calendar API** + OAuth (Google's CalDAV can't be pushed to). Pattern: incremental `syncToken` sync (full resync on HTTP 410), optional `watch` webhooks (public HTTPS endpoint). Real work (tokens, conflicts, recurrences) — **defer to v2**.
- Self-hosted CalDAV (Radicale/Baïkal) works but adds ops burden and lives outside the couple's default Google calendars — not recommended.

## First-iteration recommendation

1. **Open-Meteo `/v1/forecast`** (hourly, 7-day, HRDPS-backed, no key) via a .NET hosted-service worker; add ECCC GeoMet later for French alerts + AQHI.
2. **Worker-per-source ingestion** into normalized tables, raw payloads archived.
3. **Plain C# chore-scoring rules** over a normalized outlook model.
4. **ICS in/out with Ical.Net**: ingest Recollect + school + Google secret-address feeds; publish per-category feeds. Defer Google Calendar API two-way sync.
5. **Hydro-Québec `evenements-pointe`** Opendatasoft JSON for winter peak events.
6. Skip pollen (GCP billing) and gas prices (no free source) in v1.

Key URLs: open-meteo.com/en/docs · api.weather.gc.ca · donnees.hydroquebec.com/explore/dataset/evenements-pointe/api/ · github.com/mampfes/hacs_waste_collection_schedule · github.com/hydroqc/hydroqc-ha · github.com/Beat-YT/hydropeak-ha · developers.google.com/maps/documentation/pollen · github.com/ical-org/ical.net · docs.api.met.no · pirateweather.net
