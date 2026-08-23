# Self-hostable household / home management software — research report

> Rapport d'agent de recherche, 2026-08-23. Conservé verbatim (anglais).

## TL;DR

**Donetick** is the standout for the chores/recurring-maintenance core: it does exactly the "household recurring tasks between two people" job, has a full REST API, a verified French translation, and a data model worth copying even for a custom build. **Grocy** has the most battle-tested chore *scheduling model* (mine it for the data model even if you don't run it). For adjacent domains, **Homebox** (inventory + per-item maintenance) and **Mealie/KitchenOwl/Tandoor** (recipes/groceries) are solid reusable components behind a custom French dashboard. Nothing found does "whole-house OS" well — the realistic architecture is either *Donetick as the task engine + custom dashboard consuming its API*, or *build the task engine yourself borrowing Grocy/Donetick's recurrence model*. Notably, the two features the House OS plan already includes — **seasonal windows** and (later) **sensor-triggered due-ness** — are exactly the gaps no mainstream tool fills.

---

## Shortlist (most relevant first)

### 1. Donetick — chore & task manager (best direct fit / reuse candidate)
- **URL:** https://github.com/donetick/donetick · https://donetick.com
- **License:** AGPL-3.0 (open discussion #354 about moving to Apache 2.0, undecided)
- **Stack:** Go backend + React frontend, SQLite by default; single Docker container or plain binary
- **Recurring tasks:** richest scheduler in this space — daily/weekly/monthly/yearly, specific weekdays, day-of-month, specific months, intervals, and adaptive scheduling (learns from completion history). Crucially, per-task choice of **due-date-based vs completion-date-based** recurrence — exactly the fixed-schedule vs interval-since-last-completion distinction home maintenance needs.
- **Multi-user:** "circles" (household groups) with roles; assignee rotation strategies (round-robin, random, fewest-completed); optional points system; subtasks reset on recurrence; natural-language task entry ("Change water filter every 6 months").
- **API:** full REST API + limited "eAPI" for long-lived tokens; official Home Assistant integration (per-user todo lists); notifications via Telegram, Discord, Pushover.
- **French:** yes, verified in the repo — `public/locales/fr/` (9 languages: ar, de, en, es, fr, ja, nl, pt, zh-CN).
- **Verdict:** reusable as a component or the best data-model reference. Caveats: AGPL (fine for personal use), young-ish project, task-centric — no notion of house assets (furnace, roof) to attach tasks to.

### 2. Grocy — household ERP (best scheduling model to mine; heavyweight to run)
- **URL:** https://grocy.info · https://github.com/grocy/grocy
- **License:** MIT · **Stack:** PHP (Slim) + SQLite, server-rendered jQuery-era frontend
- **What it does well:** groceries/stock with barcodes, meal planning, chores, tasks, batteries (charge tracking), equipment (manuals, warranties). Its chore model is the reference recurrence design: **manual / "dynamic regular" (N days after last execution) / daily / weekly (chosen weekdays) / monthly (day-of-month) / yearly**, plus "due date rollover" (never-overdue chores) and assignment strategies (least-did-it-first, random, alphabetic rotation). Execution log records who/when.
- **API:** excellent — REST for everything with Swagger UI at `/api`; the frontend itself uses it, so headless use is proven. Mature Home Assistant integration. French translation exists (community, uneven quality).
- **Verdict:** could run headless as a backend, but the PHP/jQuery stack + everything-app sprawl make it a poor base for a .NET/React dev to extend. **Mine it**: chore recurrence enum + execution log + assignment-strategy schema, and the batteries/equipment modules as feature checklists.

### 3. Homebox — home inventory with maintenance log (reference for the assets module)
- **URL:** https://github.com/sysadminsmedia/homebox · https://homebox.software (active continuation of hay-kot's project)
- **License:** AGPL-3.0 · **Stack:** Go + embedded Nuxt/Vue UI, SQLite (Postgres supported), single Docker container
- **What it does well:** items with locations/labels/QR codes, purchase price, warranty, attachments (receipts, manuals), CSV import/export, per-item maintenance schedules with notifications. Multi-user households. REST API (Swagger). Translations via Weblate, French included.
- **Limits:** maintenance scheduling is simpler than Donetick/Grocy (item-attached reminders, no rotation/chore engine).
- **Verdict:** models the *things* while Donetick models the *work*. Good schema reference for Équipement → maintenance-entry, and a fallback if the custom assets module is deferred.

### 4. Vikunja — general task manager (fallback / generic engine)
- **URL:** https://vikunja.io · **License:** AGPL-3.0 · **Stack:** Go API + Vue frontend with strict API/frontend split, Docker-friendly
- **Recurring:** supported but generic (repeat-after-interval from due date or completion); no chore rotation, no seasonal logic. Multi-user projects/teams, kanban/gantt/calendar, CalDAV. Very good REST API + webhooks. French via Crowdin.
- **Verdict:** most polished generic engine, but household-specific features are absent. Only relevant if one tool for all tasks (work + home) were wanted.

### 5. Home Assistant ecosystem — for the later IoT phase
- Native To-do lists: simple, no real recurrence engine.
- **Chore Helper** (github.com/bmcclure/ha-chore-helper): recurring chores as helpers; "every N…" vs "after N…" split again.
- **Home Upkeep** (github.com/tonyroberts/home-upkeep-component): household/garden/maintenance todos with **seasonal constraints** (e.g. monthly task active only April–October) — the only project found with first-class seasonality.
- **maintenance_supporter** (github.com/iluebbe/maintenance_supporter): due-ness triggered by time OR real sensor data (runtime hours, usage counters) — the pattern for "replace HVAC filter after X blower hours" once sensors exist.
- **Verdict:** don't build the house OS inside HA, but plan for it — Donetick and Grocy both have official HA integrations, and these addons validate the seasonal + sensor-triggered patterns already in the House OS plan.

---

## Adjacent-domain components (reuse rather than rebuild)

- **Mealie** (mealie.io) — recipes/meal plan/shopping; AGPL, Python FastAPI + Vue; most polished UI, good REST API, French translation; easiest for a non-technical partner.
- **Tandoor** (tandoor.dev) — power-user recipes; AGPL, Django + Vue, requires PostgreSQL; most features (nutrition, meal cost); French available.
- **KitchenOwl** (github.com/TomBursch/kitchenowl) — shared groceries + recipes; AGPL, Flask + Flutter; best native mobile apps with real-time shared shopping-list sync — ideal for a couple; French available.
- **HortusFox** (github.com/danielbrendel/hortusfox-web) — plants/garden; MIT, PHP + MariaDB; collaborative plant care, warnings, tasks, calendar; best-of-breed garden module.
- **HomeLogger** (github.com/FrancisLaboratories/homelogger) — home maintenance log; MIT, Go (Fiber/GORM) + React, SQLite/Postgres, OpenAPI spec; appliances + repair history + receipts, but early-stage (~50 stars, v0.x), no recurring scheduling, no multi-user — mine the asset/receipt model only.
- **Mainty** (github.com/michaelstaake/mainty) — small AI-assisted PHP maintenance tracker; idea-mining only.
- **Chorecast** (github.com/KenWeTech/Chorecast) — chores completed via NFC tags; interesting for physical "tap to complete" on future IoT displays.
- **HomeHub / Yuvomi / Oikos** — all-in-one family hubs; prove demand but all immature; none recommended as a base.
- "Sticky" (chores) and "Faultmate" could not be located as findable open-source projects; "Nest Egg" / HomeManager-style apps are commercial/mobile-only.

---

## Recurring-task data-model patterns (convergent across these tools)

1. **Fixed-calendar vs completion-relative recurrence** — the fundamental split: "every 1st Saturday" vs "N days after last completion" (Grocy dynamic-regular, Donetick per-task mode, Chore Helper "every" vs "after"). Maintenance mostly wants the second; cleaning routines the first. Model as an explicit per-task flag, not two task types.
2. **Recurrence stored as type + parameters, not cron/RRULE strings** — a `frequency_type` enum plus small metadata; the **next due date is materialized on each completion**, not computed at read time. Simpler to query and display.
3. **Completion log as a separate table** (who, when, notes/cost/photo), never just a mutated due date — enables history, stats, adaptive scheduling, fairness-based assignment.
4. **Assignment strategy on the task**: fixed, round-robin, random, least-did-it-first (Grocy and Donetick both ship all four). For a couple, round-robin + least-did-it covers it.
5. **Seasonality and sensor triggers are the gap**: only Home Upkeep does month-window constraints; only maintenance_supporter does sensor/usage-based due-ness. Neither Donetick nor Grocy has these.
6. **Due-date rollover / grace behavior** (Grocy): some chores should shift forward when missed rather than piling up overdue — worth a per-task flag.
7. **API-first is proven here**: Grocy, Donetick, Vikunja, Homebox, Mealie all expose the full app over REST (Grocy and Vikunja dogfood it; Vikunja adds webhooks) — custom dashboards and wall displays consuming these APIs is an established pattern.

## Build-vs-reuse recommendation

- **Reuse path:** Donetick (tasks/chores, French, HA integration) + Homebox (assets) + KitchenOwl or Mealie (groceries/meals) in Docker, with a thin custom French dashboard aggregating their APIs. Fastest path to working tools.
- **Build path (fits the plan and a .NET/C# + React profile):** build only the task/maintenance engine — copy Grocy's recurrence enum + completion log + assignment strategies and Donetick's fixed-vs-completion-based flag, and add the two things nobody ships: seasonal windows and sensor-triggered due-ness. Consider reusing KitchenOwl/Mealie-class tools for groceries/recipes later rather than rebuilding those domains. This confirms the plan's direction: nothing to adopt outright as the core, strong models to mine.

Key sources: Donetick repo + frontend locale tree via GitHub API (French confirmed) · Grocy chores tutorial (grocy/docs) · sysadminsmedia/homebox · vikunja.io/docs + Crowdin project · HA community threads for Home Upkeep / Chore Helper / maintenance_supporter · cooklang.org recipe-manager comparisons (Tandoor vs Mealie vs KitchenOwl) · awesome-selfhosted task-management tag · alternativeto.net Donetick alternatives.
