# DIY Smart-Home Display & IoT Hardware Research — for a custom "House OS" (.NET + React)

> Rapport d'agent de recherche, 2026-08-23. Conservé verbatim (anglais). Note : les
> idées orientées enfants (KidsChores, station RFID pour enfants) sont ignorées pour
> House OS — pas d'enfants, jamais.

**Framing note that shapes everything below:** since the House OS is a custom server rather than Home Assistant (HA), the integration lingua franca to design around is **MQTT + plain HTTP**. Almost everything worth buying speaks one of these: Zigbee devices reach MQTT via **Zigbee2MQTT** (no HA required — standalone service + a coordinator dongle), ESPHome devices speak MQTT or a native API, openHASP panels are pure MQTT, TRMNL and Kindle dashboards just poll an image/JSON endpoint the server renders, and Fully Kiosk has a REST API. HA can be added alongside later as a device hub, but nothing on the shortlist forces it.

## 1. Wall dashboards / displays

### E-ink (battery-powered, "looks like a framed print", minutes-scale refresh, no touch)

- **TRMNL OG 7.5"** (usetrmnl.com) — commercial ESP32 e-ink dashboard, ~3 mo battery, 850+ plugins, **no subscription**, GPL3 firmware, self-hostable server ("BYOS"). $139–154 (TRMNL X 10.3": $219, up to ~6 mo battery). Trivial to use; custom plugins render from any JSON/webhook, or point the device entirely at your own server. Best buy-not-build e-ink option.
- **Inkplate 10** (soldered.com/products/inkplate-10) — 9.7" 1200×825 e-paper + ESP32 all-in-one, Arduino/MicroPython, 22 µA deep sleep, months on battery. ~€179. Medium difficulty; fully open — fetch a server-rendered PNG, display, sleep.
- **ESP32 + Waveshare 7.5" e-ink panel** — cheapest DIY (~$60–90 total), medium-high effort (wiring, ESPHome/Arduino config, enclosure). Good reference: github.com/chpeer/eink-calendar-display.
- **Jailbroken Kindle dashboard** — $20–40 used Paperwhite (eBay) + WinterBreak jailbreak; device polls a PNG your server renders (github.com/higoorc/kindle-dash). 2–4 weeks battery at 30-min refresh. Cheapest e-ink experiment, perfect fit for a custom server.
- **MagInkCal / MagInkDash** (github.com/speedyg0nz/MagInkCal) — the canonical "magic calendar" builds (Pi Zero/Pi + Inkplate 10 + Google Calendar + weather); derivatives ship **3D-printable frame STLs** (github.com/13Bytes/eInkCalendar, github.com/misch2/eink-portal-calendar). ~$150–250, medium difficulty, Python and fully hackable.

E-ink verdict: best family-friendliness per watt — always readable, no evening glow, battery-powered so placement is free. Ideal for a glanceable board (today's tasks, meals, weather, calendar), not for interaction.

### LCD / always-on (interactive, live, needs power)

- **Cheap Android tablet + Fully Kiosk** — refurb Fire HD 10 (~$50–60), Lenovo Tab M11, or Redmi Pad SE, wall-mounted, showing **the House OS React app** in kiosk mode. Lowest-effort touchscreen there is; Fully Kiosk's REST/JS API does wake-on-motion (via camera), screensaver, TTS, and caps charging at 80% for battery health. $50–150 + mount.
- **MagicMirror²** (magicmirror.builders) — Pi 4/5 + monitor (optionally behind two-way mirror glass); free, huge module ecosystem, fully local; custom Node modules trivially poll your API. ~$100–250 (recycled monitor helps; mirror glass is the pricey part). The *frame* is the real project — a good beginner-woodworker fit. Touch through mirror glass needs an IR touch frame or thin (≤4mm) acrylic.
- **DAKboard** — polished but cloud + subscription (~$5–10/mo) and limited custom data. Skip: the React app *is* DAKboard.
- **Skylight Calendar 15"** ($250–320 + $79/yr Plus) / **Hearth Display** — commercial family calendar + chore chart benchmarks; sync Google/iCloud/Outlook. Closed, no API — useful only as UX inspiration for what House OS replaces.
- **Raspberry Pi + official 7" touchscreen** (~$150) — Chromium kiosk on your React app; more effort than a tablet for the same result.

LCD verdict: the tablet is unbeatable for iteration speed — every House OS feature ships to the wall instantly. Tradeoffs: always-on power (cable run or wireless-charging dock) and evening glow (mitigated by motion-wake).

## 2. Physical interaction hardware

- **IKEA SOMRIG / RODRET Zigbee buttons** (~$8–10) and **Aqara Wireless Mini Switch** (~$18) — single/double/long press events via Zigbee2MQTT → MQTT. Trivial.
- **NFC tags** (NTAG215 stickers, ~$10 for 30, ~$0.30 each) — the classic "chore done" mechanic: tag on the recycling bin / plant pot / dishwasher / washing machine. Without HA, a tag can simply encode a deep link like `https://houseos.local/done/{taskId}` — zero firmware, works with any phone. (HA's tag_scanned event additionally reports which phone scanned, for per-person attribution.)
- **ESP32 touchscreen panels for openHASP** (openhasp.com): classic CYD ESP32-2432S028 ($5–10, resistive touch), **Guition JC4827W543 4.3" capacitive ESP32-S3 (~$12, officially supported — the sweet spot)**, WT32-SC01 Plus 3.5" (~$20–25, most documented), Guition JC3248W535 3.5" ($11–18), Sunton 8048S070 7" ($30–35 — PSRAM framebuffer/flicker gotchas, board-revision pinout traps). **openHASP = declarative JSON pages + pure MQTT both directions — made for non-HA backends.** Alternative: ESPHome + native LVGL (working example: github.com/DaradiciLevente/ESP32-8048S070c-ESPHOME-HOME-ASSISTANT-DASHBOARD).
- **M5Stack Dial** (~$27) — ESP32-S3 rotary knob + 1.28" round touchscreen + **RFID reader** + buzzer in a finished enclosure; ESPHome community component exists (github.com/SmartHome-yourself/m5-dial-for-esphome).
- A custom ESPHome button board (ESP32-C3 + mechanical switches + NeoPixel status LEDs, JLCPCB) is a very realistic v2 build given prior PCB experience.

Verdict: NFC tags + a couple of Zigbee buttons give the highest household-adoption per dollar.

## 3. Sensors & hub

**Hub layer (no HA needed):** run **Zigbee2MQTT + Mosquitto** in Docker on the House OS box; every Zigbee device becomes MQTT JSON the .NET backend subscribes to. Coordinator: **SMLIGHT SLZB-06** (~$40 — Ethernet/PoE so it sits centrally, stronger antenna, Thread-capable later; buy direct, Amazon listings overcharge) or budget **Sonoff ZBDongle-E** (~$20 USB, needs a firmware flash first).

Buy vs build per sensor category:
- **Temp/humidity: BUY** — Sonoff SNZB-02P ~$10 with 4-yr battery (ThirdReality ~$15 adds an LCD readout). DIY can't beat that price/battery.
- **Door/window: BUY** — Sonoff SNZB-04 / Aqara ~$8–12.
- **Water leak: BUY** — Sonoff SNZB-05P ~$10–16, ~5-yr battery (under sinks, water heater, washer). Aqara sells 3-packs.
- **Soil moisture (lawn/garden): BUILD** — ESP32 + capacitive probes < $10, multiple probes per board, deep-sleep/solar possible (SmartHomeScene DIY guide). Commercial Zigbee soil sensors report wildly inconsistent values across brands (HA community shootout); ThirdReality 3RSM0147Z is the least-bad buy option. Also a natural custom-JLCPCB-carrier-board project.
- **Air quality: EITHER** — Apollo AIR-1 ~$90–110 (ESPHome-firmware commercial device: SEN55 PM/VOC/NOx + optional SCD40 true CO2, fully local) or DIY ESP32 + **Sensirion SEN66** (~$45 — PM1–10, VOC, NOx, true CO2, temp, humidity in a single I2C module, 4 wires). The SEN66 build is a great first 3D-printed-enclosure project; AirGradient ONE (~$200) is the open-hardware premium pick.

## 4. Maker projects to steal from

- **How-To Geek interactive chore tracker** (howtogeek.com/home-assistant-interactive-chore-tracker/) — dashboard UX: per-person done buttons, approval flow.
- **nklein/chore-tracker** (GitHub) — Raspberry Pi + light-up arcade buttons; a button flashes when a chore is due, smack it when done. Cheap, joyful, printable-enclosure friendly.
- **MagInkCal derivatives** with printable frame STLs (§1) — ideal first Fusion 360 remix project.
- Ready-made firmware starting points: ESPHome-eInk-Boards YAMLs (WeatherBoard, TasksBoard) and the Sunton 7" LVGL dashboard repo (§2).

## Recommended shortlist

### First iteration (~$150–250 total, weeks not months)
**BUY:**
1. **Refurb 10" Android tablet + Fully Kiosk** (~$60–150) wall-mounted on the House OS React dashboard — fastest possible feedback loop; the wall display improves with every web-app deploy.
2. **Zigbee2MQTT + SLZB-06 coordinator** (~$40) + starter sensor pack: 2–3 Sonoff SNZB-02P temp/humidity, 2 leak sensors, 1–2 door sensors, 1–2 IKEA/Aqara buttons (~$60 total).
3. **NTAG215 NFC stickers** (~$10) encoding House OS deep links for "task done" check-offs — zero firmware.

**BUILD:** just the tablet's wall mount/frame — a good first 3D-print or small woodworking project.

### Second iteration (the fun one)
**BUILD:**
1. **Big e-ink family board** — Inkplate 10 (or TRMNL X if buying the display) polling a server-rendered PNG from House OS; printed bezel + wood frame designed in Fusion 360 (hits all three new skills: printer, Fusion, woodworking). Months of battery, MagInkCal-style.
2. **openHASP touch panels per zone** — Guition JC4827W543 (~$12) or WT32-SC01 Plus in printed wall enclosures, driven purely over MQTT by the .NET backend (pages defined in JSON, no HA dependency).
3. **DIY soil-moisture probes** (ESP32 + capacitive probes, custom JLCPCB carrier board) and an **SEN66 air-quality node** in a printed case.

**BUY (still):** all commodity Zigbee sensors — DIY only wins where products are weak (soil moisture) or where the build itself is the point.

**SKIP:** DAKboard and Skylight/Hearth (closed, subscription, redundant with House OS — but mine Skylight's chore-chart UX for ideas); reMarkable hacks (pricier and less documented than the Kindle route — a $30 jailbroken Kindle is the cheap e-ink experiment).

Key sources: usetrmnl.com · hometechhacker.com (TRMNL review) · soldered.com/products/inkplate-10 · github.com/higoorc/kindle-dash · terminalbytes.com (Kindle dashboard guide) · hackaday.com (2026 e-ink dashboards roundup) · github.com/speedyg0nz/MagInkCal + MagInkDash · magicmirror.builders · 3os.org (MagicMirror build) · joinhomeshift.com/home-assistant-tablet · the-optimization-guy.com (wall tablet guide) · openhasp.com · atomic14.com/esp32/boards (incl. 7-inch-displays comparison) · xda-developers.com (openHASP) · shop.m5stack.com (Dial ESPHome guide) · devices.esphome.io/devices/m5stack-dial · smarthomescene.com (Zigbee temp-sensor tests, coordinator picks, Apollo AIR-1 review, AirGradient ONE review, DIY soil sensor) · privacysmarthome.com (ZBDongle vs SLZB-06) · zigbeeguru.com (door/window sensors) · community.home-assistant.io (soil sensor shootout, NFC task-tracking blueprint) · howtogeek.com (NFC automations, chore tracker) · github.com/nklein/chore-tracker · github.com/DaradiciLevente/ESP32-8048S070c-ESPHOME-HOME-ASSISTANT-DASHBOARD · coffeelovingcardmakers.com + thequalityedit.com (Skylight/Hearth reviews)
