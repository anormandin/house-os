# UI/UX Research: Household Chore & Family Organizer Apps

> Rapport d'agent de recherche, 2026-08-23. Conservé verbatim (anglais).
> Cible : desktop-first + vue e-ink distincte.

**Framing note:** almost every app in this category is mobile-first, so their value is in their *concepts* (urgency models, fairness mechanics, recurrence handling), while the *interaction* patterns to copy come mostly from premium desktop task apps (Things 3 Mac, Todoist web, TickTick web) and keyboard-first web apps like Linear.

## Part 1 — App-by-app findings

### Skylight Calendar (wall display + companion app)
- Views: Schedule (1–7 day agenda), Day, Week, Month; week = scrolling agenda of event "bubbles" tinted with the owner's profile color. "Dim past events" and "Shade weekends" reduce noise.
- Chores: dedicated Tasks tab; name, color-coded profiles, daily/weekly/monthly/custom recurrence, optional time. "Routines" are separate — Morning/Afternoon/Evening groups that **auto-reset**. Set up once, the system re-arms itself.
- Completion: tap to check; emoji-burst celebration. Stars/rewards = kid-only layer, fully separable.
- Desktop relevance: person-color everywhere, auto-resetting recurrence, dimmed-past agenda.

### Hearth Display
Portrait wall display styled like framed art. Same **routines vs. to-dos** split as Skylight — the category norm. "You don't need to be techy": big targets, minimal chrome, legible across the room. The aesthetic to aim for on e-ink (minus color).

### Cozi
The cautionary tale. Per-member color-coding is its one durable idea; the rest — "dated, busy, old-fashioned", ad-riddled — is anti-pattern.

### OurHome
Person/category filter axes are the reusable part; kid points/rewards irrelevant.

### FamilyWall
The **activity feed** ("what did my partner do while I was out?") is interesting for a couple — as a *secondary panel*, a slim right-hand column on desktop.

### Maple
Most instructive negative: **creating a task takes too many steps** (App Store reviews). Capture friction is the most-felt property of these apps.

### TimeTree
**Chat/comments attached to each event** — coordination on the item, not in a separate messenger. The one social feature that earns its keep for couples; cheap in a desktop detail panel.

### Sweepy — top concept source (with Tody)
- Rooms with a **Cleanliness Meter** (green→red with time since last cleaning). Every task has an **effort score (1–3)**. Per-person daily effort budget; Smart Schedule fills each person's plan with the most-urgent tasks until the budget is filled.
- Fairness: workload distributed by effort points; couples cite load-balancing, not the leaderboard.

### Tody — the category's best-loved visualization
- **Indicator Method:** no calendar dates. Frequency ("~every 7 days") + a **continuous dirtiness bar** green→yellow→red; overdue is a gradient, not a binary. Completing visibly drains the bar.
- Home: areas/rooms with status dots (blank fine / orange 1-2 due / red several); drill in for tasks.
- Multi-user: who completed what; optional rotation; effort ranking. Best fit for couples wanting "a quiet maintenance tracker".
- Desktop bonus: room-list → task-list drill-down becomes a natural **two-pane master-detail**.

### Flatastic
Documented anti-patterns: dead-end onboarding, hated **three-stage nag** (coming up / due / late), premium upsells.

### Donetick — closest architectural cousin
- Go + React; the XDA reviewer's primary usage was **the web dashboard on a computer** — exactly House OS's shape.
- Steal: **auto-rotation to whoever has completed least**, NFC completion triggers, "Things" (tracked non-task values).
- Verdict: feature-complete, design-thin. House OS's opportunity: Donetick's data model with Things/Tody-level presentation.

### Grocy
Desktop web app, so its failure is directly instructive: "isn't a bad app, but presented poorly… took more time to maintain than it saved" (XDA). Admin-panel aesthetics + data grids make a household tool feel like a job. The fate House OS must avoid.

### One-idea apps
- **HomeRoutines:** checklists that **invisibly reset**; **Focus Zones** (rotate one area per day/week); **"One Thing" mode** — one task to kill decision fatigue.
- **Chorsee:** avoids "coins and badges"; rotation or open "anyone grabs it" chores.
- **Nipto:** couple points competition — wears out in ~2 weeks. Even couple-targeted gamification doesn't hold.

### Premium task apps — desktop craft benchmarks
- **Things 3 (Mac):** collapsible sidebar (⌘/) of smart lists + areas; Today = single calm list; paper typography; **Type Travel** (type a list name + Return to jump). Completion = subtle animation — *restraint is the delight*.
- **Todoist (web):** **`Q` anywhere = Quick Add** with **natural-language parsing** (recurrence highlighted inline); `Cmd/Ctrl+K` command menu; `E` completes; `?` shortcut overlay.
- **TickTick:** any list switches to **Kanban/column view**.
- **Structured:** the day as a vertical timeline of duration blocks — model for the wall view.
- **Apple Reminders (flaw):** Today merges all lists in one uniform style — aggregate views need visual grouping.
- **Linear (web):** **Cmd+K palette as primary interaction**, shortcut hints throughout so the UI teaches itself. The interaction model that makes a self-hosted tool feel premium.

### Why these apps get abandoned
- ~70% abandon chore systems within 100 days.
- MIT Technology Review: if one partner does all the entering/assigning, the app has **digitized the mental load, not shared it**. The system must carry the remembering (recurrence, auto-scheduling, rotation).
- Shared displays: design **per device**; e-ink = readable in seconds, large type, high contrast, periodic refresh, no animation.

## Part 2 — Synthesis for House OS (desktop-first)

### The 10 patterns that work

1. **Classic skeleton: collapsible sidebar + main pane + detail panel** (Things, Todoist, Linear). Detail in a right panel or inline — never a route change. Slim optional right column = activity feed ("Alain a fait X il y a 2 h").
2. **« Aujourd'hui » = short curated plan, not a query result.** The system decides what surfaces; extra desktop width goes to *context* (room status, upcoming, activity), not a longer list.
3. **Urgency as a continuous gradient, never a deadline** (Tody/Sweepy). Recurring maintenance shows *how due* it is; hard dates only for dated one-offs. Desktop renders this better than any phone: gradient bars in rows, all room meters visible at once.
4. **Rooms as master-detail overview** — the signature desktop view: every pièce with its state simultaneously; click a room, tasks appear beside. One glance = "what state is the house in?"
5. **Keyboard-first, Linear-style:** `Q` quick-add, `Cmd/Ctrl+K` palette, `E`/Space complete, J/K move, `?` overlay, hints everywhere.
6. **Quick-add with French natural-language recurrence:** « poubelles tous les mardis soir » parses and highlights inline. Bespoke app = the parser only needs *your* phrasings.
7. **Effort-budgeted auto-scheduling + fairness without gamification:** effort 1–3 per task, per-person budget, plan filled by urgency (Sweepy) + Donetick's assignment modes (fixed / auto-rotation to least-done / open). Quiet balance readout replaces points.
8. **Person = color everywhere, with a « nous » state.** Two fixed accents; shared tasks neutral/dual. Filter chips (Moi / Toi / Tous). **Symmetric permissions** — that's what keeps the mental load shared.
9. **Quietly satisfying completion + drag-and-drop:** full-row targets; on check the bar drains to green, row fades; hover reveals quick actions; drag task onto day or person; optional column view for projects. No confetti.
10. **Two content types honestly separated:** entretien récurrent vs tâches ponctuelles — merged in Today *with distinct visual treatment*, managed apart.

**E-ink:** separate render target, not a breakpoint. Urgency gradient maps to bar length or hatching instead of color.

### Anti-patterns (documented failures)
- **Admin-panel presentation** (Grocy/Donetick) — how self-hosted household tools die.
- **Points, stars, streaks, leaderboards** — even couple versions (Nipto) wear out in weeks.
- **Notification carpet-bombing** (Flatastic's triple nag). On-screen gradient replaces nagging; at most one calm daily digest.
- **One partner as sysadmin** — digitizes the mental load instead of sharing it.
- **Hard deadlines + red overdue badges on soft chores** — guilt accumulation drives abandonment. Gradient, not guilt.
- **Feature-sprawl dashboard as home** — home answers one question: qu'est-ce qu'on fait aujourd'hui ?
- **Multi-step task creation** (Maple); modal-heavy flows; route changes for what a panel can do.
- **Merged aggregate views without grouping** (Apple Reminders Today).
- **Mouse-only interaction** — feels like a website, not a tool.

### Worth looking at screenshots of
Things 3 (Mac) · Tody · Sweepy · Linear · Todoist web · Donetick · TickTick · Skylight/Hearth + HA e-ink builds.

Key sources: myskylight.com · thequalityedit.com · ourcal.com (Cozi) · blog.sweepy.app · apartmenttherapy.com · todyapp.com · makeuseof.com · tidywell-app.com · github.com/donetick/donetick · xda-developers.com (Donetick, Grocy) · macstories.net · todoist.com help · help.ticktick.com · 925studios.co (Linear) · technologyreview.com/2022/05/10/1051954/chore-apps · nestifyapp.org · soldered.com · howtogeek.com
