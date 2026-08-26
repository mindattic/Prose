---
codex: 1
project: Prose
layer: series
updated: 2026-08-26
---

# EVE Universe — Book Coordination Board {#SS-EVE-COORD}

> **Purpose:** The single planning/coordination surface for all EVE books — mirrors
> `docs/series/GLMZ.md`'s shape, scoped for a much smaller, non-GLMZ-arc universe (see
> `docs/planning/EVEGDD-brief.md`'s adaptation note for why the sections below diverge from
> GLMZ's). This is a pre-writing instrument, not a canon source. Canon lives in
> `Universe.WorldFacts`/`UniversePrimer`, the Entity spine (RFC 0007 Universe Interchange), and
> `docs/nodes/<CODE>.md` once books exist. Update this doc whenever a book is added, a system's
> documentation status changes, or a plant/payoff pair is confirmed. Run `codex digest && codex
> doctor` after every edit.

**First consumer / origin:** [ExperimentEve](../../../../ExperimentEve) — RFC 0007 "Universe
Interchange" (`docs/rfc/0007-universe-interchange.md`). Prose is the shared canon store for a
real, playable PS1-style survival-horror-parody game (Kingsport, one real-time night, Sunday
June 21 1998). Unlike GLMZ/SCRY, EVE's "world" already exists as running code before any Prose
book is written — books here document and extend an existing artifact, not invent one from a
seed line.

---

## 1. Book Roster {#SS-EVE-COORD-§1}

| Code | Title | Type | Status |
|---|---|---|---|
| EVEGDD | Experiment Eve — Game Design Document | Nonfiction-structured reference | Brief filed 2026-08-26 (`docs/planning/EVEGDD-brief.md`); node not yet created |
| TBD | EVE — Night One (Game Script) | Screenplay-format narrative | Planned, brief not yet filed — depends on EVEGDD existing first (see §5) |
| TBD | EVE — Prequel Novella | Literary fiction | **Deferred by author decision, 2026-08-26** — "save for future after we prove this experiment is valid." No brief; not scheduled. |

Per RFC 0007 Phase 2, all three are ordinary Prose Books in the `eve` universe using the same
Book→Chapter→Beat engine as every fiction book — only the deliverable's purpose differs. No new
hierarchy, no new laws.

---

## 2. Design-Law Compliance (the EVE equivalent of GLMZ's arc structure) {#SS-EVE-COORD-§2}

EVE has no 5-arc structure. What every EVE book must instead stay consistent with is the 11
world rules seeded as `Universe.WorldFacts` at import time. Tracking which book is responsible
for documenting/dramatizing each:

| Rule | Documented by | Dramatized by |
|---|---|---|
| No Exposition Dumps | EVEGDD Ch. 9 (the environmental/document-only narrative rule) | Night One (script must obey it, not just describe it) |
| Placeless, Wink-Named | EVEGDD Ch. 6 (World & Districts, citing `docs/GAZETTEER.md`) | Night One |
| 1998 Technology Only | EVEGDD Ch. 1, Ch. 7 (ApertureOS) | Night One |
| Nostalgia Bait, Trademark-Scrubbed | EVEGDD Ch. 8 (bauble/consumer-good item list) | Night One |
| Father's Day Is the Through-Line | EVEGDD Ch. 1 (stated, not dramatized) | Night One (this is where it actually pays off) |
| Night Deadline | EVEGDD Ch. 2 (the real-time world clock) | Night One |
| Lighthouse Saves | EVEGDD Ch. 7 | Night One |
| Hourly Chimes | EVEGDD Ch. 2 | Night One |
| Routing Is the Puzzle | EVEGDD Ch. 6 | Night One |
| Drip-Feed Blueprints | EVEGDD Ch. 8 | Night One |
| The World Is at War With Itself (A-Life) | EVEGDD Ch. 4 | Night One (creature encounters) |

---

## 3. System Documentation Status {#SS-EVE-COORD-§3}

The EVE equivalent of GLMZ's Character Arc Ledger — not narrative continuity (no book has been
written yet), but which real, implemented game systems are documented vs. still only living in
code. Updated as EVEGDD's chapters get written.

| System | Real/implemented? | Documented (post-EVEGDD)? |
|---|---|---|
| Movement, dodge-roll, bicycle mode | Yes (`playerController.ts`, `playerRig.ts`) | Pending Ch. 2 |
| Fixed-camera system + CCTV/Observer variant | Yes (`cameraManager.ts`, `cameraZone.ts`) | Pending Ch. 2 |
| ATB battle system | Yes (`battle/battle.ts`, 603 lines) | Pending Ch. 3 |
| A-Life ecology simulation | Yes (`enemies/worldAI.ts`, 484 lines) | Pending Ch. 4 |
| 20 creature classes | Yes (one `.ts` file each, `enemies/registry.ts`) | Pending Ch. 4 |
| Erasure stealth/execution vignette | Yes (`gameplay/erasureSquad.ts`) | Pending Ch. 5 |
| Level 1 (North End) | Yes (`level/level01.ts`, 12 camera zones) | Pending Ch. 6 |
| 14 other Gazetteer districts | **No — design intent only** (`docs/GAZETTEER.md`) | Pending Ch. 6 (marked aspirational) |
| Save system + ApertureOS | Yes (`saveSystem.ts`, `apertureOS.ts`) | Pending Ch. 7 |
| Crafting/economy/baubles | Yes (`gameplay/inventory.ts`) | Pending Ch. 8 |
| PS1 shader rendering pipeline | Yes (`render/ps1/`) | Pending Ch. 10 |
| Skill tree beyond 4 flat stats | **No** (`GameState.skills` is 4 numeric fields) | Pending Ch. 11 (flagged gap) |
| Vehicle/boat traversal (the Drive, the *Providence Belle*) | **No** (only basic riding momentum exists) | Pending Ch. 11 (flagged gap) |
| Faction war beyond Erasure | Partial (`worldAI.ts` faction/morale fields exist; only Erasure has a dedicated system) | Pending Ch. 11 (flagged gap) |

---

## 4. Plant/Payoff Registry {#SS-EVE-COORD-§4}

| Plant | Origin Book | Payoff | Payoff Book | Status |
|---|---|---|---|---|
| Shared mechanics vocabulary + built-vs-planned district map | EVEGDD | Consistent system references in scene direction and barks | Night One | EVEGDD not yet written; TBD |
| "No dialogue trees, environmental narrative only" design law, illustrated (M./Ray, ApertureOS trash-folder lore) | EVEGDD Ch. 9 | Night One's script obeys the rule instead of writing exposition dialogue | Night One | TBD |
| Kat's unsigned Father's Day card (`kats-card` artifact, already in canon) | Interchange import | What she does with it at Providence — the endgame beat | Night One | Planted in canon; payoff not yet written |
| "The Island" / old Asylum's 180-year history (`the-island-asylum` place, already in canon) | Interchange import | Any book that dramatizes what Erasure is actually doing there | Night One or a future episode | Planted in canon; payoff not yet written |

---

## 5. Sequencing Lock {#SS-EVE-COORD-§5}

- **Night One must not be briefed or written before EVEGDD's chapters exist** for any system
  Night One's script would need to reference (battle, save points, creature encounters, the
  district it's set in). EVEGDD is the terminology source of truth; writing Night One first
  would let its script invent vocabulary EVEGDD would then have to retrofit around, exactly
  backwards from RFC 0007 Phase 2's stated design ("Prose's normal writing flow authors these
  books; the export is just a filter" assumes the reference book comes first).
- **The Prequel Novella is deferred** (author decision, 2026-08-26) — no sequencing question
  applies until it's un-deferred.

---

## 6. Entity Seeding Roadmap {#SS-EVE-COORD-§6}

Unlike GLMZ (where each new book typically needs new entities seeded), EVE's entire current
cast/bestiary/world was seeded in one pass via RFC 0007's Universe Interchange import — verify
via CLI `prose --universe-export eve <path>` or MCP `search_universe`/`get_universe_entity`,
never raw SQL.

| Entity set | Count | Status |
|---|---|---|
| Characters | 7 | Seeded (interchange import, 2026-08-26) |
| Factions | 2 | Seeded |
| Creatures | 20 | Seeded |
| Places | 30 | Seeded |
| Artifacts | 4 | Seeded |
| Events | 5 | Seeded |
| Rules | 7 | Seeded (also mirrored into `Universe.WorldFacts`) |

Future entities (new districts, new creatures, new NPCs added as the game itself grows) should
be added to ExperimentEve's own `universe/eve.universe.json` and pushed via `npm run universe --
push` (or `prose --universe-import`) — the interchange file, not a Prose-side manual add, is the
source of truth for anything that also needs to exist in the game's own data, per RFC 0007's
whole design.
