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

**Amara Osei name collision — resolved.** The ATTE child has been renamed **Yaa Osei** (Yaa = Ghanaian day name, Thursday-born; surname Osei retained from mother Abena Osei). All three prose beats updated. The adult `Amara Osei` in *Underlying Connection* ([SS-A6](#)) is unchanged.

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
