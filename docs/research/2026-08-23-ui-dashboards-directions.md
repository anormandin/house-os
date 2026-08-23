# UI Research Report — Home Dashboards & Calm Household Software (for House OS)

> Rapport d'agent de recherche, 2026-08-23. Conservé verbatim (anglais).

## 1. Wall/family dashboard layouts

**Commercial family displays (Skylight, Hearth, Cozyla).** The category leaders converge on a few patterns. Skylight is "designed first and foremost for visibility" — everything readable at a glance: a week-strip or day-column calendar color-coded **per person**, with chores/meals/lists as secondary panels, plus a split-screen mode (calendar + one companion panel like meals or chores). Its kid-friendly chore view uses **pictures + checkable rows** so even non-readers can use it — a good proxy for "zero-training-required" UI. Hearth's differentiator is that it "looks like a piece of decor rather than a piece of tech": warm materials, softer palette, drag-and-drop scheduling. The lesson: a wall display in a home must pass an *aesthetic* bar (would you frame it?), not just a functional one. Skylight's 2026 refresh (Calendar 2, between the 15" and the 27" Max) doubled down on per-person color coding as the core information scent.

**DAKboard / MagicMirror.** The classic community layout is **photos on the left third, calendar on the right two-thirds**, with light-weight modules (clock, weather) overlaid on the photo area. MagicMirror's whole model is edge-anchored regions (top-left/top-right/bottom bar) with a deliberately empty center — information lives at the periphery, which is literally the "calm technology" pattern. Both favor white-on-black or high-contrast text, no chrome, no cards.

**Home Assistant trends 2025–2026.** Strong convergence on: Mushroom cards (large touch targets, one icon + one line of text + state color), dark themes for wall tablets specifically because a bright screen in a living room is hostile ("visible from the couch while watching TV"), sections-based drag-and-drop grids, and — importantly for House OS — HA's own 2026.1 redesign went **mobile-first with "summary cards" at the top** (lights/climate/security roll-ups), then favorites, then rooms. That's the current state of the art for "home software": summary first, detail on demand, one screen, no tabs. 2026.2 retired the colored top app bar for a flat, quiet chrome so "cards and data take center stage."

**E-ink (TRMNL, MagInkCal).** TRMNL's design system is the best documented glanceable-layout reference around: 800×480 1-bit displays, a component set of **Title / Value / Label / Description / Divider / Item / Table / Progress**, zero-config column layouts, and **pixel fonts locked to specific sizes** so nothing anti-aliases. The deep lesson for House OS: their hierarchy is essentially *one huge Value, one Label, maybe a list of Items* per panel — glanceable design means one number/word dominates each region. Their JS utilities (truncation, number abbreviation) exist because **overflow is the enemy of glanceability**. If House OS keeps its "today" data model renderable through that kind of component grammar, the future e-ink target is nearly free.

**Type size at distance (digital signage rules).** Minimum text height ≈ viewing distance (feet) × 0.007 in; comfortable ≈ ×0.010–0.014. Practically: a wall tablet read from **2–3 m needs ~20–30 pt minimum for secondary text** and much larger for the primary line; sans-serif, high contrast, max ~3–5 short lines per zone. A phone layout scaled up will *not* work on the wall — the wall view needs its own density tier (roughly: primary info 2×, secondary info culled, tap targets grown).

## 2. "Today view" design in acclaimed apps

**Things 3 — the gold standard, decomposed.** From MacStories' review and design teardowns, the calm comes from identifiable mechanics, all portable to Tailwind:
- **Dominant whitespace, restrained color**: white/near-white ground; color appears only as small "thoughtful splashes" — the blue Today star, yellow evening icon, red deadline text. Never colored card backgrounds.
- **Tasks are plain text until touched** (progressive disclosure): a row is checkbox + title, nothing else. Tap → the row expands into a **card** while the rest of the list fades back; notes, checklist, tags, deadline live *inside* the card surface, not in menus. Metadata is invisible until relevant.
- **Hierarchy by weight, not size ladders**: Areas bold, projects lighter beneath them; one type family, few sizes, weight does the work.
- **Section grouping in Today**: a single screen segmented by subtle headers — most famously **"This Evening"** — separating "anytime today" from "tonight" without another screen or tab. This maps perfectly onto a household's day (école/boulot vs. soir).
- **The Magic Plus button**: a single draggable FAB; drag it into the list to insert a task *at that position*. Purposeful animation + haptics make completing/creating feel physical.
- **No badges/urgency theater by default.** The app never shouts; overdue is a quiet red date, not a red screen.

**Fantastical**: dual-paradigm view (compact month grid + scrollable agenda side by side) so you never lose "where in the month am I" while reading "what's next"; **natural-language input** ("souper chez maman jeudi 18 h") as the friction-killer for event entry; color-coding per calendar kept subtle (dots and thin bars, not filled blocks). Won an Apple Design Award; its widgets are praised for doing "one glanceable thing per size."

**Structured**: vertical **timeline of the day** with a "now" line, each task a rounded card with icon + duration on a single spine. Its appeal is *seeing the shape of the day*; its cost is that it demands durations/times for everything — too much ceremony for chores. Good to steal: the now-line and "up next" emphasis; bad to steal: mandatory scheduling.

**Apple widget/HIG doctrine**: a widget is "a glanceable compact summary" — strong single idea, communicate via content not chrome, hierarchy via **one type family + weight**, 17 pt body as the legibility floor, deference (UI never competes with content). The widget test is a great forcing function for House OS panels: *could this panel be an iOS medium widget?* If not, it's too dense.

## 3. Aesthetics for domestic software

- **Warm minimalism** (2025–26 mainstream): minimal layouts + warm neutrals, generous radius, soft depth, one bold accent, "playfulness and unique character" layered on minimalist bones. This is the Hearth position — tech that reads as decor. Best default fit for a couple's shared tool.
- **Calm tech** (Amber Case / Weiser lineage, now a certification movement): technology should require the smallest amount of attention; inform via the periphery; work even when it fails (degrade to a static list). MagicMirror's edge-anchored emptiness and TRMNL's whole pitch are this. Design implication: **status over interaction** — the default state of House OS should answer "is everything fine chez nous?" without a single tap.
- **"Paper" interfaces**: paper-white/cream grounds, ink-dark text, hairline rules, serif or humanist headings — the Things/e-ink-adjacent look. Ages beautifully onto e-ink (it *is* the e-ink aesthetic on LCD). Risk: can feel austere in dark mode if done lazily (paper's whole identity is light).
- **Neo-brutalism**: hard borders, offset shadows, loud fills. Trend-current and genuinely accessible (high contrast), but its energy is *loud* — it reads as a Gen-Z consumer-brand voice, wrong for something a couple looks at at 7 h before coffee. Verdict: steal its **chunky tap targets and unmissable contrast**, skip the aesthetic.
- For couple vs. power-tool: chore-app research (Sweepy, Tody) shows two personalities — Sweepy's **gamified fairness** (points, leaderboard, per-room plans) vs. Tody's **quiet maintenance meter** ("dirtiness" grows over time; no due dates, just visual urgency). Reviewers consistently describe Tody's model as the better fit for couples who want "a quiet maintenance tracker" — deadline-free urgency indicators avoid the nagging-spouse dynamic. Strong argument for House OS chores: **freshness meters, not overdue alarms**.

## 4. French / French-Canadian conventions

- **Tu vs vous**: modern consumer apps in French lean **tutoiement** (younger brands, apps, startups — it signals proximity); institutions and formal sectors keep **vouvoiement**. For a private two-person household app, this question mostly dissolves: the best-practice register is **tutoiement or, better, impersonal/imperative-free labels** ("À faire ce soir", "Tout est beau ✓") — the app speaks *about the house*, not *at a user*. Where a verb is needed, tutoiement is natural between the two of you; vouvoiement would feel absurd in your own kitchen.
- **Dates**: full dates as "samedi 23 août" (no capital on day/month, no comma needed in short forms); all-numeric dates should be **ISO 8601 (2026-08-23)** — the official Canadian/Quebec standard — never 08/23.
- **Time**: the OQLF norm is the **24-hour clock with "h" as separator: "19 h 30", "9 h"** (space around the h, no leading zero on hours in running text); colon notation (19:30) is acceptable in technical/tabular contexts. An AM/PM clock in a Québécois home app reads as an anglicism.
- Other details: temperature "21 °C" (space before °C), first-word-only capitalization in labels ("Liste d'épicerie" not "Liste D'Épicerie"), non-breaking space before % and units. Getting these right is a big part of feeling *native* rather than translated.

## 5. PWA mobile ergonomics (2025–2026)

- **Bottom tab bar is the settled gold standard** for 3–5 sections; ~49 % of users drive their phone with one thumb, and the bottom third (bottom-center especially) is the safe zone. Icons + short labels beat icons alone.
- **FAB**: still current when there's one dominant create action — anchored bottom-right (or bottom-center). The consensus pattern is **tab bar + FAB together** when "add" is frequent. Things' Magic Plus is the refined version. For House OS: tab bar of 3–4 (Aujourd'hui / Tâches / Maison / Épicerie?) + one FAB for "ajouter".
- **Touch targets**: ≥ 44–48 px, ≥ 8 px gaps.
- **Pull-to-refresh**: control it explicitly — `overscroll-behavior-y: contain` on body to kill the browser's accidental reload, then implement your own if the mental model is "fetch what my partner just added" (it is, for a shared app — a visible refresh affordance builds trust in sync).
- **iOS standalone PWA**: must handle `safe-area-inset-*` (Dynamic Island top, home-indicator bottom — pad the tab bar with `env(safe-area-inset-bottom)`), `display: standalone`, and `prefers-color-scheme` for automatic dark mode. Dark mode conventions 2026: dark is expected to be a *first-class* theme, not inverted light — which the Kanagawa Wave/Lotus pair already satisfies elegantly.

---

## Synthesis — three art directions for House OS

### Direction A — « Papier d'encre » (ink-on-paper, Things-3 lineage)
**Mood**: a beautiful paper agenda that happens to be alive. Quiet, literary, Muji-esque.
**Typography**: one humanist sans (or sans body + subtle serif for the date masthead, e.g. Inter/Public Sans + Lora). Hierarchy by **weight, not size**: 3 sizes total on mobile. Big day header ("samedi 23 août") as the only large text.
**Color**: Lotus paper (#faf7ed ground, #545464 ink) as the *primary* identity; Wave dark as the night companion. Color used only as Things-style "splashes": crystal-blue for today, yellow #e6c384 for "ce soir", muted red only on true urgency. No colored card backgrounds — **hairline dividers and whitespace instead of cards**.
**Density**: airy. One column, sections "Aujourd'hui / Ce soir / Cette semaine", tasks as bare checkbox + text rows that expand in place (progressive disclosure).
**Signature elements**: the "Ce soir" section split; freshness dots (Tody-style) instead of due dates for chores; a satisfying check animation; date masthead in French with proper typography (19 h 30, 23 août).
- (a) **Wife-appeal**: excellent — reads as a lovely shared agenda, zero app-ness, zero nagging. Lowest cognitive tax of the three.
- (b) **Wall glanceability**: good *if* the wall tier scales type aggressively; whisper-quiet hierarchy needs deliberate 2× sizing at distance. Translates to **e-ink almost 1:1** — best future-proofing of the three.
- (c) **Tailwind/shadcn**: very feasible; it's mostly *removing* shadcn chrome (borders → hairlines, cards → spacing). Main work is typographic discipline, not components.

### Direction B — « Tableau de bord Kanagawa » (calm command center, HA/Mushroom lineage)
**Mood**: the elegant dark control panel — sumi-ink night sky with lantern-glow accents. Tech-forward but hushed.
**Typography**: geometric-humanist sans (e.g. Figtree/Manrope), tabular figures for times/values. TRMNL-style grammar per panel: **one big Value, one small Label**.
**Color**: Wave dark (#1f1f28) as primary identity; each *domain* gets one Kanagawa accent (tâches = crystal blue #7e9cd8, entretien = aqua #7aa89f, épicerie = green #98bb6c, alertes = orange #ffa066) used as icon/left-edge tint on #2a2a37 surfaces — never full-bleed fills. Person color-coding (you vs. her) as small avatar dots, Skylight-style.
**Density**: medium-high; a **summary-cards row on top** (HA 2026.1 pattern: "3 tâches aujourd'hui · poubelles ce soir · tout est beau"), then a sections grid of rounded cards with large touch targets.
**Signature elements**: the top "état de la maison" summary strip; big-Value stat tiles; a now-line "up next" ribbon; on the wall, the same grid with center breathing room and edge-anchored clock/weather.
- (a) **Wife-appeal**: decent but riskier — card grids read as "an app to manage", and dark-first can feel like *his* terminal aesthetic rather than *our* home tool. Needs the Lotus light mode to be genuinely first-class on phone.
- (b) **Wall glanceability**: the best of the three — dark ground is the proven choice for living-room tablets (doesn't glow at movie time), the Value/Label grammar is built for distance, and it *will* look spectacular.
- (c) **Tailwind/shadcn**: most natural fit — it's cards, grids, and stat tiles, exactly what shadcn ships. Fastest to build well.

### Direction C — « Cuisine chaleureuse » (warm-domestic, Hearth lineage)
**Mood**: fridge-door-with-good-taste — warm, rounded, slightly playful; software as decor.
**Typography**: rounded or soft-humanist family (e.g. Nunito Sans; or a display like Fraunces for headers over a clean sans body). Bigger radii (rounded-2xl), pill chips, friendly icon set (Phosphor duotone).
**Color**: Lotus warm cream #f2ecbc/#faf7ed pushed warmer as primary; accents used more generously than A — soft green fills for "fait ✓", yellow chips for "ce soir", per-person pastel avatars. Dark mode = Wave but with warmed surfaces.
**Density**: low-medium; chunky rows (56–64 px), one hero panel for "aujourd'hui" with an oversized friendly headline ("Tout est beau 🌿" / "2 choses avant dodo"), photo or illustration moment allowed (DAKboard's photo-third, on the wall).
**Signature elements**: the mood headline that summarizes house state in a sentence; gentle celebratory micro-animation on completing the day; Sweepy-style fairness view ("cette semaine : toi 6 · moi 5") framed as balance, not leaderboard.
- (a) **Wife-appeal**: strongest emotional pull — it's the direction explicitly engineered (by Hearth, Skylight) to be loved by the non-technical partner and to feel like *ours*. Slight risk of cutesiness aging poorly.
- (b) **Wall glanceability**: good for the headline/hero pattern; weaker for dense weeks — and a bright cream wall screen at night needs an auto-dim/dark schedule. E-ink translation loses the warmth (color *is* this direction's identity).
- (c) **Tailwind/shadcn**: feasible; more custom CSS than A/B (illustrations, chips, celebratory motion), and the most design-taste-dependent — easiest to do badly.

**Recommendation**: A hybrid of **A on the phone, B's grammar on the wall** is the sweet spot: Papier d'encre's calm list-first Today view (Lotus light default, Wave dark) for daily thumb use, with the data model shaped into TRMNL-style Value/Label panels so the wall tablet (dark, big type, summary-first) and eventual e-ink display are re-skins, not rebuilds. Fold in C's single signature move — the French mood headline ("Tout est beau") — as the shared brand across all three surfaces.

## Sources

(voir le rapport original pour la liste complète des ~40 URLs : thequalityedit.com, techcrunch.com Skylight Calendar 2, joinhomeshift.com, home-assistant.io releases 2026.1/2026.2, forum.magicmirror.builders, trmnl.com framework/design-system/typography, macstories.net Things 3 & Fantastical, culturedcode.com, developer.apple.com HIG & WWDC20 widgets, kanso.framer.media, index.dev, tidywell-app.com, vitrinelinguistique.oqlf.gouv.qc.ca dates/heures, canada.ca norme ISO 8601, designstudiouiux.com, junoschool.org, web.dev/learn/pwa, digitalsignage.com typography-viewing-distance, extron.com, screencloud.com)

---

# ADDENDUM v2 (desktop-first + e-ink) — remplace les sections mobile

> Le rapport v1 ci-dessus visait mobile-first ; la cible réelle est desktop d'abord +
> vue e-ink distincte. Sections nouvelles du rapport v2 (le reste est inchangé) :

## 5a. Desktop web app layout conventions for calm/home software

- **Sidebar vs top nav**: ≤5 destinations → top bar; 5+ or hierarchical → fixed left
  sidebar (eye-tracking: 20-30% faster time-to-target; strongest "sense of place").
  Calm exemplars (Things Mac, Notion, Linear) are all sidebar + content. House OS →
  **narrow, collapsible sidebar** (shadcn Sidebar component is this pattern).
- **Multi-pane**: list + detail side by side (Things Mac; Fantastical agenda+month).
  Detail appears in place or as a third pane, never a modal wall. Content column
  **readable-width ~60-75ch, centered** — full-bleed 1440px lists read as admin panels.
- **Density**: calm desktop ≠ dense desktop. One comfortable column for Today;
  density reserved for week/planning view. HA summary-first strip on top.
- **Keyboard**: ⌘K command palette (shadcn ships cmdk), global quick-add shortcut,
  single-key nav, natural-language entry as deluxe ("⌘K, 'acheter lait', Enter").

## 5b. E-ink design constraints

- **Rendering model**: server renders fixed-size HTML → headless Chrome screenshot →
  1-bit conversion → push to panel (MagInkCal/MagInkDash pattern). The e-ink view is
  just another route rendered at device resolution (800×480 / 1200×825).
- **Color/contrast**: pure black-on-white first; grays only via dithering (muddy) —
  solid black text, no gray below ~16px, no shadows/gradients. Inverted zones =
  the one "color"; use sparingly (ghosting).
- **Refresh**: full refresh ~1-2s with flash; partials accumulate ghosting (full every
  5-10 partials). Sane cadence: full image replacement every 15-60 min or on change.
  No animation, no live affordances — a printed page that reprints itself.
- **Typography**: regular-to-bold only (thin strokes break at 1-bit); 2-3 strong
  sizes + weight, one family; tabular figures; disable font smoothing at screenshot.
- **Grids**: 2-4 rectangular zones with 1-2px hairlines (borders are free on e-ink);
  each zone = small-caps Label + huge Value, or ≤5-row Item list, truncated.
  **Cull, don't shrink.**

## Per-direction e-ink treatments (v2)

- **A Papier d'encre → e-ink ~1:1**: left ⅔ day list (masthead, ≤6 items, "Ce soir"
  after a hairline), right ⅓ stacked Label/Value zones; one inverted band max (date
  masthead). Desktop and wall are the same object in two materials.
- **B Kanagawa → grid re-materialization**: 2×2/2×3 zones, Value/Label grammar,
  accents become weight/inversion. Structurally strong, loses the color identity.
- **C Cuisine chaleureuse → headline survives**: Fraunces mood band at top, rounded
  zone borders, line-art icons. Loses the most (color is its identity).

**Recommendation v2**: desktop+e-ink strengthens **A as base** — the only direction
where the wall display is the same design in its native material. Fold in B's summary
strip and C's mood headline as shared brand. Build order: shared today-model →
desktop templates → `/eink` route at 800×480 screenshot by headless Chrome.
