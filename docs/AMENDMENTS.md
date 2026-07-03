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
| SS-A11 — Pixel origin + per-strand docs | `docs/strands/PNHL.md` §3; `CLAUDE.md` |
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

<!-- Next amendment: SS-A32 -->

---

## SS-A31 — Accessible prose as the default register; Kyle's cognitive voice is his alone {#SS-A31}

**Date:** 2026-06-30 · **Author:** style-pass · **Ref:** retires Dense register; extends VOICE.md + CHARACTER.md §8.

Two linked rulings:

### §A31-1 — Accessible prose replaces Dense as the default for all new writing

The Dense version of *Street Meat* (strand 019f19ed) proved that high sentence-level complexity creates an accessibility barrier without proportional literary gain. The Accessible version (019f1a05) delivers the same world, the same voice, and the same emotional weight while expecting less decoding work from the reader. All new prose generation uses the Accessible rules:

1. **Break compound chains.** A sentence with multiple "which/and/because" subordinate clauses is split at natural joints into two or three shorter sentences.
2. **Contractions throughout narration.** Not just dialogue — the narrator uses contractions too. "He didn't" not "He did not." "She couldn't" not "She could not."
3. **Anglo-Saxon over Latinate.** Prefer the simpler root: *find* over *locate*, *needed* over *required*, *method* over *methodology*, *physical* over *kinesthetic*, *readable* over *legible*, *earlier* over *prior*, *shoulder* over *deltoid*, *end* over *ferrule*, *hum* over *harmonic*, *anchor* over *orienting mechanism*, *put together* over *assembled*, *close* over *near thing*.
4. **"X, which was Y" → "X. That was Y."** Relative-clause summaries become their own sentence.
5. **Colon lists → period-separated fragments.** "The city: the ferrocement wave, the advertisement, the wind" → "The city. The ferrocement wave. The advertisement. The wind."
6. **Semicolons → periods** in narrative prose. Reserve semicolons for lists of complex items.
7. **Cut abstract nominalizations.** "The differential of a fire door opening" → "the shift of a fire door opening." "The transfer of intent" → cut entirely. "The distribution wasn't coincidence" → "The split wasn't coincidence."
8. **Spell out numbers and abbreviations in prose.** "-36F" → "minus thirty-six Fahrenheit." "64-splice" → "sixty-four-splice."
9. **Drop filler qualifiers.** "The texture of it" → "the texture." "The rest of the work" → "the rest." "As a fact" as sentence-end qualifier → cut.

What does NOT change: sentence fragments for impact, all world proper nouns and technical terms (NeoCortex, neuretics, GLMZ, Cacophony, etc.), the emotional register, the filed/archived metaphors for Kyle specifically, all good metaphors that survive literal scrutiny.

**For existing strands:** valid as-is. Rewrite only on an active revision pass.

### §A31-2 — Kyle's cognitive vocabulary is his alone; no other POV character inherits it

Kyle thinks through a specific set of cognitive patterns that are his splice-architecture, his martial philosophy, and his neuretics overlay — not general GLMZ register. When any other protagonist has POV, these patterns are banned:

| Pattern | Why it's Kyle's |
|---|---|
| *He filed it. / In the drawer where he kept most things.* | The splice parliament's filing metaphor — 64 profiles organizing input |
| *He ran the arithmetic. / The math was exact.* | Kyle's transactional worldview + NeoCortex precision |
| *After the choice. Before the cost. The gap.* | Kyle's specific martial philosophy (Saito's teaching) |
| *The parliament. The sixty-four were quiet.* | Kyle's literal architecture — other characters don't have splices |
| *The round count as suspense hook.* | Kyle's combat methodology; other POVs use different tension anchors |
| *The NeoCortex noted. The overlay was still up.* | Kyle's hardware; only valid for neuretics-equipped POVs |
| *He resolved it. He ran the geometry.* | Kyle's pre-combat read methodology |

Each protagonist has their own cognitive register — see CHARACTER.md §8 for per-protagonist patterns.

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

---

## SS-A32 — Aerostatic Architecture: the sixth cornerstone; altitude as social axis; the Kite Rig {#SS-A32}

**Date:** 2026-06-30 · **Author:** aerostatic-worldbuild-pass · **Ref:** `memory/project_aerostatic_architecture.md`; extends geography canon; extends VTOL-Only Access Protocol; 18 entities seeded, 36 world-graph edges built

Aerostatic architecture is hereby established as the **sixth load-bearing pillar of the GLMZ** alongside AI, Nanotechnology ([SS-A17](#SS-A17)), Neuretics, Schisms ([SS-A18](#SS-A18)), and CorpoNations/GrayZones ([SS-A29](#SS-A29)).

---

### What it is (binding definition)

**Aerostatic architecture** is the engineering discipline and urban practice of constructing habitable structures that derive lift from buoyancy — specifically, from **Vacuum Aerogel Buoyancy Cell (VABC)** clusters exploiting Archimedean displacement — rather than mechanical thrust or gas bags. The lift is passive. No fuel. No combustion. A VABC cluster at altitude stays at altitude as long as internal vacuum is maintained by its embedded **Vacuum Micropump** arrays.

All aerostatic structures rest on a **Tensegrity Geodesic Platform (TGP)** — the structural skeleton that distributes load, interfaces with the VABC lift layer below, and accepts **Aerogel Composite Construction (ACC)** buildings above. The TGP's geodesic scaling means large platforms are proportionally *lighter* per unit area than small ones — at half-mile diameter, structural mass is approximately 1/1000th of enclosed air mass.

Mooring is provided by **CNT-Steel Meta Alloy** tether cables (40–80 GPa at 15–20% of steel's weight, ductile failure mode) anchored to bedrock pylons or neighboring platforms.

The theoretical foundations predate the Reach. The Reach compressed a projected sixty-year development trajectory to approximately eighteen years. The first registered aerostatic structure in Meridian 88 was commissioned approximately nineteen years post-Reach; the 7th Float District reached aeroquarter scale twenty-six years post-Reach.

---

### Scale hierarchy (LOCKED)

| Scale | Registry | Street Name | Population | Altitude |
|---|---|---|---|---|
| Single building | Aeropod | Loft | 1–50 | 50–800m |
| City block | Aerobloc | Sky Block | 50–500 | 100–1,200m |
| Neighborhood | Aeroquarter | Float District | 500–10,000 | 200–2,000m |
| Full district | Aeropolis | Cloudtown | 10,000–100,000 | 500–4,000m |
| Derelict / unmanaged | Gray Zone Derelict Platforms | Drifters / Low Clouds | Varies | 20–150m |

The **7th Float District** is a named Aeroquarter in Meridian 88's western sector; it is Sasha Võ's registered pad address (SRZR).

---

### Altitude as a social axis (narrative law)

Aerostatic architecture created a **new axis of social stratification in Meridian 88: altitude.** This is the sixth major class dimension, orthogonal to corpo affiliation, nano access, and neuretics grade. It is binding in all GLMZ prose.

- **The surface** is contested, contaminated, and surveilled.
- **The float districts** are expensive, isolated, self-governing, and above the law they choose not to follow.
- **The derelict platform layer** (Drifters, Ghost Platforms, Low Clouds) is neither — the Gray Zone at altitude. It belongs to no one and everyone.
- **The Air Tax:** VTOL-only access is the single most significant social filter in the float layer. To live at altitude, you need a VTOL, someone who has a VTOL, or resources to pay passage on commercial air carriers. None of these appear in civic code as explicit barriers; all function as barriers in practice.

The phrase **"cloudtown politics"** means governance that is abstract, self-referential, and disconnected from surface conditions — residents of an Aeropolis take it as a compliment.

The phrase **"in the hold"** (from *the Hold*, street name for the mooring tether system) means trapped, committed, or obligated. "Cut your hold" means leave everything behind. Tether sabotage carries mandatory sentencing in all Meridian 88 corponation codes without exception.

**Float lean** — the characteristic slight-but-perceptible tilt of an aerobloc with a neglected trim system — is a realistic detail available for use as atmosphere or metaphor.

---

### The Kite Rig (new technology — vertical mobility)

The **Kite Rig** is a wearable aerostatic descent harness: a compact array of miniaturized VABCs integrated into a structural vest or undersuit frame. On free-fall detection (neuretics mesh, embedded accelerometer, or manual activation), it deploys a rigid tensegrity scaffold around the wearer in approximately 1.2 seconds, expanding the cell cluster from transport configuration to operational volume.

The deployed cluster does not arrest a fall — it converts terminal velocity into a slow controlled drift:
- From 2,000m (aeroquarter altitude): ~15 minutes to surface
- From 3,500m (aeropolis altitude): ~28 minutes to surface

No propellant. No combustion. No continuous power — only vacuum integrity of the cells, maintained by an integrated micropump array with a 72-hour standby reserve. Safe floor: **80m minimum** — below this, deployment cannot complete. At less than 80m the Kite Rig is a partial drag device only, not a safe landing system.

**Gray market version:** hacked from decommissioned aerostatic platform VABC cells. Heavier, less reliable, cells degraded — accelerated descent with no warning. Largely untraceable. Common in Drifter communities and among operators who need to go from float-district altitude to surface without a VTOL.

**Neuretics integration** (Tier 3+): auto-deploy on fall detection without conscious activation.

**Classification:** not yet formally regulated in Meridian 88 civic code. Classified under aerostatic sabotage provisions if used offensively (e.g., to crack VABC cells and strand a platform rather than as personal equipment).

**Story hooks:** character leaps from aeropolis — 28 minutes of slow drift with nowhere safe to land; gray market rig with degraded cells fails mid-descent; neuretics auto-deploy fires during a fight on a platform edge; Kite used for infiltration — slow drop onto a restricted VTOL pad from above; two people on one Kite Rig — payload doubled, descent time halved.

---

### Entities seeded (2026-06-30)

**Materials** (via SQL, Entities table + Materials table):
- Polyimide Aerogel (`polyimide_aerogel`) — MAT-0041; Vac-Skin
- Silica Aerogel (`silica_aerogel`) — MAT-0039; Ghost Glass
- High-Performance Aerogel Concrete (`hpac`) — MAT-0044; Lightstone / Aero-crete
- CNT-Steel Meta Alloy (`cnt_steel_meta_alloy`) — MAT-0071; Black Wire

**Technologies** (via MCP):
- Vacuum Aerogel Buoyancy Cell (`019f1b2e-bd65-…`) — TECH-0188; Ghost ball
- Vacuum Micropump (`019f1b2e-e05a-…`) — TECH-0191; Vac-tick
- Tensegrity Geodesic Platform (`019f1b2f-062d-…`) — TECH-0193; The Web
- Aerogel Composite Construction (`019f1b2f-2a66-…`) — TECH-0197; Ghost construction
- **Kite Rig** (`019f1b32-c12c-…`) — personal descent harness; the Kite / Float coat

**Places** (via MCP):
- Aeropod (`019f1b2f-5c3e-…`) — PLAC-AERO-01; Loft
- Aerobloc (`019f1b2f-89a5-…`) — PLAC-AERO-02; Sky Block
- Aeroquarter (`019f1b2f-cb62-…`) — PLAC-AERO-03; Float District
- Aeropolis (`019f1b2f-f8a4-…`) — PLAC-AERO-04; Cloudtown
- Gray Zone Derelict Platforms (`019f1b30-36ff-…`) — PLAC-AERO-00; Drifters

**Documents** (via MCP):
- Aerostatic Architecture (`aerostatic_architecture`) — CONC-0201; canonical discipline overview
- Aerostatic Mooring (`aerostatic_mooring`) — CONC-0204; The Hold
- VTOL-Only Access Protocol (`vtol_only_access_protocol`) — CONC-0207; the Air Tax

**36 world-graph edges** built (`source: aerostatic-worldbuild-2026-06-30`): material→technology (component_of/ingredient_of), technology→technology (requires/integrates_with/builds_on), technology→place (enables/structural_basis_of/partially_maintains), material→place (moors), place→place (hierarchy: sub_unit_of/part_of/derelict_form_of), Kite Rig→place (circumvents_access_to/used_in), document→entity (documents).

---

### Three-register naming (binding for all GLMZ prose)

| Concept | Official | Meridian 88 | Glooms |
|---|---|---|---|
| Small aerostatic structure | Aeropod | — | Loft |
| Block aerostatic structure | Aerobloc | Sky Block | Sky Block |
| District aerostatic structure | Aeroquarter | Float District | Float District |
| City aerostatic structure | Aeropolis | — | Cloudtown |
| Derelict platforms | UAI (Unmanaged Aerostatic Infrastructure) | Drifters | Low Clouds / Ghost Platforms |
| Mooring tether system | Aerostatic Mooring Array | Tether System | The Hold |
| Access restriction | VTOL-Only Access Protocol | Hard Access | The Air Tax |
| VABC shell material | Polyimide Aerogel | Aerogel | Vac-Skin |
| Translucent facade | Silica Aerogel Panel | Ghost Glass | Ghost Glass |
| Buoyancy cell | Vacuum Aerogel Buoyancy Cell | Lift Cell | Ghost Ball |
| Micropump | Vacuum Micropump | Vac-Pump | Vac-Tick |
| Personal descent rig | Kite Rig | Kite Rig | The Kite / Float coat / Slow fall |

<!-- Next amendment: SS-A34 -->

---

## SS-A33 — Vertical mobility extension: The Low, Ascent Bloom, Low Runner Kit {#SS-A33}

**Date:** 2026-06-30 · **Author:** vertical-mobility-pass · **Ref:** `memory/project_aerostatic_architecture.md`; extends [SS-A32](#SS-A32). 20 entities total in the aerostatic cluster; 58 world-graph edges total.

This amendment extends SS-A32's aerostatic architecture cornerstone with **The Low** (the ungoverned altitude gap), the **Ascent Bloom** (the upward counterpart to the Kite Rig), and the **Low Runner Kit** (the professional vertical-transit rig).

---

### §A33-1 — The Low (altitude gap, binding)

**The Low** is the airspace band between approximately 30m and 320m altitude in Meridian 88 — bounded below by Arcturus Civil Security patrol ceiling (~50m) and above by the MATA radar floor (320m). Neither authority governs it. It is not formally designated; it simply falls through the gap.

**What it is:** Persistent industrial haze from two centuries of arcology manufacturing and processing exhaust holds in a loose thermal band. Visibility runs 50–200m in typical conditions. Building-wake turbulence is constant — arcologies create chimney and backwash effects that make VTOL navigation difficult and unpredictable. MATA won't fly commercial traffic here. Arcturus won't pursue above their ceiling.

**The Low is not a death zone.** Derelict platform communities (20–150m altitude, [SS-A32](#SS-A32)) live here continuously without survival gear. The hazards are navigational — reduced visibility, irregular turbulence, no radar coverage — not atmospheric. The suit you need is a navigation rig, not a hazmat suit.

The Low's practical effect is invisibility. Below MATA's radar floor, a VTOL, an Ascent Bloom, a Kite Rig, or any cargo moving between surface and the float layer simply doesn't appear on any authority's display. This is why gray-market vertical transit is a viable industry rather than an immediately-detected crime.

**Prose register:** *The Low* is standard Meridian 88 usage. In the Glooms, *"working The Low"* means operating in the ungoverned band — by extension, any activity that falls through the gap between competing authorities and is therefore nobody's problem to stop.

---

### §A33-2 — The Ascent Bloom (new technology, binding)

The **Ascent Bloom** is the upward complement to the Kite Rig. Where the Kite Rig converts a free-fall into a slow drift down, the Ascent Bloom converts a surface position into a controlled ascent to aerostatic platform altitude.

**How it works:** The platform operator drops a weighted CNT-steel guide tether down through The Low to a surface extraction point. The surface operator clips on a Bloom pack — a 40×25cm cylinder of VABC cells in compressed configuration. On activation, cells self-pressurize to full volume within 8–12 seconds (the *bloom*). The VABC cluster provides buoyancy lift; the guide tether provides directional control against wind; the platform winch assists the final 150m. Ascent time to Aeroquarter altitude: 12–18 minutes. To Aeropolis: 35–55 minutes. Payload: one person plus light kit (standard); double-pack configuration for heavy cargo or two people.

**Gray market variant:** rapidly-inflating hydrogen gas bladders. Faster (5–8 minutes to Aeroquarter altitude). Visible from a significant distance. Explosive-hazard rated under municipal code. MATA radar flags the ascent immediately above 320m. Used when speed matters more than concealment.

**The Ascent Bloom is the primary mechanism for circumventing the Air Tax from below.** No VTOL manifest. No biometric verification. No MATA record below 320m. This makes it the logistics tool of choice for gray-market cargo movement, covert extraction, and anyone whose VTOL access has been revoked.

**Cutting the bloom** — severing a tether line mid-ascent — is a killing technique distinctive enough to have its own classification in Arcturus Civil Security's crime taxonomy.

**Three-register naming:** Ascent Bloom (official) / Sky Hook, the Lift (Meridian 88) / the Bloom, Tether Bloom (Glooms)

---

### §A33-3 — The Low Runner Kit (professional rig, binding)

The **Low Runner Kit** is a professional-grade descent and ascent rig built for operators who transit the full vertical axis without VTOL access. Key systems (locked):

- **Wing membranes:** wrist-to-hip articulated extensions; ~3:1 glide ratio; 1,500m horizontal travel from 2,000m altitude; used for precision surface targeting and navigation through The Low's building-wake turbulence
- **Integrated Kite Rig:** factory-standard VABC cluster on dorsal frame; same 80m safe-floor as standalone rig
- **Bloom dock:** ventral clip-point accepting Ascent Bloom pack for return ascent
- **Impact attenuation:** aerogel composite leg/torso zones rated for hard landing at Kite Rig terminal drift velocity on unimproved terrain
- **Navigation display:** heads-up compass, altimeter, and tether-signal receiver; essential below MATA radar floor where external positioning is unavailable
- **Low-profile build:** civilian-legal configuration; no weapons mounts, no corpo insignia

The Kit is not occupation-specific. It is equipment: the practical necessity for any operator who needs surface-to-float access regularly and cannot afford VTOL passage.

**Street names:** Run coat, Low skin, the Kit.

---

### Entities seeded (2026-06-30, this pass)

| Entity | Type | ID prefix | Street name |
|---|---|---|---|
| Ascent Bloom | Technology | `019f1b3b-da9d-…` | Sky Hook / the Bloom |
| The Low | Place | `019f1b3c-3b28-…` | The Low |
| Low Runner Kit | Equipment | `019f1b3c-970b-…` | Run coat / Low skin |

**22 world-graph edges** built (`source: helldivers-fulton-worldbuild-2026-06-30`): Bloom→VABC (derived_from), Bloom↔Kite Rig (complements), Bloom→platforms (circumvents_access_to), Bloom→Derelict Platforms (supplies), Bloom→The Low (traverses), The Low→Derelict Platforms (contains), The Low→Kite Rig and Bloom (hazards traversal), Kit→Kite Rig and Bloom (integrates), Kit→The Low (designed_for), Kit→platforms (enables_access_to).

**Total aerostatic cluster (pre-faction):** 20 entities, 58 edges.

---

### §A33-4 — Vertical Axis (faction, binding)

**Vertical Axis** is the informal name for the loose network of freelance operators who specialize in full vertical transit — aerostatic platform altitude through The Low to the surface, and back up again. Not a faction in the traditional sense: no charter, no initiation, no hierarchy. The name is a reputation marker, not a membership card. Operators who consistently run The Low — who know the tether drop points, the derelict platform landing zones, the bloom stash locations, the pad contacts at aeroquarters across the float layer — are Vertical Axis. Everyone else just calls them by altitude.

The name is a dark joke about the GLMZ's social axis. Class runs horizontal here: zone, corpo, affiliation. Vertical Axis runs perpendicular.

**Structure:** Reputation-organized. The most-connected operators — platform contacts at multiple aeroquarters, surface contacts across multiple zones — carry informal weight. Veteran operators vouch for newcomers, share stash locations, make introductions. That vouching is the only leadership that exists. It transfers one relationship at a time.

**Primary client:** Gray Zone Derelict Platform communities — residents with no VTOL access who rely on Vertical Axis logistics for VABC cells, micropump components, medical supplies, and anything that won't move through a MATA manifest.

**Operative reach:** The Low (30–320m ungoverned band) is their transit corridor. Float-layer pad access and surface extraction zones are their endpoints. Their effective territory is the vertical axis itself.

**Motto:** *Up or down. We move.*

**Entity ID:** `019f1b5d-e6ee-7745-8427-9f65a3bfb411`; 7 world-graph edges: operates_in→The Low, uses→Ascent Bloom/Kite Rig/Low Runner Kit, serves↔Gray Zone Derelict Platforms (mutual), circumvents→VTOL-Only Access Protocol.

**Total aerostatic cluster (revised):** 21 entities, 65 edges.

---

## SS-A34 -- QCE retired; NSB is the canonical remote-operation term {#SS-A34}

**Date:** 2026-06-30 · **Author:** nsb-canon-pass · **Supersedes:** the QCE point in [SS-A27](#SS-A27) · **Ref:** `memory/project_nsb_qce_queued_update.md`

### Ruling

**"Quantum Crystal Entanglement" is retired as an in-universe mechanism name.** It was never physically coherent: quantum entanglement does not transmit information faster than light, and observation collapses entangled states rather than preserving them. Using it as the basis for remote consciousness projection produces a mechanism that is "it just is" rather than anything a character could fail at, be blocked by, or die from. It cannot generate the failure modes that make this technology narratively load-bearing.

**The canonical term is NSB -- Neuretic Substrate Bridging.** The theoretical underpinning is CEP -- Coherent Eigenstate Projection. See entities seeded 2026-06-30:

- **NSB (Neuretic Substrate Bridging)** -- The technology: remote consciousness projection into a substrate with a compatible neural bus
- **CEP (Coherent Eigenstate Projection)** -- The mechanism: consciousness projected as a coherent waveform into a resonant cavity; not transmitted, not copied
- **Shell** -- The operator's body during projection -- managed low-activity suspension
- **Return handshake** -- The critical vulnerability: no clean handshake, no clean return
- **Black Ice** -- Destructive resonance countermeasure -- dumb (broadband noise) or military-grade (adaptive, learns the operator's eigenstate signature)
- **Frame Pilot / Wire Jockey** -- Professional register for NSB operators
- **Ghost-rider / Skinner** -- Glooms register for NSB operators
- **Burning a frame** -- Meridian 88 register: executing a projection op

**"Rider" job title is preserved** -- decoupled from QCE and now understood as shorthand for Frame Rider / NSB operator. The compound phrase "QCE Rider" is retired; prose uses "Rider" alone or "Frame Pilot."

**"Jacked in via QCE"** (the phrase from SS-A27) is retired with it. Operators "burn a frame," "thread out," or "go deep." No jacking. No entanglement.

### The QCE entity

The `Quantum Crystal Entanglement` technology entity (`019f00a6...`) is retained in the DB as a **deprecated historical artifact** -- the brand name Glyph Analytics used when licensing neural bus hardware to clients who didn't want to understand how it worked. In-world, it is a product name that outlived its explanatory value. Technicians, Frame Pilots, and anyone who has run wire finds it embarrassing. Tagged `deprecated`; new canon does not use it.

### What does not change

- SS-A27's Neo-Cyberpunk positioning, "no crude plugs" rule, and the ban on datajacks/trode-nets all stand.
- Channeler (communing with AIs) and Splicer job titles are unaffected.
- Glyph Analytics remains the licensed hardware vendor for neural bus equipment.
- The Arcturus NS-series frame product line (Tendril, Shade, Reacher, Golem) is the canonical frame roster.


---

## SS-A35 -- Technology must extrapolate real science; anti-grav retired; NSB as pervasive infrastructure; the Glass {#SS-A35}

**Date:** 2026-06-30 · **Author:** physics-canon-pass · **Supersedes:** Graviton Manipulation Theory as anti-grav foundation; Zero-Point Energy as power source

---

### §A35-1 -- The physics law (binding for all Universes under this engine)

**All GLMZ technology must be grounded in demonstrably real or plausibly extrapolated science.** The bar is the flight analogy: heavier-than-air flight was theoretically understood in 1900 and ubiquitous by 2000. A lab-demonstrable phenomenon today can be the substrate of an entire industry in 200 years. What is physically impossible today -- violates thermodynamics, requires FTL information transfer, demands a negative-mass exotic matter that has never been detected -- stays impossible in 2226.

This is not a constraint on imagination. It is the constraint that makes failure modes real. A technology the audience cannot poke holes in cannot generate the kind of stakes where characters die.

---

### §A35-2 -- Anti-gravity is retired; confirmed orbital infrastructure

**GLMZ does not have anti-gravity, anti-graviton drives, graviton manipulation, or any form of gravity cancellation.**

The confirmation is in the infrastructure already in canon: **space elevators** (tensile + counterweight; no gravity cancellation needed or possible) and **mass drivers** (electromagnetic catapult launch; no magic required). Both are physically coherent extrapolations of technology that exists today in prototype form. Neither requires, implies, or is consistent with functional anti-gravity.

Graviton detection (predicted by quantum gravity; plausible by 2098) is retained as a scientific achievement. **Graviton manipulation for propulsion or gravity cancellation is not.** The gap between "we detected gravitons" and "we can manipulate gravity at will" is the same as the gap between "we detected the Higgs boson" and "we can control mass." Detection is measurement. Manipulation is engineering. The engineering gap is not bridged.

The `Graviton Manipulation Theory` entity is rewritten as a **theoretical research program that produced its biggest finding (detection) and then stalled on the manipulation problem**. It is an active unsolved frontier, not an achieved technology.

---

### §A35-3 -- Zero-point energy is retired as a power source

**GLMZ does not have zero-point energy extraction as a usable power source.**

The Casimir effect is real: measurable vacuum energy exists, and it has been demonstrated in laboratory conditions. The thermodynamic problem is also real: vacuum fluctuations are the ground state of the quantum field -- by definition, there is no lower state to extract work from. Extracting net energy from ZPE is a form of perpetual motion. The second law does not bend.

What survives: Casimir-effect-based precision sensing and measurement is a real and legitimate 2226 technology. The ZPERP entity is rewritten as a **fundamental research facility that proved the extraction hypothesis false** -- a canonical dead end that resolved one of the great 22nd-century science disputes.

GLMZ's actual power sources remain: advanced fusion (tokamak-based, per the UNDR infrastructure), solar collection at scale, kinetic/tidal, and the chemical/nano hybrid systems that power personal and industrial equipment.

---

### §A35-4 -- NSB is pervasive civilian infrastructure, not a military specialty

Remote consciousness projection via NSB (Neuretic Substrate Bridging) is not a niche military capability. It is the 2226 equivalent of what the internet did to remote work -- except you are not on a video call, you are there.

Any job, task, or experience that benefits from physical presence but involves unacceptable risk, distance, or cost has an NSB solution:

- **Dangerous construction, demolition, hazmat** -- you do not send a human body into a building collapse or a chemical spill; you burn a frame
- **Deep infrastructure maintenance** -- sewer systems, sub-Pulse conduit, reactor cores; Exos earn the premium rate
- **Surgery at distance** -- a specialist in the Loop projecting into a surgical frame in Z11; the patient's outcome depends on the Exo's handshake latency
- **Camera journalism and spectator presence** -- you are not watching a war or a sporting event; you are there, in the frame, at ground level; the feed is what your optics record; returning from a firefight or a crowd crush leaves Echoes
- **Logistics and freight handling** -- dockworkers burning frames at the harbor; warehouse Exos who have never physically visited the facility they work in every day
- **Inspection and compliance** -- every structural, environmental, and safety inspection that used to require a person in a hard hat
- **Remote presence services** -- the long-distance equivalent of being in the room; an Exo can attend your parent's funeral in Osaka while their Shell sits in Meridian

The social consequence: **presence is commodified**. An Exo can be in twelve places in one day. This creates a class of workers who are paid per-projection at rates that undercut any alternative -- and who carry the echo-load of dozens of lives they were never actually living. The long-term occupational hazard of professional Exo work is not injury; it is dissociation.

**The "Dollhead" failure mode** (from SS-A34: operators who burn frames so often the home body starts to feel borrowed) is not a military edge case. It is an occupational disease with a prevalence rate, an advocacy community, and a contested workers-comp framework.

---

### §A35-5 -- Virtual substrate threading: full-reality immersion and the Glass

The same CEP mechanism (Coherent Eigenstate Projection) that enables projection into physical frames works equally on constructed virtual substrates. The neuretic array does not distinguish: it projects a coherent eigenstate into a resonant cavity. Whether that cavity is a drone chassis or a server-rendered environment is a hardware question, not a physics one.

**Three grades of virtual substrate:**

**Work grade**: sensory-calibrated to real-world parameters, task-focused. The primary surface for remote office presence, simulation training, collaborative design. You are somewhere you are not, but the somewhere is governed by real physics. Failure modes: lag artifacts, partial sync, the same Echoing that accumulates in physical frame work.

**Entertainment grade**: full sensory, constructed reality, parameters real-world-adjacent. Live sports presence (you are at the match, not watching it), concert presence, historical reconstruction tourism. Passive and active tiers. The echo-load of entertainment-grade immersion is low because the events are not traumatic.

**The Glass**: entertainment substrate with sensory parameters tuned beyond real-world maxima. Pleasure responses amplified. Pain gates closed. No sensory calibration to reality. Immediately addictive: the brain adapts; the real world's untuned, unfiltered sensory signal registers as thin, grey, and insufficient by comparison. The Glass is to standard immersion what fentanyl is to ibuprofen -- the mechanism is the same; the calibration is what kills you.

The Glass's specific failure mode is **deliberate Echo contamination**: the substrate is designed to leave traces. Glass addicts carry echo-memories of experiences that feel more real and more recent than their actual lives. The clinical term is **Substrate Echo Disorder (SED)**. The street term for someone in late-stage SED is **glass-brain** -- someone whose real life has become the echo.

**Glass lingo:**

- "Going glass" -- entering a Glass-grade substrate
- "In the glass" -- currently under Glass immersion
- "Glass-brain" / "glass-head" -- addict; someone whose real life feels thin
- "Echo-drunk" -- the disorientation period after extended Glass use when memories sort wrong
- "The surface" -- real life, from the perspective of a Glass addict (derogatory; implies real life is the shallow copy)
- "Surfacing" -- returning from immersion (same word as NSB; same experience)
- "Running glass" -- doing a Glass session (professional/street register)

---

### §A35-6 -- Holographic volumetric media (Trideo equivalent)

The 2226 standard media format is **Volume** (noun and verb: "a volume," "to cast a volume"). Flat 2D media is called "flat" -- what your grandparents watched, still technically viewable, culturally associated with the pre-2100 era. The word "video" is what anthropologists use.

- **Volume** -- any holographic volumetric media; 3D space you can walk around and move through; projected into any room by standard Tessera-manufactured surface-coating
- **Cast** -- live volume broadcast ("the cast," "casting," "catch the cast")
- **Hot vol** -- live interactive volume with audience-presence participation (you are a ghost in the room where it's happening)
- **Flat** -- 2D media; the derogatory term for anything on a traditional screen; also used as an adjective for experiences that feel low-resolution: "that conversation was flat"
- **Depth** -- a Volume production with significant spatial complexity; used as a quality marker ("this is depth work") and as a casual shorthand for prestige content

Volume does not require neural connection -- it is ambient spatial media. The Glass is the neural tier. Most people can access Volume with nothing more than the Tessera surface-coating in their apartment. The Glass requires a functioning neuretic array.

---

### §A35-7 -- GLMZ technology lingo: master reference

A consolidated glossary of established GLMZ tech register terms across aerostatic, NSB, and virtual domains. Canonical; prose should draw from this.

**Aerostatic (SS-A32/A33)**
- Aeropod -- small personal aerial vehicle (individual/couple capacity)
- Aeropolis -- dense aerial district; a city above the city
- Drifter -- permanent aerial resident; no surface address
- The Low -- ungoverned band 30-320m above street level; the Churn's floor
- The Churn -- hazard band 30-300m; wind-shear, debris, unregulated traffic
- Kite Rig -- personal downward-transit harness (fall arrest + glide)
- Ascent Bloom -- upward transit device; controlled buoyant ascent
- Low Runner Kit -- full-axis rig; Kite Rig + Ascent Bloom combined
- Hell Diver -- Churn-traverse professional; extreme occupation
- Churn Diver Suit -- hardened atmospheric transit suit for Churn work
- Air Tax -- the altitude-tiered levy that makes altitude a class axis

**NSB / Frame Piloting (SS-A34)**
- NSB (Neuretic Substrate Bridging) -- canonical term for remote consciousness projection
- CEP (Coherent Eigenstate Projection) -- the mechanism; eigenstate projected into neural bus cavity
- Shell -- the operator's body during projection; managed low-activity suspension
- Return handshake -- the critical vulnerability; how the eigenstate gets home
- Burning a frame -- executing a physical projection op (Meridian 88)
- Threading out -- beginning projection (Meridian 88)
- Going deep -- being in the frame (Meridian 88)
- Surfacing -- successful return from any projection (all registers)
- Ghost-riding / Skinning -- Glooms register for NSB operation
- Haunting -- Glooms: partial sync; frame won't respond clean
- Frame Pilot / Wire Jockey -- professional registers for NSB operators
- Remote Frame Operator (RFO) -- formal/corporate job classification; used in official records, Arcturus licensing, HR, corp job postings
- Rider -- RETIRED as of SS-A38; superseded by Exo (street) and Remote Frame Operator (formal)
- Dollhead -- Glooms: operator who burns frames so much the home body feels borrowed
- Feedback Cascade -- substrate destruction propagating back through the projection
- Partitioning (Smear State) -- sync below coherence threshold; awareness smears across both bodies
- Black Ice -- destructive resonance countermeasure; dumb or military-grade adaptive
- Echoing -- residual substrate sensory data imprinting on the returning eigenstate; accumulates
- Sync ceiling -- the bandwidth limit of an operator's neuretic array; breach = dangerous
- Burned out -- Glooms: cascade flatline; the operator did not return usefully
- Gone ghost -- Glooms: echo-dissolved identity; the operator is still there but not themselves

**NS-series frames (Arcturus Defense Solutions)**
- NS-1 'Tendril' -- light recon/maintenance; ~10% bus load; entry-level training frame
- NS-4 'Shade' -- armed combat drone; ~35% bus load; most common professional platform
- NS-7 'Reacher' -- light combat mech; ~60% bus load; demanding; 200hr prerequisite
- NS-9 'Golem' -- heavy combat mech; ~80-90% bus load; near-ceiling; terminal risk is documented

**Virtual threading / the Glass (SS-A35)**
- Work grade -- task-focused virtual substrate; real-world sensory calibration
- Entertainment grade -- full sensory constructed reality; real-world-adjacent parameters
- The Glass -- sensory parameters beyond real-world maxima; immediately addictive; the BTL tier
- Going glass -- entering a Glass-grade substrate
- Glass-brain / glass-head -- addict; late-stage Substrate Echo Disorder
- Echo-drunk -- post-Glass disorientation; memories sort wrong
- The surface -- real life (addict register; derogatory toward reality)
- Running glass -- doing a Glass session
- Substrate Echo Disorder (SED) -- clinical: accumulated echo-memories feel more real than lived experience

**Holographic media / Volume (SS-A35)**
- Volume -- standard 3D holographic media format of 2226; ambient, no neural required
- Cast / the cast -- live volumetric broadcast
- Hot vol -- live interactive volume with presence participation
- Flat -- 2D media; derogatory; also used as quality judgment ("that was flat")
- Depth -- prestige volumetric production with significant spatial complexity

---

### §A36 -- Underwater community slang: "Mermaids" replaces "Fishmen"

The surface-speaker slang for GLMZ's underwater communities is **"Mermaids"** (and by extension "Mermen" for individuals, though "Mermaid" is used as a blanket insult regardless of gender). "Fishmen" is retired.

**Why Mermaids wins:** A feminine-coded mythological insult applied dismissively to the underwater communities carries more social texture than the neutral "Fishmen." Surface speakers — especially the macho, para-military, corpo-security demographic — use "Mermaids" with contempt precisely because it feminizes and mythologizes people they regard as non-citizens. The word does double work: dehumanizes AND condescends.

**Canonical usage:**
- Outsider insult (surface speakers): "Mermaids" — e.g., *"Those fucking mermaids are going to pay."*
- Underground communities call themselves by their own names (Underclan, etc.); they do not use "Mermaids" for themselves.
- Sky-dwellers calling ground-floor residents "little birdies" or "Harpies" is the vertical mirror of this.

**Scope:** All GLMZ prose. UNDR is the primary home but any surface-POV chapter may use this as ambient texture.

---

### §A37 -- UNDR data-recovery incident + direct-SQL prohibition

On 2026-07-01, all 14 chapter sub-strands of UNDR (Ch01–Ch14) and their 55 StrandBeat links were deleted via a direct SQL operation. Root cause: unknown (no CLI, MCP, or Blazor UI path for Strand deletion exists). The Beat rows themselves survived because `StrandBeats.StrandId` cascades to the junction table, not to `Beats`.

**Recovery:** Temporal tables (`FOR SYSTEM_TIME AS OF`) preserved all data exactly. All 14 chapter strands and 55 StrandBeat links were restored from the `AS OF '2026-07-01T13:07:00'` snapshot on 2026-07-02.

**Prohibition (binding rule):**
- **Never execute `DELETE FROM Strands`, `DELETE FROM Beats`, or `DELETE FROM StrandBeats` as a raw SQL statement** without first running a `FOR SYSTEM_TIME ALL` verification query and confirming the rows are genuinely orphaned/empty.
- If a delete-strand operation is ever needed through the engine, it must go through a CLI/service-layer method that (a) checks for active beats and blocks if any exist, and (b) logs the operation.
- The `sqlcmd` read-only lookups in CLAUDE.md are for SELECT only. Any write via sqlcmd requires explicit user confirmation stating "yes, delete strand X."

---

## SS-A39 — Sky Folk; VTOL-only to aeroplexes; underground foot-passage only; the Tartaria question {#SS-A39}

**Date:** 2026-07-02 · **Author:** world-connective-tissue pass · **Ref:** extends [SS-A32](#SS-A32)/[SS-A33](#SS-A33) aerostatic canon; extends [SS-A22](#SS-A22) UNDR canon; connective tissue for all GLMZ zones.

---

### §A39-1 — "Sky Folk" / "Sky People" (new term, binding)

People who live on aerostatic platforms — aeroquarters, aeropolises, Drifter communities — are called **Sky Folk** (plural noun) or **Sky People** by outsiders and, with varying degrees of irony, by themselves. Singular: **Sky Person**, though most surface speakers say "a Sky Folk" without correction.

- **Surface-speaker register:** neutral-to-slight-envy. Less contemptuous than "little birdies" or "Harpies" (which are insults hurled *downward* from the float layer, not upward from the ground — see [SS-A36](#SS-A35)).
- **Aeropolis resident self-use:** dry irony. "We Sky Folk" in a boardroom is a joke about altitude privilege that stops being funny at the third drink.
- **Underground-speaker register:** the phrase carries genuine otherness. For someone who has never seen the sun, people who live above the clouds belong to a different taxonomy entirely.
- **Not an insult in itself** — it is the ambient category word. Contrast with "little birdies" / "Harpies" (downward insults) and "Mermaids" (surface-to-underwater contempt).

**Binding for all GLMZ prose.** Replaces descriptive phrases like "float-layer residents" in casual registers. "Drifters" remains the precise term for derelict-platform residents ([SS-A32]); "Sky Folk" is the broad category covering all aerostatic-platform residents.

---

### §A39-2 — VTOL as the only legitimate transport to aeroplexes (formalizing [SS-A32])

An **Aeroplex** (any aerostatic platform above surface level) is reachable only by VTOL aircraft through official channels. The Pulse does not extend into the float layer — its vacuum-tube infrastructure is ground-anchored. Surface transit, walking, and cycling do not reach aeroplex altitude.

The informal alternatives — Ascent Bloom, Low Runner Kit, gray-market Kite Rig descent, Vertical Axis operators ([SS-A33]) — circumvent the VTOL-Only Access Protocol precisely *because* VTOL is otherwise the only path up. All non-VTOL access to the float layer is gray-market or criminal.

**Prose rule:** A character cannot take the Pulse to an Aeroplex. The Pulse runs between surface nodes. The air layer requires air transport or a gray-market vertical rig.

---

### §A39-3 — The underground: foot passage only; unmarked access

The underground is not Pulse-connected, not VTOL-accessible, and not reachable by any registered transit mode. Access is **exclusively on foot**, through passage-ways that are unregistered, unmarked, and known only to those who use them.

These passages are not signposted. They do not appear on any Meridian 88 infrastructure map — the maps show deep utility zones as blank management areas, not traversable networks. Finding an entrance requires knowing someone who knows. Navigating from entrance to destination requires memory or a guide. There is no underground station. There is a gap in a ferrocrete wall behind a decommissioned pump relay two blocks east of the old meatpacking district, and you either know about it or you don't.

The surface treats the underground as pure infrastructure (sewers, power conduits, Pulse anchoring, reactor access). The reality — that communities have navigated, lived in, and mapped this space across generations — is absent from official records because putting it on a map would mean admitting the people who live there exist.

---

### §A39-4 — "Tartaria" and the navigation failure of The Works

**"Tartaria" is a contentious word in 2226.**

Surface-speakers who use it — academics, conspiracy theorists, underground-community advocates — mean: a supposed pre-collapse civilization whose architectural ruins lie deep beneath the current city, deeper than any mapped infrastructure, distinguished by vaulted stonework, drowned beaux-arts halls, and a scale and quality of workmanship inconsistent with documented 19th/20th-century Chicago construction. The GLMZ historical establishment regards the claim as unfounded folk mythology: extraordinary architecture that far underground would require extraordinary evidence, and no verifiable evidence has ever been produced.

Underground communities — the Underclan (*the Tartar*, *the Glory*), the Engine Guild, scav foragers who go deep — insist the ruins are real. Individuals have seen them. They describe consistent details: vaulted stone, high ceilings, rooms larger than any utility corridor, decorative stonework with no business being at that depth. What they cannot do is lead anyone back to the same place.

**The navigation problem — The Works:**

The underground infrastructure beneath Meridian 88 is known as **The Works**: the accumulated, layered, never-fully-documented product of two centuries of arcology construction, corporate utility builds, abandoned transit projects, decommissioned service networks, and Pulse deep-anchoring infrastructure. It is not a system. It is not a map. It is the residue of every urban project that ever bored a hole in the ground and never fully closed it.

The Works is enormous. The Works is contradictory. Passages that connected last month may be walled by a new utility run this month. A route memorized one season may dead-end into a tokamak expansion wall the next. Guides with deep-navigation competence lose their orientation below a certain depth because the geometry below the mapped strata no longer follows the logic of infrastructure — it follows something else.

The specific claim that witnesses cannot be led back to the Tartarian ruins is not merely "navigationally difficult." People who attempt it describe a consistent experience: the route that should have been correct was not. They followed their guide exactly. They reached the right depth, the right strata indicators, the right pipe configuration. The vaulted rooms were not there.

**Binding rules (all GLMZ prose):**

- **Never confirmed as supernatural.** The Works is real, vast, and maze-like. Navigation failures have a mundane explanation: the geometry below Homewater is genuinely labyrinthine, memory is fallible, and two visits to "the same place" may use different routes that only feel identical.
- **Never confirmed as mundane.** The consistency of the experience — competent underground navigators, getting turned around at exactly the depth where the ruins should be — resists a clean infrastructure-complexity explanation.
- **"Tartaria" as a word carries political weight.** Surface establishment uses it to dismiss. Underground communities use it defiantly. Academics who use it carefully are making a claim about taking underground testimony seriously. In a corpo context it signals conspiracy thinking; in the underground it signals witness.
- **LOCKED:** Whether the ruins exist, what they are, how old they are, and why they cannot be reliably found is never authoritatively resolved. Treat exactly as the Prisoner, DEEP CURRENT, and the SRZR camel contact — the ambiguity is the canon.

**DB entities to seed:** "The Tartarian Question" (Concept); "The Works" as the GLMZ-wide accumulated underground infrastructure (Place, if not already present as its own entity distinct from the UNDR strata layer); "Tartaria" (Place, disputed/legend).

---

### §A38 -- "Rider" fully retired; Exo = street/prose, Remote Frame Operator = corporate/formal

**"Rider" is retired** as a living term. It survives only in historical amendment text (SS-A34, SS-A35 references) as documentation of prior usage. Do not use it in new prose or world-building.

**Street / prose term:** **Exo** — from "exo out," casting the conscience out of the body. Used in all narrative prose, street dialogue, and informal world-building text. Takes "an" (vowel onset): *an Exo*, *an Exo's Husk*, *an Exo's crawler*.

**Formal / corporate term:** **Remote Frame Operator** (abbrev. **RFO**) — what Arcturus licensing, corp HR departments, job postings, and official records call the role. Nobody on the street says this. A corpo filling out paperwork writes "Remote Frame Operator, Class II." An Exo says "I'm an Exo."

**Alternate street slang:** **Jockey** — same register as Exo; valid in prose, especially for older characters or corpo-adjacent street speakers. ⚠️ **SLUR GUARD (binding):** "Jockey" must NEVER appear adjacent to camel imagery — "camel jockey" is an ethnic slur, banned in all prose. The SRZR 5D contact is always **"the man on the camel,"** never Exo/Jockey/camel-jockey. Sasha Võ is not an Exo/Jockey (Signal+Noise, not QCE).

**Disambiguation:** "rider" lowercase in non-profession contexts (carousel horses, bike couriers, gang fledglings on bikes, contract addenda) is unaffected — these are literal physical riders and not the profession.

**Scope:** All GLMZ prose going forward. Export snapshots updated 2026-07-02.

---

## SS-A40 — Border Markets: the threshold economy between CorpoNations and Gray Zones {#SS-A40}

**Date:** 2026-07-02 · **Author:** connective-tissue pass · **Ref:** extends [SS-A29](#SS-A29) CorpoNation/Gray-Zone canon; adds social texture at the zone threshold; introduces "Corpozens" slang.

---

### What they are (binding)

**Border Markets** are the informal economies that form immediately outside every walled CorpoNation entrance. They are not planned, not licensed, and not on any corpo map — they grow where the two worlds touch.

The dynamic is structural: CorpoNation citizens need things the corpo cannot or will not provide at the price or speed they want. Gray Zone residents need income and access to corpo-grade goods they can't otherwise reach. The gate is the bottleneck; the market is the response to the bottleneck. Every major corpo entrance has one.

**The geography of mutual tolerance:**
- **How far out a Corpozens (corpo citizen) feels safe to venture:** the answer is not a fixed distance — it is a feeling. The Border Market is *exactly as far out as corpos still come*. It calibrates itself to that edge in real time. On a calm week, the stalls push two blocks into the Gray Zone. After a Block War skirmish, they retract to the shadow of the gate itself. The market is a living barometer of zone tension.
- **How close to an arcology a Gray Zone dweller will get:** the Border Market is the answer. Past the market, corpo security starts asking questions. At the market, security tolerates presence because the market serves corpo demand. Before the market, there is no formal threshold. Gray Zoners attend the market; they do not, as a rule, go through the gate.

The result: **the Border Market is the primary social interface between two populations that are otherwise sealed from each other.** It is where corpo aesthetics and Gray Zone aesthetics bleed. It is where corpo workers buy food they pretend isn't there. It is where Gray Zoners get secondhand corpo tech, counterfeit corpo rations, and the occasional corpo worker willing to talk.

---

### Slang — "Corpozens" and related terms (binding)

**Corpozens** (noun, plural; singular **Corpozen**) — Gray Zone slang for CorpoNation citizens. The word mashes *corpo* and *citizen* and lands with a particular register: not quite contempt, not quite envy. A Corpozen is someone who traded their autonomy for the arcology's walls and calls it safety. The word is used casually and without heat by most Gray Zoners, the way a city person might say "suburbanites." The people it describes mostly don't use it for themselves — if they've heard it at all.

| Term | Speaker | Meaning |
|---|---|---|
| **Corpozens** / **Corpozen** | Gray Zone | CorpoNation citizens — the walled-in; people who live inside |
| **Wallborn** | Gray Zone (older register) | Someone born inside an arcology; never seen unmanaged sky |
| **The inside** | Gray Zone | The arcology; the walled city within a city |
| **Gate heat** | Any | Heightened corpo security at the entrance — market contracts when gate heat rises |
| **Threshold run** | Gray Zone street | A visit to the Border Market to sell or acquire; "I'm doing a threshold run" |
| **Market shadow** | Gray Zone | The safe zone for Gray Zone activity — directly under the corpo security's willingness to turn a blind eye |
| **Gray Mouse** | Corpo pejorative | A Gray Zone person at the border market — someone scurrying close to the wall, looking for scraps. The gentler of the trio; used without heat by corpo workers who consider themselves tolerant. |
| **Gray Rat** | Corpo pejorative (hostile) | Escalated form; implies the Gray Zone person is a pest, a scavenger, possibly a thief. Used when a corpo wants to remind someone they're not welcome. |
| **Gray Scum** | Corpo pejorative (aggressive) | The ugly end of the register. Used by corpo security and corpo ultranationalists; the kind of word that starts a confrontation at the gate. |
| **Corpo Mouse** | Gray Zone pejorative | A Corpozen at the border market — someone who crept out from behind the walls for a look. Mild, almost affectionate contempt. |
| **Corpo Rat** | Gray Zone pejorative (hostile) | Escalated form; implies the Corpozen is an informant, a spy, or a corpo security plant running a soft surveillance pass. |
| **Corpo Scum** | Gray Zone pejorative (aggressive) | The ugly end. Used when a Gray Zoner wants the Corpozen to know exactly where they stand. The kind of word that clears a stall. |

---

### Narrative function (standing prose directive)

Border Markets are **connective tissue between zones**. They appear in any GLMZ strand where a character crosses between corpo and Gray Zone space. Use them to:

- Show the mundane texture of the threshold — the smell of cooked food, the voltage of a vendor argument, the sight of corpo uniforms at the edge buying things their ID shouldn't touch
- Establish zone tension through market size: a contracted market (vendors close to the gate, few Corpozens outside) signals recent trouble without exposition
- Give characters from different zones a plausible shared space where encounters happen without forced contrivance
- Show cultural bleed in both directions — corpo aesthetics in Gray Zone goods (knockoff corpo ration packs, corpo fashion recut), Gray Zone goods moving inward (underground food, handmade tech, contraband)

**The market does not need to be the scene.** It can be a paragraph of texture while characters pass through. A character who pauses at the threshold has a reason; a character who keeps walking has a different reason. The market asks the question; prose uses the answer.

---

### Entities to seed

- **Border Market** (Place, type: market/faction threshold) — generic category entity; individual named markets (e.g., "the Axiom Threshold Market," "the Meridian Gate Exchange") seeded per strand as needed
- **Corpozens** — add as a slang-register entry to the vocabulary canon; not a separate entity




## SS-A41 — Character Doctrine: circumstance → choice → definition {#SS-A41}

**Date:** 2026-07-03 · **Author:** character-doctrine-pass · **Ref:** `docs/strands/PNHL.md` §10a; extends `docs/BIBLE.md` §10 narrative laws; applies to all universes and strands.

---

All characters are defined by their choices and actions — specifically, choices made in response to circumstances beyond their control. The circumstances define what they face; the choices define who they are.

**The wound is not the character. The response to the wound is the character.**

### Application (binding for all prose)

**Pixel** (PNHL): Her mother died in a failed licensed SNT bridge. Beyond her control. She chose to come to GLMZ, keep the boots, learn the city's systems, fight back rather than leave. Those choices define her — not the loss.

**The Assessor** (PNHL): Something happened to him. What he has done with that — building a coercive market, deciding consent is expendable, running fourteen cases — is the choice that defines him. The thing that happened is backstory. The operation is character.

**Any character across all strands:** What happened to them is backstory. What they do about it is the story.

### Binding prose directive

Never anchor a character in what was done to them. Anchor them in what they do next. A wound earns its page space only by driving a choice. Backstory that doesn't produce a choice is biography, not character. If a scene explains a character only in terms of what happened to them without showing what they chose in response, cut the explanation and show the choice.

---

## SS-A42 — Deep Lake: no communities; one sealed black site; "Sky People" = the privileged class {#SS-A42}

**Date:** 2026-07-03 · **Author:** world-consolidation · **Ref:** retires §A36; confirms §A39-1

---

### Underwater communities are retired (binding)

There are no permanent human communities living beneath Lake Michigan. No hermetic-dome neighborhoods. No flooded-port communes. No lakebed settlements. The idea of a subculture choosing to live at the bottom of a freshwater lake — sustaining functional communities there — is cut from GLMZ canon. It has no social logic (the lake offers no aspiration, no benefit, no leverage), no narrative payoff, and dilutes the vertical class axis that actually works.

**What remains on the lakebed:**

- **Ruins.** Fifteen-plus decommissioned corporate research installations from the 2060s–2200s. Most are flooded, structurally failed, and dark. Salvage crews (The Lakebed Scrapers, The Silt Syndicate) work the shallower ruins for hardware and rare materials. Nobody lives in them.
- **One sealed facility.** Sensor records that Arcturus Civil Security does not officially acknowledge indicate exactly one installation on the lakebed is still drawing power. Its corporate affiliation is not in any public registry. Its coordinates are not on any public map. What it researches is not confirmed. It is not a community. It is a black site. *Treat as one of GLMZ's locked mysteries — real, occupied, never confirmed in-fiction.*
- **Surface-only operations.** Harbor gangs (The Fathom Line, The Rip), waterfront salvagers, and diving crews work the water and the shallow ruins for economic reasons. They surface. They do not live there.

**§A36 is retired.** "Mermaids" and "Fishmen" as slang for underwater communities are cut along with the communities themselves. There is no community to reference. The Underclan (underground, not underwater) use their own name; any surface-speaker insult for underground dwellers belongs to per-strand bibles, not a GLMZ-wide amendment.

---

### Sky People = the privileged class (binding, confirms §A39-1)

**Sky People** are the wealthiest and most privileged residents of GLMZ. They live above the city — aeroblocs, aeroplexes, licensed high-altitude platforms — and the altitude is not incidental. It is the point. Sky People are literally above it all: above the pollution, the Block Wars, the smells and density and consequences of the city they profit from.

Elevation costs money and signals power. The higher you live, the less the city touches you. Sky People pay a premium not just for the view but for the distance. The street does not reach them. That is the amenity.

The **vertical class axis** runs from absent/underground through street-level through sky. There is no competing underwater narrative. The lakebed is where nothing is: ruins, one sealed secret, absence. The sky is where wealth concentrates. The axis is clean.

**Prose directive:** A Sky Person's altitude is a character fact. A street-level character looking up at aeroplex lights at night is making a class statement. The distance between street and sky is not aesthetic — it is political.

---

## SS-A43 — Node hierarchy: Strand → SeriesNode / StoryNode / ChapterNode {#SS-A43}

**Date:** 2026-07-03 · **Author:** engine-refactor · **Ref:** supersedes the single-"Strand" schema described in BIBLE §4; SS-A37 rule updated in place

---

### The engine abstraction is now a typed tree (binding)

The overloaded **Strand** abstraction — one entity conflating series, story, and chapter — is
replaced by a typed hierarchy, table-per-hierarchy on the renamed **Nodes** table with a
**NodeType** discriminator:

```
SeriesNode      — top-level grouping (saga / anthology). Never holds beats.
  StoryNode     — a single story arc (book / novella / standalone). A leaf story
                  with no chapters holds its beats directly.
    ChapterNode — organizational unit inside a story; holds beats.
Beat            — prose atom (unchanged).
```

**Schema renames (data preserved, nothing dropped):** `Strands`→`Nodes`,
`StrandBeats`→`NodeBeats` (NOT `ChapterBeats` — that name belongs to the live legacy
Book/Chapter tables), `StrandAmendments`→`NodeAmendments`, `StrandSpineVersions`→
`NodeSpineVersions`, plus every Strand-named column/index/constraint (`ParentStrandId`→
`ParentNodeId`, `StrandCode`→`NodeCode`, `StrandBible`→`NodeBible`, …). System-versioned
history tables were renamed in lockstep; `NodeType` was backfilled on current AND history
rows (`series`→series, `chapter`→chapter, everything else→story). Migration:
`20260703162528_NodeHierarchyRedesign`; local backup `backups/preNodeHierarchy_20260703.bak`.

**Beat attachment rule:** beats attach to ChapterNodes and to *leaf* StoryNodes (11 existing
root stories hold beats directly — preserved, not restructured); SeriesNodes never hold beats.
`Kind` survives as a free-form display label; the CLR type / NodeType discriminator is the
structural truth. `NodeFactory.Create(kind)` maps labels to types at data-driven creation sites.

**Surface renames:** MCP `get_story` / `list_stories` / `create_series` / `create_story` /
`create_chapter` / `review_story` / story-bible family (legacy Book/Chapter tools renamed
`create_legacy_book` / `create_legacy_chapter`); CLI story-scoped flags are now `--write-story`,
`--review-node`, `--list-stories`, `--publish-story`, `--story-bible`, `--story-code`, etc.
(`--slug` and `ss --write-outline --slug` unchanged; `--review-story` and `--run-panel` remain
as legacy aliases for `--review-node`). Blazor routes `/node/{slug}` + `/nodes`
remain canonical with `/story/{slug}` and `/stories` as aliases.

**SS-A37 (no direct SQL deletes) now reads:** never `DELETE FROM Nodes`, `DELETE FROM Beats`,
or `DELETE FROM NodeBeats` via raw sqlcmd — same rule, renamed tables.

**Unchanged on purpose:** `docs/strands/<CODE>.md` per-story bibles keep their path and the
word "strand" in their prose — they are story-domain documents, not engine schema. The term
"strand" in story-domain contexts now simply means "story".
