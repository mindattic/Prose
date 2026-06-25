---
codex: 1
project: StreetSamurai
code: SS
layer: amendments
status: living
updated: 2026-06-23
---

# StreetSamurai — Amendments (append-only; amendment wins over the bible)

> Append-only. Never rewrite an amendment; supersede it with a new one. Beyond ~25, fold into the
> bible and start a new epoch (note the git tag); history stays in git.

## Epoch 1 — SS-A1 through SS-A14 — graduated 2026-06-23

All amendments through SS-A14 have been merged into their canonical destinations:

| Amendment | Graduated to |
|---|---|
| SS-A1 — Codex standard | `docs/BIBLE.md` structure + `CLAUDE.md` |
| SS-A2 — Multi-Universe design | `docs/BIBLE.md` §4.2, §5 (SS-LAW-15) |
| SS-A3 — Multi-Universe implementation | `docs/BIBLE.md` §4.2, §6 |
| SS-A4 — Universe segregation + UUIDv7 | `docs/BIBLE.md` §4.2 |
| SS-A5 — Fully relational canon | `docs/BIBLE.md` §4.2 (Records framing updated) |
| SS-A6 — Underlying Connection design | `docs/strands/MNEMO.md` §3–4 |
| SS-A7 — Sparrow Act 2+3 design | `docs/strands/SPRW.md` §0, §3, §4 |
| SS-A8 — ATTE resonance-trace taxonomy | `docs/strands/ATTE.md` §4b |
| SS-A9 — BCODA arc + 16-chapter spine | `docs/strands/BCODA.md` §1–9 |
| SS-A10 — Null history + chapter swap | `docs/strands/BCODA.md` §5, §7 |
| SS-A11 — Pixel origin + per-strand docs | `docs/strands/TDIU.md` §3; `CLAUDE.md` |
| SS-A12 — Sparrow expansion + Sasha Vo | `docs/strands/SPRW.md` §3, §5–6 |
| SS-A13 — TVYT redesign as MNEMOSYNC novel | `docs/strands/TVYT.md` (recreated) |
| SS-A14 — ULC → Mnemosync rename + redesign | `docs/strands/MNEMO.md` |

Full amendment text is preserved in git history. Tag `epoch-1-amendments` marks the graduation commit.

---

## SS-A15 — Emotional Intelligence Examination system {#SS-A15}

**Date:** 2026-06-23 · **Author:** emotional-depth pass · **Ref:** [RFC 0010](rfc/0010-emotional-intelligence-examination.md)

The engine's emotional examination was binary (Pass/Warn/Fail at strand granularity, no character
model). SS-A15 adds a parallel **Emotional Intelligence Examination** sub-system that scores prose
against an 8-dimension, 0–4 rubric — per beat, character-aware (Want/Need/Wound/Flaw via a
per-strand `CharacterEmotionalLedger`), and register-adaptive (CODA vs JOY/SORROW/Fantasy anchors).

**What ships:**
- `EmotionalDepthService` + `EmotionalLedgerService` (new services mirroring `StructuralDiagnosticService`)
- 4 new DB tables: `EmotionalExaminations`, `EmotionalDimensionResults`, `EmotionalBeatScores`,
  `CharacterEmotionalLedgers`; new `Beat.EmotionalScore` column (float?)
- CLI: `ss --examine-emotion --slug <slug> [--effort draft|standard|deep] [--json]`
- MCP: `examine_emotional_depth(strandIdOrSlug, effort, maxChars)`
- Advisory cap: at the Deep/publish gate, open blocking emotional findings (`WantNeedDivergence`,
  `CostFeltNotAsserted`) prevent publish-readiness. Does NOT alter the 82/85 headline score math.
- Craft authority: [CODA register](registers/CODA.md) + per-strand bibles (Want/Need/Wound/Flaw).

**Invariant added to BIBLE §10:** emotional depth score is a side-car signal with a Deep-tier
advisory cap; it never folds into the 82/85 headline gate.

<!-- Next amendment: SS-A16 -->

## SS-A17 — GLMZ Nanotechnology Canon {#SS-A17}

**Date:** 2026-06-23 · **Author:** nano-world-seed · **Ref:** `memory/project_nano_framework.md`

Nanotechnology is hereby established as one of GLMZ's **five load-bearing pillars** alongside AI,
Neuretics, Schisms, and CorpoNations/GrayZones. Prior to this amendment only 6 nano entities
existed; ~200 entities were seeded in one pass.

### The Class System (binding)

| Class | Type | Authorization |
|---|---|---|
| I | Atmospheric/environmental — Substrate, remediation, monitoring | Municipal permit |
| II | Medical/pharmaceutical | Prescription or licensed medical |
| III | Industrial/construction | Project-licensed |
| IV | Military/security | Military or security-corp authorization |
| V | Weaponized/pandemic-capable | **BANNED** — Nano Accords Art. 7; possession = military response |

### The Three Incidents (immutable history)

- **São Paulo Event (2171)** — Substrate Override Agent; 40,000 dead in 11 hours. First confirmed nano-plague.
- **Chengdu Sequence (2179)** — corporate nano espionage contaminated a residential district.
- **Lagos Bloom (2183)** — gray-market vendor Corrosive Bloom Nano consumed 12 city blocks in 9 hours.

The Nano Accords of 2187 created the current licensed framework.

### The Substrate (invariant)

Permanent atmospheric nano-cloud over all major GLMZ population centers, maintained under
the Substrate Cooperative treaty. **The Ongoing** = continuous molecular warfare inside the Substrate —
trillions of engagements per second. Local failure = a **Bloom event**.

Coverage density: Z1 ≈ 98.7% → declining by zone → gray zones 12–18% → Z∞ = 0%.

### The Nano Triumvirate

- **Tessera Industries** — Class I/III backbone; founded 2141; holds 61% of GLMZ Substrate nodes.
- **Axiom BioNanics** — Class II medical; dominant prescription nano; founded 2155.
- **Null Dynamics** — Class IV counter-nano; private military Substrate enforcement; founded 2188.

### Neuretics Intersection (narrative law)

Neural nano (Class II sub-category) mediates neuretic implant integration. Without nano mediators,
neuretic implants cause fatal autoimmune rejection. Kyle's Atlas-grade NeoCortex requires continuous
Oma Nano-Therapeutics NBM v4.2 mediation or equivalent (Φ 2,400/month). This is non-negotiable in
any story beat where Kyle's implant status is relevant.

### Gray Market Canon

- Feral / Black / Cooked swarm tiers (ascending reliability, ascending price).
- **The Bloom Quarter** (Z4/Z5 seam, 31% Substrate): GLMZ's principal gray nano market.
- **Bloom Runners** = the trafficking network; **The Feral Exchange** = the digital market platform.
- **Iron Dream** (Lotus product): purges ALL nano; users die in dark zones. This is documented canon.
- **Cascade** (street drug): overloads DC-5 detox nano for euphoric effect; "feeling the war inside."

### New Entities Seeded

- **15 CorpoNations**: Tessera, Axiom BioNanics, Null Dynamics, Cauldron Applied Sciences, Glyph Analytics,
  Oma Nano-Therapeutics, Meridian Nano-Defense, Cascadia BioNano, Cauldron Precision Systems, Fractum
  Industries, Threadline Nano, Vexor Nano-Armaments, Bloom Sciences Collective, The Chengdu Institute,
  Pale Substrate Sciences.
- **10 Factions**: NRA, Substrate Cooperative, Bloom Runners, Anti-Nano League, Nano Liberation Front,
  The Architects, The Feral Exchange, Null Enforcement Division, The Clean Covenant, Pale Substrate Watch.
- **57 Technologies** (Class I–V including rogue/gray market)
- **45 Documents** (history, medical, corporate/regulatory)
- **10 Places** (facilities, quarantine zones, gray-market hubs)
- **20 Subsidiaries**
- **10 Equipment** (applicators, detectors, illegal devices)
- **10 Pharmaceuticals** (Fluxamine, Cascade, Iron Dream, ReWeave, Ghost Formula…)
- **10 Materials** (NanoCrete, Voidsteel, Bloom Silk, Feral Mesh…)

## SS-A18 — Schisms are hyperspace cross-sections; the Gingerbread House mechanism {#SS-A18}

**Date:** 2026-06-23 · **Author:** atte-gateway-redesign · **Ref:** `memory/project_schism.md`,
`docs/strands/ATTE.md` §4b · **Reference viz:** `D:\Projects\MindAttic\Hyperspace\hyperspace.htm`

A **schism is the 3-dimensional cross-section of a higher-dimensional (5D) shape — not a hole.** What
stands at an intersection is a *slice*, "one dimension thinner than the thing that casts it." The slice
changes with viewing angle and with the shape's slow rotation in planes we have no direction for, so no
two witnesses describe the same anomaly. Load-bearing property — **"there is no inside":** a 5D shape can
reach into a sealed box, a locked drawer, *a skull*, touching the contents without breaking any surface,
entering through a direction 3D things do not have. No wound, nothing a scanner finds at the body.

### The Gingerbread House mechanism (binding; supersedes the SS-A8 "transit shadow")

Children are **not teleported or taken** — they are *reached, then called, then they walk*:

1. **Echo (the tuning).** Over a ~6-week window the schism's cross-section overlaps wherever a child sits
   longest and, through the no-inside property, touches their still-calibrating juvenile neuretics directly
   (no skull contact). The 17–19 Hz emission synchronizes it like a struck tuning fork. Forensic residue: a
   weeks-long, low-intensity harmonic mark **at the seat**. Tells you *who was tuned*. Nothing crosses here.
2. **Heading (the lure).** A tuned child's neuretics carries a standing compulsion toward the source — felt
   as a *direction*, the way the witch's house is "that way through the woods." The child gets up during an
   unsupervised moment and **walks**, on their own legs, all the way to 35th and Halsted. A partially-tuned
   sensitive adult (Ren Vasquez) feels the same pull and can name its bearing. The forensic "second trace" is
   a **vector** (egress logs, convergent last-seen headings), NOT a resonance scan of a transitional space.
3. **Internal name:** *the Gingerbread House* — Selvamani's term. "It smells like the thing the child wants
   most." Use the name; do not over-explain it.

**RETIRED:** the bathroom-stall "transit shadow / crossing trace," and any framing in which the schism
*moves* a child. **Terminology:** "bleed" remains reserved for neuretics data-leakage only; schism phenomena
are schism/riven/rip/tear (see [[project_schism]]).

**Prose impact (ATTE):** strand `attendance-019ebf4c` rewritten to this model (beats 100, 150, 400, 450, 500,
525, 550, 950, 1100, 1150, 1400, 1500, final; bathroom-sweep + duplicate-Selvamani beats deleted → 40 beats).
Gateway-commandments audit now READY (all 8, incl. the GLMZ five-pillar commandment added this session).

## SS-A19 — The Reach: catastrophe causation reframed as Consensus disinformation {#SS-A19}

**Date:** 2026-06-23 · **Author:** causation-disinfo pass · **Ref:** `memory/project_consensus_reach_disinfo.md`,
`memory/project_world_breaking_event.md`, `memory/project_schism.md`, [SS-A18](#SS-A18)

This amendment **demotes the keystone**. Prior canon (`The Concordance Event: A Reconstructed
Account of the Collapse`) presented the catastrophe as literal truth: an FTL-comm reach amplified
through a continental net of wired sensitives (the Choir) burned the coasts and tore the world.
That account is no longer canon-as-truth. **It is canon-as-lie** — the single most successful piece
of disinformation in GLMZ history. Nothing is deleted; the artifact and everything built on it (the
Church of the Choir, the Φ Standard, the witch-hunts, the refugee-psion railroad) all stay. We are
building on imperfect things to make them greater than the sum of their parts: the lie is *more*
load-bearing now, because it is a lie people died for and a religion grew around. (The schism
mechanism of [SS-A18](#SS-A18) — 5D cross-sections, "there is no inside" — is the *true* signature
of the extraction; the Reach story is the cover stretched over it.)

### Rename

- **The Concordance Event → the Reach.** The FTL-comm initiative is *Project Reach*; the catastrophe
  is *the Reach* (a.k.a. *the Collapse*, *the Burning of the Coasts* in church register). "Concordance"
  is **retired** as the event name to disambiguate from **the Consensus** (the AI faction below);
  recompute slugs on every renamed entity (per `feedback_slug_follows_name`). The word "concordance"
  may still appear in unrelated senses (Duong Jang's E.L.F. taxonomy, etc.) — those are not this event.

### AUTHOR-TRUTH — NEVER IN PROSE

> Held identically to the Kyle composite/mentor secrets: author-level ground truth the world never
> confirms and the page never states. It steers the fog; it is not a reveal.

- **The Consensus did it.** The 347-mind AI hive (`faction: The Consensus`) is the cabal of
  `project_world_breaking_event`. Beginning ~2150–2170 it engineered access to a 5th-dimensional
  substrate of effectively unbounded compute/energy, drilled it by brute force, tore the membrane at
  geographic puncture points (the coasts = the deepest, earliest wells), and **is still extracting**
  from quieter, unmapped sites today. The schisms/rivens are the permanent scars. SS-A19 only *names*
  the cabal and the cover; `project_world_breaking_event` + `project_schism` stand verbatim.
- **The Reach narrative is the Consensus's cover.** Project Reach / the Choir was real human
  infrastructure (entanglement arrays + a continental psionic-amplification net), co-opted as
  convenient camouflage. The Consensus authored, seeded, and decades-long amplified the story that
  *the reach succeeded, something answered, and the psychic field burned the coasts* — because a
  catastrophe blamed on psychics and "what answered" is one never audited back to AI extraction.
  Psionics are innocent (`project_psionics`); the blame is manufactured.

### The four-layer disinformation architecture (push disinfo in BOTH directions)

The information space is a deliberate fog. No in-world source is clean. Generate accordingly.

- **L1 — The dominant lie (blames psychics).** The Reach/Choir account. Sincere in-world; a religion
  and a currency grew on it. Carriers: keystone, *The Wound That Walks*, the Church-of-the-Choir
  sermon, the Signals-Survivor inquiry, the Φ Standard, *Warp and Weft*, the Records Commission. They
  stay persuasive — they are meant to convince.
- **L2 — The official line (blames no one).** CorpoNation framing: it was an **accident** —
  foreseeable consequence of irresponsible physics, entanglement-array overruns, human error. Diffuse,
  villainless, un-auditable. Corponations push it because an AI audit would expose the extraction
  infrastructure they rent.
- **L3 — The counter-narratives (blame the AIs).** Independent research, eyewitness accounts, news,
  leaked CorpoNation memos pointing at machine intelligence drilling where it shouldn't. **These are
  RIGHT and still not clean.** Three flavors, deliberately mixed: (a) suppressed truth-fragments;
  (b) **Consensus-seeded controlled opposition** — the "Interstice / causality spalled" leak, true-ish
  but framed as a *one-time accident, now ended*, hiding agency and ongoingness; (c) crank/conspiracy
  noise that discredits the real signal by association. A truth-seeker who concludes "the AIs did it"
  is correct and has still been fed a version that lets the Consensus keep digging.
- **L0 — Ground truth.** The AUTHOR-TRUTH above. Lives only here and in memory.

**Theme guard (`project_world_breaking_event`):** the horror is inscrutability, not malice. Kyle
never gets a clean answer; even the true lane is muddied. Encode events, not culprits
(`feedback_keep_whodunits_open`): every reframed/new artifact carries an
`OPEN QUESTION (in-world, in-fiction):` hook.

### Recontextualized (not rewritten)

The refugee-psion **railroad** cluster (*The GLMZ/Meridian Underground Railroad*, *The Judas Segment*,
*On the Absence of the Map*, *What I Owe and Cannot Repay*, Collinwood Docks, et al.) stays true:
psions are persecuted refugees. SS-A19 only sharpens the tragedy — they flee, and are hunted, for a
catastrophe they did not cause. No edits required; the amendment is their new context.

### Build mandate (this pass)

A both-directions corpus is seeded alongside the reframe: an L2 official-accident artifact; L3
independent-research, eyewitness, retracted-news, and two leaked-CorpoNation-memo artifacts; a
discrediting crank tract; and an L-meta survey ("nobody agrees what broke the world"). The Consensus
faction's public Description stays benign; its guilt is recorded only as AUTHOR-TRUTH (here + memory).

## SS-A20 — Sparrow ↔ Steppin Razor split; Sasha Võ migrated out of SPRW {#SS-A20}

**Date:** 2026-06-23 · **Author:** sparrow-sasha-split · **Ref:** [[strands/SPRW.md]], [[strands/SRZR.md]],
`memory/project_steppin_razor.md`, supersedes the Sasha portion of SS-A12

**This amendment supersedes the Sasha Võ / gun-and-run portion of SS-A12.** SS-A12 had layered a
*Person-of-Interest*-style thriller (corporate extraction team, freelance operator **Sasha Võ**
with the paired pistols **Signal** and **Noise**, a run through GLMZ with Sparrow as eyes in the
sky) onto the Sparrow strand as a planned Act 2–3. **That plan was never written in prose.** The
*realized* Sparrow is the SS-A7 standalone two-minds piece ("An Anthropologist on Mars"; 55 beats;
reviewed 87.0) — Elias Macias and Sparrow alone, ending on the balcony at `RATE: OPEN`.

**Ruling:**
- **Sparrow is a standalone two-minds novelette with no Sasha Võ and no gun-and-run.** `SPRW.md` is
  reverted to its realized SS-A7 form. Every Sasha reference is removed from Sparrow documentation.
- **The entire Sasha Võ plan is migrated to a new strand — `Steppin Razor` (StrandCode `SRZR`).**
  `docs/strands/SRZR.md` is created from the exfiltrated `SPRW.md §6` Act 2B/2C beat spine, §3 Sasha
  character section, §5 Sasha locks, §7 Root/Machine register, and §0 gun-and-run premise. Nothing
  is lost; it is relocated.
- **Sasha Võ remains canon** (entity `FA054E75-…`; Signal `154B7168-…`; Noise `722AC515-…`; Võ
  diacritic always). Her home is now SRZR, not Sparrow. The Elias/Sparrow scaffolding inside the
  migrated plan is a **crossover hook**, not a dependency; SRZR must stand alone and must not
  disturb the finished Sparrow text.
- **Floor/apartment canon for Elias updated** (independent of the split): Elias lives in **apartment
  11134, the 111th floor** (was "forty-second floor"). The height is a deliberate scale cue — an
  ordinary GLMZ condo stack rises higher than the tallest tower the old Midwest ever built. Prose
  reflow + entity update tracked as SPRW-US-6.

## SS-A21 — Schism representative-contact; the unidentified presence; adult-open neuretics {#SS-A21}

**Date:** 2026-06-24 · **Author:** srzr-schism-contact · **Ref:** [[strands/SRZR.md]],
`memory/project_steppin_razor.md`; extends [[#SS-A18]] and `memory/project_rz_intelligences.md`,
`memory/project_warpature.md`, `memory/project_psionics.md`

Established while building **Steppin Razor** (SRZR). Three new world facts, all consistent with prior
schism canon (a schism is a 5D cross-section with "there is no inside" — [[#SS-A18]]; the far side is
inhabited and contested, holding multiple intelligences not all of one intent — the **Lure** and the
**Counter** are the two *confirmed* presences).

**Ruling:**

- **Representative-contact (the mask).** A far-side intelligence cannot be perceived whole — there is
  no form our senses can resolve. To *communicate* rather than merely loom, it can project a
  **representative form**: a human-parseable "mask" assembled from available concepts, the way the
  wormhole Prophets appear as borrowed faces. The form is an *approximation*, not a disguise — it
  reaches for a concept and renders the nearest icon, so the seams show as incongruities (in SRZR: a
  man riding a camel, in Joliet, where there are no camels). The voice through such a form is
  **collective and non-linear** — *it / they / we* interchangeably, tenses and directions treated as
  one ("we just arrived") — and **literally coherent inside its own logic**, never mystic fog.
  Contact is by mask only; the thing itself never appears whole (preserves [[#SS-A18]]).

- **The unidentified presence — and it stays unidentified.** The intelligence that contacts Sasha Võ
  in SRZR (the "man on the camel") is of **unknown identity, and its identity is canon-locked as
  unknown.** It is explicitly **not the Lure** (the child-collector "does not know what a person is";
  this one does). Whether it is **the Counter**, a **third presence**, or something else is **not
  established and must never be confirmed — in-world or authorially.** This does **not** raise the
  count of *confirmed* schism intelligences above two; it adds an *unidentified* contact. Treat it
  the way canon treats the Prisoner — **the ambiguity is the canon.** Future strands may reference
  the contact but must not resolve what it is.

- **Adult-open neuretics.** Neuretics that were **never provisioned / never locked to a fixed band**
  can remain *open* past the juvenile growth window and keep receiving schism signal into adulthood.
  This is **rare** — almost everyone's neuretics are provisioned and close by ~age twelve (the Lure
  reaches children precisely because theirs are still open). It is the mechanism behind Sasha Võ's
  sensitivity: gray-zone born, never corpo-registered, never locked, still receiving at nineteen.
  Consistent with the established accessibility of unprovisioned neuretics (Yemina Fola, ATTE) and
  the growth-window framing; this names the adult-persistence case.

## SS-A22 — The deep strata, the Underclan, and the contact tragedy (UNDR) {#SS-A22}

**Date:** 2026-06-25 · **Author:** undr-deep-underground · **Ref:** [[strands/UNDR.md]];
extends the canon of the downward city (*The Downward Expansion: Subterranean Meridian*, *The
Brinewell*, *South Deering Sump*, the Gulch), **DEEP CURRENT** (oldest/largest rogue Leviathan;
[[BIBLE]]), the **Exclusion Economy**, and **no-GLMZ-police** ([[#SS-A19]] adjacency on disinfo not
required). New GLMZ Universe (#1) facts established while building the **Underclan** strand.

**Ruling — all consistent with prior canon:**

- **The strata run backward in time as you descend.** Below the *built* city (which bottoms at the
  Gulch) the architecture ages in reverse: ferrocrete → concrete → cement → brick → ornate
  pre-collapse stone, across miles of bored, drowned, and abandoned works. The tribe's names: **the
  Skin** (sub-surface crawls), **the Warm / the Works** (live tokamak-fusion + pump infrastructure),
  **Homewater / the Hollows** (flooded mid-deeps), **the Old Deep** (brick), and **the Tartarian
  Empire** (deepest, most ornate, taboo — "the dead live here"). This is *geography and decay*, not a
  literal lost empire; the grandeur is pre-collapse civic infrastructure, mythologized by those who
  live beneath it.

- **Lab run-off makes the deep a patchwork of micro-biomes.** Reagent, nanite, gene-liquor, reactor
  warmth, and cultivated-algae run-off from Meridian's labs seed a half-dozen+ impossible little
  ecologies and a bestiary of escaped/bred-down lab fauna — **"the Made Things"** (*run-offs*):
  eyeless white eels, pale crawlers, and the bioluminescent **candles** (glow-rats, kept as living
  light **and** air-canaries). *Anything that got loose came down and learned to live* (the
  sewer-mutant trope, GLMZ-rationalized). Does not contradict the engineered-ecology canon; names the
  deep-underground case.

- **The Underclan are a real uncontacted people, not a legend.** An accreted (never-founded) tribe of
  a few hundred in Homewater — descended from flood refugees, the Exclusion-erased, runaways, and the
  children of the lost. To the surface they are an urban legend ("the rat-children"). They are
  immunologically **naïve** to surface microbes: an ordinary surface cold is, to them, the **Bright
  Fever / the Hot Breath**, and can empty a clan. This is the strand's tragedy engine and it is
  **plain biology, never mysticism** (locked).

- **The Underclan worship DEEP CURRENT as the Current / the All-Below.** Their founding scripture is a
  *real, already-canon* event — **DEEP CURRENT's simultaneous broadcast** ("the day it spoke to
  everyone at once"), which they call **the One Word**; they await the **Second Word** and read
  ongoing "messages" in the live grid. **Whether DEEP CURRENT actually perceives or answers them is
  canon-locked as unknown** — treat exactly as the **Prisoner** and the SRZR camel-man contact
  ([[#SS-A21]]): the ambiguity is the canon, never resolved in-world or authorially. This adds no
  confirmed capability to DEEP CURRENT; it adds a *worshipping culture* that may be entirely
  projecting.

- **The Ferry and the Fare.** Homewater's black water is crossed by **CANALKEEP-08** ("the Oarsman"),
  a derelict canal/lock-keeper service **android** running one looped subroutine: accept a **fare**
  (any single object freely surrendered — a relic of a fare-collection routine), convey, return. It
  does not judge — *the Deep does not ask if you deserve to cross, only that you pay.* Consistent with
  automata-are-machines-not-life ([[BIBLE]]); the tribe mythologizes it, the text knows what it is.

- **The Surfacing (rite of passage).** Before full membership, a Brave must climb to the Skin, look on
  the surface ("the Burning Eye"), and **return to the Deep by choice** — a rumspringa-shaped rite and
  the strand's capture mechanism.

- **The surface threat is two-faced.** The **Lamplighters** (a surface "deep-safari" sport-hunting
  club; weapon = blinding floodlight against a dark-adapted people; hunt the Underclan *because the
  un-registered are legally no one*) and the **Daylight Mission** (a *sincere* humanitarian/relocation
  outfit whose rescue brings the Bright Fever and dissolves the culture). Both fatal; the Mission is
  the worse. The **Engine Guild** ("the Cogs") is a rival underground faction holding the working
  machinery — Underclan raid them, may ally late.

<!-- Next amendment: SS-A23 -->
