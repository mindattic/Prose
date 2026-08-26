---
codex: 1
project: Prose
layer: planning
code: EVEN1
title: Experiment Eve — Night One (Game Script)
universe: EVE
updated: 2026-08-26
---

# Story Brief: EVEN1 — Experiment Eve: Night One (Game Script) {#SS-BRIEF-EVEN1}

> **This brief is mandatory before creating a node bible or any DB records.**
> A book that cannot answer all 10 sections does not belong in the roster yet.
> After filing, update `docs/series/EVE.md` §1 (roster), §2 (design-law compliance), §4
> (plant/payoff registry), §5 (sequencing lock).

> **Adaptation note** (same discipline as `docs/planning/EVEGDD-brief.md`): EVE has no GLMZ-style
> 5-arc structure, no BCODA-relative timing, and no Character Arc Ledger — this is the
> **second** EVE book, not a chapter in an established series. Sections below are reinterpreted
> where the GLMZ template doesn't fit, each flagged explicitly. Unlike EVEGDD (nonfiction
> reference), this book **is** narrative fiction — screenplay-format — so most sections apply far
> more literally than they did there.

> **Grounding — not invented.** Every scene beat, location, quoted line, and story-clock timing
> below is sourced directly from the real ExperimentEve codebase
> (`D:\Projects\MindAttic\ExperimentEve`), confirmed live: `docs/GAZETTEER.md`,
> `src/level/level01.ts` (428 lines, the built North End level — 12 camera zones), the
> `timedEvents` story clock in `src/main.ts`, the `garageMachine` ApertureOS files, the
> `graffiti.ts` SOAK/REN duel text, and `src/core/worldClock.ts` (real-time 1:1, 8:00 PM arrival,
> sunrise/deadline 5:11 AM). Every quoted line in this brief is verbatim from source, not
> paraphrase. This book's job is to script the **built vertical slice** — it does not extend past
> what `docs/GAZETTEER.md` and the code actually contain. Providence, the Bell Bridge crossing,
> and the *Providence Belle* are confirmed **not implemented** beyond design-intent text — this
> book's ending must not pretend otherwise (see §1, §9).

---

## 1. Series Position {#SS-BRIEF-EVEN1-§1}

**Universe:** EVE (`eve`) — second book, after **EVEGDD** (the Game Design Document, fully
drafted 2026-08-26), which this book depends on directly for terminology and system definitions
(per `docs/series/EVE.md` §5's sequencing lock).

**Story type:** Screenplay-format narrative script for the game's playable vertical slice — not
a caper-of-the-week, not a standalone short story. It dramatizes the one level that actually
exists (Level 1 / the North End) across the real-time hours the game's own `timedEvents` clock
defines, from arrival to the level's actual, built stopping point.

**Book(s) this story serves:** Itself (first EVE fiction) and the future, currently-deferred
**EVE — Prequel Novella**, which per `docs/series/EVE.md` §1 depends on world geography beyond
this slice — Night One is a *prerequisite reference*, not something the Novella continues from
directly.

**Approximate in-universe timing:** Sunday, June 21, 1998, 8:00 PM through the pre-dawn hours
before the confirmed 5:11 AM sunrise/deadline — but this book's own content stops earlier, where
the built slice stops (see §9). "Approximate in-universe timing relative to BCODA" doesn't apply
— EVE and GLMZ share no timeline; this is EVE's own, first internal clock.

---

## 2. Arc Contribution {#SS-BRIEF-EVEN1-§2}

Reinterpreted per the adaptation note: EVE's equivalent of GLMZ's 5 arcs is the 11 design laws
tracked in `docs/series/EVE.md` §2. This book is the one every law's "Dramatized by" column
points to — EVEGDD only *documents* the laws; Night One is where they're *obeyed in practice*:

- **[x] No Exposition Dumps.** Every scene direction below routes lore through inspectable
  objects (the `dadCard`, `megahitVideo`, `zelnaPoster`) or overheard fragments (the payphone),
  never a character explaining the Experiment aloud. Kat herself never gets an internal-monologue
  info-dump — see Locks (§9).
- **[x] Father's Day Is the Through-Line.** Dramatized, not stated: `kats-card` (her own unsigned
  card) is carried the whole night; the `for_ray.txt`/`note_to_ray.txt` beat (Ch. 6) is the
  night's emotional core — a father who never came back, on the same day.
- **[x] Night Deadline / Hourly Chimes.** The chapter spine (§9) is built directly from the real
  `timedEvents` schedule — this is not backfilled onto invented scenes, the scenes exist because
  the clock exists.
- **[x] The World Is at War With Itself (A-Life).** The three Grafted debuts (9:30, 10:30, 11:30
  PM) and the Chimera Ecology's presence throughout are load-bearing plot events, not flavor.
- **[ ] None** — not applicable.

---

## 3. Prerequisites {#SS-BRIEF-EVEN1-§3}

| Prerequisite | NodeCode | Why Required |
|---|---|---|
| EVEGDD complete | EVEGDD | Terminology/system source of truth (battle, camera, save, A-Life) this script must not contradict. **Complete** — 46 beats drafted 2026-08-26. |
| EVE entity seeding | RFC 0007 import | All named characters/places/factions/artifacts this script references. **Complete and current** — Kat, the Observer, SOAK, REN, the Pawnbroker, M., Ray, the Erasure Team, the Grafted (10 named variants), North End, the Bell Bridge, the Island, `kats-card`, ApertureOS 98, `the-march`/`the-night`/`six-forty-seven` events all already exist as entities (verified via `--universe-export eve`, 2026-08-26 — the roster has grown well past the original 75 via a later ExperimentEve push). |

---

## 4. Character Entry States {#SS-BRIEF-EVEN1-§4}

No prior EVE fiction exists, so there is no ledger to inherit from — this book establishes entry
states for the first time:

| Character | Entry State (this book's opening) |
|---|---|
| Kat (Katie "Kat" Weiss) | Arrives 8:00 PM, alone. Carries `kats-card` — unsigned, unsealed, three weeks carried, not for sale (confirmed in `inventory.ts` flavor text). Reaction to the district on arrival, per source: *"What a shit hole."* |
| The Observer | Payphone-only presence at the start — voice, not yet a body. First line (recurring, per `main.ts`): *"Keep moving, Katherine."* — followed by dial tone and Kat's own irritation: *"Nobody calls me Katherine."* |
| SOAK / REN | Off-page, known only through their escalating graffiti duel (`graffiti.ts`, 3-stage). Neither appears in person in the built slice. |
| The Pawnbroker | Voice-only behind bars: *"Baubles only. Bullets back."* Never seen. |
| M. / Ray | Both absent from the level in person. M.'s presence is entirely the ApertureOS notes left in the garage; Ray never appears — only his empty workbench and the unclaimed card. |

---

## 5. Character Exit States {#SS-BRIEF-EVEN1-§5}

| Character | Exit State | Ledger Update Needed? |
|---|---|---|
| Kat | Exits this book's content at the slice's real stopping point (`sliceEnd` trigger, `main.ts`): *"Providence. Right."* / HUD: *"SLICE COMPLETE — the route continues toward the bridge."* Not at Providence — mid-journey, deliberately unresolved. | Yes — first entry in a future EVE Character Arc Ledger equivalent (not yet built; note in `docs/series/EVE.md` §3 instead per its existing shape). |
| The Observer | Still voice-only; CCTV mechanism established as its diegetic form (per EVEGDD Ch. 2) but the Observer never physically confronts Kat in this book's content. | No — matches the built slice exactly. |
| M. / Ray | Unresolved. `note_to_ray.txt` ("If you get back before me... Keep it lit") and `for_ray.txt` (the boys' gift, still on the bench) are both live, neither paid off — Ray's fate is explicitly not known even to M. | Yes — plant, not payoff (see §6). |

---

## 6. What It Plants {#SS-BRIEF-EVEN1-§6}

| Plant Description | Payoff Story (NodeCode) | Payoff Chapter/Beat (if known) |
|---|---|---|
| Ray's disappearance (`shift_log.txt`: "He did not bring it back") and M.'s unanswered vigil | Future EVE work (Prequel Novella or a later episode — not yet scheduled) | Unknown — deliberately left open; the built slice never answers it. |
| Kat's unsigned card, still uncarried-to-Providence | Deferred Prequel Novella, per `docs/series/EVE.md` §4 ("What she does with it at Providence") | Not this book — Providence isn't built. |
| SOAK/REN's third graffiti line — "THEN WHY DO THE BOATS GO NORTH?" — raises a routing/geography question the built slice never answers | Future EVE work (a built-out World & Districts episode) | Unknown. |
| The Island's "THEY TAKE THEM TO THE ISLAND" / "the island was FIRST" tag | Future EVE work — `the-island-asylum`'s 180-year history entity already exists in canon, unused narratively until dramatized | Unknown. |

After filing, these rows are mirrored into `docs/series/EVE.md` §4.

---

## 7. What It Pays {#SS-BRIEF-EVEN1-§7}

**None** — first EVE fiction book; nothing precedes it to pay off. (EVEGDD is nonfiction and
plants no narrative threads of its own.)

---

## 8. Thematic Complement {#SS-BRIEF-EVEN1-§8}

**Theme:** A night that keeps almost being about a father and never says so. Every location Kat
passes through is someone else's unfinished Father's Day — a stranger's sealed card, Ray's
unclaimed gift, her own unsigned one — and the game never lets a character state that pattern
aloud (see Locks, §9).

**Register:** Screenplay-format — scene direction + environmental description + minimal, load-
bearing dialogue (mostly one-sided: payphone, ApertureOS text, overheard barks). NOT literary
prose (that's the deferred Novella) and NOT a systems reference (that's EVEGDD).

**Adjacent EVE work:** EVEGDD (system definitions this script must not contradict — see §3). No
other fiction exists yet.

**What would be duplicated if this book didn't exist:** Without it, the game's actual narrative
content (the ApertureOS notes, the graffiti duel, the timed story beats) would remain scattered
in-code with no single authored read — exactly the drift EVEGDD's own existence prevents for
mechanics, applied here to story.

---

## 9. Structural Blueprint Seed {#SS-BRIEF-EVEN1-§9}

**Resolution mode:** **Unresolved / open** — not a craft choice, a fact: the built slice's real
ending (`sliceEnd`: *"Providence. Right." / "the route continues toward the bridge"*) is a
mid-journey cliffhanger by construction, not a climax. Writing a false resolution (Kat reaching
Providence, confronting the Observer, resolving Ray's fate) would violate the built-vs-planned
discipline EVEGDD's Ch. 11 exists to protect. **This book ends where the game ends.**

**Moral polarity:** Ambivalent (default) — the Erasure Team's execution bark (*"One less."* →
*"A folded paper falls out of his helmet. Crayon."*) is the clearest statement of this: even the
antagonists are somebody's parent. No clean villain framing.

**Ending style:** Neither avalanche nor epilogue — **honest cliffhanger**, matching the actual
`sliceEnd` trigger and HUD text verbatim as the book's final beat. This is a deliberate deviation
from CRAFT.md's avalanche-ending default, justified by the built-vs-planned Lock (see below) —
the alternative (inventing a false climax) is worse than the deviation.

**Escalation curve shape:** Real, clock-driven, not invented — three Grafted debuts (9:30, 10:30,
11:30 PM) provide mechanical escalation; the emotional escalation runs in parallel and peaks
later, at the Ch. 6 garage scene (2:00 AM), after the mechanical threat has already crested. The
two curves are deliberately offset, not synchronized — matches Storr/King's causal-chain
craft law without collapsing story tension into just "more monsters."

**Event-type palette:** revelation (environmental-text discoveries), escape/evasion (Grafted
encounters, Erasure stealth), quiet-moment (payphone calls, the garage). No pure combat-for-
spectacle beats — CombatProseGuidance still applies per-beat where mechanically triggered, but
the palette is weighted toward revelation/evasion over pure confrontation, matching the game's
own survival-horror (not action) register.

**Intertextual anchors** (same three EVEGDD names, now applied to prose voice rather than systems
documentation):
1. *Silent Hill 2*'s environmental-text storytelling (notes, answering machines) as the
   dominant narrative delivery mechanism — never a cutscene monologue.
2. *Kentucky Route Zero*'s treatment of found objects (the pawnshop, the ApertureOS files) as
   carrying more emotional weight than any spoken line.
3. *Twin Peaks*' placeless-but-specific small-town dread — matches the "Placeless, Wink-Named"
   design law directly.

**Subplot thread:** M. and Ray's unanswered story, running underneath Kat's own — thematically
parallel (both are "a parent and a Father's Day that didn't resolve"), never intersecting Kat
directly (she never meets either), which is itself the point: she reads their story the way the
player reads hers.

**Form device:** None imposed — the game's own real-time clock and diegetic ApertureOS/payphone
delivery already are the form device; adding a literary frame on top would contradict the
environmental-only Lock.

---

## 10. Entity Seeding Required {#SS-BRIEF-EVEN1-§10}

All named entities this book references are **already seeded** (confirmed via
`--universe-export eve`, 2026-08-26):

| Entity | Type | In DB? |
|---|---|---|
| Kat, the Observer, SOAK, REN, the Pawnbroker, M., Ray | character | [x] |
| The Erasure Team, the Grafted (faction) | faction | [x] |
| The 10 named Grafted variants (The Quarrel, The Applause, etc.) | creature | [x] |
| North End, the Bell Bridge, the Island (old Asylum), Kingsport, Providence | place | [x] |
| `kats-card`, ApertureOS 98 | artifact | [x] |
| The June 20th March, The Night (June 21 1998), 6:47, The Experiment, The Collapse | event | [x] |

No new entity seeding required. `prose --scan-entity-mentions` (corpus-wide) will be re-run after
prose drafting, per standard practice.

---

## Checklist Before Proceeding

- [x] All 10 sections filled
- [x] `docs/series/EVE.md` §1 roster row to be updated (this brief filed) — pending this edit
- [x] Entity seeding confirmed complete — no ledger update needed (§10)
- [x] Plant/Payoff rows drafted (§6) — to be mirrored into `docs/series/EVE.md` §4
- [x] World-Revelation Sequencing: this book does not spoil anything — Providence/the ending
      remain explicitly unreached, matching the built slice
- [x] BookNode `EVEN1` created in DB (universe `eve`, slug
      `experiment-eve-night-one-game-script-01a03fa5`)
- [x] ChapterNodes created (7 chapters per §9's clock-driven spine)
- [x] Node bible authored (`set_book_bible`) — arc/mission, chapter spine, Locks
- [x] Structural blueprint committed via `--set-structural-blueprint` (beat-granularity, 30
      beats): subplot=M./Ray, temporal=linear, resolution=unresolved, moral=ambivalent,
      ending=quiet/no-epilogue — matches §9 exactly
- [x] Prose: all 30 beats drafted (`--auto-run --no-repair`), **then corrected** — see below.

**QA correction pass, 2026-08-26 (same day as drafting).** A full end-to-end read-through (not
just spot-checks) found `--auto-run` had invented substantial content the Locks explicitly
forbid, despite tightly-scoped per-beat goals: beats 13/14/15/17 had Kat physically reach and
cross the bridge in Chapter 4 (the book's central Lock is that the built slice — and this book —
never reaches the bridge); beat 17 invented a "bridge maintenance structure" interior and had a
Grafted drown in the bay; beat 19 gave the voice-only Pawnbroker a full physical shop (cot, gun
cabinet, a fabricated "Blue Dress" trade-and-defection subplot) in violation of the "never seen"
Lock; beats 14 and 15 were near-verbatim duplicates of each other; beat 26 self-contradicted (a
digital `shift_log.txt` became a physical "logbook" pulled from Kat's jacket in the same
paragraph), and that fabricated object then drove an entirely invented "warehouse district"
journey through beats 27–30 that never used the real, required verbatim ending line at all; the
planned "One Less" Erasure Team beat (beat 27) — the book's single most important thematic
beat — never got written, replaced by more monster-chase filler.

**Fixed**: beats 11–17, 19, 20, and 22–30 (18 of 30 beats) hand-rewritten from the real source
material, strictly within the built North End geography, with the actual "One Less" execution
beat restored and the real verbatim `sliceEnd` line (`"Providence. Right." / "SLICE COMPLETE —
the route continues toward the bridge."`) now the book's actual final beat. Re-verified via a
full contamination grep (logbook/warehouse/Blue Dress/maintenance-structure/bridge-approach/
duplicate-paragraph) across the corrected text — clean. Re-exported to manuscript.

**Process lesson, recorded for future books**: `--auto-run` with a detailed, source-grounded beat
spine is not sufficient on its own for a book with hard built-vs-unbuilt or voice-only-character
Locks — a full end-to-end read is mandatory before treating auto-generated prose as final,
not just spot-checks of the opening/ending. A tight per-beat goal did not prevent the model from
inventing physical props, extra dialogue, and geography across nearly two-thirds of the book.
