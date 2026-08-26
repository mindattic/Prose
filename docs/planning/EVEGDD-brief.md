---
codex: 1
project: Prose
layer: planning
code: EVEGDD
title: Experiment Eve — Game Design Document
universe: EVE
updated: 2026-08-26
---

# Story Brief: EVEGDD — Experiment Eve: Game Design Document {#SS-BRIEF-EVEGDD}

> **This brief is mandatory before creating a node bible or any DB records.**
> A book that cannot answer all 10 sections does not belong in the roster yet.
> After filing, `docs/series/EVE.md` (new coordination board, mirroring `docs/series/GLMZ.md`'s
> shape but scoped for EVE) is created alongside this brief rather than an existing doc updated
> in place — EVE has no prior roster.

> **Adaptation note (same pattern as `docs/planning/TFAH-brief.md` for the FICTION universe):**
> the `_TEMPLATE.md` this brief is built from is written for GLMZ's narrative-fiction, 5-arc,
> character-continuity model. This book is neither GLMZ nor narrative fiction — it's a
> **nonfiction-structured reference document** (chapters = systems, beats = sections, per RFC
> 0007 Phase 2) for a real, already-substantially-built video game. Several sections below are
> reinterpreted rather than answered as literally worded; each says so explicitly where it
> diverges, same discipline TFAH's brief used for GSPL.

> **Grounding**: this brief is NOT speculative game design invented for the occasion. Every
> system named below was confirmed live against the actual ExperimentEve codebase
> (`D:\Projects\MindAttic\ExperimentEve`, 63 TypeScript files, ~7,900 lines) — a genuinely
> playable PS1-style survival-horror-parody vertical slice, not a prototype. The 75-entity
> canon this book documents is already fully seeded in Prose (RFC 0007 Universe Interchange,
> 2026-08-26): 7 characters, 2 factions, 20 creatures, 30 locations, 4 artifacts, 5 events, 7
> rules — 0 stubs, 100% relationship resolution. This GDD's job is to **systematize what
> already exists** (code + canon) into one coherent reference, clearly separating built-and-
> confirmed from aspirational-and-planned — not to invent a game from a one-line seed.

---

## 1. Series Position {#SS-BRIEF-EVEGDD-§1}

**Universe:** EVE (`Experiment Eve` — slug `eve`, DB-backed, same Universe/Node/Beat schema as
GLMZ/SCRY, added via RFC 0007 as the first non-literary/game universe).

**Story type:** Reference/nonfiction-structured book — the **first** EVE book, and the
foundational one every later EVE book must agree with. Not a chapter in a series; not a
standalone adventure. It is the shared canon-of-mechanics document.

**Book(s) this story serves:** Itself, plus every future EVE book. Per RFC 0007 Phase 2, two
more are already planned: **EVE — Night One (Game Script)** (the vertical slice's narrative
script — barks, scene direction, Observer interventions, ApertureOS file texts) and **EVE —
Prequel Novella** (deferred by author decision, 2026-08-26, "save for future after we prove
this experiment is valid"). Both depend on this book's terminology and system definitions
staying consistent — Night One's script cannot describe a battle exchange, a save-point
interaction, or a creature's telegraph in language that contradicts this GDD's chapter on that
system.

**Approximate in-universe timing:** N/A. This book is *about* the game, not set within its
fiction. The game's own diegetic clock (Sunday, June 21, 1998, 8:00 PM arrival → 5:11 AM
deadline, real-time 1:1) belongs to Night One, not here.

---

## 2. Arc Contribution {#SS-BRIEF-EVEGDD-§2}

EVE has no GLMZ-style 5-arc structure — it has no prior books at all. The equivalent question:
**what does this book establish that the EVE project's whole mission depends on?**

- **[x] Canon consistency infrastructure.** Every one of the 11 world rules imported as
  `Universe.WorldFacts` (no-exposition, placeless-winks, tech-1998-only, nostalgia-parody,
  fathers-day-throughline, night-deadline, lighthouse-saves, hourly-chimes, routing-is-the-
  puzzle, drip-feed-blueprints, a-life-ecology) is a *design law*, not flavor text. This book is
  where each law gets translated into a concrete, checkable system description (e.g.
  "no-exposition" → the ApertureOS/inspect-text lore-delivery convention documented in Ch. 7/9;
  "a-life-ecology" → the `worldAI.ts` pack/faction/growth simulation documented in Ch. 4).
- **[x] Built-vs-planned honesty.** The codebase is a real vertical slice (one full district,
  20 implemented creatures, a working ATB battle system, a working save/lore OS) sitting inside
  a much larger stated vision (a ~15-district, three-act campaign per `docs/GAZETTEER.md` in the
  ExperimentEve repo). Conflating the two — writing the GDD as if the whole campaign already
  exists — would immediately corrupt anything written against it later (Night One's script,
  future level design). Chapter 11 (Roadmap) exists specifically to keep that line honest.
- **[ ] None** — not applicable; this book has a clear mission.

---

## 3. Prerequisites {#SS-BRIEF-EVEGDD-§3}

**None**, in the sense of prior EVE books — this is the first. But two Locked-Pipeline stages
are already satisfied, not skipped:

| Stage | Status |
|---|---|
| Stage 1 — Entity Seeding | **Done.** All 75 entities imported via RFC 0007 Universe Interchange (`prose --universe-import`), universe `eve`. |
| Stage 2 — Relationship Linking (100% gate) | **Done.** 38 edges created on import; 0 dangling stubs — the source file's cross-references were internally complete. Verified via `--universe-export` round-trip (0 diffs). |

No world facts need inventing here — `Universe.WorldFacts` (the 11 rules) and
`Universe.UniversePrimer` (logline/setting/tagline) are already populated from the interchange
import and are the authoritative starting point for this book's Chapter 1.

---

## 4. Character Entry States {#SS-BRIEF-EVEGDD-§4}

Not a narrative-continuity question (no prior EVE book exists). Reinterpreted as: **what state
is each character/system's documentation in today**, before this book is written?

| Character / System | Entry State |
|---|---|
| Kat (Katie "Kat" Weiss) | One-paragraph interchange summary + `details` (voice line, what she carries, that she "clearly knows" what the Experiment was and never says). Movement/dodge/bicycle mechanics exist in code (`playerController.ts`) but are undocumented outside comments. |
| The Observer | Interchange summary only ("proves existence by intervening"). The CCTV fixed-camera variant (`cameraZone.ts` `cctv` mode — servo-lag panning, fisheye) that mechanically *is* the Observer's presence is real, working code, entirely undocumented as a design system. |
| SOAK / REN | Interchange summaries (graffiti duel, blueprint-bearing stencils). The actual duel escalation (`level/props/graffiti.ts`) is implemented; not documented as a system. |
| The Pawnbroker | Interchange summary only. The bauble economy it anchors (`gameplay/inventory.ts` — pocket watches, pearls, 1998-nostalgia baubles, one unsellable keepsake) is a real, implemented item taxonomy with zero design documentation. |
| M. / Ray | Interchange summaries (absent NPCs, environmental-storytelling only — notes, an unclaimed card). No mechanical system attached beyond being fixed narrative content inside Level 1. |
| Battle system | **No entity at all yet** — a real, substantial ATB system (`battle/battle.ts`, 603 lines, Parasite-Eve-inspired) exists in code with no canon representation. This book's Chapter 3 is its first documentation; §10 below flags whether it warrants its own `concept`-typed entity. |
| Chimera Ecology / A-Life | Faction entity exists (`chimera-ecology`) with a one-line summary. The real simulation (`worldAI.ts`, 484 lines — packs, rival factions, corpse-eating, growth stages, nests, noise/alarm) is undocumented as a system beyond that summary. |

---

## 5. Character Exit States {#SS-BRIEF-EVEGDD-§5}

Target state by the end of this book — every row above must be **fully specified against the
real implemented mechanics**, not left at interchange-summary depth:

| Character / System | Exit State |
|---|---|
| Kat | Ch. 2 fully specifies movement, dodge-roll (i-frames/cooldown), bicycle mode (momentum/wide turns), and her position inside the fixed-camera system — grounded in `playerController.ts`/`playerRig.ts`, not re-invented. |
| The Observer | Ch. 2 documents the CCTV camera-zone mode (hysteresis, servo-lag, fisheye) explicitly as the Observer's diegetic mechanism — the point where "mystery framing device" and "camera system" are the same design object. |
| SOAK / REN | Ch. 9 (Cast & Narrative Delivery) documents the graffiti-duel as a live-environment storytelling system (escalates over the night), citing `graffiti.ts`. |
| The Pawnbroker | Ch. 8 (Crafting, Economy & Blueprints) fully specifies the bauble taxonomy, the unsellable-keepsake exception (Kat's card), and how baubles convert to bullets/currency — grounded in `inventory.ts`. |
| M. / Ray | Ch. 9 documents them as the model case for "no dialogue trees, only environmental/document narrative" — the design rule this game runs on, illustrated by a real example already built. |
| Battle system | Ch. 3 fully specifies the ATB phase machine (active/menu/aim/sweepH/sweepV/fire), part-targeting/weak points, Precision Aim, and the Limit gauge — this is the single largest documentation gap this book closes. |
| Chimera Ecology / A-Life | Ch. 4 fully specifies the simulation (wandering, packs, rival-faction combat, corpse-eating/growth, nests, the `ring()` noise/alarm mechanic) against all 20 implemented creature classes, cross-referenced to their existing entity records. |

---

## 6. What It Plants {#SS-BRIEF-EVEGDD-§6}

| Plant | Payoff |
|---|---|
| Shared mechanics vocabulary (battle terms, creature telegraph language, save/ApertureOS conventions, the built-vs-planned district map) | **EVE — Night One (Game Script)** — cannot be written consistently without this book's Ch. 2–9 existing first. |
| The built-vs-aspirational district map (Ch. 6, Ch. 11) | Any future level-design work, and eventually **EVE — Prequel Novella** if/when it draws on world geography beyond the North End slice. |
| The "no-exposition, environmental-only narrative" design law, illustrated with real examples (M./Ray, ApertureOS trash-folder lore) | Night One's script — a screenplay-format temptation to write exposition dialogue is exactly what this book's Ch. 9 exists to prevent. |

---

## 7. What It Pays {#SS-BRIEF-EVEGDD-§7}

**None** — first book in the universe; nothing precedes it to pay off.

---

## 8. Thematic Complement {#SS-BRIEF-EVEGDD-§8}

**Theme:** This is the book every other EVE book answers to, not one that competes with them
for the reader's attention — a reference work, not an experience.

**Register:** Technical/reference, plain declarative prose, code-grounded citations throughout.
Explicitly **not** screenplay format (Night One) and **not** literary narrative prose (the
deferred Prequel Novella) — the one EVE book whose "voice" is a design document's voice, not a
character's.

**Adjacent EVE work:** None yet (first book). Once Night One exists, the two are adjacent in
the sense that Night One's script must never contradict this book's system definitions — but
they are not tonal siblings; that's an intentional split, not a gap.

**What would be duplicated if this book didn't exist:** Without it, Night One's script (and any
future level-design or systems work) would have to re-derive mechanics context ad hoc from raw
code and thin entity summaries every time, with no single source of truth — the exact kind of
drift RFC 0007's whole Dynamic Context Memory discipline exists to prevent for narrative prose,
applied here to game-systems documentation instead.

---

## 9. Structural Blueprint Seed {#SS-BRIEF-EVEGDD-§9}

**Not applicable as narrative anti-tell countermeasures** (Stage 6's `--generate-blueprint` is
built for fiction's resolution-mode/moral-polarity/ending-style concerns). This book has no
plot to resolve and no moral polarity. What Stage 6 becomes for a nonfiction-structured GDD is
a **chapter-sequencing check**: does the chapter order teach concepts in the order a reader
needs them, and does no later chapter depend on an undefined term from an earlier one?

**Chapter sequence** (11 chapters; "beats" = sections within each):
1. Vision & Design Pillars — the 11 world rules translated into design law, tone, and scope
   (built-vs-planned framing stated up front)
2. Player & Core Loop — Kat's movement/dodge/bicycle mechanics; the fixed-camera system
   including the Observer's CCTV variant; the one-real-time-night world clock
3. Battle System — the ATB phase machine, part-targeting, Precision Aim, Limit gauge
   (Parasite-Eve lineage stated explicitly as an intertextual anchor)
4. The Chimera Ecology & Bestiary — the A-Life simulation plus all 20 creature classes,
   cross-referenced to their entity records
5. Erasure Team & Stealth — the human antagonist faction, view-cone detection, the
   off-guard execution mechanic
6. World & Districts — Level 1 (North End) documented in full (12 camera zones, ~20
   interactables) as the one built example; the Gazetteer's ~14 other districts documented as
   design intent, explicitly marked not-yet-built
7. Save System & ApertureOS — lighthouses as diegetic save points, the ApertureOS UI, the
   UNDELETE-from-Trash lore mechanic
8. Crafting, Economy & Blueprints — component/crafted items, the bauble pawnshop economy,
   the drip-feed blueprint-discovery design law in practice
9. Cast & Narrative Delivery — the seven characters, and the "environmental/document-only,
   no dialogue trees" rule illustrated against real examples (M./Ray, ApertureOS files)
10. Technical Architecture & Art Direction — Vite/TypeScript/Three.js, the PS1 shader
    pipeline (vertex snap, affine texture mapping), the procedural-only asset philosophy
    (one CC0 kit, everything else generated or synthesized)
11. Roadmap & Future Episodes — the built-vs-aspirational gap made explicit as a punch list
    (14 unbuilt districts, faction-war depth beyond Erasure, skill-tree depth, vehicle/boat
    content, sequel hooks like the Out Island)

**Event-type palette:** N/A (nonfiction). Each chapter's internal shape instead: stated
purpose → mechanic description grounded in a named source file → design-law connection (which
of the 11 rules this system serves) → built/planned status.

**Intertextual anchors** (works whose *documentation* approach, not plot, is the model):
1. *Parasite Eve* (1998) — the battle system's explicitly-stated lineage; Ch. 3 should read like
   a design document for that lineage, not obscure it
2. *Silent Hill* / *Resident Evil* fixed-camera convention — the camera system's genre home;
   Ch. 2 should name this convention plainly rather than reinvent vocabulary for it
3. *STALKER*'s A-Life — Ch. 4's own code comments already cite this; the GDD should make that
   citation explicit and specific rather than a vague "immersive sim" gesture

**Subplot thread:** N/A — no plot. The one throughline that *should* run across every chapter:
explicitly tying each system back to which of the 11 world rules it serves, so the book reads
as one coherent design philosophy applied 11 times, not 11 unrelated feature write-ups.

**Form device:** A short "Design Law" callout at the start of each system chapter (2–3
sentences, citing the specific rule(s) from Ch. 1 that chapter's system exists to serve) —
proposed, not yet locked; confirm before Chapter 1 prose begins.

---

## 10. Entity Seeding Required {#SS-BRIEF-EVEGDD-§10}

All 75 named characters, factions, creatures, places, and artifacts this book will reference
are **already seeded** (RFC 0007 Universe Interchange import, 2026-08-26, universe `eve`) — no
new entity seeding is required to write this book's content against the existing cast/bestiary/
world.

One open design question, not a blocker: should the **battle system** and the **A-Life
simulation** get their own `concept`-typed entities (à la a `RepositoryDefinition`-registered
generic type, same mechanism RFC 0007 uses for `creature`/`artifact`/`event`/`rule`) so future
books can cross-reference "the ATB battle system" as a linkable entity rather than only prose
text? **Recommendation: not yet.** The `chimera-ecology` faction entity already anchors A-Life
canonically; the battle system has no in-fiction equivalent to hang an entity off (it's a game
mechanic, not a named in-world thing) — revisit only if Night One's script needs to formally
link a scene to "the battle system" the way it might link to a character or place.

| Entity | Type | In DB? |
|---|---|---|
| All 7 characters (Kat, Observer, SOAK, REN, Pawnbroker, M., Ray) | character | [x] |
| Both factions (Erasure Team, Chimera Ecology) | faction | [x] |
| All 20 creatures | creature | [x] |
| All 30 locations | place | [x] |
| All 4 artifacts | artifact | [x] |
| All 5 events | event | [x] |
| All 7 rules | rule | [x] |

---

## Checklist Before Proceeding

- [x] All 10 sections filled
- [x] Entity seeding confirmed complete (RFC 0007 import; 0 stubs, 100% relationship resolution)
- [x] Existing design material surveyed (`ExperimentEve/docs/GAZETTEER.md`, `CREDITS.md`, full
      `src/` code read for real-vs-aspirational status) — no competing GDD exists; this is the
      first
- [x] `docs/series/EVE.md` — new coordination board created (mirrors `docs/series/GLMZ.md`'s
      shape; EVE has no prior roster to update in place)
- [ ] BookNode `EVEGDD` created in DB (universe `eve`)
- [ ] ChapterNodes created (11 chapters per §9, SortKey 100–1100)
- [ ] Node bible authored (`set_book_bible`) — arc/mission per §2, chapter spine per §9, the
      "Design Law" callout device confirmed or dropped
- [ ] Structural blueprint — **skip or adapt**: `--generate-blueprint`'s narrative anti-tell
      machinery (resolution mode, moral polarity, ending style) doesn't apply; author decision
      needed on whether to run it anyway (harmless no-op) or bypass Stage 6 entirely for this
      book with that justification recorded
- [ ] Prose: Sonnet draft → Opus polish per standard SOP, once the above stages close
