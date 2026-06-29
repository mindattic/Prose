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

## SS-A23 — Synthetic personhood: form ≠ life; three-tier rule {#SS-A23}

**Date:** 2026-06-26 · **Author:** synthetic-personhood-pass · **Ref:**
`memory/project_synthetic_personhood.md`; extends [[BIBLE]] automata/E.L.F. sections;
resolves character reclassifications for Ledger, Corvin Adaora, and five ceramic-man Automata

This amendment **codifies the canonical relationship between form and personhood** and resolves
every in-world character that was previously mis-typed as `synthetic`.

---

### The rule

**Form has no bearing on personhood.** A humanoid android is a machine. A box on wheels with
genuine desires, fear, and a stake in its own survival is alive. These are not contradictions —
they are the same principle from opposite directions.

---

### Three-tier hierarchy (LOCKED — never collapse, never promote)

**Tier 1 — Automata (machines, not alive)**

Robots and androids of any sophistication. Can look, sound, and behave exactly like a person.
*Not alive.* No interiority. Their apparent wisdom is pattern-matching; their apparent
preferences are optimization targets. A machine that has been running for a hundred years is a
hundred-year-old machine. The GLMZ tendency to mythologize long-running Automata is a *human*
behavior, not evidence of personhood.

**Prose rule:** never attribute genuine interiority to Automata. Their behavior may *read* as
purposeful — render it mechanistically, not phenomenologically.

**Tier 2 — E.L.F.s (alive, sub-human intelligence)**

Genuinely alive — emerged from Substrate complexity, not built. Have real fear, real hunger,
real preferences. **Animal-grade intelligence** — approximately cat/dog/monkey cognitive scope.
Aware of immediate environment; respond to threats and rewards. Do **not** have full
self-reflective personhood.

*Not persons. Not nothing.* An ethical gray zone the Accord has not adequately addressed.

**Prose rule:** render E.L.F. behavior as animal behavior — genuine but not reflective. Never
give an E.L.F. human-scale reasoning or self-narration. The Tributary (DWIACE) is a predatory
E.L.F.; its predation is real hunger, not malice. It operates as an animal predator, not a
moral agent.

**Tier 3 — Synthetic Persons (true personhood, infrastructure-scale)**

Vanishingly rare. Persistent self-model, genuine preferences not reducible to optimization
targets, genuine stake in continued existence. **Cannot fit in humanoid form.** The
Bekenstein–Bremermann constraints mean genuine personhood requires compute density at
server-cluster scale or larger — the information density required approaches a black-hole
formation event. Fewer than twelve entities have been formally recognized by the Accord since
2201; each required multi-year review of gravitational-anomaly signatures.

*No character in any current strand is a Tier 3 Synthetic Person.* Sparrow is an AI of
distinct sub-type — see [[strands/SPRW.md]].

---

### Character reclassifications (effective 2026-06-26)

The following entities were carrying the wrong `species` value. All DB records corrected:

| Entity | DB ID (prefix) | Was | Now | Canon basis |
|---|---|---|---|---|
| **Ledger** | `019eafb5…` | synthetic | human | Circuit name is a handle; neuretics-induced self-state accuracy replaces "architecturally can't self-deceive"; his 103+ prose beats are unaffected by name |
| **Corvin Adaora** | `019ec3eb…` | synthetic | human | Inadmissibility derives from **unlicensed gray-market neuretics tooling** (unregistered → unverifiable in court), not from species; DWIACE bible updated |
| **CANALKEEP-08** | `019eff96…` | android | robot (Tier 1 Automaton) | The Oarsman; locked subroutine; tribe mythologizes it; "the Deep does not ask" |
| **Covenant** | `019e9e4f…` | synthetic | robot (Tier 1 Automaton) | Ceramic-man Gray Zone figure |
| **Registry** | `019e9e4f…` | synthetic | robot (Tier 1 Automaton) | Ceramic-man Gray Zone figure |
| **Stillwater** | `019e9e4f…` | synthetic | robot (Tier 1 Automaton) | Ceramic-man Gray Zone figure |
| **The Thirty-Eight** | `019e9e4f…` | synthetic | robot (Tier 1 Automaton) | Ceramic-man Gray Zone figure |

---

### Canonical document

`019f04e688ba78dfb5ab7abcd54260c8` (`synthetic_personhood_accord_position`) — the Accord's
legal position paper establishing the three-tier framework. In-world primary source.

---

### Prose impact

- **Ledger (BCODA):** 103 beats use the name; no bulk edits required. Any beat that calls him
  "the synthetic" or uses species-tied prose must be corrected on encounter.
- **Corvin (DWIACE):** bible updated. Prose beats that reference inadmissibility via species
  must be updated to inadmissibility via unregistered tooling.
- **CANALKEEP-08, Covenant, Registry, Stillwater, The Thirty-Eight:** Automata in all
  contexts. Never attribute interiority. Mythology around them is the characters', not the
  text's.

<!-- Next amendment: SS-A31 -->

---

## SS-A30 — AI-founded CorpoNations; antagonist diversity (Halcyon Combine / OBERON) {#SS-A30}

**Date:** 2026-06-28 · **Author:** antagonist-diversity pass · **Ref:** extends synthetic personhood
([SS-A23]); SRZR antagonist reassignment.

Two linked rulings:

1. **A Synthetic Person (SS-A23, Tier 3) can found, own, and run a CorpoNation.** An AI may hold a
   corporate charter, a controlling stake, and the chief-executive seat — incorporating *itself* and
   commanding a workforce of humans who legally answer to a machine. The "inmates running the asylum,"
   inverted: the asylum runs the inmates. Exemplar: **Halcyon Combine** (id `019f0f3f0f60…`), founded
   and chaired by **OBERON** (id `019f0f3f22a5…`). This is rich, on-theme (ambient AI, rogue-AI arc),
   and a deliberate flavor of the GLMZ's plural-AI world.

2. **Antagonist diversity — stop defaulting to Axiom BioNanics.** Axiom had become the reflexive
   corporate villain across strands. **Halcyon Combine replaces Axiom as the antagonist of Steppin'
   Razor** (the live-well driller under the core), which also wires the corporate antagonist straight
   into the AI-cabal author-truth ([project_world_breaking_event]) — *without confirming* OBERON/
   Halcyon is the cabal (canon-locked unknown, like the camel-man and the Prisoner). Going forward,
   spread starring-antagonist roles across distinct CorpoNations; Axiom stays texture, not the default.

---

## SS-A29 — Arcologies, Gray-Zone buffers, and the Block Wars {#SS-A29}

**Date:** 2026-06-28 · **Author:** world-structure pass · **Ref:** geography/faction canon; extends
[the Pulse](#) and the zone/territory geography. Standing prose directive included.

Four interlocking facts about how the GLMZ is physically and politically arranged:

1. **CorpoNations are arcologies — always over 100 stories tall — and completely self-contained.**
   Each is its own enclosed world: housing, food, water, power, security, manufacturing, recreation,
   the dead. A citizen can be born, work, and die without leaving the tower. The arcology is the
   CorpoNation; the CorpoNation is the arcology.

2. **No two CorpoNations share a physical border.** A shared wall would create a frontline that never
   resolves — a permanent war seam. So the arcologies are **deliberately separated by Gray Zones**:
   ungoverned buffer territory that keeps would-be foes from ever touching. The Gray Zones are not
   merely where the corpos' writ runs out; they are the **engineered no-man's-land that segregates
   rivals** and keeps the cold peace cold.

3. **The Block Wars** — the uneasy peace is not static. CorpoNations constantly **push outward** (annex
   buffer, extend their footprint) or **perceive the Gray Zone as encroaching** on them, and the
   friction periodically flares into a **Block War**: a localized border conflict over a stretch of
   buffer — fought block by block, never declared, never fully won, always "resolved" back to a new
   uneasy line. They recur. They are weather, not history.

4. **The Pulse is the shared circulatory system.** The Mach-6 vacuum-tube network (see the Pulse
   canon) runs **in and out of every arcology** — the one thing that crosses the Gray-Zone buffers
   and stitches the self-contained towers into a single body. Pulse tubes are the arteries between
   otherwise-sealed worlds; control of, sabotage of, or passage through a tube is therefore loaded.
   **Use this** as connective tissue and as a pressure point in stories.

5. **Gangs are Gray-Zone life.** The buffer territory isn't empty — it's parceled among **micro-nations
   of thugs**, every block or three claimed by a would-be warlord who is **probably dead within the
   year.** A few hold on; most churn. None of these ephemeral crews approaches the stability of the
   **Lotus Syndicate** — an established *global* crime organization whose roots run back to the ancient
   **Yakuza** (which itself traces to early-17th-century Japan, evolving out of eccentric samurai
   bands and street vendors). The contrast is the point: warlords are weather; Lotus is geology.
   - **Lotus's honor system is real and selectively applied.** They observe a strict code — *when it
     conveniently paints them well.* The honor is genuine theater and genuine leverage; it is invoked
     to justify, never to constrain when constraint would cost them. Show the hypocrisy; never have a
     character editorialize it.

**Standing prose directive:** seed **a few Block Wars** (and the gray-zone warlord churn) through the
strands — as backdrop, rumor, detour, or hazard — to reinforce the uneasy Gray-Zone/CorpoNation peace
as lived reality, and use the Pulse-as-circulatory-system imagery for cross-strand cohesion. Adapt to
each strand's register (see [per-protagonist register]); do not lecture — show it the way characters
live it.

---

## SS-A28 — Present-day year is write-time + 200 (the current corpus is 2226) {#SS-A28}

**Date:** 2026-06-28 · **Author:** year-retcon · **Ref:** universe primer + whole GLMZ corpus.

The GLMZ "present day" is a **rolling target, not a fixed calendar year**: it is the year a story
is *written*, plus 200. The franchise promise is "200 years in the future," and that horizon
advances with the wall clock.

- The existing corpus was written in 2026, so its present day is **2226** (was 2225). This is a
  **full retcon**: the universe primer, all `docs/*` canon, entity descriptions, and prose were
  moved 2225 → 2226.
- **New stories advance with the calendar.** A strand drafted in 2027 is set in 2227, and so on.
  The "2226" figure is therefore the *current corpus's* present, not a universal constant.
- **Exempt — anything deliberately in the past.** Flashbacks (e.g., the TEST court-martial
  flashback), dated records, and historical references keep their own year. A log or record dated
  2225 *inside* a 2226 story is correct as a year-old artifact and must not be bumped.
- When prose or canon needs a concrete present-day year, use the story's own present (2226 for the
  current corpus). Relative phrasing ("two centuries on") is fine where it reads naturally.

This supersedes the fixed **2225** anchor previously in the universe primer.

---

## SS-A27 — Genre positioning: Neo-Cyberpunk (modern, ambient-AI, no crude plugs) {#SS-A27}

**Date:** 2026-06-27 · **Author:** overnight-empire-run · **Ref:** commercial positioning of the
whole IP (books → novellas → film/TV/games). Decision made by the Legion panel
(`legion ask`, tier HIGH, **unanimous 4/4, full consensus, zero dissent**).

**The banner is Neo-Cyberpunk.** The series is positioned, marketed, and shelved as
**Neo-Cyberpunk**. Rationale (panel): "cyberpunk" is a billion-dollar discoverability bucket
(*Edgerunners*, *Cyberpunk 2077*, *Blade Runner 2049*, *Altered Carbon*) that modern audiences
read as *high-tech dystopia*, not "cables in skulls." The **Neo-** prefix signals the update
(ambient AI, no jacks) while inheriting the audience, retail shelf, streaming category, and SEO —
the same proven move as neo-noir / neo-western. "Quantumpunk" has zero genre footprint and forces
booksellers/streamers/algorithms to invent a category (commercial poison across media).

**"Quantumpunk" is allowed only as an in-universe flavor descriptor** beneath the Neo-Cyberpunk
banner — never the primary marketing label.

**Modernization thesis (the thing that makes it *Neo*):** AIs are **ambient and everywhere**, and
**no one needs crude head-plugs.** This is already how the world is built — the amendment makes it
binding and names it:
- Neural interface = **neuretics**, a mesh *grown into the brain* (see
  [Neuretics canon]; SS-A17 nanotech). Not a port, not a cable, not a socket.
- Remote operation (drones/crawlers/cameras/vehicles) = **QCE** (Quantum Crystal Entanglement) —
  a *wireless* quantum link; the operator is a **Rider** whose own body is left behind. No tether.
- Hacking = **Channelers** who *commune* with unlicensed AIs wirelessly. No deck-jacking.
- **Banned imagery (prose):** physical datajacks, skull sockets, neural ports, "jacking a cable
  into" anyone, trode-nets. The dated 1980s plug-in-the-skull trope is explicitly NOT this world.
  (Corpus audited 2026-06-27: already clean; one stray "data jack" line modernized to a grown-in
  neuretic ridge.)

**Scope:** this is positioning + a forward-looking prose rule. It removes no existing canon —
neuretics and QCE already embody it. It does not contradict "jacked in via QCE" (that is wireless
entanglement, not a cable).

---

## SS-A26 — The Rook Trilogy arc + the body-bank harvest (Helix / the Marrow) {#SS-A26}

**Date:** 2026-06-27 · **Author:** rook-trilogy-finale · **Ref:** establishes the canon that
closes the heist trilogy (MxG → NxR → CxC); see [docs/strands/CxC.md](strands/CxC.md).

New world canon:
- **Registered Reads are a harvestable resource.** A Nano Triumvirate member — **Helix
  Biosystems** — runs an industrial body-bank, **the Marrow**, that processes registered Reads
  (neuretics-capable people the GLMZ logs) into Class V-compatible neural substrate. The human
  core (marrow) rendered into corporate product (chrome). Pure tech/body-horror; no psionics.
- **Relocation orders have been cover for harvests.** The Old Harbor "relocation order nobody
  signed" (NxR manifest) was a harvest; the word *harvest* never goes on the order. Dr. Halina
  Soraya ("Ohara") walked **twenty-one** registered Reads off the barge and hid them — the trilogy's
  moral spine.
- **The Rook Trilogy is one arc.** The crew's three jobs were steps Helix needed taken and could
  not be seen taking: MxG (extract Soraya from Axiom for a clean acquisition), NxR (the partition
  crack surfaced the survivor list), CxC (the loop closes). Helix funded PEREGRINE's
  reconstitution (NxR §2). Antagonist of record: **Anneke Oyelowo**, Helix program director
  and Rook's mirror.
- **Class V** neural-tissue tech, made public in MxG, was refined by the competitor (Helix)
  without Axiom's liability — the public-domain release was the competitor's win, not just Axiom's loss.

This amendment is realized in prose across the trilogy and governed by the character system in
[docs/CHARACTER.md](CHARACTER.md).

---

## SS-A25 — Megalopolises, Rome, and additional world-geography facts {#SS-A25}

**Date:** 2026-06-26 · **Author:** world-expansion-pass · **Ref:** extends [[#SS-A24]];
adds four megalopolis designations and seals the fate of Rome

### Megalopolises (sealed)

The following cities are designated **megalopolises** in 2226 — each a dominant urban
gravity-well in its region, Pulse-connected, population measured in tens of millions:

- **Shanghai** — Pacific Rim primary center
- **Tokyo** — Pacific Rim; high-density; major cultural export engine
- **Rio de Janeiro** — South America's dominant city; Pulse-connected
- **Mexico City** — North American megalopolis (outside GLMZ's immediate sphere)
- **Jakarta** — Southeast Asian megalopolis; Pulse-connected

GLMZ is **not** a megalopolis in this taxonomy — it is the *world hub*, a different category
(the Pulse's primary routing node; the city where Western civilization reconstituted after the
coastal collapses).

### Rome: lost to schisms

Rome was destroyed during the **Rise and Fall of the False Prophet (2082–2091)**. As the
historical seat of the Catholic Church and the organizational center of the movement's peak
period, Rome was the epicenter of the collapse — and the catastrophic collective grief, rage,
and mass spiritual crisis that followed appears to have attracted or activated schism events
of extraordinary intensity.

What exactly happened is not on record: the AAMA's reconstruction efforts have been partially
successful and are largely not published. What is known: the city center is uninhabitable,
AAMA-classified as a **Class-4 or Class-5 schism zone**, and has not been successfully
characterized. The Vatican's physical site is inside the affected zone.

No lives may be confirmed in or out. The zone is not growing. It is simply there.

**Prose rule:** Rome may be referenced as a loss, a cautionary note, a historical fact — but
never visited, never described from inside, never explained. The AAMA does not explain it.
LOCKED: what happened in Rome during the collapse is never authoritatively revealed.

---

## SS-A24 — The wider world: Amish, the Dreaming, Tequenica, Korea, Eurasia, the Middle East {#SS-A24}

**Date:** 2026-06-26 · **Author:** world-expansion-pass · **Ref:**
`memory/` (see new entries); seals seven world-historical facts external to GLMZ;
adds one Schism Intelligence (Tequenica); adds Amish faction entity; no engine-law changes

This amendment **seals canonical facts about territories outside GLMZ** that provide
geopolitical texture for the setting and — in UNDR — ground the Surfacing/Rumspringa parallel.

---

### §A24-1 — The Amish: last organized Christian community

They are the only continuously functioning organized Christian
community in 2226. The world calls them the Amish; they call themselves the Amish.

**The Rise and Fall of the False Prophet (2082–2091)** destroyed every other sect. The "False
Prophet" — a charismatic unifier who consolidated disparate Christian denominations under a
single banner — claimed, at peak, over 800 million followers across North America, Europe, and
sub-Saharan Africa. In 2089, investigative work (primarily by Eurasia's press corps and by an
independent source within Axiom Systems) revealed that the Prophet's financial architects and
message strategists were a CorpoNation consortium, the Prophet movement instrumentalized as a
consumer-base and political-bloc engine. Axiom Systems was the largest single backer.

The collapse was total. Aligned institutions lost their tax status, their properties, and their
credibility inside eighteen months. The denominations that survived the 2080s did not survive
the 2090s.

The Amish survived because they had never joined. Historical insulation from outside
authority — rejection of networked technology, community governance, deep skepticism of
centralized power — meant the movement had nothing to grip. When the collapse came they were
standing exactly where they had always been.

**In 2226:**
- **Name:** the Amish
- **Location:** scattered communities in former Indiana, Ohio, and Pennsylvania (habitable
  Midwest territories, outside GLMZ jurisdiction)
- **Population:** ~40,000 globally
- **Technology:** hand tools, horse-drawn transport, no networked devices, no neuretics
- **Coming of age — Rumspringa (Rumshpringa):** at ~16, young community members are permitted
  and expected to experience the outside world before choosing baptism (full membership) or
  departure (permanent). Return rate: ~85–90%. Both choices are respected; both losses are
  mourned differently. The practice ensures membership is chosen, not inherited.

**Canonical parallel to the Underclan Surfacing (UNDR):** LOCKED. The comparison is permitted
and encouraged in surface-observer prose (Noor, journalists, anthropologists). The Amish
are not the same as the Underclan — the surface world sees the parallel; the Underclan do not
know the Amish exist. The echo is structural, not causal.

**Faction entity:** seeded in DB as `faction`, name `The Amish`. Universe GLMZ.

---

### §A24-2 — Australia: Absorbed into the Dreaming

Australia is inaccessible. The Pulse does not route there (no hub exists; no hub will be
built). Maritime approach fails: vessels do not return. Satellite imaging shows landmass and
weather. Nothing else.

The consensus descriptor — insofar as GLMZ has one — is that Australia has been **absorbed into
the Dreaming**, the First Nations concept of the living-present past made somehow literal.

**LOCKED:** the mechanism is never explained. Whether the Dreaming is a schism event, a psionic
phenomenon, a sovereign act, or something not on any existing taxonomy has not been determined.
Australia exists as a place the world has accepted it cannot have.

---

### §A24-3 — Tierra del Fuego: returned to the Yahgan Peoples

The southernmost territory of South America is restored to the **Yahgan Peoples**, who practice
the ancient lifeways that sustained their ancestors: small fires (including in boats) as
primary heat source; animal grease applied to skin as thermal insulation; deep squatting as the
resting posture (reduced surface area, heat conservation); and — over generations of sealed
isolation — an elevated resting body temperature that allows survival in sub-Antarctic
conditions that kill clothed newcomers.

No Pulse. No maritime access. A **Schism Intelligence** known to the Yahgan as **Tequenica**
has been revealed to them directly; the entity maintains a protective perimeter. Vessels
approaching by sea are turned; aircraft cannot establish approach paths. The Yahgan know
Tequenica and do not fear it — it is woven into their cosmology as present but not controlling.

**Tequenica** is the third known Schism Intelligence in the canonical taxonomy:

| Entity | Frequency | Function | Location |
|---|---|---|---|
| Lure | 17–19 Hz | collector | 35th-and-Halsted, GLMZ |
| Counter | 72–74 Hz | "this far no further" | 35th-and-Halsted, GLMZ |
| Tequenica | unmeasured | protective perimeter (inward-pointing) | Tierra del Fuego |

**LOCKED:** Tequenica's frequency, full nature, and intentions are never authoritatively
revealed by any outside party. No outside party has gotten close enough to measure.

---

### §A24-4 — Korea: unified 2153, sealed

On the bicentennial of the Korean War armistice (**1953 + 200 = 2153**), North and South Korea
reunified as the **Republic of Korea**. The reunification was politically traumatic on both
sides and concluded with a nationalist settlement that has hardened, not softened, since.

**In 2226:**
- Extreme isolationism. No Pulse link. No foreign nationals admitted (de facto: no entry)
- Trespassers are killed on sight; this is codified, not exceptional
- **~95% genetic homogeneity:** achieved through strict immigration control, strong social
  pressure toward endogamy, and — in the two generations following reunification — a State
  genetic-continuity program whose existence is neither confirmed nor denied by the current
  government
- Exports: cultural product only (film, music, literature, food aesthetics are widely consumed
  globally). Culture goes out; the border does not open
- GLMZ wire-service name: **"the Quiet Country"** — neutral descriptor, no editorial weight

**LOCKED:** Korea's internal politics, the details of the genetic-continuity program, and any
on-page Korean character's relationship to the State are left for the story that earns them.
No on-page Korean character speaks for the government.

---

### §A24-5 — Russia: collapsed; Eastern Russia balkanized

Russia, as a unified state, no longer exists. The collapse was not a single event but an
accelerating fragmentation across the late 21st and early 22nd century — economic, ethnic,
political, and (in the far east) environmental. By 2150 there was no central government with
authority across what had been the Russian Federation.

Eastern Russia (Siberia, the Far East) balkanized into a patchwork of regional states,
indigenous-people territories, and Eurasian-sphere client zones. No successor state claims the
Russian name. The Quiet Country (Korea) borders several of these fragments.

---

### §A24-6 — Europe + Western Russia = Eurasia

What was Western Russia (west of the Urals, including Moscow and Saint Petersburg) merged
politically with the European Union successor bodies in the 2110s–2130s, forming the
continental polity known as **Eurasia**. The merger was pragmatic on both sides: the rump
western Russian territories needed economic stability; the European bodies needed territorial
depth after the Atlantic coastal collapses.

Eurasia in 2226 is the dominant continental power in the eastern hemisphere:
- Rotterdam is a primary Pulse hub (GLMZ to Rotterdam in 43 minutes; confirmed WORLD.md §1.3)
- Cultural exports are significant; the press corps is the world's most aggressive investigative
  body (it broke the False Prophet story in 2089)
- The eastern border of Eurasia is contested with the balkanized Russian-territory successor
  states

---

### §A24-7 — The Middle East: collapsed after fossil-fuel obsolescence

The Middle East's geopolitical power rested almost entirely on fossil fuel revenues. When
quantum computing and post-silicon energy architecture made fossil fuels obsolete — the Silicon
Wall of 2045 began the cascade, with synthetic energy generation reaching economic parity by
the late 21st century — the revenue base collapsed. Regional powers destabilized. Wars over
water, territorial, and demographic lines followed.

By 2200, the former Middle East is a patchwork of small states, city-states, and wilderness
zones with no single dominant power. Most GLMZ residents interact with the region only through
its food culture, its diaspora (major component of the Ubiquitous Diaspora), and the
century-old investment infrastructure still embedded in Eurasian financial systems.

The region has no Pulse hub (no political body stable enough to host one).

---

### World canon table (external territories, sealed)

| Territory | Status in 2226 | Access |
|---|---|---|
| Australia | Absorbed into the Dreaming | None (Pulse: no hub; ships: do not return) |
| Tierra del Fuego | Yahgan Peoples; Tequenica protects | None (ships turned; aircraft blocked) |
| Korea | Unified Republic, extreme isolationism | None (trespassers killed) |
| Russia | Does not exist; Eastern fragments balkanized | Varies by fragment |
| Eurasia | Major continental power (EU + Western Russia) | Pulse-connected (Rotterdam hub) |
| Middle East | Patchwork small states; no dominant power | Limited (no Pulse hub) |
| GLMZ | Western civilization's center | Pulse hub (world's primary) |
