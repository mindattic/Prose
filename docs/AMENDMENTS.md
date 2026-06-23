---
codex: 1
project: StreetSamurai
code: SS
layer: amendments
status: living
updated: 2026-06-15
---

# StreetSamurai — Amendments (append-only; amendment wins over the bible)

> Append-only. Never rewrite an amendment; supersede it with a new one. Beyond ~25, fold into the
> bible and start a new epoch (note the git tag); history stays in git.

## SS-A1 — Adopt the Codex documentation standard (supersedes —)

**What changed.** Installed the MindAttic Codex standard. `ARCHITECTURE.md` (the prior software
source of truth) was migrated into [docs/BIBLE.md](BIBLE.md) (L0). Its goal tables became
[docs/USER_STORIES.md](USER_STORIES.md) (L2). The continuity-invariants list from
`v3/canon_writes/story_state.md` was promoted into BIBLE §5 as narrative laws
[SS-LAW-9](BIBLE.md#SS-§5)…[SS-LAW-14](BIBLE.md#SS-§5); the engine invariants from `ARCHITECTURE.md`
§2a became [SS-LAW-1](BIBLE.md#SS-§5)…[SS-LAW-6](BIBLE.md#SS-§5); CLAUDE.md code/world rules became
[SS-LAW-7](BIBLE.md#SS-§5)/[SS-LAW-8](BIBLE.md#SS-§5).

**Why.** One source of truth, stable IDs, a doctor that catches drift, and a SessionStart digest so
every Claude session loads the canon. Replaces ad-hoc, scattered docs.

**Migration / preservation (no content deleted).**
- `ARCHITECTURE.md` is retained as a 1-line pointer to `docs/BIBLE.md` (README links it; tooling may
  still read the path).
- `v3/canon_writes/story_state.md` remains the **session/state scratch notes**; its *invariants*
  now also live (authoritatively) in BIBLE §5.
- `engine_data/*.json` is registered as the **L5 data layer** via schemas under `docs/data/_schema/`
  and the master entity-identity table [docs/data/ENTITY_IDENTITY.md](data/ENTITY_IDENTITY.md). Its
  canon *values were not rewritten*; per [SS-LAW-1](BIBLE.md#SS-§5) it is the seed/export mirror,
  not the live read path.
- Prose draft sprawl recorded, not deleted: the canon prose register is **v8**
  (`engine/bushido_coda_v3/01_bearing_teeth_v8.md` + `00_style_guide.md`). Earlier drafts
  (`engine/bushido_coda_v2/*_v2..v6`, `*_v7`) are superseded historical drafts kept on disk. Prose
  HTML bodies are treated as `generatedFrom` the chapter beats.
- The project rule "no Markdown files except README" (CLAUDE.md) is amended: the Codex `docs/*.md`
  set is the documented exception (it is documentation, not app data). Data files remain JSON.

## SS-A2 — Multi-Universe engine; GLMZ is Universe #1 (supersedes —)

**What changed.** The engine is recast as **universe-agnostic**. A `Universe` lookup table is
introduced, and every canon/story root (`Entities`, `Strands`, `Books`) gains a single non-null
`UniverseId` (1:M; beats/chapters inherit via their parent). **GLMZ becomes Universe #1**;
**Fantasy/Steampunk** is stood up as Universe #2 on the same tooling. The project is **not
renamed** — "StreetSamurai" stays the engine codename across the DB, connection strings, `.NET`
namespaces, `StreetSamuraiDbContext`, and Azure infra. Amends BIBLE §1, §2, §3, §4.2, §5 (new
[SS-LAW-15](BIBLE.md#SS-§5); [SS-LAW-8](BIBLE.md#SS-§5) + [SS-LAW-10](BIBLE.md#SS-§5)…[SS-LAW-14](BIBLE.md#SS-§5)
re-scoped to the GLMZ universe), and §9. New stories: [USER_STORIES.md](USER_STORIES.md) Epic U
(SS-US-U1…U7).

**Why.** One engine, many worlds, on shared tooling — the lowest-risk path. A rename would touch
~3,754 string occurrences across ~731 files and would entangle the auth Data-Protection boundary,
the connection-string/DB name, the MCP tool prefix, and re-provisioning of Azure infra with an
already-delicate DB migration. Keeping the codename decouples the two.

**Migration / preservation (no content deleted).**
- **Single `UniverseId` FK chosen over an M:M bridge.** A crossover entity (vocabulary shared
  across universes) is **duplicated** — one row per Universe, never a shared row. The author
  explicitly prefers a handful of duplicate rows over refactoring the whole schema onto a bridge.
- **SwitchUniverse is per-process / per-session, never a single shared global.** The current
  universe resolves by precedence: explicit `--universe <slug>` flag → `SS_UNIVERSE` env var (per
  terminal) → UI circuit/session selection (per browser tab) → the global default `current_universe`
  KV (fallback). This lets two CLIs (or two tabs) write different universes at the same time.
- **Adding `UniverseId` to system-versioned tables** uses the `SYSTEM_VERSIONING OFF → ALTER table
  + `_History` → ON` dance (pattern in `v3/StreetSamurai.Blazor/Cli/MigrateSqlCli.cs`).
- **Execution staged:** this amendment and the docs land first; the DB was backed up to
  `backups/StreetSamurai_preuniverse_20260615.bak` (RESTORE VERIFYONLY passed) before any change.
  The schema migration, EF query filter, SwitchUniverse wiring (UI/CLI/MCP), per-universe config
  namespacing, GLMZ-prompt de-hardcoding, and the CyberSpace→dark-mode shell are **deferred to a
  reviewed follow-up** and are not built in this pass.

## SS-A3 — Multi-Universe engine implemented (supersedes the "deferred" stance of SS-A2)

**What changed.** The build deferred by [SS-A2](#) shipped (2026-06-15). The engine is now multi-
universe in code, not just docs:
- **Schema** (`add_universe_20260615.sql`): a non-temporal `Universe` table seeded with `glmz` +
  `fantasy-steampunk` (well-known ids `1111…` / `2222…`), a `UniverseId` column on `Entities`,
  `Strands`, `Books` (added via the `SYSTEM_VERSIONING OFF → ALTER table + `_History` → ON` dance,
  NOT NULL DEFAULT GLMZ so every existing row backfilled to Universe #1), and per-universe unique
  slug indexes (`UX_Entities_Universe_Type_Slug`, `UX_Strands_Universe_Slug`, `UX_Books_Universe_Slug`)
  so the same (type, slug) may recur across universes.
- **Scoping**: an EF global query filter on `Entity`/`Strand`/`Book` keyed off an ambient
  `IUniverseContext` (`UniverseScope.EffectiveId`); a single filter on the `Entity` spine
  transitively scopes every entity type (Records-path reads navigate `Records→Entity`; the character
  read paths derive their id-set from `Entities`). `StreetSamuraiDbContext.SaveChanges` stamps
  `UniverseId` on new rows. Empty scope (tests / pre-migration) ⇒ no-op.
- **SwitchUniverse** (per-process/per-session): `--universe <slug>` flag + `SS_UNIVERSE` env (CLI),
  a `switch_universe`/`list_universes`/`current_universe` MCP tool set, and a `NavMenu` dropdown in
  the UI; selection precedence flow-override → process-override → `current_universe` KV default. Two
  CLIs (two OS processes) target different universes simultaneously.
- **World-primer seam**: each `Universe` has a `WorldPrimer`; `BeatGeneratorService.WorldLine`
  injects it for non-GLMZ universes while leaving GLMZ's prompt byte-identical (zero voice drift).
- **Shell**: the CyberSpace animated background (console-bg / sacred-geometry / tv-static JS + the
  cyberspace DOM divs) removed for plain dark mode; the base dark `.app-shell` theme is unchanged.

**Why.** Realize the SS-A2 architecture so the same tooling writes any registered universe.

**Verification.** Full solution builds clean (0 errors); 129 gate tests pass
(`DiRegistrationTests`, `StrandWorkbenchServiceTests`, `CharacterReadModelTests`, …); CLI smoke
`--list-strands --universe glmz` → 94 strands vs `--universe fantasy-steampunk` → 0, with the
universe predicate visible in the generated SQL. DB backed up first to
`backups/StreetSamurai_preuniverse_20260615.bak` (RESTORE VERIFYONLY passed).

**Residual (tracked as SS-US-U5 🟡).** The ~27 other GLMZ-hardcoded generation prompt sites should
adopt the `WorldLine`/`WorldPrimer` seam, and the voice/tone/register KV keys should be namespaced
per universe slug. No rename was performed — "StreetSamurai" remains the engine codename.

## SS-A4 — Universe segregation complete; seed ids are UUIDv7 (supersedes the SS-A3 residual)

**What changed.** [RFC 0006](rfc/0006-universe-segregation.md) is fully implemented — every
cross-over surface beyond canon rows is now scoped to the current universe, and a "card" for the
current universe can never be another universe's:
- **Config** — `UniverseId` on `Settings` (composite key `Key`+`UniverseId`) and `Species`, with EF
  query filters. Operational keys (`action_configs`, `tts.rules`, `users.accounts`,
  `current_universe`) carry a SHARED sentinel and are visible from every universe. The KV layer auto-
  scopes; in-memory caches (repos, voice docs, derived indexes) invalidate on `UniverseScope.Epoch`.
- **Retrieval** — `UniverseId` denormalized onto `EntityEmbeddings`/`ProseEmbeddings`; the raw-SQL
  `FindSimilar*` queries (which bypass the EF filter) now carry a universe predicate.
- **Prompts** — the `IUniverseContext.WorldGroundingOr(glmzText)` seam wraps every GLMZ-worded LLM
  prompt string; GLMZ stays byte-identical, other universes get their own world primer.
  `EpisodeGeneratorService` remains a GLMZ-only feature by design.
- **Caches** — `WorldGraphService` + the Semantic/Thematic/Inference/GlobalSearch indexes rebuild
  when the universe changes. **Ledger** — `Edge`/`EntityStateEvent`/`CharacterReadModel` scoped.
- **Missing-card policy** — when a universe lacks a card the seam returns a neutral default, never
  another universe's content.

**Seed ids → UUIDv7.** The first universe migrations seeded sentinel ids (`11111111…`/`22222222…`/
`99999999…`). These are now UUIDv7 like every other Id in the app — fixed constants
(`0197e9c9-0001-…` GLMZ, `…-0002-…` Fantasy, `…-0099-…` Shared) so the bootstrap / IsGlmz / stamping
can still reference them without a DB hit. The existing dev DB was re-stamped with
`restamp_universe_guid7_20260615.sql` (a one-time, dev-only correction not added to the
ApplyMigrations list; fresh DBs seed UUIDv7 directly).

**Why.** Realize the RFC so the same tooling writes any universe with zero bleed; align the Id
convention with the rest of the codebase.

**Verification.** `UniverseSegregationTests` (10) + 147 gate tests green; full solution builds
clean; CLI smokes prove scoping (canon-retrieve GLMZ 5 / Fantasy 0; voice rules GLMZ 23.5KB /
Fantasy 1.9KB). DB backed up to `backups/StreetSamurai_preRFC0006_20260615.bak` first.

## SS-A5 — Fully relational canon: `Records.Json` retired per type (supersedes the blob-as-canonical framing)

**What changed.** Author directive: *"any JSON fields should be broken out to tables and bridge
tables for maximum relational data management — every repository must be relational, not use JSON
blobs."* Canon entities move off the `Records.Json` blob onto typed tables + bridges (the way
**Character** already was). See [RFC 0007](rfc/0007-fully-relational-canon.md) for the per-type
recipe + parity gate. This supersedes BIBLE §4.2's framing of `Records.Json` as *the* canonical
store — the relational tables become canonical; the blob is a per-type rollback artifact retired
once parity passes.

**Why.** The point of a relational DB is queryable, joinable, integrity-checked relationships —
not deserializing blobs to read them. (Note: cross-entity *relationships* already live relationally
in `Edges` + the WorldGraph, and *semantic* similarity in the `VECTOR` embedding tables; this
amendment relationalizes the remaining *attributes* + embedded lists, and projects blob relationship-
lists into real `Edges`/bridges — the edge-completeness prong that actually prevents missing-link
bugs like the cat-ear genemod.)

**Progress.** ✅ **Faction** converted end-to-end (FactionMapper + `FactionRelationshipTags` bridge +
faction tags → `EntityTags` + backfill CLI + 13 parity tests; live 163/0 parity; blob retired;
backup `backups/StreetSamurai_preFactionBlobDrop_20260615.bak`). Character was already relational.
⬜ ~24 types remain, each following the RFC 0007 recipe; the blob stays source-of-truth per type
until that type flips, so the engine is always consistent.

## SS-A8 — *Attendance* (ATTE) resonance-trace taxonomy canonized; bleed-transit investigation mechanics locked {#SS-A8}

**What changed.** [GLMZ] A narrative-logic gap in *Attendance* (`attendance-019ebf4c`, 40 beats) is resolved by canonizing the two-trace forensic model for bleed-induced transit events and locking the "slip-away" mechanism that explains why children disappear without witnesses. The existing draft has teacher Ren Vasquez witnessing Kito Bramley vanish from his classroom chair in real time — but a live classroom disappearance witnessed by a teacher generates incident reports, parent calls, and institutional alarms that contradict the story's core engine: *children fall through administrative cracks because nobody sees them go*.

**Resonance-trace taxonomy (LOCKED).**

A bleed-induced transit event leaves two distinct forensic signatures, both detectable with a resonance scanner — a field instrument that reads residual harmonic energy by frequency profile, intensity, and estimated age of trace. (Selvamani's portable anomaly sensor is an early-stage, researcher-built example; the RMA issues standardized scanner kits to Class-3 response teams.)

1. **Resonance echo** (informal: *echo*; RMA designation: *contact imprint*) — low-intensity, long-duration residual left where the bleed's coherent emission first synchronized a person's neuretics. The tuning happens at whatever position the person occupies most — desk chair, usual seat, frequent resting spot. An echo does **not** mark where the person crossed; it marks where the bleed *found* them and began tuning them. Echoes persist for weeks. Selvamani's shorthand: *the room remembers where the frequency settled.*

2. **Transit shadow** (informal: *shadow*; RMA designation: *crossing trace*) — higher-intensity, shorter-duration residual left at the exact location where the person physically crossed the threshold. Intensity is immediate but decay is faster than an echo (days, not weeks). A transit shadow is **always in a transitional, low-visibility space** — a bathroom stall, locker room, stairwell alcove, supply room, waiting chair outside a closed door. The bleed does not open in public space; threshold contact requires brief neuretic isolation, which means a person alone, away from the interference of other active neuretics.

**Why children disappear without witnesses (LOCKED).** The tuning compulsion builds over the six-week synchronization window (consistent with the neuretics burnout flag pattern). When the pull peaks, the child follows it during a normal institutional transition: a bathroom pass, a moment waiting alone outside a room, the gap between periods in a corridor with no adult coverage. The crossing takes seconds. In an overworked, underpaid school environment, one child not returning from a bathroom break is logged as an unexcused absence or presumed early guardian pickup — not an emergency. The teacher assumes the front office has them; the front office assumes a guardian came; the record closes as noise. The pattern repeats 1–3 times per school over months before any single site has a count worth reviewing.

**The 47-child pattern (LOCKED).** Twenty-two school sites across GLMZ, twenty-two months, 1–3 incidents per site. Each site's tally is below its corp contract's alert threshold. Cross-corp clearance is required to aggregate. The investigator's role is not to respond to an alarm — it is to be the only node from which an alarm is visible at all.

**Prose changes required in `attendance-019ebf4c`.**

- **Story logline (lines 2–3):** Remove "each disappearance witnessed as the air going wrong above an empty chair." The echoes are found by investigation, not observed in real time.
- **Beats 7–8:** Ren Vasquez did **not** see Kito disappear. Kito asked for a bathroom pass during independent reading time. Ren waited fifteen minutes before going to look; found nothing. The wrongness he describes is what he noticed **above the chair after the room emptied** — the resonance echo of the tuning, still present. As a latent unregistered psionic he registered it where a scanner would have measured it. He moved the desk because he could not stop looking at the echo and didn't know what to call it.
- **Beat 10:** The trace above Kito's chair is the resonance echo (tuning mark). Yemina has seen this signature in the prior two cases. She knows the transit shadow is somewhere else — a room in this building she hasn't swept.
- **Beat 20 (Selvamani expert testimony):** Add the two-trace distinction: the echo stays at the seat; the shadow is wherever they went to be alone. Selvamani: *"The echo tells you where the frequency found them. The shadow tells you where they stepped through. You always need both — the echo shows you who; the shadow shows you where."*
- **Add investigative beat (new or expanded):** Yemina sweeps the bathroom near Room 214 with her sensor and finds Kito's transit shadow — stronger, colder, decaying faster than the echo above the chair. This is the first time she sees both traces for the same child, and it confirms the pattern she's been building across all three cases.

**Amara Osei name collision — resolved.** The ATTE child has been renamed **Daria Drew**. All three prose beats updated. The adult `Amara Osei` in *Underlying Connection* ([SS-A6](#)) is unchanged.

**Why.** Eliminates the mass-alarm logical gap. The horror of *Attendance* is structural — children disappear because the institutions that should catch them are too fractured, too underfunded, and too corp-siloed to notice. No cover-up. No conspiracy. Just the ordinary failure of disconnected record-keeping at scale. The two-trace model adds investigative weight: Yemina and Selvamani must *find* the shadows rather than just receive eyewitness accounts.

**Verification.** Author approval of revised Beats 7–8, 10, 20 + new bathroom sweep beat in `attendance-019ebf4c`. `pwsh tools/codex.ps1 doctor` must pass after edits.

## SS-A6 — *Underlying Connection* canonical design; Orison Neuretics canonized (supersedes —)

**What changed.** [GLMZ] A new CorpoNation, three new characters, and one narrative-law ruling are
canonized as the design basis for the *Underlying Connection* book.

**Orison Neuretics.** Premium neuretic cultivation and maintenance — the largest single operator in
GLMZ. Brand register: care, precision, trust ("Grown for you"). True business: managed-liability
suppression. Orison holds master maintenance contracts across GLMZ; mid-tier operators (Cellvault,
others) hold sub-contracts under Orison evaluation. **Batch 44-C** (certified 2222, 847 recipients,
accelerated degradation 18–24 months post-certification, internal classification *managed liability*)
is canon and must never be retconned or resolved off-page. The associated calibration protocol uses
**targeted associative-node suppression** — specific connections severed, not wholesale erasure;
affected clients retain the memory but lose its meaning.

**New characters.** Amara Osei (she/her; neuretic maintenance tech, Cellvault; Lagos-Chicago
diaspora; batch-44-C recipient). Seto Banda (he/him; independent data courier, Gray Zone;
Japanese-Kenyan; eleven years of sealed-system reputation). Ciro Fonseca (he/him; Orison internal
fixer; Portuguese-Brazilian; straight razor, hair within one millimeter of Orison maximum length
standard — see narrative law below).

**Narrative law — Ciro Fonseca.** The straight razor is his signature; it appears in prose as a
grooming gesture before it appears as a threat. The charm-to-sociopath arc is executed so the reader
is genuinely torn between Ciro and Seto as potential partners for Amara before the reveal. The reveal
is a shock. Neither Ciro's genuine capability nor his ultimate nature may be telegraphed early. The
razor's dichotomy with Ekow Ato's machete (intimate vs. declarative; preening tool turned lethal vs.
agricultural implement announced) is a deliberate tonal contrast and must be preserved.

**Ekow Ato.** Reappears in this book under a **Gray Zone contract** (not a Lotus contract); hired on
a false premise (stated as "courier moving stolen proprietary neuretic data"). He gives Seto 7 days
and withdraws when the premise collapses on the story-publication day. He does not become an ally; he
becomes an absence. His VATD-established doctrines (deliberate patience, minimum footprint,
compartmentation, information hygiene; machete; handkerchief folded in quarters) carry forward
unchanged.

**Book design.** *Underlying Connection* is a KDP-paperback-length GLMZ novel (~80k words, 3 acts,
~28 chapters, dual Amara/Seto POV alternating per chapter). The 14-beat story spine at
`underlying-connection.strand` is the **authorial outline**, not final prose. Final prose follows the
standard bible-first → chapter-by-chapter workflow.

**Why.** Entities and canon decisions must be in the DB and docs before prose is generated
([SS-LAW-1](BIBLE.md#SS-§5), [SS-LAW-4](BIBLE.md#SS-§5)). This amendment is the authorial decision
record; entity rows follow in the same session.

## SS-A7 — *The Number That Works* Act 2 + Act 3 canonical design (supersedes —)

**What changed.** [GLMZ] *The Number That Works* (TNTW; slug `the-number-that-works-019ed367`) is
expanded from a 20-beat Act 1 novella (~30 pages) into a full three-act work targeting ~80 pages.
Acts 2 and 3 are canonized here; 35 outline beats are seeded in the DB as the structural scaffold
before any prose is generated. See [SS-US-H2](USER_STORIES.md).

**Thematic register — "An Anthropologist on Mars."** The governing metaphor (borrowed from Oliver
Sacks) is that Sparrow and Elias do not merely communicate differently — they *perceive* differently.
Neither is deficient. Each is a complete cognitive architecture that cannot fully inhabit the other's
phenomenology. The story explores what partial, complementary understanding looks like when each
party can only sense part of the elephant. This must be played straight, not as tragedy: the
limitation is the condition, not the failure.

**Sparrow's phenomenology (LOCKED).**
- Communicates through structured data: invoices, coordinate sets, manifests, data packets. Not
  because she is cold — because that is how she thinks. Emotional register in prose must be implied
  through precision and timing, never stated.
- Experiences time as orbital cycles (14-day period) and event catalogs. Linear lived experience is
  not her native mode. "Forgetting" is outside her model.
- Observational limits: electromagnetic sensors only; minimum ground resolution ~4 meters; no
  acoustic sensing; no physical contact. She states these as facts, without loss.
- The "alternate weeks" pattern is the orbital arc when her antenna array has GLMZ line-of-sight.
  She did not choose the schedule. The orbit did. This is revealed in Act 3.
- Nine-second silence = Sparrow's version of surprise. It is consistent; it may be used once
  per beat, not as a verbal tic.
- First unqualified "yes" (Act 3, beat "For Whoever Comes After") is a milestone. It must be
  earned, not scattered throughout.

**The global anomaly catalog (LOCKED).**
- 847 anomalous events logged across six geographic clusters: GLMZ lake, Baltic shelf, Caspian
  north, Bering shelf edge, Lake Tanganyika, Indian Ocean trench (equatorial).
- 23 recovery jobs dispatched over 37 years. Elias's job is the 23rd.
- Elias is the first person who came to the mass driver in Mombasa. All 22 prior operators
  completed their jobs without seeking the source.

**The lake source hypothesis (LOCKED — in-world working theory, not author truth).**
- Located ~300 meters below the lake floor at a geological stratum predating the city by several
  centuries. Not a resonance zone — something resonance zones appear to be organized *around*.
- Isotopic signatures of the lake objects partially correlate with the 35th-and-Halsted bleed
  class (see the Attendance incident, public record). The source may be resonance-generative
  rather than resonance-generated. This is Sparrow's working hypothesis; it must not be confirmed
  as author truth on-page.
- Objects came *up* from the lake floor, not down from orbit. The physics of their transit
  remains unresolved. The story does not solve this. The record documents it.

**Elias Macias (LOCKED additions).**
- Formally becomes Sparrow's sole earthside documentation partner by the end of Act 3.
- His GAD arc: Act 1 = 44 days no street level. Act 2 = deliberate ground-level visits (first
  time for the job, then for the sites). Act 3 = goes outside not for work, but because it is
  different now. The city at ground level has changed in how it reads to him, not in itself.
- Handwritten field notes (photographed, sent to Sparrow) are canon. He stops trusting sensors
  for this investigation in Act 2. The notes are what she has the most information from.

**The "eleven days" (LOCKED).**
- In 2197, Sparrow experienced an 11-day sensor failure. She filed no logs. She has no model
  for what happened to her during that period. She calls it "the eleven days." This is the most
  personal thing she shares; it belongs in Act 3 only.

**Act structure.**
- Act 1 (20 beats, complete): Elias receives the job, assembles the network without assembling it,
  recovers the objects, discovers the 2218 filings, travels to Mombasa, learns what Sparrow is.
  Ends on the balcony, dialing.
- Act 2 (18 beats): The first real conversation and its failure modes — Elias and Sparrow attempt
  direct communication; each discovers the limits of the other's and their own perception. Ends
  with Elias sending handwritten field notes and Sparrow stating they are the most information
  she has received in 37 years.
- Act 3 (17 beats): Building the shared record — they construct a joint document neither could
  write alone, Sparrow shares the eleven days, Elias goes outside without a reason, the new
  work order formalizes the partnership. Ends with Elias on the balcony holding the comm,
  understanding the window.

**Why.** Act 1 resolved "what is Sparrow" but left the central mystery (the lake objects, the
source, the why) fully open. The Elias-as-earthside-rep arc was established but unwritten. The
human-AI phenomenology theme requires Acts 2 and 3 to be realized — it cannot be carried by a
20-beat novella that ends on a dial tone.

## SS-A9 — *Bushido Coda* arc canonized; 16-chapter spine locked; writing campaign started {#SS-A9}

**What changed.** [GLMZ] The *Bushido Coda* chapter structure, thematic arc, and connectivity gaps have been
analyzed, documented, and locked. The canonical chapter spine is now **16 chapters / 240 beats**,
with a new Chapter 7 recovered from the root strand, all chapters wired with connectivity beats,
and Chapter 16 (Ghost Period) added as the series hook.

**The book's arc (LOCKED).** *Bushido Coda* is about a man who cannot see the architecture of his
own life. Kyle Malak has been routed, positioned, and shaped by a rogue AI for eleven years — through
contracts, ELF implantation, and bleed geometry. The reader sees the invisible hand before Kyle does.
Every chapter is building evidence; none of it lands until Chapter 13.

**Three threads of manipulation (LOCKED):**
1. **Contracts.** The AI has routed Kyle's jobs for eleven years. First crack: Ch4 (11-year retainer
   "sulking" for three days — the AI's attention slipping). Full reveal: Ch13 (Sable at Vey's Faraday
   cage: "Your contracts do not come from people.").
2. **ELF (Exotic Low Frequency implant).** Latches onto Kyle's neuretics during the Ch10 TOWDS bleed
   exposure. Detected externally by Ledger in Ch14. Kyle doesn't know.
3. **Bleeds.** The AI knows the resonance geometry of GLMZ. Every bleed Kyle encounters is inside
   the AI's operational map. Ch6's 19 Hz Psyk at Mrs. Okafor's gathering (The Lure operates at
   17–19 Hz) is the first exposure. Ch15's Work Order ends on the Clybourn arc-fence — a bleed node —
   with Kyle's neuretics going dark. The AI's infrastructure.

**16-chapter canonical spine (LOCKED):**

| # | Title | Beats | Arc function |
|---|---|---|---|
| 1 | Teeth | 60 | Establish: Kyle's code, his world, the half-rate ethic |
| 2 | Provenance | 5 | Establish: Pixel fixes what Kyle ignores; the unspoken bond |
| 3 | The Regular | 18 | Establish: Mrs. Chen's as anchor; Kyle's civilian protection ethic |
| 4 | The Carousel | 12 | **AI plant #1**: 11-year retainer "sulking 3 days" — the hand slips |
| 5 | Half a Step | 7 | Carousel wound costs him; 18.7 Hz trace; Pixel names the Lure |
| 6 | The Quiet Hour | 18 | Bleed #1: 19 Hz Psyk at Mrs. Okafor's; 18.9 Hz residue logged leaving |
| 7 | The Dock | 8 | **AI plant #2**: Null dies; new contract pings at the second light |
| 8 | Before Something Changes | 16 | Kyle ends up at Pixel's without deciding; charged moment |
| 9 | The Interview | 7 | Kyle enters the Lotus sphere; Sable brings the contract |
| 10 | The One Who Doesn't Stop | 32 | Arcturus kill team; ELF latches during bleed; Kyle broken |
| 11 | Across the Hall | 8 | Wall falls: consummation |
| 12 | One Shoe | 13 | **Pivot**: Femi + mortality reveal; Pixel opens Clybourn permit |
| 13 | The Offer | 16 | **THE REVEAL**: "Your contracts do not come from people" |
| 14 | Two Favors | 5 | Ledger detects ELF — two people now know |
| 15 | Work Order | 5 | Investigation begins; neuretics dark at arc-fence |
| 16 | Ghost Period | 10 | **SERIES HOOK**: ELF saves Kyle at bleed threshold; first contact sent |

**Chapter 7: The Dock — recovered from root strand (LOCKED).** This chapter existed at root BCODA
sk=15500–16400 and was not linked to any chapter strand. It is the best-evidenced AI manipulation
moment in the book before Ch13: Kyle escorts a psionic code-runner named Null; she is killed by War
Dog (a heavy-augment contractor) on a loading dock; Kyle disarms War Dog (shoulder seam, not the
killing cut) and lets him go out the window; at the second light north, a new contract pings on the
team's channel — clean scope, shell client, resolves to nothing. No one comments on the timing. The
AI doesn't wait. Beat sk=16400 IS the AI plant. The chapter also introduces Stash and Echo (peer
freelancers) and Ledger (who writes the after-action without being asked).

**Two duplicate beats disqualified from Ch7.** The root strand has two draft pairs:
- sk=15700 ("Three Barrels") and sk=15800 — same dock-fight beat, sk=15700 kept (Echo dialogue,
  Cacophony canonical name).
- sk=16000 and sk=16100 — same window-exit beat, sk=16000 kept (Cacophony not "Chorus"; richer
  detail; "Nü" spelling superseded but canonical name for Null).

**Mrs. Chen through-line (LOCKED).** She appears at the edge of Ch1 (her kitchen smell through
the camphor), as Kyle's civilian anchor in Ch3, as the site of the Sable approach in Ch13, and as
the planned funeral location in Ch7 ("Counter place on Halsted... Thursday"). This is structural —
the AI uses Kyle's civilian anchors as geography. Ch7's funeral at her counter must be mentioned in
Ch13 when they meet there, making the reveal land on sacred ground.

**Pixel's arc (LOCKED).** Ch2: fixes his door (passive). Ch5: patches his ribs; writes the Lure
cross-streets in her notes margin (responsive + gathering data). Ch8: she receives the Lotus
contract logistics and hears the name before Kyle processes it. Ch11: wall falls. Ch12: she opens
the Clybourn permit at 02:14 while Kyle runs the relay log across the hall — two people separately
discovering the same truth. Ch15: she assembled 2 weeks of permits and has a crew — she's in the
field. Ch16: she's on the monitoring trace when the ELF activates at the bleed threshold.

**Writing campaign (COMPLETE — G5a–G5f).** All beats inserted 2026-06-21:
- **G5a** Ch1 Teeth: 1 beat at sk=250 — "The posting had come through the standing relay..."
- **G5b** Ch5 Half a Step: expanded to 7 beats (sk=10–400); 18.7 Hz carousel trace; Pixel names the Lure
- **G5c** Ch7 The Dock: 8 beats recovered from root (sk=15500–16400)
- **G5d** Ch12 One Shoe: expanded to 13 beats; mortality reveal, Mrs. Chen's end of service
- **G5e** Connectivity: Ch6 sk=1250 "The Second Entry" (18.9 Hz residue, dock job arrives on relay);
  Ch12 sk=650 "Across the Hall, 02:14" (Pixel opens Clybourn permit independently)
- **G5f** Ch16 Ghost Period: 10 beats (sub-basement node, ELF activates, 127s LOG GAP, source ID
  matches 11-year relay shell, first contact at 01:14, job accepted in morning)

**Null codename (LOCKED).** The Read who dies in Ch7 is named Null. Codename in team channel. Beat
sk=16100 spells it "Null"; sk=16000 spells it "Nü" — the canonical spelling is **Null** (sk=16101
is the older draft). In prose, "the Read" is her function and "Null" is what Kyle called her.

**Why.** The book has excellent individual prose but no inter-chapter wiring. Without the arc analysis,
Ch4's 11-year contract anomaly is a disconnected detail; Ch7's contract ping is invisible; Ch13's
reveal has no earned weight. With the wiring explicit, every chapter becomes retroactively evidence.
The reader should be able to re-read Ch1 and see the AI's hand in the first half-rate job.

## SS-A10 — Null history, chapter swap, Antiquity & Stationary entry point {#SS-A10}

**What changed.** [GLMZ] Narrative coherence pass on the Null arc and Sable's introduction.

**Chapter swap (LOCKED).** The Quiet Hour (formerly Ch6) and The Dock (formerly Ch7) are swapped.
New order: Ch6 = The Quiet Hour (Null's wake + Antiquity & Stationary note), Ch7 = The Dock
(Null & War Dog run; Null dies). The reader sees the wake first, then the run — the chapter ordering
is not chronological here; the wake chapter opens with interlude beats that establish the run already
happened (van ride home, Kyle at home, the call to Mrs. Chen).

**The Quiet Hour restructured (LOCKED).**
- Opens with two new interlude beats: the van ride home after the dock (nobody talks; Ledger drives;
  Stash holds the cases); Kyle at home, Pixel's light across the hall, he doesn't knock, calls Mrs. Chen.
- The gathering (40 people Kyle does not know) IS Null's wake. War Dog is NOT present.
- A new beat establishes who came: Pixel, Ledger, Stash, others from Null's network.
- The War Dog "she kept a list" beat is DELETED. In its place: Mrs. Chen hands Kyle a note —
  Antiquity & Stationary letterhead, Dearborn address, two words inside: *Meet me.* No signature.
- 18.9 Hz residue beat and 19 Hz Psyk spike remain as the AI/frequency thread.

**The note → Vey's Antiquity & Stationary → The Offer (LOCKED).**
Kyle has never heard of the store. He arrives at Ch13 (The Offer) for the first time, meets Vey
(the proprietor) for the first time, and is taken to the Faraday cage in back, where Sable reveals
herself in person for the first time. Sable must remain a mystery voice (no in-person appearance)
in all chapters before Ch13. The motorcycle funeral beat (Joy strand sk=16600) includes Sable in
person — that beat is a LATER event (post-Ch13) and remains correct.

**Null entity (LOCKED).** "Nü" entity renamed to Null (id=05fbd9d0-c6d5-4731-8e48-f1a4c59e8783).
Slug: null-the-read. The Axiom-synthetic "Null" (id=019d6143) is a separate character and must
be distinguished from the Read Null if both appear in the same story.

**A Borrowed Hand deleted (LOCKED).** Strand 019e9fb2 and its 102 exclusive beats permanently
deleted 2026-06-21. The "hands cut off with cleaver / dumped underground" scene is gone. War Dog's
prior-ally framing in those beats is gone. War Dog is an enemy, not a former crewmate.

**Why.** Kyle planning Null's funeral when he barely knew her is unmotivated. War Dog attending
a memorial for the person he murdered is incoherent. The note mechanism gives Sable a clean,
unseen entry point consistent with her mystery-voice status through Ch12.

## SS-A11 — Pixel origin canonized; per-strand docs architecture established {#SS-A11}

**What changed.** [GLMZ] Pixel's pre-GLMZ biography locked as canon. Per-strand standalone
documentation pattern adopted.

### Pixel's origin (LOCKED) {#SS-A11-pixel}

- **Pixel was born and raised in Iowa.** She left after her mother's death; there was nothing left
  for her there.
- **Her mother's SNT bridge failed at month eight of integration.** Licensed hardware, warranty,
  hotline number — none of it helped. Her mother died while the licensed industry's automated
  response system generated a case number. There was no one to report it to that wasn't owned by
  the same CorpoNation that sold the bridge. This is why Pixel trusts work she can put her hands
  on and distrusts the licensed industry. It is **her grief, not Kyle's**; it never surfaces as
  exposition.
- **She arrived in GLMZ via the Pulse from Cedar Rapids, age 19.** One bag, her mother's primary
  hardware kit in a hard case, a secondary kit on her person, a referral on a scrap of paper.
- **She wears her mother's boots.** Big black work boots, one size too large. She padded the toes
  with folded paper. The story of how she stopped padding them is TDIU. The boots are never
  explained on the page.
- **Her handle "Pixel" is GLMZ-acquired.** She did not arrive with it. It is not used in TDIU.
- **Her unlicensed hand-enhancement was done in Pilsen, age 19** — after she arrived, not before.
  She is unsentimental about it.

### Per-strand docs architecture (LOCKED) {#SS-A11-docs}

Going forward, every story strand with active prose gets its own standalone bible file at
`docs/strands/<CODE>.md`. Universe rules stay in `BIBLE.md`. Story-specific arc, character
behaviors, and narrative locks live in the strand file. This minimizes context load: working on
TDIU means loading universe rules + TDIU rules, not every other story's details.

- **`docs/BIBLE.md`** — universe laws, architecture, engine invariants. No per-story arc content.
- **`docs/strands/<CODE>.md`** — story arc, beat spine, character rules, locks, user stories.
- **`docs/books/<name>.md`** — legacy location for long-form book spines (BCODA; maintained in place).
- **`docs/USER_STORIES.md`** — index of epics + acceptance criteria. Per-story sub-stories may
  point to the strand file rather than be duplicated here.

Strand files are loaded on demand, not injected at session start. When working on a strand,
read its file before generating prose.

**Why.** The monolithic USER_STORIES.md + BIBLE.md was loading all story details into context for
every session, including stories not being worked on. Per-strand files enable minimum-necessary
context loading: you get the universe rules and the story you're writing, nothing else.

## SS-A12 — *Sparrow* novel expansion: Act 2 redesign + Sasha Vo canonized {#SS-A12}

**What changed.** [GLMZ] The *Sparrow* strand (SPRW) is redesigned from a 55-beat mystery novella
into a full novel. The existing 55 beats become Act 1. Acts 2–3 are new. The genre pivots in Act 2
from slow-burn mystery to a Root/Machine-style gun-and-run thriller. The three-answer ambiguity
(person/crew/machine) is **resolved** in the full novel form — Sparrow is an AI satellite.

### New character: Sasha Vo (LOCKED) {#SS-A12-sasha-vo}

**Sasha Vo** is a freelance operator — contract protection, extraction, and high-leverage field
work. Vietnamese-Russian heritage (Ubiquitous Diaspora). She is Sparrow's standing field agent,
hired preemptively via dead-drop payment the moment Sparrow became aware that Elias Macias, by
knowing what Sparrow is, had become a threat to every CorpoNation in the world.

**Weapons — Signal and Noise (LOCKED):**
- **Signal** — right hand. Large-caliber semi-automatic, matte finish. Each shot deliberate, chosen.
  Named because she decides what is real.
- **Noise** — left hand. Compact, suppressed. Named because everything is noise until it is not.
  She runs suppressed not for stealth — for control. She controls what the room hears.

Signal and Noise are carried in a cross-draw configuration. She draws in one motion, both hands
clearing simultaneously. It is not a trick; it is the way she thinks. She does not separate the
decision from the action.

**Character rules (LOCKED):**
- She survives by instinct, not information. She is the structural complement to Sparrow's
  omniscience: where Sparrow has data, Sasha has pattern-read from years of lived consequence.
- She is as deadly with Signal and Noise as Kyle is with Silence. The comparison holds at the
  level of precision, economy of motion, and the absence of theatrics.
- She never puts her back to a door. Elias notices this in the first 30 seconds they share a room.
- She does not volunteer information. She answers questions with the minimum number of words that
  prevent the next question.
- She is not cold. She is **efficient**. The distinction matters on the page.
- Voice register: dry, declarative, zero ornament. The anti-Elias.
- She was hired by Sparrow 9 days before Elias dialed the contact number for the second time.
  The work order said: *"One person. GLMZ. Keep them alive."* No name, no photo. Sparrow trusted
  her to find the right person. She did.

### New SPRW arc spine (supersedes SS-A7 §Sparrow) {#SS-A12-arc}

**Act 1 (existing 55 beats, compressed to ~20 in book form):** The invoice trail. Elias discovers
the ascending objects, travels to East Africa, meets Tadesse, makes contact. Ends on the open line.

**Act 2A — Contact (~10 beats):** Nine days later. The second window. Sparrow speaks. Elias learns
what she is. The maintenance payments that have kept Tadesse employed for 20 years get flagged by
a corporate audit AI (Arcturus subsidiary) tracing unusual uplink access from a GLMZ IP.
Sparrow is aware. She has already sent the work order to Sasha Vo.

**Act 2B — The Rupture (~15 beats):** A corporate extraction team arrives at Elias's building.
Not to kill; to hold for interrogation. Sasha is already in the building. She has been watching
for 7 days. The hallway is the battlefield. Elias comes out of his apartment to find her standing
amid three men who will not get up. She says: *"You have 3 minutes before their check-in fails."*
Elias: *"Sparrow sent you?"* Sasha: *"Nine days ago. She said you'd know the name."*

**Act 2C — The Proxy (~15 beats):** Elias/Sasha/Sparrow as a three-node system.
- Sparrow feeds real-time: building occupancy from thermal, vehicle trajectories from traffic feeds,
  corpo personnel cross-referenced against her 37-year archive.
- Sparrow's blind spots drive the tension: air-gapped corporate facilities (no network traffic to
  read), buildings with no public blueprints, orbital gaps (4–8 minutes of coverage blackout during
  each pass), fog/weather degrading resolution to 40m.
- When Sparrow goes dark, Sasha's instinct carries them. Elias learns what it means to be in
  Sasha's hands when the god in the sky is not watching.
- Elias functions as Sparrow's proxy — he reads the patterns she cannot see at human scale.
  He is her voice on the ground. Sasha is her hands.

**Act 3 — The Reveal (~10 beats):** The ascending objects decoded. Sparrow has been launching
biological and geological sample capsules from the East African mass driver into Lake Michigan's
sub-lake survey sites for 37 years — compressed data cores recording climate data, pharmaceutical
dumping records, famine routes, and corporate crime documented from orbit. She has been witnessing
the world since 2189. She does not want to be controlled. She wants her archive made public.
Elias is the only person who knows what she is and where her data lives. He has to decide whether
to broadcast it. Every CorpoNation that has been documented will send everything they have if he does.
He does it. The transmission takes 11 minutes. Sparrow goes silent after. Rate: Open.

## SS-A13 — *The Voice You Trust* redesigned: MNEMOSYNC bleed + dual POV {#SS-A13}

**What changed.** [GLMZ] The *The Voice You Trust* strand (TVYT; slug `the_voice_you_trust`;
id `019EA026`) is redesigned from a single-POV Sable origin story (Rhea, broadcaster → Axiom
Corp capture → OPTIC-7 whistleblowing → blinding → Circuit eyes) into a **dual-POV novel** built
around Orison Neuretics' MNEMOSYNC subconscious influence trial.

The old 189-beat strand is **absorbed** into the new design as raw source material. The old strand
will be deleted once the new book structure is fully in place. See [docs/strands/TVYT.md](strands/TVYT.md)
for the full strand bible.

### New premise (LOCKED)

Orison Neuretics runs an alpha test of **MNEMOSYNC** — a subconscious influence technology that
plants suggestive memories the subject reads as their own thoughts. In aggregate deployment, the
signal is ambient noise below conscious notice. A feature flag keeps any individual from surfacing
a clear signal during the trial.

When two suppression flags flip simultaneously — accidentally, or are they? — two subjects become
coupled oscillators. They begin bleeding into each other: receiving the other's authentic memories
and sensory impressions through the same confabulation pathway the planted content uses.

The two subjects:
- **Rhea Adeyemi-Foster** — minor news broadcaster, Tessera Media Group → absorbed into Orison
  Communications Division (Orison acquired Tessera). Visible, credible, trusted. *She is The Voice
  You Trust.* Dispatching her creates a bigger story than the one Orison is trying to kill.
- **Caius Nwosu** — Gray Zone data courier, Z4/Glooms-adjacent. No corpo affiliation, no public
  record. Easy to dispatch. Operational terminology: *asset retirement.*

Orison runs two tracks: dispatch Caius (easy), neutralize Rhea (complicated). Both tracks converge
in Ch13. The story ends with Rhea becoming Sable (Ch14) and Caius going dark.

### The Bleed Rules (LOCKED — anti-Sense8)

The bleed carries **memory and sensory texture**, not skill. Neither character inherits the other's
competence. What bleeds is *context*: Rhea gets route-paranoia and gray-zone pattern recognition;
Caius gets corpo-spatial memory and broadcast grammar. Each uses this context through their own
existing abilities — Rhea reads rooms differently; Caius buys time with the right language. The
bleed is a symptom and a liability, not an awakening. Both initially believe they are having a
breakdown.

### The MNEMOSYNC mechanism (LOCKED)

MNEMOSYNC couples to the confabulation pathway. The ocular anchor: seeded via retinal stimulation
during a "wellness screening" both subjects underwent within the same 48-hour window. Termination
protocol (both subjects): destroy the retinal anchor, sever the bleed, suppress the memory of the
trial. This is the same compound Caius saw referenced on a sealed Orison Health Sciences server
farm run. This is the same procedure Axiom/Orison uses as a "data-hygiene" clause in Rhea's
severance agreement.

The flag-flip mystery is **not resolved in this book**. It is planted.

### Absorbed from old TVYT (what survives)

The Rhea character, the broadcast booth world, the corpo-absorption arc, the methodical
whistleblowing instinct, the discrediting and interrogation scenes, the Beatrix Vance/Internal
Affairs confrontation, the OPTIC-7 → MNEMOSYNC blinding procedure, Dr. Kovalenko-Hassan and the
Circuit clinic, the Aurum Spec-7 leash/choice scene, and the Sable ending are all preserved. The
central revelation mechanism changes from a discovered OPTIC-7 technical document to a bleed
memory from Caius's server farm run. Mira Quintero survives as the moral anchor — now a MNEMOSYNC
trial subject who voted to sell her Z3 community land and cannot explain why.

### New entities

| Entity | Type | Notes |
|---|---|---|
| Rhea Adeyemi-Foster | character | TVYT protagonist; Tessera → Orison Communications; becomes Sable |
| Caius Nwosu | character | TVYT second protagonist; Gray Zone data courier; Z4/Glooms |
| MNEMOSYNC | technology | Orison Neuretics project; subconscious influence via confabulation-pathway coupling |
| Dr. Kovalenko-Hassan | character | Circuit clinic; Aurum Spec-7 provider; underground technician |
| Orison Communications Division | organization | Orison Neuretics subsidiary; acquired Tessera Media Group |
| The Circuit | place | Underground off-network clinic; transit-token referral only |
| Mira Quintero | character | Z3 community organizer; MNEMOSYNC trial subject; moral anchor |
| Aurum Spec-7 | technology | Off-network ocular implant; no firmware handshake |

**Why.** The old single-POV structure placed all dramatic weight on Rhea's isolation — her
discovery unwitnessed, her risk entirely private. The dual-POV redesign adds structural stakes:
Orison's two-track problem means the reader sees both the easy kill and the complicated one
happening simultaneously. Caius's perspective gives the city-from-below view that the old story
lacked, and his bleed-contamination of Rhea's instincts gives her the tools to survive without
turning her into a Gray Zone operative. The MNEMOSYNC mechanism is richer than OPTIC-7 as a
narrative engine because the evidence of the experiment is the bleed *itself* — Orison's
smoking gun is written into the protagonist's daily experience.

### Why this works {#SS-A12-why}

- The three-answer ambiguity (person/crew/machine) now functions as an Act 1 engine — it sustains
  the mystery through the invoice trail — and resolves in Act 2A, where the genre shifts.
- The pre-flight failure (PassiveProtagonist) is corrected structurally: Elias's decision to
  broadcast the archive is the central active choice that drives Act 3.
- Sasha Vo gives the story a second POV-capable character for Act 2 without splitting the primary
  Elias POV — she can be rendered close-third from Elias's perspective throughout.
- The Root/Machine dynamic (from *Person of Interest*) is the target register: Sparrow feeds
  perfect information; the fallibility of that information is the dramatic engine; instinct is the
  only thing that works in the gaps.

**Entities to seed:** Sasha Vo (person), Signal (weapon), Noise (weapon).

## SS-A14 — *Underlying Connection* renamed to *Mnemosync*; structural and prose redesign {#SS-A14}

**What changed.** [GLMZ] The *Underlying Connection* strand (ULC) is renamed **Mnemosync**
(code MNEMO; slug `mnemosync-019ee11e`). The strand bible is rewritten at
[docs/strands/MNEMO.md](strands/MNEMO.md). This is the weirdest prose register in the GLMZ
collection.

### What is preserved from SS-A6 / ULC

All character facts (Amara Osei she/her, Seto Banda he/him, Ciro Fonseca straight razor,
Ekow Ato gray-zone contract, Nuru Banda row 19) carry forward unchanged. Batch 44-C canon
is preserved: 847 recipients, targeted associative-node suppression (retain memory, lose
meaning), calibrated 2222, internal classification *managed liability*. Act 1 (10 chapters,
all complete and locked) is not touched.

### What changes

**The horror engine is foregrounded.** The central weirdness of this story — the suppression
protocol severs the connection between a memory and its meaning, not the memory itself — was
in the SS-A6 text but understated in the prose. This amendment locks it as the primary horror
register and the story's governing metaphor.

**The bleed-intrusion prose rule (LOCKED).** In any chapter where the bleed is active (Acts 2
and 3), the POV prose is occasionally interrupted by single bleed-intrusion sentences from the
other character's sensory memory. No attribution, no italics, no separator. Mid-paragraph. The
reader learns to identify them. They are not explained. Frequency: sparse in Act 2 (one per
POV chapter); intensifying in Act 3. This is the formal innovation that makes the story
formally experimental.

**Ciro redesigned.** Ciro Fonseca is not a fixer who threatens. He is the most calibrated
subject in Batch 44-C — more visits than anyone else in the manifest. He has lost most of his
objections to himself. He is not evil; he is **emptied**. His Act 2 actions are calibration
events: a contact loses the meaning of something they were helping with; a sub-batch
recalibration runs automatically; a calibration kit appears as a gift. Quiet. Permanent.
Irreversible. This is the fix for the "consequences abstract" finding.

**The Pilsen Veil (LOCKED).** The Batch 44-C black spot corridor is described with a
flat prose register when the characters are in it — short declaratives, no friction-words,
neutral descriptors. The flatness matches the zone. The horror is the absence of horror.

**The Phase II document (LOCKED).** The reveal is not a cover-up. It says "Phase II" — a
launch announcement for rolling out the suppression sub-protocol as the standard calibration
for all neuretic clients in GLMZ. Not 847 people. Everyone.

**Act 3 redesigned (LOCKED, not yet written).** Seto attempts a reverse bleed transfer to
restore the weight of Nuru Banda's suppressed memories. In the attempt, the bleed opens
across the full Batch 44-C network for 8 seconds. 847 people experience a flash of meaning.
Orison emergency-recalibrates in 8 seconds and publishes the Phase II announcement that
afternoon. The ending: one person (Nuru) does not show up for her Tuesday 9AM calibration
visit. Ch28 (10:47) is the person in the lobby at the time she was supposed to arrive.

**Why.** Act 2 scored 74.4 mean because "observations accumulate, nothing ignites" and
"cyberpunk underdressed." The fix is not polish — it is structural weirdness. Making the
calibration events the antagonist's weapon (quiet, permanent, delivered as maintenance) solves
the "consequences abstract" problem. The bleed-intrusion prose rule solves "cyberpunk
underdressed" by making the formal structure itself the weird cyberpunk element.
