---
id: canon-sync-2026-07-04
title: GLMZ Canon Synchronization Survey
date: 2026-07-04
status: completed
categories: [date, vocabulary, rule, geography, technology, social, faction, biology, name]
questions: 34
---

# GLMZ Canon Synchronization Survey {#SS-SURVEY-2026-07-04}

> Generated from a contradiction-discovery workflow that extracted 753 factual claims from
> `docs/BIBLE.md`, `docs/GLMZ_SETTING.md`, `docs/universes/GLMZ.md`, story node bibles,
> and DB entity descriptions. 34 contradictions across 9 categories.
>
> **Instructions:** mark the canonical answer with `[x]`. One answer per question unless
> marked `[multi-select]`. Write free text on the blank after option C/D when choosing Custom.
> After answering, run: `ss --survey apply docs/surveys/canon-sync-2026-07-04.md`

---

## DATE / TIMELINE

### Q-001 · date · Blue Massacre year
> Which year did the Blue Massacre occur?
> Context: GLMZ.md dates the event to 2065 when ArcSec destroyed city police; GLMZ_SETTING.md
> places it in 2096 across all 23 precincts. The year anchors the entire GLMZ power-vacuum timeline.

- [ ] **A** — 2065 — ArcSec destroyed the city police *(source: GLMZ.md)*
- [x] **B** — 2096 — across all 23 GLMZ police precincts *(source: GLMZ_SETTING.md)*
- [ ] **C** — Custom: _____________________

---

### Q-002 · date · Story present year
> Is the GLMZ story present year 2226 (canonical docs) or 2200 (DB cyberware entries)?
> Context: BIBLE.md and all node bibles fix the present at 2226 per SS-A28; multiple
> cyberware DB entries use 2200 as their current-year anchor. This affects how far any
> historical reference is from "now."

- [x] **A** — 2226 — canonical per SS-A28 *(source: BIBLE.md)*
- [ ] **B** — 2200 — used as present in DB cyberware entries *(source: DB:Cyberware)*
- [ ] **C** — Custom: _____________________

---

### Q-003 · date · E.L.F. emergence vs 2187 Rights Act
> Did E.L.F.s exist before 2187, or did they emerge later in the 2210s?
> Context: GLMZ.md places E.L.F. emergence in the 2210s; a DB cyberware entry records a
> 2187 Cognitive Rights Expansion Act granting E.L.F. personhood — 23+ years before they
> allegedly existed.

- [ ] **A** — E.L.F.s emerged in the 2210s; the 2187 Act covered something else *(source: GLMZ.md)*
- [ ] **B** — E.L.F.s existed by 2187; emergence predates what GLMZ.md states *(source: DB:Cyberware)*
- [ ] **C** — Both — context-dependent
- [x] **D** — Custom: E.L.F.s are alive but they don't have personhood, AIs have personhood

---

## VOCABULARY

### Q-004 · vocabulary · NSB operator job title
> Is the in-world term for an NSB operator "Rider" or "Exo / RFO"?
> Context: DB:Technology defines "Rider" as the current GLMZ freelance term for NSB
> operators; SS-A38 retired Rider in favor of Exo (street) and RFO (formal/corpo). Affects
> every prose line and entity description referencing NSB pilots.

- [ ] **A** — Rider — current GLMZ freelance term *(source: DB:Technology)*
- [ ] **B** — Exo (street) / RFO (formal) — Rider is retired per SS-A38 *(source: nodes/PNHL.md)*
- [ ] **C** — Both — Rider as legacy/historical slang, Exo/RFO as current
- [x] **D** — Custom: Exo is the term

---

### Q-005 · vocabulary · Rider in active prose beats
> Should enabled prose beats using capitalized "Rider" as a job title be corrected to Exo/RFO?
> Context: Four enabled prose beats (019EC96D, DB888A00, CF0F5987, 5E05F5F1) use "Rider"
> as an active NSB job title; SS-A38 retired the term. DB:Automaton Black Ice description
> also uses "the Rider" as present-tense vocabulary.

- [x] **A** — Yes — replace Rider with Exo/RFO per SS-A38 *(source: nodes/PNHL.md)*
- [ ] **B** — No — keep Rider in those beats as valid in-world usage *(source: DB:Prose)*
- [ ] **C** — Custom: _____________________

---

### Q-006 · vocabulary · ANGEL buoyancy mechanism name
> Is the aerobloc lift mechanism called "ANGEL buoyancy" or "vacuum-cell buoyancy"?
> Context: GLMZ.md uses "ANGEL buoyancy"; nodes/PNHL.md uses "vacuum-cell buoyancy" for
> the same floating city-block lift system. Could be brand name vs. technical description,
> or one source needs updating.

- [ ] **A** — ANGEL buoyancy *(source: GLMZ.md)*
- [x] **B** — Vacuum-cell buoyancy *(source: nodes/PNHL.md)*
- [ ] **C** — Both — ANGEL is the brand; vacuum-cell is the mechanism type
- [ ] **D** — Custom: _____________________

---

### Q-007 · vocabulary · Armed Gray Zone contractor job title
> Is the in-world job title for armed Gray Zone contract runners "Exo" or "street samurai"?
> Context: GLMZ.md establishes Exo as the canonical in-world street term; several DB
> character entries use "street samurai" as an active job title. Active prose confirms
> "street samurai" appears zero times in enabled beats.

- [ ] **A** — Exo — canonical in-world street-level freelancer term *(source: GLMZ.md)*
- [ ] **B** — Street samurai — used in active DB character descriptions *(source: DB:Character)*
- [ ] **C** — Both — "street samurai" is out-of-universe genre label; Exo is in-world
- [x] **D** — Custom: Kyle is a "street samurai" its something he's called; an Exo is an operator that can connect to robots to pilot; the name for runners is operator/freelancer

---

### Q-008 · vocabulary · Anti-grav in four transportation entities
> Should the four Transportation entities still using "anti-grav" be updated to reflect SS-A35?
> Context: SS-A35 retired "anti-grav" and active prose has zero uses, but Grav-Board, SN
> Tempest Air Racer, SN Kestrel Hover-Bike, and Axiom Executive Helicopter descriptions still
> reference it.

- [ ] **A** — Yes — update all four entities to remove anti-grav per SS-A35 *(source: DB:Prose)*
- [ ] **B** — No — keep anti-grav in entity descriptions as acceptable legacy text *(source: DB:Transportation)*
- [x] **C** — Custom: No, there is no antigrav technology; if it suspended in air its due to ANGEL
  cells (*terminology update 2026-07-06: "VacCell"/"VABC" retired in favor of the branded ANGEL
  cell. Superseded again 2026-07-07: ANGEL itself is retired — Eigenlift (Coherent Mass-State
  Suspension), CEP's mass-branch sibling technology, is now the one and only aerostatic
  technology at every scale — see [[../universes/GLMZ]] Aerostatic Architecture*)

---

### Q-009 · vocabulary · E.L.F. sentience vocabulary
> Are E.L.F.s sentient (BIBLE.md) or pre-sentient synthetic life (DB:Faction)?
> Context: BIBLE.md classifies E.L.F. as a "sentient Species"; DB:Faction (The Orphanage)
> calls them "pre-sentient synthetic life below the Vasquez-Obi personhood threshold."
> The distinction determines whether E.L.F.s can hold rights or contracts in-story.

- [ ] **A** — Sentient — E.L.F. is a sentient Species *(source: BIBLE.md)*
- [ ] **B** — Pre-sentient — below the Vasquez-Obi personhood threshold *(source: DB:Faction)*
- [x] **C** — Both — tiered: some sentient, some pre-sentient
- [ ] **D** — Custom: _____________________

---

### Q-010 · vocabulary · "Scav" capitalization
> Is "scav" always lowercase or capitalized as "Scav / Scavs"?
> Context: GLMZ.md explicitly mandates the lowercase form "scav"; GLMZ_SETTING.md treats
> it as a proper noun and uses "Scavs" throughout. A single convention must apply consistently.

- [x] **A** — Always lowercase — scav *(source: GLMZ.md)*
- [ ] **B** — Capitalized proper noun — Scav / Scavs *(source: GLMZ_SETTING.md)*
- [ ] **C** — Custom: _____________________

---

### Q-011 · vocabulary · In-world currency symbol
> Should in-world monetary amounts use Φ (QUANTA) or € (euro sign)?
> Context: BIBLE.md establishes Φ as the only recognized QUANTA currency symbol; a DB
> character description uses "€120 UBC bonus" with the euro sign. All in-world monetary
> values must use the same symbol.

- [x] **A** — Φ — the canonical QUANTA currency symbol *(source: BIBLE.md)*
- [ ] **B** — € — as used in the character DB entry *(source: DB:Character)*
- [ ] **C** — Custom: _____________________

---

## RULES

### Q-012 · rule · E.L.F. legal personhood status
> Do E.L.F.s have recognized legal personhood or are they animal-grade with no personhood?
> Context: nodes/SPRW.md classifies Tier 2 E.L.F.s as animal-grade (Tier 3 = Synthetic
> Persons with personhood); DB:Cyberware states E.L.F.s achieved recognized personhood
> under the 2187 Cognitive Rights Expansion Act.

- [x] **A** — Tier 2 animal-grade — no personhood; Tier 3 Synthetic Persons have it *(source: nodes/SPRW.md)*
- [ ] **B** — Recognized personhood under the 2187 Cognitive Rights Expansion Act *(source: DB:Cyberware)*
- [ ] **C** — Both — tiered: some have personhood, others do not
- [ ] **D** — Custom: _____________________

---

### Q-013 · rule · Default QA methodology
> Is the default QA gate a Legion panel vote (≥82%) or a logic sweep?
> Context: BIBLE.md's prose workflow step 4 prescribes a Legion panel vote targeting ≥82%
> before the next chapter; SS-LAW-17 in the same file states the default QA is a logic
> sweep, not a vote panel. The conflict is internal to BIBLE.md.

- [ ] **A** — Legion panel vote — target ≥82% before next chapter *(source: BIBLE.md §workflow)*
- [x] **B** — Logic sweep — SS-LAW-17 / SS-A44 overrides as default *(source: BIBLE.md §SS-LAW-17)*
- [ ] **C** — Custom: _____________________

---

## GEOGRAPHY

### Q-014 · geography · GLMZ total population
> Is the GLMZ total population approximately 58 million or 40 million?
> Context: GLMZ.md gives approximately 58 million; GLMZ_SETTING.md gives approximately
> 40 million. Population scale affects how dense the city feels in prose and how entity
> counts and factions are described.

- [x] **A** — ~58 million *(source: GLMZ.md)*
- [ ] **B** — ~40 million *(source: GLMZ_SETTING.md)*
- [ ] **C** — Custom: _____________________

---

### Q-015 · geography · Z∞ nature and name
> Is Z∞ the underwater "Bathysphere" or the above-ground ungoverned zone "The Null"?
> Context: GLMZ.md defines Z∞ as the Bathysphere — submerged ruins below the Lake with
> one sealed black site; GLMZ_SETTING.md defines Z∞ as "The Null," an above-ground
> ungoverned area where scavs operate.

- [ ] **A** — The Bathysphere — submerged ruins, one sealed black site *(source: GLMZ.md)*
- [ ] **B** — The Null — above-ground ungoverned zone where scavs operate *(source: GLMZ_SETTING.md)*
- [x] **C** — Custom: There are no "Zones" things are just called w/e they are called

---

### Q-016 · geography · "The Spine" definition
> Is "The Spine" the western lakeshore corridor (Chicago–Milwaukee–Green Bay) or zone Z2?
> Context: GLMZ.md defines The Spine as the multi-city western lakeshore corridor;
> GLMZ_SETTING.md applies the name to zone Z2, a single mid-tier commercial district
> described as Kyle's operating base.

- [x] **A** — The western lakeshore corridor: Chicago → Milwaukee → Green Bay *(source: GLMZ.md)*
- [ ] **B** — Zone Z2 — mid-tier commercial district, Kyle's operating base *(source: GLMZ_SETTING.md)*
- [ ] **C** — Custom: _____________________

---

### Q-017 · geography · Permanent underwater communities
> Are permanent underwater communities prohibited, or does the Abyssal Threshold exist?
> Context: GLMZ.md and SS-A42 (nodes/SPRW.md) prohibit permanent lakebed communities;
> DB:Place contains the Abyssal Threshold as a city 420 feet below Lake Superior. The
> two cannot both be true without reconciliation.

- [x] **A** — Prohibited — lakebed holds only ruins and one black site; no communities *(source: GLMZ.md)*
- [ ] **B** — The Abyssal Threshold exists as a city under Lake Superior *(source: DB:Place)*
- [ ] **C** — Custom: _____________________

---

## TECHNOLOGY

### Q-018 · technology · Pulse pod form
> Does a Pulse pod hold one passenger per sealed sphere, or are they shared cylindrical carriages?
> Context: GLMZ.md and nodes/PNHL.md describe individual sealed spheres with one passenger
> each; GLMZ_SETTING.md describes shared cylindrical "slug" carriages with Berth and Bench
> Transit classes implying multiple passengers.

- [x] **A** — Individual sealed sphere — one passenger per pod *(source: GLMZ.md)*
- [ ] **B** — Shared cylindrical "slug" carriages with Berth and Bench Transit classes *(source: GLMZ_SETTING.md)*
- [ ] **C** — Custom: _____________________

---

### Q-019 · technology · Pulse pod interior experience
> Is the Pulse pod's train-like interior a neuretic hallucination or physical reality?
> Context: GLMZ.md and nodes/PNHL.md state the train interior is a shared neuretic
> hallucination projected over a sealed sphere; GLMZ_SETTING.md presents rectangular
> interiors as physical fact with no hallucination mentioned.

- [x] **A** — Neuretic hallucination — actual pod is a sealed sphere; interior is projected *(source: GLMZ.md)*
- [ ] **B** — Physical reality — rectangular interior, spherical exterior *(source: GLMZ_SETTING.md)*
- [ ] **C** — Custom: _____________________

---

### Q-020 · technology · "The Ongoing" nanotech characterization
> Is "The Ongoing" continuous molecular warfare or a designed utility system?
> Context: GLMZ.md characterizes The Ongoing as continuous molecular warfare between
> competing nano-organisms inside the Substrate; GLMZ_SETTING.md describes it as an
> active designed system used for drug delivery and environmental monitoring.

- [x] **A** — Continuous molecular warfare between competing nano-organisms *(source: GLMZ.md)*
- [ ] **B** — Active designed system for drug delivery and environmental monitoring *(source: GLMZ_SETTING.md)*
- [ ] **C** — Both — warfare is the nature; utility applications are a byproduct
- [ ] **D** — Custom: _____________________

---

### Q-021 · technology · Substrate nanotech activity level
> Is Substrate nanotech active (remediation, monitoring, medical response) or passive (scaffolding only)?
> Context: GLMZ.md describes the Substrate actively providing atmospheric remediation,
> structural monitoring, and basic medical response city-wide. GLMZ_SETTING.md limits it
> to passive structural scaffolding and wound closure.

- [ ] **A** — Active: atmospheric remediation, structural monitoring, and medical response *(source: GLMZ.md)*
- [ ] **B** — Passive and structural: medical scaffolding and wound closure only *(source: GLMZ_SETTING.md)*
- [x] **C** — Both — context-dependent
- [ ] **D** — Custom: _____________________

---

### Q-022 · technology · Lure frequency
> Is the Lure a single fixed frequency (19 Hz) or a 17–19 Hz range?
> Context: GLMZ.md pins the Lure at exactly 19 Hz; GLMZ_SETTING.md and BCODA.md describe
> a 17–19 Hz range with in-prose readings of 18.7 Hz (Ch5) and 18.9 Hz (Ch6) that are not 19 Hz.

- [ ] **A** — Single fixed frequency: 19 Hz *(source: GLMZ.md)*
- [ ] **B** — A 17–19 Hz range (18.7 Hz and 18.9 Hz observed in BCODA) *(source: GLMZ_SETTING.md / nodes/BCODA.md)*
- [ ] **C** — Both — context-dependent
- [x] **D** — Custom: 5D is 19 Hz, things inside 5D have a range (19.415 and 19.771 hz)

---

### Q-023 · technology · ANGEL unit size range
> Are ANGEL units universally 8–20 cm, or does a platform-scale variant (1.4m+) also exist?
> Context: GLMZ.md describes ANGEL units as 8–20 cm with no other sizes mentioned.
> DB:Technology adds a platform-scale tier starting at 1.4m, treating 8–20 cm as the
> personal Axis Rig scale only.

- [ ] **A** — 8–20 cm only — no larger platform-scale variant *(source: GLMZ.md)*
- [ ] **B** — Two scales: personal 8–20 cm (Axis Rig) and platform-scale min 1.4m *(source: DB:Technology)*
- [x] **C** — Custom: They can be built at multiple sizes

---

## SOCIAL / TIERS

### Q-024 · social · GLMZ tier count
> How many social/citizenship tiers does GLMZ have — four (Tier 1–4) or seven?
> Context: DB:Character and GLMZ.md consistently describe a Tier 1–4 system. DB:Faction
> (Franchise Compact) names seven legally distinct citizenship tiers since 2171.

- [ ] **A** — Four tiers: Tier 1 through Tier 4 *(source: DB:Character / GLMZ.md)*
- [ ] **B** — Seven legally distinct citizenship tiers *(source: DB:Faction)*
- [ ] **C** — Both — context-dependent
- [x] **D** — Custom: 5 tiers, but 5+ might have its own sub versions for the 1% of the 1%

---

### Q-025 · social · Tier 5 existence
> Does Tier 5 exist as a distinct tier, or does the scale cap at Tier 4?
> Context: GLMZ.md and DB:Character cap the scale at Tier 4; DB:Transportation (Meridian
> Monorail connects Tier 3–5) and DB:Faction both reference Tier 5 as a real access level
> with distinct infrastructure.

- [ ] **A** — Scale caps at Tier 4 — no Tier 5 *(source: GLMZ.md / DB:Character)*
- [x] **B** — Tier 5 exists as a distinct tier above Tier 4 *(source: DB:Transportation / DB:Faction)*
- [ ] **C** — Custom: _____________________

---

## FACTIONS

### Q-026 · faction · ArcSec operational scope
> Does ArcSec operate only in Tier 4 zones, or city-wide as a civil security monopoly?
> Context: GLMZ.md restricts ArcSec to Tier 4 zones; GLMZ_SETTING.md says ArcSec
> displaced the city police entirely via the Blue Massacre and holds a city-wide monopoly.

- [ ] **A** — Tier 4 zones only *(source: GLMZ.md)*
- [x] **B** — City-wide civil security monopoly — replaced the police entirely *(source: GLMZ_SETTING.md)*
- [x] **C** — Both — context-dependent
- [x] **D** — Custom: They police where they are paid to police

---

### Q-027 · faction · ArcSec organizational classification
> Is ArcSec a private security contractor or a civil law-enforcement monopoly?
> Context: GLMZ.md labels ArcSec a private security contractor; GLMZ_SETTING.md classifies
> it as simultaneously a law enforcement agency and a for-profit corporation after the
> Blue Massacre.

- [ ] **A** — Private security contractor *(source: GLMZ.md)*
- [x] **B** — Civil security monopoly — law enforcement and for-profit corporation *(source: GLMZ_SETTING.md)*
- [ ] **C** — Both — context-dependent
- [ ] **D** — Custom: _____________________

---

### Q-028 · faction · Blue Massacre date (factions perspective)
> What year did the Blue Massacre occur — from the Factions / ArcSec perspective?
> Context: Two sections of GLMZ_SETTING.md give conflicting dates: the Factions section
> says 2065, the History & Timeline section says 2096. The date anchors the corporate
> takeover of law enforcement.

- [ ] **A** — 2065 *(source: GLMZ_SETTING.md — Factions section)*
- [x] **B** — 2096 *(source: GLMZ_SETTING.md — History & Timeline)*
- [ ] **C** — Custom: _____________________

---

### Q-029 · faction · Dominant E/SE Asian crime organization
> Is the dominant E/SE Asian organized crime org in GLMZ the Lotus Syndicate or the Jade Syndicate?
> Context: GLMZ.md and GLMZ_SETTING.md name the Lotus Syndicate as the primary blood-purity
> E/SE Asian criminal organization with Yakuza roots; DB:Faction presents the Jade Syndicate
> as GLMZ's largest crime operation with identical geographic origins.

- [x] **A** — Lotus Syndicate — blood-purity, early Yakuza lineage roots *(source: GLMZ.md / GLMZ_SETTING.md)*
- [ ] **B** — Jade Syndicate — largest crime operation, pre-Meridian Pacific Rim roots *(source: DB:Faction)*
- [ ] **C** — Both exist as distinct, separate organizations
- [ ] **D** — Custom: _____________________

---

## BIOLOGY

### Q-030 · biology · E.L.F. cognitive ceiling
> Are E.L.F.s a sentient species (no ceiling) or capped at animal-grade cognition (cat/dog/monkey tier)?
> Context: BIBLE.md classifies E.L.F. as a sentient species with no cognitive ceiling
> implied; GLMZ.md explicitly caps E.L.F. cognition at animal tier. Determines whether
> E.L.F. characters can hold speaking roles, express complex motivations, or claim legal
> personhood.

- [ ] **A** — Sentient species — no cognitive ceiling implied *(source: BIBLE.md)*
- [ ] **B** — Animal-grade cognition: cat/dog/monkey tier *(source: GLMZ.md)*
- [x] **C** — Custom: E.L.F.s emerge from code fragments, they have various abilities and sentience

---

### Q-031 · biology · Neuretics adult-lock universality
> Is the neuretics growth-window lock universal after age twelve, or are rare adult-open exceptions possible?
> Context: ATTE.md states the lock is universal with no exceptions after the growth window;
> SRZR.md and SS-A21 codify rare adult-open cases as real, which is the foundation for
> Sasha Võ's character.

- [ ] **A** — Universal after ~age twelve — no exceptions stated *(source: nodes/ATTE.md)*
- [x] **B** — Nearly universal — rare adult-open exceptions exist (SS-A21) *(source: nodes/SRZR.md)*
- [ ] **C** — Custom: _____________________

---

## NAMES

### Q-032 · name · GLMZ full name expansion
> What does GLMZ stand for — "Greater Lake Michigan Zone" or "Great Lakes Metropolitan Zone"?
> Context: BIBLE.md expands GLMZ as Greater Lake Michigan Zone; DB:Place expands the same
> acronym as Great Lakes Metropolitan Zone. Every formal reference to the city's official
> name depends on this being settled.

- [ ] **A** — Greater Lake Michigan Zone *(source: BIBLE.md)*
- [x] **B** — Great Lakes Metropolitan Zone *(source: DB:Place)*
- [ ] **C** — Custom: _____________________

---

### Q-033 · name · GLMZ primary colloquial name
> Is "The Glooms" the primary informal name for GLMZ, or does "Meridian 88" hold that role?
> Context: BIBLE.md cites only "The Glooms" as the informal name; DB:Place elevates
> "Meridian 88" to the primary populist label and demotes "the Glooms" to a secondary
> alternative.

- [x] **A** — "The Glooms" — the primary and only informal name cited *(source: BIBLE.md)*
- [ ] **B** — "Meridian 88" is the primary populist name; "the Glooms" is secondary *(source: DB:Place)*
- [ ] **C** — Both are co-equal informal names
- [ ] **D** — Custom: _____________________

---

### Q-034 · name · Vey's shop formal name
> Is the shop's formal name "Vey's Antiquity & Stationary" or just "Antiquity & Stationary"?
> Context: BIBLE.md includes "Vey's" as part of the compound proper name; BCODA.md explicitly
> states the shop is called "Antiquity & Stationary" without the owner's name attached.
> Appears in signage and the Faraday vault scene.

- [ ] **A** — Vey's Antiquity & Stationary *(source: BIBLE.md)*
- [x] **B** — Antiquity & Stationary — owner's name not part of the shop name *(source: nodes/BCODA.md)*
- [ ] **C** — Custom: _____________________
